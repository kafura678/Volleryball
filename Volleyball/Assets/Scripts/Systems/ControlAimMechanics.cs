using UnityEngine;

namespace Volleyball
{
    public static class ControlAimMechanics
    {
        public static bool IsFinite(Vector2 aim) =>
            !float.IsNaN(aim.x) && !float.IsNaN(aim.y) &&
            !float.IsInfinity(aim.x) && !float.IsInfinity(aim.y);

        // Gameplay aim X is already court-relative, as in AttackMechanics.
        public static Vector3 TargetForInput(Vector3 actorPosition, Vector2 aim,
            TeamId team, ActionType action, CourtDefinition court)
        {
            PrototypeSettings settings = court.Settings;
            float direction = CourtDefinition.Direction(team);
            float strength = action == ActionType.Set ? settings.setAimStrength : settings.receiveAimStrength;
            aim = IsFinite(aim) ? Vector2.ClampMagnitude(aim, 1f) : Vector2.zero;
            Vector3 target = actorPosition;
            target.z += direction * (action == ActionType.Set ? 1f : 0.35f);
            target.x += aim.x * Mathf.Max(0f, strength);
            target.z += direction * aim.y * Mathf.Max(0f, strength);
            target = court.Clamp(target, team);
            target.z = -direction * Mathf.Max(1.4f, -direction * target.z);
            target.y = action == ActionType.Set ? 2.4f : 1.6f;
            return target;
        }
    }
}
