using UnityEngine;
namespace Volleyball
{
    public static class BallTrajectory
    {
        public static Vector3 AtTime(Vector3 position, Vector3 velocity, float gravity, float time)
            => position + velocity * time + Vector3.down * (0.5f * gravity * time * time);
        public static Vector3 ToTarget(Vector3 start, Vector3 target, float gravity, float time)
        {
            time = Mathf.Max(0.1f, time);
            return (target - start) / time + Vector3.up * (0.5f * gravity * time);
        }
        public static Vector3 Arc(Vector3 start, Vector3 target, float gravity, float arc)
        {
            gravity = Mathf.Max(0.1f, gravity);
            float peak = Mathf.Max(start.y, target.y) + Mathf.Max(0.1f, arc);
            float time = Mathf.Sqrt(2 * (peak - start.y) / gravity) + Mathf.Sqrt(2 * (peak - target.y) / gravity);
            return ToTarget(start, target, gravity, time);
        }
        public static float DescendingTime(Vector3 position, Vector3 velocity, float gravity, float height)
        {
            float discriminant = velocity.y * velocity.y + 2 * gravity * (position.y - height);
            if (gravity <= 0 || discriminant < 0) return -1;
            float time = (velocity.y + Mathf.Sqrt(discriminant)) / gravity;
            return time >= 0 ? time : -1;
        }
    }
}
