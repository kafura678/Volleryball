using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Volleyball.Editor
{
    public static class WebBuildPreparation
    {
        [MenuItem("Volleyball/Build Web Development")]
        public static void BuildDevelopment()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                throw new InvalidOperationException("Web Build Support is not available.");
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                throw new InvalidOperationException("Web platform switch failed.");
            Debug.Log("Web Build Support recognized; active platform=WebGL");
            PlayerSettings.WebGL.template = "APPLICATION:Default";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            var settings = new SerializedObject(Resources.FindObjectsOfTypeAll<PlayerSettings>()[0]);
            settings.FindProperty("webGLThreadsSupport").boolValue = false;
            settings.FindProperty("webGLInitialMemorySize").intValue = 128;
            settings.FindProperty("webGLMaximumMemorySize").intValue = 1024;
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (!scenes.Contains("Assets/Scenes/Match.unity")) throw new InvalidOperationException("Match is not enabled.");
            string path = Environment.GetEnvironmentVariable("VOLLEYBALL_WEB_BUILD_PATH") ?? "/tmp/Volleyball-Web";
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes, locationPathName = path, target = BuildTarget.WebGL, options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Web build failed: " + report.summary.result);
            if (Environment.GetEnvironmentVariable("VOLLEYBALL_WEB_STANDARD_TEMPLATE") != "1") AddBrowserLayout(path);
            Debug.Log("Web Development Build succeeded: " + path + ", bytes=" + report.summary.totalSize);
        }

        static void AddBrowserLayout(string path)
        {
            string index = Path.Combine(path, "index.html");
            string extension = @"
<style>
#unity-container { position:fixed !important; left:env(safe-area-inset-left,0px) !important; right:env(safe-area-inset-right,0px); top:env(safe-area-inset-top,0px) !important; bottom:env(safe-area-inset-bottom,0px); transform:none !important; overscroll-behavior:contain; }
#unity-canvas { width:100% !important; height:100% !important; touch-action:none; user-select:none; -webkit-user-select:none; }
#unity-footer { display:none; }
#volleyball-landscape { position:absolute; inset:0; background:#102030; color:white; display:none; align-items:center; justify-content:center; font:24px sans-serif; text-align:center; z-index:100; }
</style>
<script>
const game = document.querySelector('#unity-container');
const rotate = document.createElement('div'); rotate.id = 'volleyball-landscape';
rotate.textContent = '端末を横向きにしてください / Rotate to landscape'; game.appendChild(rotate);
function updateLandscape() { rotate.style.display = (navigator.maxTouchPoints > 0 && innerHeight > innerWidth) ? 'flex' : 'none'; }
addEventListener('resize', updateLandscape); updateLandscape();
let viewport = document.querySelector('meta[name=viewport]');
if (!viewport) { viewport = document.createElement('meta'); viewport.name='viewport'; document.head.appendChild(viewport); }
viewport.content = 'width=device-width, initial-scale=1, viewport-fit=cover';
</script>
";
            File.WriteAllText(index, File.ReadAllText(index).Replace("</body>", extension + "</body>"));
        }
    }
}
