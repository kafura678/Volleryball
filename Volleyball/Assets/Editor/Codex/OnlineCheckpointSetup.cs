using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Volleyball.Editor
{
    public static class OnlineCheckpointSetup
    {
        const string ScenePath = "Assets/Scenes/Match.unity";
        const string PlayerPrefabPath = "Assets/Prefabs/OnlineNetworkPlayer.prefab";
        const string PrefabListPath = "Assets/DefaultNetworkPrefabs.asset";

        [MenuItem("Volleyball/Configure Online Checkpoint 1")]
        public static void Configure()
        {
            GameObject playerPrefab = CreateOrUpdatePlayerPrefab();
            NetworkPrefabsList prefabList = RegisterNetworkPrefab(playerPrefab);
            ConfigureScene(playerPrefab, prefabList);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Online Checkpoint 1 scene and network prefab configured.");
        }

        static GameObject CreateOrUpdatePlayerPrefab()
        {
            GameObject root = new GameObject("OnlineNetworkPlayer");
            root.AddComponent<NetworkObject>();
            root.AddComponent<OwnerNetworkTransform>();
            root.AddComponent<OnlineNetworkPlayer>();

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "RemotePlayerVisual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.up;
            visual.transform.localScale = new Vector3(0.82f, 1f, 0.82f);
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            Object.DestroyImmediate(root);
            if (!prefab) throw new System.InvalidOperationException("Failed to create OnlineNetworkPlayer prefab.");
            return prefab;
        }

        static NetworkPrefabsList RegisterNetworkPrefab(GameObject playerPrefab)
        {
            NetworkPrefabsList list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(PrefabListPath);
            if (!list)
            {
                list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
                AssetDatabase.CreateAsset(list, PrefabListPath);
            }

            if (!list.Contains(playerPrefab))
            {
                list.Add(new NetworkPrefab
                {
                    Override = NetworkPrefabOverride.None,
                    Prefab = playerPrefab
                });
                EditorUtility.SetDirty(list);
            }
            return list;
        }

        static void ConfigureScene(GameObject playerPrefab, NetworkPrefabsList prefabList)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            MatchController match = Object.FindFirstObjectByType<MatchController>();
            if (!match) throw new System.InvalidOperationException("MatchController was not found in Match scene.");

            GameObject root = scene.GetRootGameObjects().FirstOrDefault(item => item.name == "OnlineNetworkRoot");
            if (!root)
            {
                root = new GameObject("OnlineNetworkRoot");
                SceneManager.MoveGameObjectToScene(root, scene);
            }
            root.SetActive(false);

            NetworkManager manager = GetOrAdd<NetworkManager>(root);
            UnityTransport transport = GetOrAdd<UnityTransport>(root);
            GetOrAdd<NetworkManagerGuard>(root);
            OnlineSessionController controller = GetOrAdd<OnlineSessionController>(root);

            manager.NetworkConfig.NetworkTransport = transport;
            manager.NetworkConfig.PlayerPrefab = playerPrefab;
            manager.NetworkConfig.EnableSceneManagement = false;
            manager.NetworkConfig.ForceSamePrefabs = true;
            manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Clear();
            manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(prefabList);

            controller.NetworkManager = manager;
            controller.Match = match;
            controller.CpuController = match.Cpu.GetComponent<CpuController>();
            controller.DesktopInput = match.Human.GetComponent<PlayerInputController>();
            controller.MobileInput = Object.FindFirstObjectByType<MobileInputController>();

            OnlineMenuInstaller menu = GetOrAdd<OnlineMenuInstaller>(match.gameObject);
            menu.Controller = controller;

            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(match.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component ? component : target.AddComponent<T>();
        }
    }
}
