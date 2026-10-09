using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode;

namespace Volleyball.Tests
{
    public class OnlineSessionTests
    {
        [Test]
        public async Task Host_ValidGateway_AuthenticatesBeforeCreatingSession()
        {
            var gateway = new FakeGateway();
            using var flow = new OnlineSessionFlow(gateway);

            Assert.True(await flow.HostAsync());
            Assert.AreEqual("authenticate,host", string.Join(",", gateway.Calls));
            Assert.AreEqual(OnlineMode.Host, flow.Mode);
            Assert.AreEqual(OnlineConnectionState.Connected, flow.State);
            Assert.AreEqual("ABC123", flow.JoinCode);
        }

        [Test]
        public async Task Join_WhitespaceCode_RejectsBeforeAuthentication()
        {
            var gateway = new FakeGateway();
            using var flow = new OnlineSessionFlow(gateway);

            Assert.False(await flow.JoinAsync("  "));
            Assert.Zero(gateway.Calls.Count);
            Assert.AreEqual(OnlineConnectionState.Error, flow.State);
            Assert.AreEqual(OnlineMode.Offline, flow.Mode);
        }

        [Test]
        public async Task Join_ValidCode_NormalizesAndConnects()
        {
            var gateway = new FakeGateway();
            using var flow = new OnlineSessionFlow(gateway);

            Assert.True(await flow.JoinAsync(" ab-c12 "));
            Assert.AreEqual("AB-C12", gateway.JoinCode);
            Assert.AreEqual(OnlineMode.Client, flow.Mode);
            Assert.AreEqual(OnlineConnectionState.Connected, flow.State);
        }

        [Test]
        public async Task AuthenticationFailure_ReturnsToOfflineWithoutCreatingSession()
        {
            var gateway = new FakeGateway { AuthenticationError = new InvalidOperationException("auth unavailable") };
            using var flow = new OnlineSessionFlow(gateway);

            Assert.False(await flow.HostAsync());
            Assert.False(gateway.Calls.Contains("host"));
            Assert.AreEqual(OnlineMode.Offline, flow.Mode);
            Assert.AreEqual(OnlineConnectionState.Error, flow.State);
            StringAssert.Contains("auth unavailable", flow.Status);
        }

        [Test]
        public async Task ConnectionFailure_CleansUpAndReturnsToOffline()
        {
            var gateway = new FakeGateway { HostError = new InvalidOperationException("relay unavailable") };
            using var flow = new OnlineSessionFlow(gateway);

            Assert.False(await flow.HostAsync());
            Assert.AreEqual(1, gateway.LeaveCount);
            Assert.AreEqual(OnlineMode.Offline, flow.Mode);
            Assert.AreEqual(OnlineConnectionState.Error, flow.State);
        }

        [Test]
        public async Task Disconnect_ConnectedSession_LeavesAndRestoresOfflineState()
        {
            var gateway = new FakeGateway();
            using var flow = new OnlineSessionFlow(gateway);
            Assert.True(await flow.HostAsync());

            await flow.DisconnectAsync();

            Assert.AreEqual(1, gateway.LeaveCount);
            Assert.AreEqual(OnlineMode.Offline, flow.Mode);
            Assert.AreEqual(OnlineConnectionState.Offline, flow.State);
            Assert.IsEmpty(flow.JoinCode);
        }

        [Test]
        public async Task UnexpectedDisconnect_ConnectedSession_RestoresOfflineErrorState()
        {
            var gateway = new FakeGateway();
            using var flow = new OnlineSessionFlow(gateway);
            Assert.True(await flow.JoinAsync("ABC123"));

            gateway.RaiseDisconnected("Host closed the session");

            Assert.AreEqual(OnlineMode.Offline, flow.Mode);
            Assert.AreEqual(OnlineConnectionState.Error, flow.State);
            StringAssert.Contains("Host closed", flow.Status);
        }

        [Test]
        public async Task Cleanup_RepeatedNotifications_LeavesOnceAndCanHostAndJoinAgain()
        {
            var gateway = new FakeGateway { LeaveCompletion = new TaskCompletionSource<bool>() };
            int stops = 0;
            using var flow = new OnlineSessionFlow(gateway, () => { stops++; return Task.CompletedTask; });
            Assert.True(await flow.JoinAsync("OLD123"));
            Task first = flow.HandleDisconnectAsync("Host disconnected");
            Task second = flow.DisconnectAsync();
            Assert.AreSame(first, second);
            Assert.IsEmpty(flow.JoinCode);
            Assert.True(flow.IsBusy);
            Assert.False(await flow.HostAsync());
            gateway.LeaveCompletion.SetResult(true);
            await first;
            Assert.AreEqual(1, gateway.LeaveCount);
            Assert.AreEqual(1, stops);
            Assert.AreEqual(OnlineMode.Offline, flow.Mode);
            await flow.DisconnectAsync();
            Assert.AreEqual(1, gateway.LeaveCount);
            Assert.True(await flow.HostAsync());
            await flow.DisconnectAsync();
            Assert.True(await flow.JoinAsync("NEW123"));
            Assert.AreEqual("NEW123", flow.JoinCode);
        }

        [Test]
        public async Task Disconnect_InFlightCreate_WaitsBeforeLeaveAndCannotRestoreOldSession()
        {
            var gateway = new FakeGateway { HostCompletion = new TaskCompletionSource<OnlineSessionInfo>() };
            using var flow = new OnlineSessionFlow(gateway);
            Task<bool> host = flow.HostAsync();
            Task cleanup = flow.DisconnectAsync();
            Assert.False(cleanup.IsCompleted);
            Assert.Zero(gateway.LeaveCount);
            Assert.False(await flow.JoinAsync("NEW123"));
            gateway.HostCompletion.SetResult(new OnlineSessionInfo("old", "OLD123"));
            Assert.False(await host);
            await cleanup;
            Assert.AreEqual(1, gateway.LeaveCount);
            Assert.AreEqual(OnlineMode.Offline, flow.Mode);
            Assert.IsEmpty(flow.JoinCode);
            Assert.True(await flow.JoinAsync("NEW123"));
        }

        [Test]
        public async Task Cleanup_LeaveFailure_StillStopsNetworkAndClearsCode()
        {
            var gateway = new FakeGateway { LeaveError = new InvalidOperationException("leave failed") };
            bool stopped = false;
            using var flow = new OnlineSessionFlow(gateway, () => { stopped = true; return Task.CompletedTask; });
            await flow.HostAsync();
            await flow.DisconnectAsync();
            Assert.True(stopped);
            Assert.IsEmpty(flow.JoinCode);
            Assert.AreEqual(OnlineMode.Offline, flow.Mode);
            StringAssert.Contains("leave failed", flow.Status);
        }

        [Test]
        public void Ownership_OnlySpawnedOwnerCanDriveLocalAvatar()
        {
            Assert.True(OnlineNetworkPlayer.IsLocalInputAllowed(true, true));
            Assert.False(OnlineNetworkPlayer.IsLocalInputAllowed(true, false));
            Assert.False(OnlineNetworkPlayer.IsLocalInputAllowed(false, true));
        }

        [Test]
        public void Mode_OnlyOfflineEnablesCpu()
        {
            Assert.True(OnlineSessionController.CpuShouldBeEnabled(OnlineMode.Offline));
            Assert.False(OnlineSessionController.CpuShouldBeEnabled(OnlineMode.Host));
            Assert.False(OnlineSessionController.CpuShouldBeEnabled(OnlineMode.Client));
        }

        [Test]
        public void TeamAssignment_ServerOwnsTeamA_OtherClientOwnsTeamB()
        {
            Assert.AreEqual(TeamId.Human, OnlineGameplayRules.TeamForClient(0, 0));
            Assert.AreEqual(TeamId.Cpu, OnlineGameplayRules.TeamForClient(1, 0));
        }

        [Test]
        public void OwnershipValidation_RejectsAnotherConnectionsRequest()
        {
            Assert.True(OnlineGameplayRules.IsOwnedRequest(5, 5));
            Assert.False(OnlineGameplayRules.IsOwnedRequest(5, 9));
        }

        [Test]
        public void BallNetworkState_Equality_IncludesPhysicsAndServeState()
        {
            var first = new BallNetworkState
            {
                Position = new UnityEngine.Vector3(1, 2, 3),
                Rotation = UnityEngine.Quaternion.identity,
                Velocity = new UnityEngine.Vector3(4, 5, 6),
                IsLive = true,
                IsKinematic = false,
                HitSerial = 7
            };
            var same = first;
            var changed = first;
            changed.IsLive = false;
            Assert.True(first.Equals(same));
            Assert.False(first.Equals(changed));
        }

        [Test]
        public void ActionNetworkState_PreservesMobileAimDirection()
        {
            UnityEngine.Vector2 localAim = MobileInputMath.GameplayAim(
                new UnityEngine.Vector2(100,100), new UnityEngine.Vector2(300,180), 24, 140, TeamId.Cpu);
            var sent = new ActionNetworkState { Aim = localAim };
            using var writer = new FastBufferWriter(128, Allocator.Temp);
            writer.WriteNetworkSerializable(sent);
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            reader.ReadNetworkSerializable(out ActionNetworkState received);
            Assert.AreEqual(localAim, received.Aim);
        }

        [Test]
        public void NetworkActionPhases_AllowOnlyValidAttackLifecycle()
        {
            Assert.True(OnlineGameplayRules.IsActionPhaseAllowed(ActionType.Receive, NetworkActionPhase.Performed));
            Assert.True(OnlineGameplayRules.IsActionPhaseAllowed(ActionType.Receive, NetworkActionPhase.Started));
            Assert.True(OnlineGameplayRules.IsActionPhaseAllowed(ActionType.Attack, NetworkActionPhase.Started));
            Assert.True(OnlineGameplayRules.IsActionPhaseAllowed(ActionType.Attack, NetworkActionPhase.Released));
            Assert.False(OnlineGameplayRules.IsActionPhaseAllowed(ActionType.Attack, NetworkActionPhase.Performed));
        }

        [Test]
        public void MatchSnapshot_ClientRulesUseAuthoritativeScoresAndState()
        {
            var rules = new MatchRules(3);
            rules.Synchronize(new MatchNetworkState
            {
                State = MatchState.PointFinished,
                Server = TeamId.Cpu,
                LastTouch = TeamId.Human,
                TouchCount = 2,
                TeamAScore = 1,
                TeamBScore = 2,
                TargetScore = 5
            });
            Assert.AreEqual(MatchState.PointFinished, rules.State);
            Assert.AreEqual(TeamId.Cpu, rules.Server);
            Assert.AreEqual(1, rules.HumanScore);
            Assert.AreEqual(2, rules.CpuScore);
            Assert.AreEqual(5, rules.TargetScore);
        }

        [Test]
        public void OnlineRestart_OnlyHostServerCanAuthorize()
        {
            Assert.True(OnlineGameplayRules.CanRestart(true, true));
            Assert.False(OnlineGameplayRules.CanRestart(false, true));
            Assert.False(OnlineGameplayRules.CanRestart(true, false));
        }

        sealed class FakeGateway : IOnlineSessionGateway
        {
            public event Action<string> Disconnected;
            public readonly System.Collections.Generic.List<string> Calls = new();
            public Exception AuthenticationError;
            public Exception HostError;
            public string JoinCode;
            public int LeaveCount;
            public TaskCompletionSource<bool> LeaveCompletion;
            public Exception LeaveError;
            public TaskCompletionSource<OnlineSessionInfo> HostCompletion;

            public Task AuthenticateAsync()
            {
                Calls.Add("authenticate");
                return AuthenticationError == null ? Task.CompletedTask : Task.FromException(AuthenticationError);
            }

            public Task<OnlineSessionInfo> CreateHostSessionAsync()
            {
                Calls.Add("host");
                if (HostCompletion != null) return HostCompletion.Task;
                return HostError == null
                    ? Task.FromResult(new OnlineSessionInfo("session", "ABC123"))
                    : Task.FromException<OnlineSessionInfo>(HostError);
            }

            public Task<OnlineSessionInfo> JoinSessionAsync(string joinCode)
            {
                Calls.Add("join");
                JoinCode = joinCode;
                return Task.FromResult(new OnlineSessionInfo("session", joinCode));
            }

            public Task LeaveAsync()
            {
                LeaveCount++;
                return LeaveError != null ? Task.FromException(LeaveError) : LeaveCompletion?.Task ?? Task.CompletedTask;
            }

            public void RaiseDisconnected(string reason)
            {
                Disconnected?.Invoke(reason);
            }
        }
    }
}
