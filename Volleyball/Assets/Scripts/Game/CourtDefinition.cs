using UnityEngine;
namespace Volleyball
{
    public sealed class CourtDefinition : MonoBehaviour
    {
        public PrototypeSettings Settings;
        public static float Direction(TeamId side) => side == TeamId.Human ? 1 : -1;
        public Vector3 Clamp(Vector3 position, TeamId side)
        {
            float d = Direction(side);
            position.x = Mathf.Clamp(position.x, -Settings.halfWidth + 0.4f, Settings.halfWidth - 0.4f);
            position.z = -d * Mathf.Clamp(-d * position.z, 0.65f, Settings.halfLength - 0.4f);
            return position;
        }
        public Vector3 Spawn(TeamId side) => new Vector3(0, 0, -Direction(side) * (Settings.halfLength - 1));
        public bool OnSide(Vector3 position, TeamId side) => position.z * Direction(side) <= 0.15f;
    }
}
