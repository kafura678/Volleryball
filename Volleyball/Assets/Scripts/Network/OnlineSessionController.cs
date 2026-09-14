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

        OnlineSessionFlow flow;
        IOnlineSessionGateway gateway;
        OnlineNetworkPlayer localNetworkPlayer;
        TeamId originalHumanTeam = TeamId.Human;
        bool hasOriginalTeam;

        public static OnlineSessionController Instance { get; private set; }
        public event Action Changed;
        public OnlineMode Mode => flow?.Mode ?? OnlineMode.Offline;
        public OnlineConnectionState State => flow?.State ?? OnlineConnectionState.Offline;
        public string Status => flow?.Status ?? "Offline CPU Match";
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
            flow = new OnlineSessionFlow(gateway);
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
        }

        void OnFlowChanged()
        {
            ApplyMode();
            Changed?.Invoke();
        }

        void ApplyMode()
        {
            RebindSceneReferences();
            bool online = Mode != OnlineMode.Offline;
            SetCpuAvailable(!online);

            if (!online)
            {
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
            if (Match && Match.Cpu) Match.Cpu.gameObject.SetActive(available);
        }

        void ConfigureLocalOnlineSide()
        {
            if (!Match || !Match.Human || Match.Human.Motor == null) return;
            if (!hasOriginalTeam)
            {
                originalHumanTeam = Match.Human.Motor.Team;
                hasOriginalTeam = true;
            }

            TeamId side = Mode == OnlineMode.Client ? TeamId.Cpu : TeamId.Human;
            Match.Human.Motor.Team = side;
            if (Match.Court) Match.Human.ResetForRally(Match.Court.Spawn(side));
        }

        void RestoreOfflinePlayer()
        {
            if (!Match || !Match.Human || Match.Human.Motor == null) return;
            Match.Human.Motor.Team = hasOriginalTeam ? originalHumanTeam : TeamId.Human;
            if (Match.Rules != null) Match.Restart();
        }

        void SetLocalInputEnabled(bool enabled)
        {
            if (DesktopInput) DesktopInput.enabled = enabled;
            if (MobileInput) MobileInput.enabled = enabled;
        }

        public Transform RegisterLocalNetworkPlayer(OnlineNetworkPlayer player)
        {
            if (!player || !player.IsOwner) return null;
            localNetworkPlayer = player;
            ConfigureLocalOnlineSide();
            SetLocalInputEnabled(true);
            Changed?.Invoke();
            return Match && Match.Human ? Match.Human.transform : null;
        }

        public void UnregisterLocalNetworkPlayer(OnlineNetworkPlayer player)
        {
            if (localNetworkPlayer != player) return;
            localNetworkPlayer = null;
            if (Mode != OnlineMode.Offline) SetLocalInputEnabled(false);
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
            Changed?.Invoke();
        }

        void OnClientDisconnected(ulong clientId)
        {
            Changed?.Invoke();
        }

        void OnServerStopped(bool wasClient)
        {
            Changed?.Invoke();
        }

        void OnClientStopped(bool wasServer)
        {
            Changed?.Invoke();
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

        public int ConnectedPlayerCount => NetworkManager && NetworkManager.IsListening
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
