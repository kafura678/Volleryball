using System.Collections;
using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;
using Object = UnityEngine.Object;

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
            Assert.NotNull(manager.GetComponent<OnlineSessionController>().MatchSynchronizerPrefab);
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
            OnlineMatchSynchronizer synchronizer = manager.GetComponent<OnlineSessionController>().MatchSynchronizerPrefab;
            int synchronizers = manager.NetworkConfig.Prefabs.NetworkPrefabsLists
                .SelectMany(list => list.PrefabList)
                .Count(item => item.Prefab == synchronizer.gameObject);
            Assert.AreEqual(1, synchronizers);
        }

        [UnityTest]
        public IEnumerator ClientBallSnapshot_DisablesLocalPhysicsAndAppliesAuthoritativeState()
        {
            yield return SceneManager.LoadSceneAsync("Match");
            yield return null;

            BallController ball = Object.FindFirstObjectByType<BallController>();
            var snapshot = new BallNetworkState
            {
                Position = new Vector3(2f, 4f, -3f),
                Rotation = Quaternion.Euler(0f, 25f, 0f),
                Velocity = new Vector3(1f, 2f, 3f),
                IsLive = true,
                IsKinematic = false,
                HitSerial = 9
            };
            ball.SetSimulationAuthority(false);
            ball.ApplyNetworkState(snapshot, true);

            Assert.False(ball.HasSimulationAuthority);
            Assert.True(ball.Body.isKinematic);
            Assert.That(ball.Body.position, Is.EqualTo(snapshot.Position).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(ball.Velocity, Is.EqualTo(snapshot.Velocity).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.True(ball.IsLive);
            Assert.AreEqual(9, ball.HitSerial);
            ball.SetSimulationAuthority(true);
        }

        [UnityTest]
        public IEnumerator ClientMatchSnapshot_UpdatesScoreAndCannotRestartLocally()
        {
            yield return SceneManager.LoadSceneAsync("Match");
            yield return null;

            MatchController match = Object.FindFirstObjectByType<MatchController>();
            match.SetNetworkAuthority(false);
            match.ApplyNetworkState(new MatchNetworkState
            {
                State = MatchState.MatchFinished,
                Server = TeamId.Cpu,
                LastTouch = TeamId.Cpu,
                TeamAScore = 2,
                TeamBScore = 3,
                TargetScore = 3,
                HasWinner = true,
                Winner = TeamId.Cpu
            });
            match.Restart();
            yield return null;

            Assert.False(match.HasMatchAuthority);
            Assert.AreEqual(MatchState.MatchFinished, match.Rules.State);
            Assert.AreEqual(2, match.Rules.HumanScore);
            Assert.AreEqual(3, match.Rules.CpuScore);
            Assert.AreEqual(string.Empty, match.LastMessage);
        }

        [UnityTest]
        public IEnumerator MatchScene_ContainsExactlyOneGameplayBall()
        {
            yield return SceneManager.LoadSceneAsync("Match");
            yield return null;
            Assert.AreEqual(1, Object.FindObjectsByType<BallController>(FindObjectsSortMode.None).Length);
        }

        [UnityTest]
        public IEnumerator LocalHost_StartsOneOwnedPlayerAndOneAuthoritativeMatchSynchronizer()
        {
            yield return SceneManager.LoadSceneAsync("Match");
            yield return null;

            NetworkManager manager = Resources.FindObjectsOfTypeAll<NetworkManager>()
                .First(item => item.gameObject.scene == SceneManager.GetActiveScene());
            manager.gameObject.SetActive(true);
            yield return null;
            OnlineSessionController controller = manager.GetComponent<OnlineSessionController>();
            controller.InitializeForTests(new LocalHostGateway(manager));
            Task<bool> hostTask = controller.HostMatchAsync();
            while (!hostTask.IsCompleted) yield return null;
            Assert.True(hostTask.Result);
            yield return null;
            yield return null;

            OnlineNetworkPlayer[] players = Object.FindObjectsByType<OnlineNetworkPlayer>(FindObjectsSortMode.None)
                .Where(item => item.IsSpawned).ToArray();
            OnlineMatchSynchronizer[] synchronizers = Object.FindObjectsByType<OnlineMatchSynchronizer>(FindObjectsSortMode.None)
                .Where(item => item.IsSpawned).ToArray();
            MatchController match = Object.FindFirstObjectByType<MatchController>();
            Assert.AreEqual(1, players.Length);
            Assert.True(players[0].IsOwner);
            Assert.AreEqual(TeamId.Human, players[0].Team);
            Assert.AreEqual(1, synchronizers.Length);
            Assert.True(match.Ball.HasSimulationAuthority);
            Assert.False(match.Cpu.GetComponent<CpuController>().enabled);
            Assert.AreEqual(MatchState.Waiting, match.Rules.State);

            Task disconnectTask = controller.DisconnectAsync();
            while (!disconnectTask.IsCompleted) yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator TeamBHumanControlledServe_UsesTossThenTimedStrike()
        {
            yield return SceneManager.LoadSceneAsync("Match");
            yield return null;

            MatchController match = Object.FindFirstObjectByType<MatchController>();
            match.Cpu.GetComponent<CpuController>().enabled = false;
            match.Cpu.Actions.UsePlayerServeControls = true;
            match.Rules.Synchronize(new MatchNetworkState
            {
                State = MatchState.ServePreparation,
                Server = TeamId.Cpu,
                LastTouch = TeamId.Cpu,
                TargetScore = match.Settings.winningScore
            });
            match.Cpu.ResetForRally(match.Court.Spawn(TeamId.Cpu));
            match.Ball.Hold(match.Cpu.transform.position +
                new Vector3(0f, 1.5f, CourtDefinition.Direction(TeamId.Cpu) * 0.7f));
            int hitSerial = match.Ball.HitSerial;

            Assert.True(match.Cpu.RequestAction(ActionType.Serve));
            Assert.AreEqual(ServeStage.Tossed, match.Cpu.Actions.CurrentServeStage);
            Assert.AreEqual(hitSerial + 1, match.Ball.HitSerial);
            yield return new WaitForSeconds(match.Settings.servePerfectTime);
            Assert.True(match.Cpu.RequestAction(ActionType.Serve));
            yield return new WaitForFixedUpdate();

            Assert.AreEqual(MatchState.Playing, match.Rules.State);
            Assert.AreEqual(hitSerial + 2, match.Ball.HitSerial);
            Assert.Less(match.Cpu.Actions.Interaction.LastServeTarget.z, 0f);
        }

        [UnityTest]
        public IEnumerator TeamBLocalPlayer_ServePreparationShowsMobileServeArea()
        {
            yield return SceneManager.LoadSceneAsync("Match");
            yield return null;

            MatchController match = Object.FindFirstObjectByType<MatchController>();
            match.Human.Motor.Team = TeamId.Cpu;
            match.SetNetworkAuthority(false);
            match.ApplyNetworkState(new MatchNetworkState
            {
                State = MatchState.ServePreparation,
                Server = TeamId.Cpu,
                LastTouch = TeamId.Cpu,
                TargetScore = match.Settings.winningScore
            });
            yield return null;

            GameObject serveArea = GameObject.Find("MobileServeArea");
            Assert.NotNull(serveArea);
            Assert.True(serveArea.activeInHierarchy);
        }

        sealed class LocalHostGateway : IOnlineSessionGateway
        {
            readonly NetworkManager manager;
            public event Action<string> Disconnected { add { } remove { } }

            public LocalHostGateway(NetworkManager manager)
            {
                this.manager = manager;
            }

            public Task AuthenticateAsync() => Task.CompletedTask;
            public Task<OnlineSessionInfo> CreateHostSessionAsync()
            {
                if (!manager.StartHost()) throw new InvalidOperationException("Local host failed to start");
                return Task.FromResult(new OnlineSessionInfo("local-test", "LOCAL1"));
            }
            public Task<OnlineSessionInfo> JoinSessionAsync(string joinCode) =>
                Task.FromException<OnlineSessionInfo>(new NotSupportedException());
            public Task LeaveAsync()
            {
                if (manager.IsListening) manager.Shutdown();
                return Task.CompletedTask;
            }
        }
    }
}
