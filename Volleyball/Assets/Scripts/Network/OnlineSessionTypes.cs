using System;
using System.Threading.Tasks;

namespace Volleyball
{
    public enum OnlineMode
    {
        Offline,
        Host,
        Client
    }

    public enum OnlineConnectionState
    {
        Offline,
        Authenticating,
        Connecting,
        Connected,
        Disconnecting,
        Error
    }

    public readonly struct OnlineSessionInfo
    {
        public OnlineSessionInfo(string sessionId, string joinCode)
        {
            SessionId = sessionId ?? string.Empty;
            JoinCode = joinCode ?? string.Empty;
        }

        public string SessionId { get; }
        public string JoinCode { get; }
    }

    public interface IOnlineSessionGateway
    {
        event Action<string> Disconnected;
        Task AuthenticateAsync();
        Task<OnlineSessionInfo> CreateHostSessionAsync();
        Task<OnlineSessionInfo> JoinSessionAsync(string joinCode);
        Task LeaveAsync();
    }

    public sealed class OnlineSessionFlow : IDisposable
    {
        readonly IOnlineSessionGateway gateway;
        bool busy;

        public OnlineSessionFlow(IOnlineSessionGateway gateway)
        {
            this.gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            gateway.Disconnected += OnGatewayDisconnected;
        }

        public event Action Changed;
        public OnlineMode Mode { get; private set; } = OnlineMode.Offline;
        public OnlineConnectionState State { get; private set; } = OnlineConnectionState.Offline;
        public string Status { get; private set; } = "Offline CPU Match";
        public string JoinCode { get; private set; } = string.Empty;
        public bool IsBusy => busy;
        public bool IsOnline => Mode != OnlineMode.Offline;

        public Task<bool> HostAsync()
        {
            return ConnectAsync(OnlineMode.Host, null);
        }

        public Task<bool> JoinAsync(string joinCode)
        {
            string normalized = NormalizeJoinCode(joinCode);
            if (string.IsNullOrEmpty(normalized))
            {
                Set(OnlineMode.Offline, OnlineConnectionState.Error, "Enter a Join Code", string.Empty);
                return Task.FromResult(false);
            }

            return ConnectAsync(OnlineMode.Client, normalized);
        }

        async Task<bool> ConnectAsync(OnlineMode requestedMode, string joinCode)
        {
            if (busy || State == OnlineConnectionState.Connected) return false;
            busy = true;
            Set(requestedMode, OnlineConnectionState.Authenticating, "Signing in anonymously...", string.Empty);
            try
            {
                await gateway.AuthenticateAsync();
                Set(requestedMode, OnlineConnectionState.Connecting,
                    requestedMode == OnlineMode.Host ? "Creating Relay session..." : "Joining Relay session...",
                    string.Empty);
                OnlineSessionInfo session = requestedMode == OnlineMode.Host
                    ? await gateway.CreateHostSessionAsync()
                    : await gateway.JoinSessionAsync(joinCode);
                Set(requestedMode, OnlineConnectionState.Connected,
                    requestedMode == OnlineMode.Host ? "Hosting - share the Join Code" : "Connected to Host",
                    session.JoinCode);
                return true;
            }
            catch (Exception exception)
            {
                await SafeLeaveAsync();
                Set(OnlineMode.Offline, OnlineConnectionState.Error,
                    "Connection failed: " + FriendlyMessage(exception), string.Empty);
                return false;
            }
            finally
            {
                busy = false;
                Changed?.Invoke();
            }
        }

        public async Task DisconnectAsync()
        {
            if (busy) return;
            busy = true;
            Set(Mode, OnlineConnectionState.Disconnecting, "Disconnecting...", JoinCode);
            try
            {
                await gateway.LeaveAsync();
                Set(OnlineMode.Offline, OnlineConnectionState.Offline, "Offline CPU Match", string.Empty);
            }
            catch (Exception exception)
            {
                Set(OnlineMode.Offline, OnlineConnectionState.Error,
                    "Disconnected with cleanup error: " + FriendlyMessage(exception), string.Empty);
            }
            finally
            {
                busy = false;
                Changed?.Invoke();
            }
        }

        async Task SafeLeaveAsync()
        {
            try
            {
                await gateway.LeaveAsync();
            }
            catch
            {
                // The original connection error is more useful than a secondary cleanup error.
            }
        }

        void OnGatewayDisconnected(string reason)
        {
            if (State == OnlineConnectionState.Disconnecting || Mode == OnlineMode.Offline) return;
            busy = false;
            string message = string.IsNullOrWhiteSpace(reason) ? "Connection closed" : reason;
            Set(OnlineMode.Offline, OnlineConnectionState.Error, message, string.Empty);
        }

        void Set(OnlineMode mode, OnlineConnectionState state, string status, string joinCode)
        {
            Mode = mode;
            State = state;
            Status = status;
            JoinCode = joinCode;
            Changed?.Invoke();
        }

        public static string NormalizeJoinCode(string joinCode)
        {
            return string.IsNullOrWhiteSpace(joinCode) ? string.Empty : joinCode.Trim().ToUpperInvariant();
        }

        static string FriendlyMessage(Exception exception)
        {
            return string.IsNullOrWhiteSpace(exception.Message) ? exception.GetType().Name : exception.Message;
        }

        public void Dispose()
        {
            gateway.Disconnected -= OnGatewayDisconnected;
        }
    }
}
