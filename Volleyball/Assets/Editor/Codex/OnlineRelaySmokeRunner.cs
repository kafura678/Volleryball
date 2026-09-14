using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Volleyball.Editor
{
    public static class OnlineRelaySmokeRunner
    {
        const string RunningKey = "Volleyball.OnlineRelaySmokeRunner.Running";
        const string JoinCodePath = "/tmp/volleyball-relay-join-code.txt";
        const string ResultPath = "/tmp/volleyball-relay-result.txt";
        static bool started;

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            if (!SessionState.GetBool(RunningKey, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            if (EditorApplication.isPlaying) EditorApplication.delayCall += RunHost;
        }

        public static void HostAndWaitForClient()
        {
            File.Delete(JoinCodePath);
            File.Delete(ResultPath);
            SessionState.SetBool(RunningKey, true);
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorSceneManager.OpenScene("Assets/Scenes/Match.unity", OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode) RunHost();
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(RunningKey, false))
            {
                SessionState.SetBool(RunningKey, false);
                EditorApplication.Exit(File.Exists(ResultPath) ? 0 : 1);
            }
        }

        static async void RunHost()
        {
            if (started) return;
            started = true;
            try
            {
                OnlineSessionController[] controllers = Resources.FindObjectsOfTypeAll<OnlineSessionController>();
                if (controllers.Length == 0) throw new InvalidOperationException("OnlineSessionController was not found");
                OnlineSessionController controller = controllers[0];
                bool hosted = await controller.HostMatchAsync();
                if (!hosted) throw new InvalidOperationException(controller.Status);
                File.WriteAllText(JoinCodePath, controller.JoinCode);
                Debug.Log("Relay smoke host join code: " + controller.JoinCode);

                DateTime deadline = DateTime.UtcNow.AddMinutes(5);
                while (controller.ConnectedPlayerCount < 2 && DateTime.UtcNow < deadline)
                    await Task.Delay(250);

                if (controller.ConnectedPlayerCount < 2)
                    throw new TimeoutException("Client did not connect within five minutes");
                File.WriteAllText(ResultPath, "PASS players=" + controller.ConnectedPlayerCount);
                await controller.DisconnectAsync();
            }
            catch (Exception exception)
            {
                File.WriteAllText(ResultPath, "FAIL " + exception);
                Debug.LogException(exception);
            }
            finally
            {
                SessionState.SetBool(RunningKey, true);
                EditorApplication.ExitPlaymode();
            }
        }
    }
}
