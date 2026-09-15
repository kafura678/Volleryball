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
        public IVolleyballCommandSink CommandSink { get; set; }
        public ServeStage CurrentServeStage => CommandSink?.CurrentServeStage ?? Actions.CurrentServeStage;
        public AttackStage CurrentAttackStage => CommandSink?.CurrentAttackStage ?? Actions.CurrentAttackStage;
        public float CurrentAttackCharge => CommandSink?.CurrentAttackCharge ?? Actions.ChargeAmount;

        void Awake()
        {
            Motor = GetComponent<CharacterMotor>();
            Actions = GetComponent<VolleyballActions>();
        }

        public void Move(Vector3 direction)
        {
            if (CommandSink != null)
            {
                CommandSink.Move(direction);
                return;
            }
            ApplyMove(direction);
        }

        public void ApplyMove(Vector3 direction)
        {
            float multiplier = Actions != null && Actions.IsAttackJumpActive
                ? Actions.Settings.attackMovementMultiplier
                : 1f;
            Motor.MoveDirection = direction * multiplier;
        }

        public void Aim(Vector2 input)
        {
            AimInput = Vector2.ClampMagnitude(input, 1f);
            if (CommandSink != null)
            {
                CommandSink.Aim(AimInput);
                return;
            }
            ApplyAim(input);
        }

        public void ApplyAim(Vector2 input)
        {
            AimInput = Vector2.ClampMagnitude(input, 1f);
        }

        public bool RequestAction(ActionType action)
        {
            return CommandSink != null ? CommandSink.RequestAction(action) : ApplyAction(action);
        }

        public bool ApplyAction(ActionType action) => Actions.Request(action);

        public bool AttackStarted(Vector2 aimDirection)
        {
            AimInput = Vector2.ClampMagnitude(aimDirection, 1f);
            return CommandSink != null
                ? CommandSink.AttackStarted(AimInput)
                : ApplyAttackStarted(aimDirection);
        }

        public bool ApplyAttackStarted(Vector2 aimDirection)
        {
            ApplyAim(aimDirection);
            return Actions.BeginAttack(AimInput);
        }

        public void AttackHeld(Vector2 aimDirection)
        {
            AimInput = Vector2.ClampMagnitude(aimDirection, 1f);
            if (CommandSink != null) CommandSink.AttackHeld(AimInput);
            else ApplyAttackHeld(aimDirection);
        }

        public void ApplyAttackHeld(Vector2 aimDirection)
        {
            ApplyAim(aimDirection);
            Actions.HoldAttack(AimInput);
        }

        public bool AttackReleased(Vector2 aimDirection)
        {
            AimInput = Vector2.ClampMagnitude(aimDirection, 1f);
            return CommandSink != null
                ? CommandSink.AttackReleased(AimInput)
                : ApplyAttackReleased(aimDirection);
        }

        public bool ApplyAttackReleased(Vector2 aimDirection)
        {
            ApplyAim(aimDirection);
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
