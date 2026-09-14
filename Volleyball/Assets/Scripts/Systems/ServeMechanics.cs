using UnityEngine;

namespace Volleyball
{
    public static class ServeMechanics
    {
        public static ServeTimingGrade EvaluateTiming(float elapsed, float ballHeight, PrototypeSettings settings)
        {
            if (elapsed < settings.serveMinimumStrikeDelay ||
                ballHeight < settings.serveMinimumContactHeight ||
                ballHeight > settings.serveMaximumContactHeight)
            {
                return ServeTimingGrade.None;
            }

            float difference = Mathf.Abs(elapsed - settings.servePerfectTime);
            if (difference <= settings.servePerfectWindow)
            {
                return ServeTimingGrade.Perfect;
            }

            return difference <= settings.serveGoodWindow
                ? ServeTimingGrade.Good
                : ServeTimingGrade.None;
        }

        public static Vector3 TargetForInput(
            Vector2 input,
            TeamId servingTeam,
            PrototypeSettings settings,
            ServeTimingGrade grade = ServeTimingGrade.Perfect)
        {
            float direction = CourtDefinition.Direction(servingTeam);
            float aimStrength = grade == ServeTimingGrade.Good ? settings.serveGoodAimStrength : 1f;
            input = Vector2.ClampMagnitude(input, 1f) * aimStrength;

            float xLimit = settings.halfWidth - settings.serveTargetMargin;
            float near = settings.serveTargetMargin;
            float far = settings.halfLength - settings.serveTargetMargin;
            float centerDepth = (near + far) * 0.5f;
            float z = direction * (centerDepth + input.y * settings.serveAimDepth);

            return new Vector3(
                Mathf.Clamp(input.x * settings.serveAimWidth, -xLimit, xLimit),
                0.25f,
                direction > 0 ? Mathf.Clamp(z, near, far) : Mathf.Clamp(z, -far, -near));
        }

        public static Vector3 Velocity(
            Vector3 start,
            Vector3 target,
            ServeTimingGrade grade,
            PrototypeSettings settings)
        {
            float arc = grade == ServeTimingGrade.Perfect
                ? settings.servePerfectArc
                : settings.serveGoodArc;
            return BallTrajectory.Arc(start, target, settings.gravity, arc);
        }
    }
}
