using Unity.Netcode;
using UnityEngine;

namespace Volleyball
{
    [RequireComponent(typeof(NetworkObject), typeof(OwnerNetworkTransform))]
    public sealed class OnlineNetworkPlayer : NetworkBehaviour
    {
        [SerializeField] Renderer[] visuals;
        OnlineSessionController sessionController;
        Transform localAvatar;

        public bool CanDriveLocalAvatar => IsLocalInputAllowed(IsSpawned, IsOwner);

        public override void OnNetworkSpawn()
        {
            sessionController = FindFirstObjectByType<OnlineSessionController>();
            SetVisuals(!IsOwner);
            if (!CanDriveLocalAvatar || !sessionController) return;
            localAvatar = sessionController.RegisterLocalNetworkPlayer(this);
            if (localAvatar)
            {
                transform.SetPositionAndRotation(localAvatar.position, localAvatar.rotation);
            }
        }

        void LateUpdate()
        {
            if (!CanDriveLocalAvatar || !localAvatar) return;
            transform.SetPositionAndRotation(localAvatar.position, localAvatar.rotation);
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner && sessionController) sessionController.UnregisterLocalNetworkPlayer(this);
            localAvatar = null;
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
