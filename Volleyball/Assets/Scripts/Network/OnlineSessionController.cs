using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Volleyball
{
    public sealed class OnlineSessionController : MonoBehaviour
    {
        public NetworkManager NetworkManager;
        public MatchController Match;
        public CpuController CpuController;
        public PlayerInputController DesktopInput;
        public MobileInputController MobileInput;
        public OnlineMatchSynchronizer MatchSynchronizerPrefab;

        OnlineSessionFlow flow;
        IOnlineSessionGateway gateway;
        OnlineNetworkPlayer localNetworkPlayer;
        OnlineMatchSynchronizer activeMatchSynchronizer;
        string matchStatusOverride;
        TeamId originalHumanTeam = TeamId.Human;
        bool hasOriginalTeam;

        public static OnlineSessionController Instance { get; private set; }
        public event Action Changed;
        public OnlineMode Mode => flow?.Mode ?? OnlineMode.Offline;
        public OnlineConnectionState State => flow?.State ?? OnlineConnectionState.Offline;
        public string Status => State != OnlineConnectionState.Connected || string.IsNullOrEmpty(matchStatusOverride)
            ? flow?.Status ?? "Offline CPU Match"
            : matchStatusOverride;
        public string JoinCode => flow?.JoinCode ?? string.Empty;
        public bool IsBusy => flow != null && flow.IsBusy;
        public bool HasLocalNetworkPlayer => localNetworkPlayer != null;

        void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            RebindSceneReferences();
            Initialize(new UnityMultiplayerSessionGateway());
            SceneManager.sceneLoaded += OnSceneLoaded;
            SubscribeNetworkEvents();
        }

        void Initialize(IOnlineSessionGateway serviceGateway)
        {
            if (flow != null)
            {
                flow.Changed -= OnFlowChanged;
                flow.Dispose();
            }
            if (gateway is IDisposable disposable) disposable.Dispose();
            gateway = serviceGateway;
            flow = new OnlineSessionFlow(gateway, StopNetworkAsync);
            flow.Changed += OnFlowChanged;
            ApplyMode();
        }

        public async Task<bool> HostMatchAsync()
        {
            EnsureActiveAndInitialized();
            return await flow.HostAsync();
        }

        public async Task<bool> JoinMatchAsync(string joinCode)
        {
            EnsureActiveAndInitialized();
            return await flow.JoinAsync(joinCode);
        }

        public async Task DisconnectAsync()
        {
            EnsureActiveAndInitialized();
            await flow.DisconnectAsync();
        }

        void EnsureActiveAndInitialized()
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            if (flow == null) Initialize(new UnityMultiplayerSessionGateway());
            if (NetworkManager)
                ConfigureWebTransport(NetworkManager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>(), WebPlatformPolicy.IsWeb);
        }

        public static void ConfigureWebTransport(Unity.Netcode.Transports.UTP.UnityTransport transport, bool web)
        {
            if (transport && web)
            {
                transport.UseWebSockets = true;
                if (Debug.isDebugBuild) Debug.Log("Online Web transport: WSS / WebSocket");
            }
        }

        void OnFlowChanged()
        {
            ApplyMode();
            Changed?.Invoke();
        }

        void ApplyMode()
        {
            RebindSceneReferences();
            if (State == OnlineConnectionState.Disconnecting)
            {
                SetLocalInputEnabled(false);
                return;
            }
            bool online = Mode != OnlineMode.Offline;
            SetCpuAvailable(!online);

            if (!online)
            {
                matchStatusOverride = string.Empty;
                localNetworkPlayer = null;
                RestoreOfflinePlayer();
                SetLocalInputEnabled(true);
                return;
            }

            ConfigureLocalOnlineSide();
            SetLocalInputEnabled(localNetworkPlayer != null);
        }

        void SetCpuAvailable(bool available)
        {
            if (!CpuController && Match && Match.Cpu)
                CpuController = Match.Cpu.GetComponent<CpuController>();
            if (CpuController) CpuController.enabled = available;
            if (Match && Match.Cpu)
            {
                bool keepAuthoritativeRemotePlayer = !available && NetworkManager && NetworkManager.IsServer;
                Match.Cpu.gameObject.SetActive(available || keepAuthoritativeRemotePlayer);
            }
        }

        void ConfigureLocalOnlineSide(TeamId? assignedTeam = null)
        {
            if (!Match || !Match.Human || Match.Human.Motor == null) return;
            if (!hasOriginalTeam)
            {
                originalHumanTeam = Match.Human.Motor.Team;
                hasOriginalTeam = true;
            }

            TeamId side = assignedTeam ?? (Mode == OnlineMode.Client ? TeamId.Cpu : TeamId.Human);
            Match.Human.Motor.Team = side;
            if (Match.Court) Match.Human.ResetForRally(Match.Court.Spawn(side));
        }

        void RestoreOfflinePlayer()
        {
            activeMatchSynchronizer = null;
            if (Match)
            {
                Match.SetNetworkAuthority(true);
                if (Match.Ball) Match.Ball.SetSimulationAuthority(true);
                if (Match.Cpu) Match.Cpu.Actions.UsePlayerServeControls = false;
            }
            if (!Match || !Match.Human || Match.Human.Motor == null) return;
            Match.Human.CommandSink = null;
            Match.Human.Motor.Team = hasOriginalTeam ? originalHumanTeam : TeamId.Human;
            if (Match.Rules != null) Match.Restart();
        }

        void SetLocalInputEnabled(bool enabled)
        {
            if (DesktopInput) DesktopInput.enabled = enabled;
            if (MobileInput) MobileInput.enabled = enabled;
        }

        public VolleyballCharacterController RegisterLocalNetworkPlayer(OnlineNetworkPlayer player)
        {
            if (!player || !player.IsOwner) return null;
            localNetworkPlayer = player;
            ConfigureLocalOnlineSide(player.Team);
            if (Match && Match.Human) Match.Human.CommandSink = player;
            SetLocalInputEnabled(true);
            Changed?.Invoke();
            return Match && Match.Human ? Match.Human : null;
        }

        public void UnregisterLocalNetworkPlayer(OnlineNetworkPlayer player)
        {
            if (localNetworkPlayer != player) return;
            if (Match && Match.Human && ReferenceEquals(Match.Human.CommandSink, player)) Match.Human.CommandSink = null;
            localNetworkPlayer = null;
            if (Mode != OnlineMode.Offline) SetLocalInputEnabled(false);
            Changed?.Invoke();
        }

        public VolleyballCharacterController GetAuthoritativeAvatar(TeamId team)
        {
            if (!Match) return null;
            return team == TeamId.Human ? Match.Human : Match.Cpu;
        }

        public void NotifyMatchStatus(string status)
        {
            matchStatusOverride = status ?? string.Empty;
            Changed?.Invoke();
        }

        void SubscribeNetworkEvents()
        {
            if (!NetworkManager) return;
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
            NetworkManager.OnServerStopped += OnServerStopped;
            NetworkManager.OnClientStopped += OnClientStopped;
        }

        void UnsubscribeNetworkEvents()
        {
            if (!NetworkManager) return;
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.OnServerStopped -= OnServerStopped;
            NetworkManager.OnClientStopped -= OnClientStopped;
        }

        void OnClientConnected(ulong clientId)
        {
            EnsureMatchSynchronizer();
            Changed?.Invoke();
        }

        void EnsureMatchSynchronizer()
        {
            if (!NetworkManager || !NetworkManager.IsServer || activeMatchSynchronizer || !MatchSynchronizerPrefab) return;
            activeMatchSynchronizer = Instantiate(MatchSynchronizerPrefab);
            activeMatchSynchronizer.GetComponent<NetworkObject>().Spawn();
        }

        void OnClientDisconnected(ulong clientId)
        {
            if (Mode == OnlineMode.Client && NetworkManager && clientId == NetworkManager.LocalClientId)
                _ = flow.HandleDisconnectAsync("Host disconnected");
            Changed?.Invoke();
        }

        void OnServerStopped(bool wasClient)
        {
            if (Mode != OnlineMode.Offline) _ = flow.HandleDisconnectAsync("Network server stopped");
            Changed?.Invoke();
        }

        void OnClientStopped(bool wasServer)
        {
            if (!wasServer && Mode != OnlineMode.Offline) _ = flow.HandleDisconnectAsync("Host disconnected");
            Changed?.Invoke();
        }

        async Task StopNetworkAsync()
        {
            if (NetworkManager)
            {
                if (NetworkManager.IsListening && !NetworkManager.ShutdownInProgress)
                    NetworkManager.Shutdown();
                while (NetworkManager && (NetworkManager.IsListening || NetworkManager.ShutdownInProgress))
                    await Task.Yield();
            }
            activeMatchSynchronizer = null;
            localNetworkPlayer = null;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RebindSceneReferences();
            ApplyMode();
        }

        void RebindSceneReferences()
        {
            if (!Match) Match = FindFirstObjectByType<MatchController>();
            if (Match)
            {
                if (!DesktopInput && Match.Human) DesktopInput = Match.Human.GetComponent<PlayerInputController>();
                if (!CpuController && Match.Cpu) CpuController = Match.Cpu.GetComponent<CpuController>();
            }
            if (!MobileInput) MobileInput = FindFirstObjectByType<MobileInputController>();
        }

        public int ConnectedPlayerCount => Mode != OnlineMode.Offline && State != OnlineConnectionState.Disconnecting && NetworkManager && NetworkManager.IsListening
            ? NetworkManager.ConnectedClientsIds.Count
            : 0;

        public static bool CpuShouldBeEnabled(OnlineMode mode)
        {
            return mode == OnlineMode.Offline;
        }

        public void InitializeForTests(IOnlineSessionGateway serviceGateway)
        {
            Initialize(serviceGateway);
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnsubscribeNetworkEvents();
            if (flow != null)
            {
                flow.Changed -= OnFlowChanged;
                flow.Dispose();
            }
            if (gateway is IDisposable disposable) disposable.Dispose();
            if (Instance == this) Instance = null;
        }
    }
}
