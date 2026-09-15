namespace Volleyball
{
    public enum NetworkActionPhase : byte { Performed, Started, Released }

    public static class OnlineGameplayRules
    {
        public static TeamId TeamForClient(ulong clientId, ulong serverClientId)
        {
            return clientId == serverClientId ? TeamId.Human : TeamId.Cpu;
        }

        public static bool IsOwnedRequest(ulong ownerClientId, ulong senderClientId)
        {
            return ownerClientId == senderClientId;
        }

        public static bool IsActionPhaseAllowed(ActionType action, NetworkActionPhase phase)
        {
            return action == ActionType.Attack
                ? phase == NetworkActionPhase.Started || phase == NetworkActionPhase.Released
                : phase == NetworkActionPhase.Performed;
        }

        public static bool CanRestart(bool isServer, bool isHost)
        {
            return isServer && isHost;
        }
    }
}
