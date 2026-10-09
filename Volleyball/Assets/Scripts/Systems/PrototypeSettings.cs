using UnityEngine;
namespace Volleyball
{
    [CreateAssetMenu(menuName = "Volleyball/Prototype Settings")]
    public sealed class PrototypeSettings : ScriptableObject
    {
        [Header("Court and match")]
        [Min(2)] public float halfWidth = 4;
        [Min(3)] public float halfLength = 6;
        [Min(1)] public float netHeight = 2.15f;
        [Min(1)] public int winningScore = 5;
        [Min(0.1f)] public float pointDelay = 1.4f;
        [Header("Movement and ball")]
        [Min(1)] public float moveSpeed = 6;
        [Min(1)] public float gravity = 9.81f;
        [Min(5)] public float maxBallSpeed = 20;
        [Min(0.1f)] public float hitRadius = 1.65f;
        [Min(0.01f)] public float actionWindow = 0.38f;
        [Min(0.01f)] public float hitCooldown = 0.3f;
        [Header("Shots: arc heights above contact / target")]
        [Min(0.1f)] public float receiveArc = 3;
        [Min(0.1f)] public float setArc = 3.6f;
        [Min(0.1f)] public float serveArc = 2.6f;
        [Min(0.01f)] public float attackFlightTime = 0.85f;
        [Header("Receive / Set direction assistance")]
        [Min(0)] public float receiveAimStrength = 1.0f;
        [Min(0)] public float setAimStrength = 1.4f;
        [Header("Two-step human serve")]
        [Min(0.1f)] public float serveTossSpeed = 6;
        [Min(0.1f)] public float serveTossTimeout = 1.35f;
        [Min(0)] public float serveMinimumStrikeDelay = 0.2f;
        [Min(0)] public float servePerfectTime = 0.6f;
        [Min(0.01f)] public float servePerfectWindow = 0.1f;
        [Min(0.01f)] public float serveGoodWindow = 0.32f;
        [Min(0.1f)] public float serveMinimumContactHeight = 2.2f;
        [Min(0.1f)] public float serveMaximumContactHeight = 4.2f;
        [Min(0.1f)] public float serveAimWidth = 3.2f;
        [Min(0.1f)] public float serveAimDepth = 2.1f;
        [Min(0.1f)] public float serveTargetMargin = 0.55f;
        [Range(0,1)] public float serveGoodAimStrength = 0.82f;
        [Min(0.1f)] public float servePerfectArc = 1.15f;
        [Min(0.1f)] public float serveGoodArc = 1.65f;
        [Header("Charged attack")]
        [Min(0.1f)] public float jumpHeight = 1.5f;
        [Min(0.1f)] public float jumpDuration = 0.95f;
        [Min(0.1f)] public float attackContactHeight = 1.9f;
        [Min(0.1f)] public float attackHeightTolerance = 1.0f;
        [Min(0.1f)] public float attackMaxChargeTime = 0.6f;
        [Min(0)] public float attackMinimumReleaseTime = 0.12f;
        [Min(0)] public float attackPerfectTime = 0.48f;
        [Min(0.01f)] public float attackPerfectWindow = 0.14f;
        [Min(0.01f)] public float attackGoodWindow = 0.38f;
        [Min(0)] public float attackMinimumJumpHeight = 0.25f;
        [Min(0.01f)] public float attackPerfectJumpTolerance = 0.5f;
        [Min(0.01f)] public float attackPerfectBallHeightTolerance = 0.5f;
        [Range(0,1)] public float attackGoodTimingQuality = 0.78f;
        [Range(0,1)] public float attackMinimumPower = 0.42f;
        [Min(0.1f)] public float attackFastFlightTime = 0.65f;
        [Min(0.1f)] public float attackSlowFlightTime = 1.15f;
        [Min(0.1f)] public float attackAimWidth = 3.2f;
        [Min(0.1f)] public float attackAimDepth = 2.1f;
        [Min(0.1f)] public float attackTargetMargin = 0.55f;
        [Range(0,1)] public float attackGoodAimStrength = 0.84f;
        [Range(0,1)] public float attackMovementMultiplier = 0f;
        [Header("Mobile controls")]
        [Range(0,0.95f)] public float mobileJoystickDeadZone = 0.15f;
        [Min(0)] public float mobileSwipeDeadZone = 24f;
        [Min(1)] public float mobileSwipeFullScale = 140f;
        public bool showMobileControlsInEditor = true;
        [Header("CPU")]
        [Min(0.02f)] public float cpuReaction = 0.12f;
        [Min(0.1f)] public float cpuServeDelay = 0.9f;
    }
}
