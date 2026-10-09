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
        readonly Func<Task> stopNetwork;
        bool busy;
        Task cleanupTask;
        TaskCompletionSource<bool> connectionCompletion;
        int connectionVersion;

        public OnlineSessionFlow(IOnlineSessionGateway gateway, Func<Task> stopNetwork = null)
        {
            this.gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            this.stopNetwork = stopNetwork;
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
            if (busy || IsOnline) return Task.FromResult(false);
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
            int version = ++connectionVersion;
            var connecting = new TaskCompletionSource<bool>();
            connectionCompletion = connecting;
            Set(requestedMode, OnlineConnectionState.Authenticating, "Signing in anonymously...", string.Empty);
            try
            {
                await gateway.AuthenticateAsync();
                if (version != connectionVersion) return false;
                Set(requestedMode, OnlineConnectionState.Connecting,
                    requestedMode == OnlineMode.Host ? "Creating Relay session..." : "Joining Relay session...",
                    string.Empty);
                OnlineSessionInfo session = requestedMode == OnlineMode.Host
                    ? await gateway.CreateHostSessionAsync()
                    : await gateway.JoinSessionAsync(joinCode);
                if (version != connectionVersion)
                {
                    return false;
                }
                Set(requestedMode, OnlineConnectionState.Connected,
                    requestedMode == OnlineMode.Host ? "Hosting - share the Join Code" : "Connected to Host",
                    session.JoinCode);
                return true;
            }
            catch (Exception exception)
            {
                if (version != connectionVersion) return false;
                Set(requestedMode, OnlineConnectionState.Disconnecting, "Cleaning up failed connection...", string.Empty);
                await SafeLeaveAsync();
                Set(OnlineMode.Offline, OnlineConnectionState.Error,
                    "Connection failed: " + FriendlyMessage(exception), string.Empty);
                return false;
            }
            finally
            {
                if (ReferenceEquals(connectionCompletion, connecting)) connectionCompletion = null;
                connecting.TrySetResult(true);
                if (version == connectionVersion)
                {
                    busy = false;
                    Changed?.Invoke();
                }
            }
        }

        public Task DisconnectAsync()
        {
            return CleanupAsync(null);
        }

        public Task HandleDisconnectAsync(string reason)
        {
            if (State == OnlineConnectionState.Disconnecting && cleanupTask == null) return Task.CompletedTask;
            if (!IsOnline && cleanupTask == null) return Task.CompletedTask;
            return CleanupAsync(string.IsNullOrWhiteSpace(reason) ? "Connection closed" : reason);
        }

        Task CleanupAsync(string reason)
        {
            if (cleanupTask != null) return cleanupTask;
            if (!IsOnline && !busy) return Task.CompletedTask;
            // Publish the task before callbacks run, so repeated stop notifications share cleanup.
            var completion = new TaskCompletionSource<bool>();
            cleanupTask = completion.Task;
            ++connectionVersion;
            CompleteCleanupAsync(reason, completion);
            return completion.Task;
        }

        async void CompleteCleanupAsync(string reason, TaskCompletionSource<bool> completion)
        {
            busy = true;
            Set(Mode, OnlineConnectionState.Disconnecting, "Disconnecting...", string.Empty);
            Exception cleanupError = null;
            try
            {
                // Wait for an in-flight MPS create/join before releasing its session.
                if (connectionCompletion != null) await connectionCompletion.Task;
                await gateway.LeaveAsync();
            }
            catch (Exception exception)
            {
                cleanupError = exception;
            }
            finally
            {
                try
                {
                    if (stopNetwork != null) await stopNetwork();
                }
                catch (Exception exception)
                {
                    cleanupError ??= exception;
                }
                busy = false;
                cleanupTask = null;
                Set(OnlineMode.Offline,
                    cleanupError != null || reason != null ? OnlineConnectionState.Error : OnlineConnectionState.Offline,
                    cleanupError != null ? "Disconnected with cleanup error: " + FriendlyMessage(cleanupError)
                        : reason != null ? "Disconnected: " + reason : "Disconnected - Offline CPU Match",
                    string.Empty);
                completion.TrySetResult(true);
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
            finally
            {
                if (stopNetwork != null) await stopNetwork();
            }
        }

        void OnGatewayDisconnected(string reason)
        {
            if (State == OnlineConnectionState.Disconnecting || Mode == OnlineMode.Offline) return;
            _ = HandleDisconnectAsync(reason);
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
