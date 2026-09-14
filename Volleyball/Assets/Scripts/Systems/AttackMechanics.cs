using UnityEngine;

namespace Volleyball
{
    public static class AttackMechanics
    {
        public static float Charge(float elapsed, PrototypeSettings settings)
        {
            return Mathf.Clamp01(elapsed / settings.attackMaxChargeTime);
        }

        public static AttackTimingGrade EvaluateTiming(
            float elapsed,
            float jumpOffset,
            Vector3 ballOffset,
            bool isJumping,
            PrototypeSettings settings)
        {
            float horizontalDistance = new Vector2(ballOffset.x, ballOffset.z).magnitude;
            float heightDifference = Mathf.Abs(ballOffset.y - settings.attackContactHeight);
            float timeDifference = Mathf.Abs(elapsed - settings.attackPerfectTime);

            if (!isJumping ||
                elapsed < settings.attackMinimumReleaseTime ||
                timeDifference > settings.attackGoodWindow ||
                jumpOffset < settings.attackMinimumJumpHeight ||
                horizontalDistance > settings.hitRadius ||
                heightDifference > settings.attackHeightTolerance)
            {
                return AttackTimingGrade.Miss;
            }

            bool perfect = timeDifference <= settings.attackPerfectWindow &&
                Mathf.Abs(jumpOffset - settings.jumpHeight) <= settings.attackPerfectJumpTolerance &&
                heightDifference <= settings.attackPerfectBallHeightTolerance;
            return perfect ? AttackTimingGrade.Perfect : AttackTimingGrade.Good;
        }

        public static float Power(float charge, AttackTimingGrade grade, PrototypeSettings settings)
        {
            float timingQuality = grade == AttackTimingGrade.Perfect
                ? 1f
                : grade == AttackTimingGrade.Good ? settings.attackGoodTimingQuality : 0f;
            return Mathf.Clamp01(charge) * timingQuality;
        }

        public static Vector3 TargetForInput(
            Vector2 aimDirection,
            TeamId attackingTeam,
            PrototypeSettings settings,
            AttackTimingGrade grade = AttackTimingGrade.Perfect)
        {
            float direction = CourtDefinition.Direction(attackingTeam);
            float aimStrength = grade == AttackTimingGrade.Good ? settings.attackGoodAimStrength : 1f;
            aimDirection = Vector2.ClampMagnitude(aimDirection, 1f) * aimStrength;

            float xLimit = settings.halfWidth - settings.attackTargetMargin;
            float near = settings.attackTargetMargin;
            float far = settings.halfLength - settings.attackTargetMargin;
            float centerDepth = (near + far) * 0.5f;
            float z = direction * (centerDepth + aimDirection.y * settings.attackAimDepth);

            return new Vector3(
                Mathf.Clamp(aimDirection.x * settings.attackAimWidth, -xLimit, xLimit),
                0.25f,
                direction > 0 ? Mathf.Clamp(z, near, far) : Mathf.Clamp(z, -far, -near));
        }

        public static Vector3 Velocity(
            Vector3 start,
            Vector3 target,
            float charge,
            AttackTimingGrade grade,
            PrototypeSettings settings)
        {
            float power = Mathf.Lerp(
                settings.attackMinimumPower,
                1f,
                Power(charge, grade, settings));
            float flightTime = Mathf.Lerp(settings.attackSlowFlightTime, settings.attackFastFlightTime, power);
            Vector3 velocity = BallTrajectory.ToTarget(start, target, settings.gravity, flightTime);

            for (int i = 0; i < 30; i++)
            {
                if (Mathf.Abs(velocity.z) < 0.001f) break;
                float netCrossingTime = -start.z / velocity.z;
                if (netCrossingTime <= 0 || netCrossingTime >= flightTime ||
                    BallTrajectory.AtTime(start, velocity, settings.gravity, netCrossingTime).y > settings.netHeight + 0.4f)
                {
                    break;
                }

                flightTime += 0.05f;
                velocity = BallTrajectory.ToTarget(start, target, settings.gravity, flightTime);
            }

            return Vector3.ClampMagnitude(velocity, settings.maxBallSpeed);
        }
    }
}
