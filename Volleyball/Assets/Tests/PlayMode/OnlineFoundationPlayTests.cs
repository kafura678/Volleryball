using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Volleyball.Tests
{
    public class OnlineFoundationPlayTests
    {
        [UnityTest]
        public IEnumerator MatchScene_ContainsSingleConfiguredInactiveNetworkManager()
        {
            yield return SceneManager.LoadSceneAsync("Match");
            yield return null;

            NetworkManager[] managers = Resources.FindObjectsOfTypeAll<NetworkManager>()
                .Where(item => item.gameObject.scene == SceneManager.GetActiveScene())
                .ToArray();
            Assert.AreEqual(1, managers.Length);
            NetworkManager manager = managers[0];
            Assert.False(manager.gameObject.activeSelf, "Network services stay dormant during Offline mode");
            Assert.NotNull(manager.GetComponent<UnityTransport>());
            Assert.NotNull(manager.GetComponent<NetworkManagerGuard>());
            Assert.NotNull(manager.GetComponent<OnlineSessionController>());
            Assert.NotNull(manager.NetworkConfig.PlayerPrefab);
            Assert.NotNull(manager.NetworkConfig.PlayerPrefab.GetComponent<NetworkObject>());
            Assert.NotNull(manager.NetworkConfig.PlayerPrefab.GetComponent<OnlineNetworkPlayer>());
            Assert.AreEqual(manager.GetComponent<UnityTransport>(), manager.NetworkConfig.NetworkTransport);
            Assert.False(manager.NetworkConfig.EnableSceneManagement);
        }

        [UnityTest]
        public IEnumerator MatchScene_OfflineModeKeepsCpuAndExistingInputsEnabled()
        {
            yield return SceneManager.LoadSceneAsync("Match");
            yield return null;

            MatchController match = Object.FindFirstObjectByType<MatchController>();
            Assert.NotNull(match);
            Assert.True(match.Cpu.gameObject.activeSelf);
            Assert.True(match.Cpu.GetComponent<CpuController>().enabled);
            Assert.True(match.Human.GetComponent<PlayerInputController>().enabled);
            Assert.NotNull(Object.FindFirstObjectByType<MobileInputController>());
            Assert.NotNull(match.GetComponent<OnlineMenuInstaller>());
        }

        [UnityTest]
        public IEnumerator NetworkPrefabList_ContainsConfiguredPlayerPrefabOnce()
        {
            yield return SceneManager.LoadSceneAsync("Match");
            yield return null;

            NetworkManager manager = Resources.FindObjectsOfTypeAll<NetworkManager>()
                .First(item => item.gameObject.scene == SceneManager.GetActiveScene());
            int matches = manager.NetworkConfig.Prefabs.NetworkPrefabsLists
                .SelectMany(list => list.PrefabList)
                .Count(item => item.Prefab == manager.NetworkConfig.PlayerPrefab);
            Assert.AreEqual(1, matches);
        }
    }
}
