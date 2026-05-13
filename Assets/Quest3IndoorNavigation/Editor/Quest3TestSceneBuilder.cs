using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Quest3IndoorNavigation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Quest3IndoorNavigation.Editor
{
    public static class Quest3TestSceneBuilder
    {
        private const string TestScenePath = "Assets/Scenes/Quest3_APK_TestScene.unity";
        private const string LineMaterialPath = "Assets/Quest3IndoorNavigation/NavigationLine.mat";

        [MenuItem("Quest3 Indoor Navigation/Create Quest3 APK Test Scene")]
        public static void CreateAndConfigureTestScene()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                Debug.LogWarning("Quest3TestSceneBuilder: Editor is busy, aborting.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var root = new GameObject("IndoorNavigationRoot");
            var xrRig      = BuildXrRig(root.transform);
            var controller = BuildNavigationController(root.transform);
            var planner    = BuildFloorRoutePlanner(root.transform, controller);
            var targets    = BuildTestTargets(root.transform, controller);
            BuildRegistry(root.transform, controller, planner, targets);
            BuildNavMeshSurface(root.transform);
            BuildQuestMeshing(root.transform);
            BuildWorldSpaceUI(root.transform);
            BuildEventSystem();

            EditorSceneManager.SaveScene(scene, TestScenePath);
            AssetDatabase.ImportAsset(TestScenePath);
            AddSceneToBuildSettings(TestScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Quest3TestSceneBuilder] Done. Scene: {TestScenePath}");
        }

        // ── XR Rig ──────────────────────────────────────────────────────────────
        private static GameObject BuildXrRig(Transform parent)
        {
            var rig = new GameObject("XR Origin / Camera Rig");
            rig.transform.SetParent(parent, false);

            TryAddByName(rig, "OVRCameraRig");

            var eyeAnchor = new GameObject("CenterEyeAnchor");
            eyeAnchor.transform.SetParent(rig.transform, false);
            eyeAnchor.tag = "MainCamera";

            var cam = eyeAnchor.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.clear;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 100f;

            var xrOrigin = TryAddByName(rig, "Unity.XR.CoreUtils.XROrigin");
            if (xrOrigin != null)
            {
                TrySetRef(xrOrigin, "m_Camera", cam);
                TrySetRef(xrOrigin, "m_CameraFloorOffsetObject", rig);
                TrySetFloat(xrOrigin, "m_CameraYOffset", 0f);
            }

            var ovrManager = TryAddByName(rig, "OVRManager");
            if (ovrManager != null)
            {
                TrySetBool(ovrManager, "isInsightPassthroughEnabled", true);
                TrySetBool(ovrManager, "requestScenePermissionOnStartup", true);
                TrySetBool(ovrManager, "usePositionTracking", true);
                TrySetBool(ovrManager, "useRotationTracking", true);
            }

            TryAddByName(rig, "OVRSceneManager");

            var passthrough = TryAddByName(rig, "OVRPassthroughLayer");
            if (passthrough != null)
            {
                TrySetInt(passthrough, "placement", 0);
                TrySetInt(passthrough, "compositionDepth", -1);
            }

            return rig;
        }

        // ── Navigation Controller ────────────────────────────────────────────────
        private static GameObject BuildNavigationController(Transform parent)
        {
            var obj = new GameObject("NavigationController");
            obj.transform.SetParent(parent, false);
            obj.AddComponent<IndoorNavigationController>();

            var lr = obj.GetComponent<LineRenderer>() ?? obj.AddComponent<LineRenderer>();
            var lineMat = EnsureLineMaterial();
            if (lineMat != null) lr.sharedMaterial = lineMat;
            lr.startColor = new Color(0f, 1f, 0.85f, 1f);
            lr.endColor   = new Color(0f, 0.5f, 1f, 1f);
            lr.startWidth  = 0.08f;
            lr.endWidth    = 0.08f;
            lr.numCornerVertices = 6;
            lr.numCapVertices   = 6;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;
            return obj;
        }

        // ── FloorRoutePlanner ────────────────────────────────────────────────────
        private static GameObject BuildFloorRoutePlanner(Transform parent, GameObject controller)
        {
            var obj = new GameObject("FloorRoutePlanner");
            obj.transform.SetParent(parent, false);
            var planner = obj.AddComponent<FloorRoutePlanner>();
            TrySetRef(planner, "navigationController", controller.GetComponent<IndoorNavigationController>());
            return obj;
        }

        // ── Test Targets ─────────────────────────────────────────────────────────
        private static List<NavigationTarget> BuildTestTargets(Transform parent, GameObject controller)
        {
            var root = new GameObject("NavigationTargets");
            root.transform.SetParent(parent, false);

            var data = new (string name, Vector3 pos, Color color)[]
            {
                ("Target_A", new Vector3( 1.5f, 0.05f, 2.0f), Color.yellow),
                ("Target_B", new Vector3(-1.5f, 0.05f, 2.5f), new Color(1f, 0.25f, 0.2f)),
            };

            var list = new List<NavigationTarget>();
            foreach (var (name, pos, color) in data)
            {
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = name;
                sphere.transform.SetParent(root.transform, false);
                sphere.transform.localPosition = pos;
                sphere.transform.localScale = Vector3.one * 0.25f;
                var colorMat = EnsureColorMaterial(name, color);
                if (colorMat != null) sphere.GetComponent<MeshRenderer>().sharedMaterial = colorMat;

                var target = sphere.AddComponent<NavigationTarget>();
                TrySetRef(target, "navigationController", controller.GetComponent<IndoorNavigationController>());
                list.Add(target);
            }

            return list;
        }

        // ── Registry ─────────────────────────────────────────────────────────────
        private static void BuildRegistry(Transform parent, GameObject controller, GameObject plannerObj, List<NavigationTarget> targets)
        {
            var obj = new GameObject("NavigationTargetRegistry");
            obj.transform.SetParent(parent, false);
            var registry = obj.AddComponent<NavigationTargetRegistry>();

            TrySetRef(registry, "navigationController", controller.GetComponent<IndoorNavigationController>());
            var planner = plannerObj.GetComponent<FloorRoutePlanner>();
            TrySetRef(registry, "floorRoutePlanner", planner);
            TrySetRef(planner, "targetRegistry", registry);

            var so = new SerializedObject(registry);
            var prop = so.FindProperty("targets");
            prop.arraySize = targets.Count;
            for (var i = 0; i < targets.Count; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = targets[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── NavMesh Surface ───────────────────────────────────────────────────────
        private static void BuildNavMeshSurface(Transform parent)
        {
            var obj = new GameObject("RuntimeNavMeshSurface");
            obj.transform.SetParent(parent, false);
            TryAddByName(obj, "Unity.AI.Navigation.NavMeshSurface");
        }

        // ── QuestMeshing / Depth ─────────────────────────────────────────────────
        private static void BuildQuestMeshing(Transform parent)
        {
            var obj = new GameObject("QuestDepthMeshing");
            obj.transform.SetParent(parent, false);
            obj.AddComponent<MeshFilter>();
            obj.AddComponent<MeshRenderer>();
            obj.AddComponent<MeshCollider>();

            var rig = FindByName(parent, "XR Origin / Camera Rig");

            var preprocessor = TryAddByName(obj, "Uralstech.UXR.QuestMeshing.DepthPreprocessor");
            if (preprocessor != null && rig != null)
            {
                TrySetRef(preprocessor, "_cameraRig", rig.GetComponent("OVRCameraRig" ) as Component ?? rig.GetComponent<Transform>());
                TrySetRef(preprocessor, "_shader", AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.uralstech.uxr.questmeshing/Runtime/Shaders/DepthPreprocessor.compute"));
            }

            var mesher = TryAddByName(obj, "Uralstech.UXR.QuestMeshing.DepthMesher");
            if (mesher != null)
            {
                if (rig != null)
                {
                    TrySetRef(mesher, "_cameraRig", rig.GetComponent("OVRCameraRig") as Component ?? rig.GetComponent<Transform>());
                }
                TrySetRef(mesher, "_shader",               AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.uralstech.uxr.questmeshing/Runtime/Shaders/SurfaceNets.compute"));
                TrySetRef(mesher, "_meshFilterConsumer",   obj.GetComponent<MeshFilter>());
                TrySetRef(mesher, "_meshColliderConsumer", obj.GetComponent<MeshCollider>());
            }

            var cpuSampler = TryAddByName(obj, "Uralstech.UXR.QuestMeshing.CPUDepthSampler");
            if (cpuSampler != null)
            {
                TrySetRef(cpuSampler, "_shader", AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.uralstech.uxr.questmeshing/Runtime/Shaders/DepthSampler.compute"));
            }
        }

        // ── World Space UI ────────────────────────────────────────────────────────
        private static void BuildWorldSpaceUI(Transform parent)
        {
            var uiObj = new GameObject(
                "Quest3 World Space Navigation UI",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster),
                typeof(QuestNavigationWorldSpaceUI));

            uiObj.transform.SetParent(parent, false);
            uiObj.transform.position = new Vector3(0f, 1.45f, 1.6f);
            uiObj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        // ── EventSystem ───────────────────────────────────────────────────────────
        private static void BuildEventSystem()
        {
            var obj = new GameObject("EventSystem", typeof(EventSystem));
            var xrInputType = FindTypeSafe("UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule, Unity.XR.Interaction.Toolkit");
            obj.AddComponent(xrInputType ?? typeof(StandaloneInputModule));
        }

        // ── Build Settings ────────────────────────────────────────────────────────
        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == scenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
                Debug.Log($"[Quest3TestSceneBuilder] Added {scenePath} to Build Settings.");
            }
        }

        // ── Materials ─────────────────────────────────────────────────────────────
        private static Material EnsureLineMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(LineMaterialPath);
            if (mat != null) return mat;

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (shader == null) return null; // batchmode -nographics: skip material creation

            mat = new Material(shader);
            mat.color = new Color(0f, 1f, 0.85f, 1f);
            AssetDatabase.CreateAsset(mat, LineMaterialPath);
            return mat;
        }

        private static Material EnsureColorMaterial(string name, Color color)
        {
            var path = $"Assets/Quest3IndoorNavigation/{name}_Marker.mat";
            var mat  = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
            {
                mat.color = color;
                return mat;
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
            if (shader == null) return null; // batchmode -nographics: skip material creation

            mat = new Material(shader);
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────
        private static GameObject FindByName(Transform root, string name)
        {
            foreach (Transform t in root)
            {
                if (t.name == name)
                {
                    return t.gameObject;
                }
            }
            return null;
        }

        private static Component TryAddByName(GameObject go, string typeName)
        {
            var type = FindTypeSafe(typeName);
            if (type == null || !typeof(Component).IsAssignableFrom(type))
            {
                return null;
            }
            return go.GetComponent(type) ?? go.AddComponent(type);
        }

        private static Type FindTypeSafe(string typeName)
        {
            var t = Type.GetType(typeName);
            if (t != null)
            {
                return t;
            }
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(typeName))
                .FirstOrDefault(found => found != null);
        }

        private static void TrySetRef(UnityEngine.Object target, string prop, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            var p  = so.FindProperty(prop);
            if (p == null)
            {
                return;
            }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void TrySetBool(Component c, string prop, bool value)
        {
            var so = new SerializedObject(c);
            var p  = so.FindProperty(prop);
            if (p != null)
            {
                p.boolValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void TrySetInt(Component c, string prop, int value)
        {
            var so = new SerializedObject(c);
            var p  = so.FindProperty(prop);
            if (p != null)
            {
                p.intValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void TrySetFloat(Component c, string prop, float value)
        {
            var so = new SerializedObject(c);
            var p  = so.FindProperty(prop);
            if (p != null)
            {
                p.floatValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
