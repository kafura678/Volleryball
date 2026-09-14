using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Volleyball.Editor
{
    public static class MobileBuildPreparation
    {
        [MenuItem("Volleyball/Prepare Mobile Build Settings")]
        public static void Apply()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            PlayerSettings[] settingsObjects = Resources.FindObjectsOfTypeAll<PlayerSettings>();
            if (settingsObjects.Length == 0)
                throw new System.InvalidOperationException("PlayerSettings object was not available.");

            var serializedSettings = new SerializedObject(settingsObjects[0]);
            SetBool(serializedSettings, "allowedAutorotateToPortrait", false);
            SetBool(serializedSettings, "allowedAutorotateToPortraitUpsideDown", false);
            SetBool(serializedSettings, "allowedAutorotateToLandscapeLeft", true);
            SetBool(serializedSettings, "allowedAutorotateToLandscapeRight", true);
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settingsObjects[0]);
            AssetDatabase.SaveAssets();

            ReportSupport();
            Debug.Log("Mobile build settings prepared: landscape orientations only.");
        }

        [MenuItem("Volleyball/Report Mobile Build Support")]
        public static void ReportSupport()
        {
            bool android = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android);
            bool ios = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS);
            Debug.Log($"Mobile build support: Android={android}, iOS={ios}");
        }

        [MenuItem("Volleyball/Build Android Development APK")]
        public static void BuildAndroidDevelopment()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new System.InvalidOperationException("Android Build Support is not installed.");

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (!scenes.Contains("Assets/Scenes/Match.unity"))
                throw new System.InvalidOperationException("Match scene is not enabled in Build Settings.");
            if (PlayerSettings.allowedAutorotateToPortrait || PlayerSettings.allowedAutorotateToPortraitUpsideDown)
                throw new System.InvalidOperationException("Portrait orientation must be disabled before building.");

            bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.Android, BuildTarget.Android);
            if (!switched || EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new System.InvalidOperationException("Failed to switch the active platform to Android.");

            string outputPath = System.Environment.GetEnvironmentVariable("VOLLEYBALL_ANDROID_BUILD_PATH");
            if (string.IsNullOrWhiteSpace(outputPath))
                outputPath = "/tmp/Volleyball-Development.apk";
            string outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDirectory)) Directory.CreateDirectory(outputDirectory);

            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.InvalidOperationException(
                    $"Android Development Build failed: {report.summary.result}, errors={report.summary.totalErrors}");

            Debug.Log($"Android Development Build succeeded: path={report.summary.outputPath}, " +
                $"size={report.summary.totalSize}, warnings={report.summary.totalWarnings}, " +
                $"duration={report.summary.totalTime}");
        }

        static void SetBool(SerializedObject settings, string propertyName, bool value)
        {
            SerializedProperty property = settings.FindProperty(propertyName);
            if (property == null)
                throw new System.InvalidOperationException($"PlayerSettings property was not found: {propertyName}");
            property.boolValue = value;
        }
    }
}
