using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Quest3IndoorNavigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Quest3IndoorNavigation.Editor
{
    [InitializeOnLoad]
    public static class Quest3SceneBootstrap
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string MaterialPath = "Assets/Quest3IndoorNavigation/NavigationLine.mat";

        static Quest3SceneBootstrap()
        {
            EditorApplication.delayCall += EnsureSceneSkeleton;
        }

        [MenuItem("Quest3 Indoor Navigation/Build MR Navigation Scene Skeleton")]
        public static void EnsureSceneSkeleton()
        {
            if (Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var root = GetOrCreate("IndoorNavigationRoot");
            var xrRig = EnsureXrRig(root.transform);
            var controller = EnsureNavigationController(root.transform);
            var floorRoutePlanner = EnsureFloorRoutePlanner(root.transform, controller);
            var targets = EnsureTargets(root.transform, controller);
            EnsureRegistry(root.transform, controller, floorRoutePlanner, targets);
            EnsureNavMeshSurface(root.transform);
            EnsureQuestMeshing(root.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Quest 3 MR navigation scene skeleton is ready.");
        }

        private static GameObject EnsureXrRig(Transform parent)
        {
            var rig = GetOrCreate("XR Origin / Camera Rig", parent);
            var ovrCameraRig = TryAddComponent(rig, "OVRCameraRig");
            var cameraObject = FindChild(rig.transform, "CenterEyeAnchor") ?? Camera.main?.gameObject ?? GetOrCreate("CenterEyeAnchor", rig.transform);
            cameraObject.transform.SetParent(rig.transform, false);
            cameraObject.transform.localPosition = Vector3.zero;
            cameraObject.transform.localRotation = Quaternion.identity;
            cameraObject.tag = "MainCamera";

            var camera = cameraObject.GetComponent<Camera>() ?? cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;

            var xrOrigin = TryAddComponent(rig, "Unity.XR.CoreUtils.XROrigin");
            if (xrOrigin != null)
            {
                TrySetObjectReference(xrOrigin, "m_Camera", camera);
                TrySetObjectReference(xrOrigin, "m_CameraFloorOffsetObject", rig);
                TrySetSerializedProperty(xrOrigin, "m_CameraYOffset", 0.0f);
            }

            var fallbackCamera = FindChild(rig.transform, "Main Camera");
            if (fallbackCamera != null && fallbackCamera != cameraObject)
            {
                fallbackCamera.tag = "Untagged";
                var fallbackCameraComponent = fallbackCamera.GetComponent<Camera>();
                if (fallbackCameraComponent != null)
                {
                    fallbackCameraComponent.enabled = false;
                }

                var fallbackAudioListener = fallbackCamera.GetComponent<AudioListener>();
                if (fallbackAudioListener != null)
                {
                    fallbackAudioListener.enabled = false;
                }
            }

            var ovrManager = TryAddComponent(rig, "OVRManager");
            if (ovrManager != null)
            {
                TrySetSerializedProperty(ovrManager, "isInsightPassthroughEnabled", true);
                TrySetSerializedProperty(ovrManager, "requestScenePermissionOnStartup", true);
                TrySetSerializedProperty(ovrManager, "requestPassthroughCameraAccessPermissionOnStartup", false);
                TrySetSerializedProperty(ovrManager, "usePositionTracking", true);
                TrySetSerializedProperty(ovrManager, "useRotationTracking", true);
                TrySetSerializedProperty(ovrManager, "resetTrackerOnLoad", false);
                TrySetSerializedProperty(ovrManager, "AllowRecenter", true);
            }

            TryAddComponent(rig, "OVRSceneManager");

            var passthrough = TryAddComponent(rig, "OVRPassthroughLayer");
            if (passthrough != null)
            {
                TrySetSerializedProperty(passthrough, "placement", 0);
                TrySetSerializedProperty(passthrough, "compositionDepth", -1);
            }

            if (ovrCameraRig != null)
            {
                TrySetSerializedProperty(ovrCameraRig, "disableEyeAnchorCameras", false);
            }

            return rig;
        }

        private static GameObject EnsureNavigationController(Transform parent)
        {
            var navigationObject = GetOrCreate("NavigationController", parent);
            var controller = navigationObject.GetComponent<IndoorNavigationController>() ?? navigationObject.AddComponent<IndoorNavigationController>();
            var userCamera = Camera.main != null ? Camera.main.transform : FindChild(parent, "CenterEyeAnchor")?.transform;
            TrySetObjectReference(controller, "user", userCamera);

            var lineRenderer = navigationObject.GetComponent<LineRenderer>() ?? navigationObject.AddComponent<LineRenderer>();
            lineRenderer.sharedMaterial = EnsureLineMaterial();
            lineRenderer.startColor = new Color(0.0f, 1.0f, 0.85f, 1.0f);
            lineRenderer.endColor = new Color(0.0f, 0.5f, 1.0f, 1.0f);
            lineRenderer.startWidth = 0.08f;
            lineRenderer.endWidth = 0.08f;
            lineRenderer.numCornerVertices = 6;
            lineRenderer.numCapVertices = 6;
            lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;

            return navigationObject;
        }

        private static GameObject EnsureFloorRoutePlanner(Transform parent, GameObject controller)
        {
            var plannerObject = GetOrCreate("FloorRoutePlanner", parent);
            var planner = plannerObject.GetComponent<FloorRoutePlanner>() ?? plannerObject.AddComponent<FloorRoutePlanner>();
            TrySetObjectReference(planner, "navigationController", controller.GetComponent<IndoorNavigationController>());
            TrySetSerializedProperty(planner, "currentFloorId", 1);
            return plannerObject;
        }

        private static List<NavigationTarget> EnsureTargets(Transform parent, GameObject controller)
        {
            var targetRoot = GetOrCreate("NavigationTargets", parent);
            var targetData = new[]
            {
                ("Target_A", new Vector3(1.5f, 0.05f, 2.0f), Color.yellow, 1),
                ("Target_B", new Vector3(-1.5f, 0.05f, 2.5f), new Color(1.0f, 0.25f, 0.2f), 1),
                ("Target_C", new Vector3(0.0f, 0.05f, 4.0f), new Color(0.2f, 0.8f, 1.0f), 1)
            };

            var targets = new List<NavigationTarget>();
            foreach (var (name, position, color, floorId) in targetData)
            {
                var targetObject = GetOrCreate(name, targetRoot.transform);
                targetObject.transform.localPosition = position;
                targetObject.transform.localScale = Vector3.one * 0.25f;

                var marker = targetObject.GetComponent<MeshRenderer>();
                if (marker == null)
                {
                    var primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    primitive.name = name;
                    primitive.transform.SetParent(targetRoot.transform, false);
                    primitive.transform.localPosition = position;
                    primitive.transform.localScale = Vector3.one * 0.25f;
                    UnityEngine.Object.DestroyImmediate(targetObject);
                    targetObject = primitive;
                    marker = targetObject.GetComponent<MeshRenderer>();
                }

                marker.sharedMaterial = EnsureTargetMaterial(name, color);
                var target = targetObject.GetComponent<NavigationTarget>() ?? targetObject.AddComponent<NavigationTarget>();
                TrySetObjectReference(target, "navigationController", controller.GetComponent<IndoorNavigationController>());
                TrySetObjectReference(target, "floorRoutePlanner", FindFirstComponent(typeof(FloorRoutePlanner).FullName));
                TrySetSerializedProperty(target, "floorId", floorId);
                targets.Add(target);
            }

            return targets;
        }

        private static void EnsureRegistry(Transform parent, GameObject controller, GameObject floorRoutePlanner, List<NavigationTarget> targets)
        {
            var registryObject = GetOrCreate("NavigationTargetRegistry", parent);
            var registry = registryObject.GetComponent<NavigationTargetRegistry>() ?? registryObject.AddComponent<NavigationTargetRegistry>();
            TrySetObjectReference(registry, "navigationController", controller.GetComponent<IndoorNavigationController>());
            TrySetObjectReference(registry, "floorRoutePlanner", floorRoutePlanner.GetComponent<FloorRoutePlanner>());
            TrySetObjectReference(floorRoutePlanner.GetComponent<FloorRoutePlanner>(), "targetRegistry", registry);

            var serializedObject = new SerializedObject(registry);
            var targetsProperty = serializedObject.FindProperty("targets");
            targetsProperty.arraySize = targets.Count;
            for (var i = 0; i < targets.Count; i++)
            {
                targetsProperty.GetArrayElementAtIndex(i).objectReferenceValue = targets[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureNavMeshSurface(Transform parent)
        {
            var navMeshObject = GetOrCreate("RuntimeNavMeshSurface", parent);
            TryAddComponent(navMeshObject, "Unity.AI.Navigation.NavMeshSurface");
        }

        private static void EnsureQuestMeshing(Transform parent)
        {
            var meshingObject = GetOrCreate("QuestDepthMeshing", parent);
            if (meshingObject.GetComponent<MeshFilter>() == null)
            {
                meshingObject.AddComponent<MeshFilter>();
            }

            if (meshingObject.GetComponent<MeshRenderer>() == null)
            {
                meshingObject.AddComponent<MeshRenderer>();
            }

            if (meshingObject.GetComponent<MeshCollider>() == null)
            {
                meshingObject.AddComponent<MeshCollider>();
            }

            var depthPreprocessor = TryAddComponent(meshingObject, "Uralstech.UXR.QuestMeshing.DepthPreprocessor");
            if (depthPreprocessor != null)
            {
                TrySetObjectReference(depthPreprocessor, "_shader", AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.uralstech.uxr.questmeshing/Runtime/Shaders/DepthPreprocessor.compute"));
                TrySetObjectReference(depthPreprocessor, "_cameraRig", FindFirstComponent("OVRCameraRig"));
            }

            var depthMesher = TryAddComponent(meshingObject, "Uralstech.UXR.QuestMeshing.DepthMesher");
            if (depthMesher != null)
            {
                TrySetObjectReference(depthMesher, "_shader", AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.uralstech.uxr.questmeshing/Runtime/Shaders/SurfaceNets.compute"));
                TrySetObjectReference(depthMesher, "_cameraRig", FindFirstComponent("OVRCameraRig"));
                TrySetObjectReference(depthMesher, "_meshFilterConsumer", meshingObject.GetComponent<MeshFilter>());
                TrySetObjectReference(depthMesher, "_meshColliderConsumer", meshingObject.GetComponent<MeshCollider>());
                TrySetObjectReference(depthMesher, "_navMeshSurface", FindFirstComponent("Unity.AI.Navigation.NavMeshSurface"));
            }

            var cpuDepthSampler = TryAddComponent(meshingObject, "Uralstech.UXR.QuestMeshing.CPUDepthSampler");
            if (cpuDepthSampler != null)
            {
                TrySetObjectReference(cpuDepthSampler, "_shader", AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.uralstech.uxr.questmeshing/Runtime/Shaders/DepthSampler.compute"));
            }
        }

        private static GameObject GetOrCreate(string name, Transform parent = null)
        {
            var objects = Resources.FindObjectsOfTypeAll<GameObject>();
            var gameObject = objects.FirstOrDefault(obj => obj.name == name && obj.scene.IsValid());

            if (gameObject == null)
            {
                gameObject = new GameObject(name);
            }

            if (parent != null && gameObject.transform.parent != parent)
            {
                gameObject.transform.SetParent(parent, false);
            }

            return gameObject;
        }

        private static GameObject FindChild(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private static Component TryAddComponent(GameObject gameObject, string typeName)
        {
            var type = FindType(typeName);
            if (type == null || !typeof(Component).IsAssignableFrom(type))
            {
                return null;
            }

            return gameObject.GetComponent(type) ?? gameObject.AddComponent(type);
        }

        private static Type FindType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }

            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(typeName))
                .FirstOrDefault(foundType => foundType != null);
        }

        private static Component FindFirstComponent(string typeName)
        {
            var type = FindType(typeName);
            if (type == null || !typeof(Component).IsAssignableFrom(type))
            {
                return null;
            }

            return Resources.FindObjectsOfTypeAll(type)
                .OfType<Component>()
                .FirstOrDefault(component => component.gameObject.scene.IsValid());
        }

        private static Material EnsureLineMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            material.color = new Color(0.0f, 1.0f, 0.85f, 1.0f);
            return material;
        }

        private static Material EnsureTargetMaterial(string name, Color color)
        {
            var path = $"Assets/Quest3IndoorNavigation/{name}_Marker.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            return material;
        }

        private static void TrySetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void TrySetSerializedProperty(Component component, string propertyName, int value)
        {
            var serializedObject = new SerializedObject(component);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void TrySetSerializedProperty(Component component, string propertyName, bool value)
        {
            var serializedObject = new SerializedObject(component);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void TrySetSerializedProperty(Component component, string propertyName, float value)
        {
            var serializedObject = new SerializedObject(component);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
