using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Quest3IndoorNavigation.Editor
{
    public static class Quest3Build10HandTrackingFix
    {
        private const string TestScenePath = "Assets/Scenes/Quest3_APK_TestScene.unity";

        [MenuItem("Quest3 Indoor Navigation/Build10/Fix Hand Tracking Components")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);

            ConfigureHandAnchor("LeftHandAnchor", OVRHand.Hand.HandLeft, OVRSkeleton.SkeletonType.HandLeft, OVRMesh.MeshType.HandLeft);
            ConfigureHandAnchor("RightHandAnchor", OVRHand.Hand.HandRight, OVRSkeleton.SkeletonType.HandRight, OVRMesh.MeshType.HandRight);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[Build10] Hand tracking components configured on LeftHandAnchor and RightHandAnchor.");
        }

        private static void ConfigureHandAnchor(
            string anchorName,
            OVRHand.Hand handType,
            OVRSkeleton.SkeletonType skeletonType,
            OVRMesh.MeshType meshType)
        {
            var anchor = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name == anchorName);

            if (anchor == null)
            {
                throw new MissingReferenceException($"[Build10] Could not find {anchorName} in {TestScenePath}.");
            }

            var hand = EnsureComponent<OVRHand>(anchor.gameObject);
            var skeleton = EnsureComponent<OVRSkeleton>(anchor.gameObject);
            var skeletonRenderer = EnsureComponent<OVRSkeletonRenderer>(anchor.gameObject);
            var mesh = EnsureComponent<OVRMesh>(anchor.gameObject);
            var meshRenderer = EnsureComponent<OVRMeshRenderer>(anchor.gameObject);

            SetField(hand, "HandType", handType);

            SetField(skeleton, "_skeletonType", skeletonType);
            SetObject(skeleton, "_dataProvider", hand);
            SetBool(skeleton, "_enablePhysicsCapsules", false);

            SetObject(skeletonRenderer, "_dataProvider", hand);

            SetObject(mesh, "_dataProvider", hand);
            SetField(mesh, "_meshType", meshType);

            SetObject(meshRenderer, "_dataProvider", hand);
            SetObject(meshRenderer, "_ovrMesh", mesh);
            SetObject(meshRenderer, "_ovrSkeleton", skeleton);

            EditorUtility.SetDirty(anchor.gameObject);
            EditorUtility.SetDirty(hand);
            EditorUtility.SetDirty(skeleton);
            EditorUtility.SetDirty(skeletonRenderer);
            EditorUtility.SetDirty(mesh);
            EditorUtility.SetDirty(meshRenderer);

            Debug.Log($"[Build10] {anchorName}: OVRHand={handType}, OVRSkeleton={skeletonType}, OVRMesh={meshType}.");
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            return gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();
        }

        private static void SetField(UnityEngine.Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
            {
                Debug.LogWarning($"[Build10] {target.GetType().Name}.{fieldName} was not found.");
                return;
            }

            field.SetValue(target, value);
            EditorUtility.SetDirty(target);
        }

        private static void SetBool(UnityEngine.Object target, string propertyName, bool value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"[Build10] {target.GetType().Name}.{propertyName} was not found.");
                return;
            }

            property.boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObject(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"[Build10] {target.GetType().Name}.{propertyName} was not found.");
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
