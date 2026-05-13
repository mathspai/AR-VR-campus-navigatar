using System.IO;
using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Quest3IndoorNavigation.Editor
{
    public static class Quest3APKBuilder
    {
        private const string TestScenePath = "Assets/Scenes/SpatialAnchorMVP.unity";
        private const string OutputPath    = @"D:\VR_navigation\Builds\Quest3SpatialAnchorMVP.apk";

        public static void BuildAPK()
        {
            var outputDir = Path.GetDirectoryName(OutputPath);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            Quest3ProjectBootstrap.ApplyQuest3AndroidSettings();
            TryCreateSpatialAnchorMvpScene();

            var options = new BuildPlayerOptions
            {
                scenes           = new[] { TestScenePath },
                locationPathName = OutputPath,
                target           = BuildTarget.Android,
                options          = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Quest3APKBuilder] Build succeeded. APK: {OutputPath}  Size: {summary.totalSize / 1048576f:F1} MB");
            }
            else
            {
                Debug.LogError($"[Quest3APKBuilder] Build failed. Result: {summary.result}  Errors: {summary.totalErrors}");
                EditorApplication.Exit(1);
            }
        }

        private static void TryCreateSpatialAnchorMvpScene()
        {
            var builderType = Type.GetType("Quest3IndoorNavigation.Editor.SpatialAnchorMVPSceneBuilder");
            builderType ??= typeof(Quest3APKBuilder).Assembly.GetType("Quest3IndoorNavigation.Editor.SpatialAnchorMVPSceneBuilder");
            var createScene = builderType?.GetMethod("CreateScene", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            createScene?.Invoke(null, null);
        }
    }
}
