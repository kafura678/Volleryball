using Unity.Netcode;
using UnityEngine;

namespace Volleyball
{
    [DefaultExecutionOrder(-10000)]
    [RequireComponent(typeof(NetworkManager))]
    public sealed class NetworkManagerGuard : MonoBehaviour
    {
        void Awake()
        {
            NetworkManager candidate = GetComponent<NetworkManager>();
            if (IsDuplicate(candidate, NetworkManager.Singleton))
            {
                Destroy(gameObject);
            }
        }

        public static bool IsDuplicate(NetworkManager candidate, NetworkManager singleton)
        {
            return candidate != null && singleton != null && singleton != candidate;
        }
    }
}
