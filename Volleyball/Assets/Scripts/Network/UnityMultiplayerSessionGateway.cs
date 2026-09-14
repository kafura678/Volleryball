using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;

namespace Volleyball
{
    public sealed class UnityMultiplayerSessionGateway : IOnlineSessionGateway, IDisposable
    {
        ISession activeSession;
        bool leaving;
        bool disconnectReported;

        public event Action<string> Disconnected;

        public async Task AuthenticateAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }

        public async Task<OnlineSessionInfo> CreateHostSessionAsync()
        {
            EnsureNoActiveSession();
            var options = new SessionOptions
            {
                Name = "Volleyball 1v1",
                MaxPlayers = 2,
                IsPrivate = true
            }.WithRelayNetwork();

            Attach(await Unity.Services.Multiplayer.MultiplayerService.Instance.CreateSessionAsync(options));
            return new OnlineSessionInfo(activeSession.Id, activeSession.Code);
        }

        public async Task<OnlineSessionInfo> JoinSessionAsync(string joinCode)
        {
            EnsureNoActiveSession();
            string normalized = OnlineSessionFlow.NormalizeJoinCode(joinCode);
            if (string.IsNullOrEmpty(normalized)) throw new ArgumentException("Join Code is required", nameof(joinCode));
            Attach(await Unity.Services.Multiplayer.MultiplayerService.Instance.JoinSessionByCodeAsync(normalized));
            return new OnlineSessionInfo(activeSession.Id, activeSession.Code);
        }

        public async Task LeaveAsync()
        {
            ISession session = activeSession;
            if (session == null) return;
            leaving = true;
            Detach(session);
            activeSession = null;
            try
            {
                await session.LeaveAsync();
            }
            finally
            {
                leaving = false;
            }
        }

        void EnsureNoActiveSession()
        {
            if (activeSession != null) throw new InvalidOperationException("An online session is already active");
            disconnectReported = false;
        }

        void Attach(ISession session)
        {
            activeSession = session ?? throw new InvalidOperationException("Multiplayer Services returned no session");
            session.StateChanged += OnSessionStateChanged;
            session.RemovedFromSession += OnRemovedFromSession;
            session.Deleted += OnSessionDeleted;
            session.Network.StateChanged += OnNetworkStateChanged;
            session.Network.StartFailed += OnNetworkStartFailed;
            session.Network.StopFailed += OnNetworkStopFailed;
        }

        void Detach(ISession session)
        {
            session.StateChanged -= OnSessionStateChanged;
            session.RemovedFromSession -= OnRemovedFromSession;
            session.Deleted -= OnSessionDeleted;
            session.Network.StateChanged -= OnNetworkStateChanged;
            session.Network.StartFailed -= OnNetworkStartFailed;
            session.Network.StopFailed -= OnNetworkStopFailed;
        }

        void OnSessionStateChanged(SessionState state)
        {
            if (state == SessionState.Disconnected || state == SessionState.Deleted)
                ReportDisconnect("Session disconnected");
        }

        void OnNetworkStateChanged(NetworkState state)
        {
            if (state == NetworkState.Stopped) ReportDisconnect("Network connection stopped");
        }

        void OnNetworkStartFailed(SessionError error)
        {
            ReportDisconnect("Network start failed: " + error);
        }

        void OnNetworkStopFailed(SessionError error)
        {
            ReportDisconnect("Network stop failed: " + error);
        }

        void OnRemovedFromSession()
        {
            ReportDisconnect("Removed from session");
        }

        void OnSessionDeleted()
        {
            ReportDisconnect("Host closed the session");
        }

        void ReportDisconnect(string reason)
        {
            if (leaving || disconnectReported) return;
            disconnectReported = true;
            if (activeSession != null)
            {
                Detach(activeSession);
                activeSession = null;
            }
            Disconnected?.Invoke(reason);
        }

        public void Dispose()
        {
            if (activeSession != null) Detach(activeSession);
            activeSession = null;
        }
    }
}
