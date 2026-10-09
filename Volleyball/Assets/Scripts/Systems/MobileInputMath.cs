using UnityEngine;

namespace Volleyball
{
    public static class MobileInputMath
    {
        public static Vector2 ApplyRadialDeadZone(Vector2 input, float deadZone)
        {
            float magnitude = Mathf.Clamp01(input.magnitude);
            deadZone = Mathf.Clamp(deadZone, 0, 0.95f);
            if (magnitude <= deadZone) return Vector2.zero;
            float scaledMagnitude = (magnitude - deadZone) / (1f - deadZone);
            return input.normalized * Mathf.Clamp01(scaledMagnitude);
        }

        public static Vector2 SwipeAim(Vector2 start, Vector2 current, float deadZone, float fullScale)
        {
            Vector2 delta = current - start;
            float magnitude = delta.magnitude;
            deadZone = Mathf.Max(0, deadZone);
            fullScale = Mathf.Max(deadZone + 1f, fullScale);
            if (magnitude <= deadZone) return Vector2.zero;
            float scaledMagnitude = (magnitude - deadZone) / (fullScale - deadZone);
            return delta.normalized * Mathf.Clamp01(scaledMagnitude);
        }

        // PointerEventData.position is already expressed in the current display orientation.
        // Convert its screen-relative right/left into the player's court-relative right/left here.
        public static Vector2 GameplayAim(
            Vector2 start,
            Vector2 current,
            float deadZone,
            float fullScale,
            TeamId team)
        {
            return ScreenAimToCourtAim(SwipeAim(start, current, deadZone, fullScale), team);
        }

        public static Vector2 ScreenAimToCourtAim(Vector2 screenAim, TeamId team)
        {
            screenAim = Vector2.ClampMagnitude(screenAim, 1f);
            return new Vector2(screenAim.x * CourtDefinition.Direction(team), screenAim.y);
        }
    }
}
