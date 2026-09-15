using Unity.Netcode;
using UnityEngine;

namespace Volleyball
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class OnlineMatchSynchronizer : NetworkBehaviour
    {
        const float SnapshotInterval = 0.05f;
        const float ClientInterpolation = 18f;

        readonly NetworkVariable<BallNetworkState> ballState = new NetworkVariable<BallNetworkState>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        readonly NetworkVariable<MatchNetworkState> matchState = new NetworkVariable<MatchNetworkState>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        MatchController match;
        BallNetworkState targetBallState;
        float nextSnapshotTime;

        public override void OnNetworkSpawn()
        {
            match = FindFirstObjectByType<MatchController>();
            if (!match || !match.Ball) return;
            match.Ball.SetSimulationAuthority(IsServer);
            if (IsServer)
            {
                match.SetNetworkAuthority(true);
                match.Changed += OnAuthoritativeMatchChanged;
                NetworkManager.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
                match.PauseOnline("Waiting for opponent");
                PublishBallState();
                PublishMatchState();
                EvaluateConnectedPlayers();
            }
            else
            {
                match.SetNetworkAuthority(false);
                targetBallState = ballState.Value;
                match.Ball.ApplyNetworkState(targetBallState, true);
                ballState.OnValueChanged += OnBallStateChanged;
                matchState.OnValueChanged += OnMatchStateChanged;
                match.ApplyNetworkState(matchState.Value);
                OnlineSessionController.Instance?.NotifyMatchStatus("Online Match");
            }
        }

        void Update()
        {
            if (!IsSpawned || !match || !match.Ball) return;
            if (IsServer)
            {
                if (Time.unscaledTime < nextSnapshotTime) return;
                nextSnapshotTime = Time.unscaledTime + SnapshotInterval;
                PublishBallState();
                return;
            }

            match.Ball.ApplyNetworkState(targetBallState, false, ClientInterpolation);
        }

        void PublishBallState()
        {
            BallController ball = match.Ball;
            ballState.Value = new BallNetworkState
            {
                Position = ball.Body.position,
                Rotation = ball.Body.rotation,
                Velocity = ball.Velocity,
                IsLive = ball.IsLive,
                IsKinematic = ball.Body.isKinematic,
                HitSerial = ball.HitSerial
            };
        }

        void OnBallStateChanged(BallNetworkState previous, BallNetworkState current)
        {
            targetBallState = current;
        }

        void OnAuthoritativeMatchChanged()
        {
            PublishMatchState();
        }

        void PublishMatchState()
        {
            if (IsServer && match) matchState.Value = match.CaptureNetworkState();
        }

        void OnMatchStateChanged(MatchNetworkState previous, MatchNetworkState current)
        {
            if (match) match.ApplyNetworkState(current);
        }

        void OnClientConnected(ulong clientId)
        {
            EvaluateConnectedPlayers();
        }

        void OnClientDisconnected(ulong clientId)
        {
            if (!IsServer || clientId == NetworkManager.ServerClientId) return;
            match.PauseOnline("Opponent Disconnected");
            OnlineSessionController.Instance?.NotifyMatchStatus("Opponent Disconnected");
        }

        void EvaluateConnectedPlayers()
        {
            if (!IsServer || !match || NetworkManager.ConnectedClientsIds.Count < 2) return;
            match.BeginOnlineMatch();
            OnlineSessionController.Instance?.NotifyMatchStatus("Online Match");
        }

        public override void OnNetworkDespawn()
        {
            ballState.OnValueChanged -= OnBallStateChanged;
            matchState.OnValueChanged -= OnMatchStateChanged;
            if (IsServer && NetworkManager)
            {
                NetworkManager.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            }
            if (match)
            {
                match.Changed -= OnAuthoritativeMatchChanged;
                if (match.Ball) match.Ball.SetSimulationAuthority(true);
                match.SetNetworkAuthority(true);
            }
        }
    }
}
