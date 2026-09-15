using Unity.Netcode;
using UnityEngine;

namespace Volleyball
{
    [RequireComponent(typeof(NetworkObject), typeof(OwnerNetworkTransform))]
    public sealed class OnlineNetworkPlayer : NetworkBehaviour, IVolleyballCommandSink
    {
        const float InputSendInterval = 0.05f;

        [SerializeField] Renderer[] visuals;
        readonly NetworkVariable<TeamId> team = new NetworkVariable<TeamId>(
            TeamId.Human, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        readonly NetworkVariable<ActionNetworkState> actionState = new NetworkVariable<ActionNetworkState>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        OnlineSessionController sessionController;
        VolleyballCharacterController localAvatar;
        VolleyballCharacterController authoritativeAvatar;
        Vector3 pendingMove;
        Vector2 pendingAim;
        float nextInputSendTime;
        float nextActionStateTime;

        public TeamId Team => team.Value;
        public ServeStage CurrentServeStage => actionState.Value.ServeStage;
        public AttackStage CurrentAttackStage => actionState.Value.AttackStage;
        public float CurrentAttackCharge => actionState.Value.Charge;
        public bool CanDriveLocalAvatar => IsLocalInputAllowed(IsSpawned, IsOwner);

        public override void OnNetworkSpawn()
        {
            sessionController = FindFirstObjectByType<OnlineSessionController>();
            if (IsServer)
            {
                team.Value = OnlineGameplayRules.TeamForClient(OwnerClientId, NetworkManager.ServerClientId);
                authoritativeAvatar = sessionController
                    ? sessionController.GetAuthoritativeAvatar(team.Value)
                    : null;
                if (authoritativeAvatar && authoritativeAvatar.Actions)
                    authoritativeAvatar.Actions.UsePlayerServeControls = true;
                if (authoritativeAvatar)
                    transform.SetPositionAndRotation(authoritativeAvatar.transform.position, authoritativeAvatar.transform.rotation);
            }

            SetVisuals(!IsServer && !IsOwner);
            if (!CanDriveLocalAvatar || !sessionController) return;
            localAvatar = sessionController.RegisterLocalNetworkPlayer(this);
        }

        void Update()
        {
            if (!CanDriveLocalAvatar || IsServer || Time.unscaledTime < nextInputSendTime) return;
            nextInputSendTime = Time.unscaledTime + InputSendInterval;
            SubmitInputRpc(pendingMove, pendingAim);
        }

        void LateUpdate()
        {
            if (!IsSpawned) return;
            if (IsServer && authoritativeAvatar)
            {
                transform.SetPositionAndRotation(
                    authoritativeAvatar.transform.position,
                    authoritativeAvatar.transform.rotation);
                if (Time.unscaledTime >= nextActionStateTime)
                {
                    nextActionStateTime = Time.unscaledTime + InputSendInterval;
                    actionState.Value = new ActionNetworkState
                    {
                        ServeStage = authoritativeAvatar.Actions.CurrentServeStage,
                        AttackStage = authoritativeAvatar.Actions.CurrentAttackStage,
                        Aim = authoritativeAvatar.AimInput,
                        Charge = authoritativeAvatar.Actions.ChargeAmount
                    };
                }
            }
            else if (IsOwner && localAvatar && localAvatar.Motor)
            {
                localAvatar.Motor.ApplyNetworkPose(transform.position, transform.rotation);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner && sessionController) sessionController.UnregisterLocalNetworkPlayer(this);
            if (IsServer && authoritativeAvatar && authoritativeAvatar.Actions && team.Value == TeamId.Cpu)
                authoritativeAvatar.Actions.UsePlayerServeControls = false;
            localAvatar = null;
            authoritativeAvatar = null;
        }

        public void Move(Vector3 direction)
        {
            pendingMove = Vector3.ClampMagnitude(new Vector3(direction.x, 0f, direction.z), 1f);
            if (IsServer) ApplyInput(pendingMove, pendingAim);
        }

        public void Aim(Vector2 input)
        {
            pendingAim = Vector2.ClampMagnitude(input, 1f);
            if (IsServer) ApplyInput(pendingMove, pendingAim);
        }

        public bool RequestAction(ActionType action)
        {
            if (!CanDriveLocalAvatar || !OnlineGameplayRules.IsActionPhaseAllowed(action, NetworkActionPhase.Performed)) return false;
            if (IsServer) return ApplyAction(action, NetworkActionPhase.Performed, pendingAim);
            RequestGameplayActionRpc(action, NetworkActionPhase.Performed, pendingAim);
            return true;
        }

        public bool AttackStarted(Vector2 aimDirection)
        {
            if (!CanDriveLocalAvatar) return false;
            Aim(aimDirection);
            if (IsServer) return ApplyAction(ActionType.Attack, NetworkActionPhase.Started, pendingAim);
            RequestGameplayActionRpc(ActionType.Attack, NetworkActionPhase.Started, pendingAim);
            return true;
        }
        public void AttackHeld(Vector2 aimDirection) => Aim(aimDirection);
        public bool AttackReleased(Vector2 aimDirection)
        {
            if (!CanDriveLocalAvatar) return false;
            Aim(aimDirection);
            if (IsServer) return ApplyAction(ActionType.Attack, NetworkActionPhase.Released, pendingAim);
            RequestGameplayActionRpc(ActionType.Attack, NetworkActionPhase.Released, pendingAim);
            return true;
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
        void SubmitInputRpc(Vector3 move, Vector2 aim, RpcParams rpcParams = default)
        {
            if (!OnlineGameplayRules.IsOwnedRequest(OwnerClientId, rpcParams.Receive.SenderClientId)) return;
            ApplyInput(
                Vector3.ClampMagnitude(new Vector3(move.x, 0f, move.z), 1f),
                Vector2.ClampMagnitude(aim, 1f));
        }

        [Rpc(SendTo.Server)]
        void RequestGameplayActionRpc(
            ActionType action,
            NetworkActionPhase phase,
            Vector2 aim,
            RpcParams rpcParams = default)
        {
            if (!OnlineGameplayRules.IsOwnedRequest(OwnerClientId, rpcParams.Receive.SenderClientId) ||
                !OnlineGameplayRules.IsActionPhaseAllowed(action, phase)) return;
            ApplyAction(action, phase, Vector2.ClampMagnitude(aim, 1f));
        }

        void ApplyInput(Vector3 move, Vector2 aim)
        {
            if (!IsServer || !authoritativeAvatar) return;
            authoritativeAvatar.ApplyMove(move);
            authoritativeAvatar.ApplyAim(aim);
            if (authoritativeAvatar.Actions && authoritativeAvatar.Actions.CurrentAttackStage == AttackStage.Charging)
                authoritativeAvatar.ApplyAttackHeld(aim);
        }

        bool ApplyAction(ActionType action, NetworkActionPhase phase, Vector2 aim)
        {
            if (!IsServer || !authoritativeAvatar) return false;
            authoritativeAvatar.ApplyAim(aim);
            if (action != ActionType.Attack) return authoritativeAvatar.ApplyAction(action);
            return phase == NetworkActionPhase.Started
                ? authoritativeAvatar.ApplyAttackStarted(aim)
                : authoritativeAvatar.ApplyAttackReleased(aim);
        }

        void SetVisuals(bool visible)
        {
            if (visuals == null || visuals.Length == 0) visuals = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer visual in visuals)
            {
                if (visual) visual.enabled = visible;
            }
        }

        public static bool IsLocalInputAllowed(bool isSpawned, bool isOwner)
        {
            return isSpawned && isOwner;
        }
    }
}
