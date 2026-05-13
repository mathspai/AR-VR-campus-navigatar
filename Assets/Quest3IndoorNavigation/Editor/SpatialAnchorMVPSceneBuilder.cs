using System;
using System.Linq;
using Meta.XR.MRUtilityKit;
using Quest3IndoorNavigation.Anchors;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Quest3IndoorNavigation.Editor
{
    public static class SpatialAnchorMVPSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/SpatialAnchorMVP.unity";
        private const string LineMaterialPath = "Assets/Quest3IndoorNavigation/MVP_NavigationLine.mat";
        private const string MarkerMaterialPath = "Assets/Quest3IndoorNavigation/MVP_AnchorMarker.mat";
        private const string HandMaterialPath = "Packages/com.meta.xr.sdk.core/Materials/BasicHandMaterial.mat";
        private const string HandPrefabPath = "Packages/com.meta.xr.sdk.core/Prefabs/OVRHandPrefab.prefab";
        private const string MRUKPrefabPath = "Packages/com.meta.xr.mrutilitykit/Core/Tools/MRUK.prefab";

        [MenuItem("Quest3 Indoor Navigation/Create Spatial Anchor MVP Scene")]
        public static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var rig = BuildXrRig(null);
            var camera = FindChild(rig.transform, "CenterEyeAnchor")?.GetComponent<Camera>() ?? Camera.main;
            var anchorRoot = new GameObject("SpatialAnchorPoints");

            var store = new GameObject("SpatialAnchorStore").AddComponent<SpatialAnchorStore>();

            BuildMRUK();
            BuildSceneNavigation();
            new GameObject("MRUKRoomAnchorVisibility").AddComponent<MRUKRoomAnchorVisibility>();
            var navigator = BuildNavigator(null, camera, anchorRoot.transform);
            var ui = BuildWorldSpaceUi(null, store, navigator, anchorRoot.transform);
            BuildLeftHandRecall(null, rig.transform, ui.transform, ui.StatusText);
            BuildEventSystem();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SpatialAnchorMVPSceneBuilder] Scene ready: {ScenePath}");
        }

        private static GameObject BuildXrRig(Transform parent)
        {
            var rig = new GameObject("OVRCameraRig");
            if (parent != null)
            {
                rig.transform.SetParent(parent, false);
            }
            TryAddComponent(rig, "OVRCameraRig");
            var cameraRig = rig.GetComponent<OVRCameraRig>();
            if (cameraRig != null)
            {
                cameraRig.usePerEyeCameras = false;
                cameraRig.disableEyeAnchorCameras = false;
                cameraRig.EnsureGameObjectIntegrity();
            }

            var trackingSpace = FindChild(rig.transform, "TrackingSpace")?.transform ?? rig.transform;
            var centerEye = GetOrCreateChild(trackingSpace, "CenterEyeAnchor");
            centerEye.tag = "MainCamera";
            var camera = centerEye.GetComponent<Camera>() ?? centerEye.AddComponent<Camera>();
            camera.stereoTargetEye = StereoTargetEyeMask.Both;
            camera.enabled = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;

            ConfigurePerEyeCamera(FindChild(trackingSpace, "LeftEyeAnchor"));
            ConfigurePerEyeCamera(FindChild(trackingSpace, "RightEyeAnchor"));

            var ovrManager = TryAddComponent(rig, "OVRManager");
            if (ovrManager != null)
            {
                SetBool(ovrManager, "isInsightPassthroughEnabled", true);
                SetBool(ovrManager, "usePositionTracking", true);
                SetBool(ovrManager, "useRotationTracking", true);
                SetBool(ovrManager, "requestScenePermissionOnStartup", true);
                SetBool(ovrManager, "resetTrackerOnLoad", false);
                SetBool(ovrManager, "launchSimultaneousHandsControllersOnStartup", false);
                SetBool(ovrManager, "SimultaneousHandsAndControllersEnabled", false);
                SetInt(ovrManager, "controllerDrivenHandPosesType", 0);
                SetInt(ovrManager, "trackingOriginType", 1); // FloorLevel: floor = Y:0, camera ≈ Y:1.7
                SetBool(ovrManager, "shouldBoundaryVisibilityBeSuppressed", true);
            }

            TryAddComponent(rig, "OVRSceneManager");

            var passthrough = TryAddComponent(rig, "OVRPassthroughLayer");
            if (passthrough != null)
            {
                SetInt(passthrough, "placement", 0);
                SetInt(passthrough, "compositionDepth", -1);
            }

            var leftHandAnchor = GetOrCreateChild(trackingSpace, "LeftHandAnchor");
            ConfigureHandAnchor(leftHandAnchor, OVRHand.Hand.HandLeft);
            AddHandRenderPrefab(leftHandAnchor.transform, isLeft: true);

            var rightHandAnchor = GetOrCreateChild(trackingSpace, "RightHandAnchor");
            ConfigureHandAnchor(rightHandAnchor, OVRHand.Hand.HandRight);
            AddHandRenderPrefab(rightHandAnchor.transform, isLeft: false);

            return rig;
        }

        private static SimpleAnchorNavigator BuildNavigator(Transform parent, Camera camera, Transform anchorRoot)
        {
            var obj = new GameObject("SimpleAnchorNavigator");
            if (parent != null)
            {
                obj.transform.SetParent(parent, false);
            }
            var navigator = obj.AddComponent<SimpleAnchorNavigator>();
            SetObject(navigator, "userCamera", camera != null ? camera.transform : null);
            SetObject(navigator, "anchorRoot", anchorRoot);
            SetObject(navigator, "markerMaterial", EnsureMaterial(MarkerMaterialPath, new Color(1f, 0.82f, 0.18f, 1f)));
            var doorNav = obj.AddComponent<MRUKDoorNavigationService>();
            SetObject(navigator, "sceneRouteProviderBehaviour", doorNav);

            var line = obj.GetComponent<LineRenderer>();
            line.sharedMaterial = EnsureMaterial(LineMaterialPath, new Color(0f, 0.95f, 1f, 1f));
            line.startColor = new Color(0f, 1f, 0.85f, 1f);
            line.endColor = new Color(0.15f, 0.55f, 1f, 1f);
            line.startWidth = 0.06f;
            line.endWidth = 0.06f;
            line.numCornerVertices = 6;
            line.numCapVertices = 6;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return navigator;
        }

        private static SpatialAnchorNavigationUI BuildWorldSpaceUi(Transform parent, SpatialAnchorStore store, SimpleAnchorNavigator navigator, Transform anchorRoot)
        {
            var canvasObject = new GameObject("Spatial Anchor MVP Menu", typeof(RectTransform), typeof(Canvas));
            if (parent != null)
            {
                canvasObject.transform.SetParent(parent, false);
            }
            canvasObject.transform.position = new Vector3(0f, 1.38f, 0.85f);
            canvasObject.transform.rotation = Quaternion.identity;
            canvasObject.transform.localScale = Vector3.one * 0.00110f;

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvasObject.AddComponent<OVRRaycaster>();
            canvasObject.AddComponent<GraphicRaycaster>();
            var canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1100f, 680f);
            // Pivot aligned to main-panel centre (340 / 1100) so transform.position stays at the
            // main panel's world position — GetPlacementPose() depends on this.
            canvasRect.pivot = new Vector2(340f / 1100f, 0.5f);

            // Main panel — left 680 px, full height
            var panel = CreateImage("Panel", canvasObject.transform, new Color(0.035f, 0.045f, 0.05f, 0.94f));
            var panelRect = panel.rectTransform;
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot   = new Vector2(0f, 0.5f);
            panelRect.offsetMin = new Vector2(0f,   0f);
            panelRect.offsetMax = new Vector2(680f, 0f);

            // Right panel — x 700‥1080, full height, clipped, hidden by default
            var rightPanelBg = CreateImage("RightPanel", canvasObject.transform, new Color(0.04f, 0.06f, 0.07f, 0.96f));
            var rightBgRect = rightPanelBg.rectTransform;
            rightBgRect.anchorMin = new Vector2(0f, 0f);
            rightBgRect.anchorMax = new Vector2(0f, 1f);
            rightBgRect.pivot   = new Vector2(0f, 0.5f);
            rightBgRect.offsetMin = new Vector2(700f,  0f);
            rightBgRect.offsetMax = new Vector2(1080f, 0f);
            rightPanelBg.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            rightPanelBg.gameObject.SetActive(false);

            // Right panel header (top 54 px)
            var rightHeader = CreateText("RightPanelHeader", rightPanelBg.transform, "Select", 34, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetTop(rightHeader.rectTransform, 0f, 0f, 0f, 54f);


            // Main panel children
            var title = CreateText("Title", panel.transform, "Quest 3 MR Navigation v3", 38, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetTop(title.rectTransform, 32f, -24f, 32f, 56f);

            var status = CreateText("Status", panel.transform, "Ready", 30, FontStyle.Bold, TextAnchor.MiddleRight);
            SetTop(status.rectTransform, 32f, -88f, 32f, 48f);
            status.color = new Color(0.62f, 0.95f, 0.78f, 1f);

            var place = CreateButton("PlaceAnchorButton", panel.transform, "Place Anchor", new Color(0.12f, 0.30f, 0.33f, 0.96f));
            SetTop(place.GetComponent<RectTransform>(), 32f, -152f, 204f, 74f);

            var openNamePanel = CreateButton("OpenNamePanelButton", panel.transform, "Name ▶", new Color(0.10f, 0.22f, 0.22f, 0.96f));
            SetTop(openNamePanel.GetComponent<RectTransform>(), 484f, -152f, 32f, 74f);

            var clear = CreateButton("ClearAnchorsButton", panel.transform, "Clear All", new Color(0.30f, 0.10f, 0.10f, 0.96f));
            SetTop(clear.GetComponent<RectTransform>(), 32f, -240f, 32f, 74f);

            var select = CreateButton("SelectAnchorButton", panel.transform, "Select", new Color(0.13f, 0.22f, 0.32f, 0.96f));
            SetTop(select.GetComponent<RectTransform>(), 32f, -328f, 32f, 74f);

            var selectedLabel = CreateText("SelectedAnchorLabel", panel.transform, "", 26, FontStyle.Normal, TextAnchor.MiddleCenter);
            SetTop(selectedLabel.rectTransform, 32f, -406f, 32f, 36f);
            selectedLabel.color = new Color(0.65f, 0.85f, 1f, 0.85f);

            var start = CreateButton("StartNavigationButton", panel.transform, "Navigate", new Color(0.26f, 0.22f, 0.38f, 0.96f));
            SetBottom(start.GetComponent<RectTransform>(), 32f, 32f, 32f, 74f);

            var ui = canvasObject.AddComponent<SpatialAnchorNavigationUI>();
            SetObject(ui, "anchorStore", store);
            SetObject(ui, "navigator", navigator);
            SetObject(ui, "anchorRoot", anchorRoot);
            SetObject(ui, "placeAnchorButton", place);
            SetObject(ui, "selectAnchorButton", select);
            SetObject(ui, "startNavigationButton", start);
            SetObject(ui, "clearAnchorsButton", clear);
            SetObject(ui, "openNamePanelButton", openNamePanel);
            SetObject(ui, "selectedAnchorLabel", selectedLabel);
            SetObject(ui, "rightPanelRoot", rightBgRect);
            SetObject(ui, "rightPanelHeader", rightHeader);
            SetObject(ui, "statusText", status);
            SetObject(ui, "markerMaterial", EnsureMaterial(MarkerMaterialPath, new Color(1f, 0.82f, 0.18f, 1f)));

            var touch = TryAddComponent(canvasObject, "Quest3IndoorNavigation.UI.SimpleHandTouchUI");
            SetObject(touch, "eventCamera", Camera.main);
            SetObject(touch, "statusText", status);
            SetFloat(touch, "touchDepth", 0.05f);
            return ui;
        }

        private static void BuildLeftHandRecall(Transform parent, Transform rig, Transform menuRoot, Text statusText)
        {
            var obj = new GameObject("LeftHandMenuRecall");
            if (parent != null)
            {
                obj.transform.SetParent(parent, false);
            }
            var recall = obj.AddComponent<LeftHandMenuRecall>();
            var leftHandAnchor = FindChild(rig, "LeftHandAnchor");
            var leftHand = leftHandAnchor?.GetComponentInChildren<OVRHand>(true);
            SetObject(recall, "leftHand", leftHand);
            SetObject(recall, "menuRoot", menuRoot);
            SetObject(recall, "statusText", statusText);
            SetFloat(recall, "distance", 0.85f);
            SetBool(recall, "showOnStart", false);
            menuRoot.gameObject.SetActive(false);
        }

        private static void BuildMRUK()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MRUKPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[SceneBuilder] MRUK.prefab not found — install com.meta.xr.mrutilitykit.");
                return;
            }
            PrefabUtility.InstantiatePrefab(prefab);
        }

        private static void BuildSceneNavigation()
        {
            var obj = new GameObject("SceneNavigation");
            var sceneNav = obj.AddComponent<SceneNavigation>();
            // NavigableSurfaces = FLOOR (1 << Classification.Floor = 1 << 0 = 1)
            SetInt(sceneNav, "NavigableSurfaces", 1);
            // SceneObstacles = WALL_FACE|TABLE|COUCH|STORAGE|BED|SCREEN|LAMP|PLANT|INVISIBLE_WALL_FACE|INNER_WALL_FACE
            // = (1<<2)|(1<<3)|(1<<4)|(1<<8)|(1<<9)|(1<<10)|(1<<11)|(1<<12)|(1<<15)|(1<<18) = 302876
            SetInt(sceneNav, "SceneObstacles", 302876);
            SetBool(sceneNav, "UseSceneData", true);
            SetBool(sceneNav, "CustomAgent", true);
            SetFloat(sceneNav, "AgentRadius", 0.2f);
            SetFloat(sceneNav, "AgentHeight", 1.8f);
            SetFloat(sceneNav, "AgentClimb", 0.04f);
            SetFloat(sceneNav, "AgentMaxSlope", 5.5f);
            // BuildOnSceneLoaded = CurrentRoomOnly = 1
            SetInt(sceneNav, "BuildOnSceneLoaded", (int)MRUK.RoomFilter.CurrentRoomOnly);
        }

        private static void BuildEventSystem()
        {
            var obj = new GameObject("EventSystem", typeof(EventSystem));
            obj.AddComponent<OVRInputModule>();
        }

        private static void ConfigureHandAnchor(GameObject anchor, OVRHand.Hand handType)
        {
            // Anchor holds OVRHand for pinch detection (LeftHandMenuRecall).
            // Visual mesh is the official prefab added as a child below.
            RemoveComponent<OVRMeshRenderer>(anchor);
            RemoveComponent<OVRMesh>(anchor);
            RemoveComponent<OVRSkeleton>(anchor);
            RemoveComponent<OVRSkeletonRenderer>(anchor);

            var hand = anchor.GetComponent<OVRHand>() ?? anchor.AddComponent<OVRHand>();
            SetInt(hand, "HandType", (int)handType);
        }

        private static void AddHandRenderPrefab(Transform parent, bool isLeft)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HandPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[SceneBuilder] OVRHandPrefab not found: {HandPrefabPath}");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.localPosition = Vector3.zero;

            var hand = instance.GetComponent<OVRHand>();
            var skeleton = instance.GetComponent<OVRSkeleton>();
            var mesh = instance.GetComponent<OVRMesh>();

            if (!isLeft)
            {
                if (hand != null) SetInt(hand, "HandType", (int)OVRHand.Hand.HandRight);
                if (skeleton != null) SetInt(skeleton, "_skeletonType", (int)OVRSkeleton.SkeletonType.HandRight);
                if (mesh != null) SetInt(mesh, "_meshType", (int)OVRMesh.MeshType.HandRight);
            }

            // Per official Meta docs: anchor drives root position, prefab only animates bones.
            // _updateRootScale=true lets the skeleton adapt to the user's actual hand size.
            if (skeleton != null)
            {
                SetBool(skeleton, "_updateRootPose", false);
                SetBool(skeleton, "_updateRootScale", true);
            }
        }

        private static void ConfigurePerEyeCamera(GameObject eyeAnchor)
        {
            if (eyeAnchor == null)
            {
                return;
            }

            eyeAnchor.tag = "Untagged";
            var camera = eyeAnchor.GetComponent<Camera>();
            if (camera != null)
            {
                camera.enabled = false;
            }
        }

        private static void RemoveComponent<T>(GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            if (component != null)
            {
                UnityEngine.Object.DestroyImmediate(component);
            }
        }

        private static Button CreateButton(string name, Transform parent, string label, Color color)
        {
            var image = CreateImage(name, parent, color);
            var button = image.gameObject.AddComponent<Button>();
            var text = CreateText("Label", image.transform, label, 34, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 22f, 8f, 22f, 8f);
            return button;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent, false);
            var image = obj.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(string name, Transform parent, string text, int size, FontStyle style, TextAnchor anchor)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            obj.transform.SetParent(parent, false);
            var label = obj.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = anchor;
            label.color = Color.white;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 18;
            label.resizeTextMaxSize = size;
            return label;
        }

        private static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetTop(RectTransform rect, float left, float top, float right, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, top - height);
            rect.offsetMax = new Vector2(-right, top);
        }

        private static void SetBottom(RectTransform rect, float left, float bottom, float right, float height)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, bottom + height);
        }

        private static GameObject GetOrCreateChild(Transform parent, string name)
        {
            var existing = FindChild(parent, name);
            if (existing != null)
            {
                return existing;
            }

            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static GameObject FindChild(Transform parent, string name)
        {
            return parent == null
                ? null
                : parent.GetComponentsInChildren<Transform>(true).FirstOrDefault(child => child.name == name)?.gameObject;
        }

        private static Component TryAddComponent(GameObject obj, string typeName)
        {
            var type = FindType(typeName);
            return type != null && typeof(Component).IsAssignableFrom(type)
                ? obj.GetComponent(type) ?? obj.AddComponent(type)
                : null;
        }

        private static Type FindType(string typeName)
        {
            return Type.GetType(typeName) ?? AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(typeName))
                .FirstOrDefault(type => type != null);
        }

        private static Material EnsureMaterial(string path, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                material.color = color;
                return material;
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            material = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            var existing = scenes.FirstOrDefault(scene => scene.path == scenePath);
            if (existing != null)
            {
                existing.enabled = true;
            }
            else
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void SetObject(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            if (target == null)
            {
                return;
            }

            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetBool(UnityEngine.Object target, string propertyName, bool value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetInt(UnityEngine.Object target, string propertyName, int value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetFloat(UnityEngine.Object target, string propertyName, float value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

    }
}
