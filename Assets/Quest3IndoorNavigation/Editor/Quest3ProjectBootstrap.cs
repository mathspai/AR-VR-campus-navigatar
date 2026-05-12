using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace Quest3IndoorNavigation.Editor
{
    public static class Quest3ProjectBootstrap
    {
        [MenuItem("Quest3 Indoor Navigation/Apply Quest 3 Android Settings")]
        public static void ApplyQuest3AndroidSettings()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.example.quest3indoornavigation");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });

            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Medium);

            AssetDatabase.SaveAssets();
            Debug.Log("Quest 3 Android project settings applied. Enable OpenXR and Meta Quest features in Project Settings > XR Plug-in Management if package settings have not appeared yet.");
        }

        [MenuItem("Quest3 Indoor Navigation/Apply Quest 3 MR OpenXR Settings")]
        public static void ApplyQuest3MROpenXRSettings()
        {
            ApplyQuest3AndroidSettings();
            EnableXrManagement();
            EnableOpenXRFeatures();
            EnableOculusProjectConfig();

            AssetDatabase.SaveAssets();
            Debug.Log("Quest 3 MR OpenXR project settings applied.");
        }

        private static void EnableXrManagement()
        {
            var xrSettings = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
            if (xrSettings == null)
            {
                Debug.LogWarning("XRGeneralSettingsPerBuildTarget.asset was not found.");
                return;
            }

            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath("Assets/XR/XRGeneralSettingsPerBuildTarget.asset"))
            {
                var serializedObject = new SerializedObject(asset);
                SetBool(serializedObject, "m_AutomaticLoading", true);
                SetBool(serializedObject, "m_AutomaticRunning", true);
                SetBool(serializedObject, "m_InitManagerOnStart", true);
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void EnableOpenXRFeatures()
        {
            const string openXrSettingsPath = "Assets/XR/Settings/OpenXR Package Settings.asset";
            var assets = AssetDatabase.LoadAllAssetsAtPath(openXrSettingsPath);
            var enabledNames = new[]
            {
                "MetaXRFeature Android",
                "ARCameraFeature Android",
                "AROcclusionFeature Android",
                "ARMeshFeature Android",
                "ARPlaneFeature Android",
                "ARAnchorFeature Android",
                "ARSessionFeature Android",
                "ARRaycastFeature Android",
                "OculusTouchControllerProfile Android",
                "MetaQuestTouchPlusControllerProfile Android",
                "MetaXRFoveationFeature Android",
                "MetaXRSubsampledLayout Android"
            };

            foreach (var asset in assets)
            {
                if (asset == null || System.Array.IndexOf(enabledNames, asset.name) < 0)
                {
                    continue;
                }

                var serializedObject = new SerializedObject(asset);
                SetBool(serializedObject, "m_enabled", true);

                if (asset.name == "ARCameraFeature Android")
                {
                    SetBool(serializedObject, "m_PassthroughPreSplashScreen", true);
                }

                if (asset.name == "AROcclusionFeature Android")
                {
                    SetBool(serializedObject, "m_EnableHandRemoval", true);
                }

                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void EnableOculusProjectConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Oculus/OculusProjectConfig.asset");
            if (config == null)
            {
                Debug.LogWarning("OculusProjectConfig.asset was not found.");
                return;
            }

            var serializedObject = new SerializedObject(config);
            SetBool(serializedObject, "anchorSupport", true);
            SetBool(serializedObject, "sceneSupport", true);
            SetBool(serializedObject, "insightPassthroughEnabled", true);
            SetBool(serializedObject, "_insightPassthroughSupport", true);
            SetBool(serializedObject, "isPassthroughCameraAccessEnabled", true);
            SetBool(serializedObject, "boundaryVisibilitySupport", true);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
        }
    }
}
