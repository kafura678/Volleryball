using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Volleyball.Editor
{
    public static class MobileControlsSceneUpdater
    {
        const string ScenePath = "Assets/Scenes/Match.unity";

        [MenuItem("Volleyball/Apply Mobile Controls Prototype")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            MatchController match = Object.FindFirstObjectByType<MatchController>();
            if (!match) throw new System.InvalidOperationException("MatchController was not found in Match scene.");

            MobileInputController input = match.GetComponent<MobileInputController>();
            if (!input) input = match.gameObject.AddComponent<MobileInputController>();
            input.Character = match.Human;
            input.Match = match;
            input.ViewCamera = Camera.main;
            input.Settings = match.Settings;

            MobileControlsInstaller installer = match.GetComponent<MobileControlsInstaller>();
            if (!installer) installer = match.gameObject.AddComponent<MobileControlsInstaller>();
            installer.Match = match;
            installer.Input = input;
            installer.ShowMobileControlsInEditor = true;

            EditorUtility.SetDirty(match.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Mobile controls prototype applied to Match scene.");
        }
    }
}
