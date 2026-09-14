using UnityEngine;

namespace Volleyball
{
    [RequireComponent(typeof(CharacterMotor), typeof(VolleyballActions))]
    public sealed class VolleyballCharacterController : MonoBehaviour
    {
        public MatchController Match;

        public CharacterMotor Motor { get; private set; }
        public VolleyballActions Actions { get; private set; }
        public TeamId Team => Motor.Team;
        public Vector2 AimInput { get; private set; }

        void Awake()
        {
            Motor = GetComponent<CharacterMotor>();
            Actions = GetComponent<VolleyballActions>();
        }

        public void Move(Vector3 direction)
        {
            float multiplier = Actions != null && Actions.IsAttackJumpActive
                ? Actions.Settings.attackMovementMultiplier
                : 1f;
            Motor.MoveDirection = direction * multiplier;
        }

        public void Aim(Vector2 input)
        {
            AimInput = Vector2.ClampMagnitude(input, 1f);
        }

        public bool RequestAction(ActionType action)
        {
            return Actions.Request(action);
        }

        public bool AttackStarted(Vector2 aimDirection)
        {
            Aim(aimDirection);
            return Actions.BeginAttack(AimInput);
        }

        public void AttackHeld(Vector2 aimDirection)
        {
            Aim(aimDirection);
            Actions.HoldAttack(AimInput);
        }

        public bool AttackReleased(Vector2 aimDirection)
        {
            Aim(aimDirection);
            return Actions.ReleaseAttack(AimInput);
        }

        public bool AutomatedAttack(Vector2 aimDirection)
        {
            Aim(aimDirection);
            return Actions.BeginAutomatedAttack(AimInput);
        }

        public void CancelAttack()
        {
            if (Actions) Actions.CancelAttack();
        }

        void Update()
        {
            Motor.CanMove = Match && Match.Rules != null && Match.Rules.State == MatchState.Playing;
        }

        public void ResetForRally(Vector3 position)
        {
            CancelActions();
            Motor.ResetPosition(position);
        }

        public void CancelActions()
        {
            Actions.Cancel();
            Motor.MoveDirection = Vector3.zero;
            AimInput = Vector2.zero;
        }
    }
}
