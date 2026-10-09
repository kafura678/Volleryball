using UnityEngine;

namespace Volleyball
{
    public sealed class VolleyballActions : MonoBehaviour
    {
        public PrototypeSettings Settings;
        public BallInteractionSystem Interaction;
        public MatchController Match;

        public ActionType? AimedAction { get; private set; }
        public Vector2 AimedActionInput { get; private set; }

        CharacterMotor motor;
        ActionType current;
        float expiresAt;
        float readyAt;
        float tossedAt;
        float attackStartedAt;
        float releasedCharge;

        public bool IsActive { get; private set; }
        public ActionType? ActiveAction => IsActive ? current : (ActionType?)null;
        public ServeStage CurrentServeStage { get; private set; }
        public ServeTimingGrade PendingServeGrade { get; private set; }
        public Vector2 ServeAimInput { get; private set; }
        public float TossElapsed => CurrentServeStage == ServeStage.Ready ? 0 : Time.time - tossedAt;
        public AttackStage CurrentAttackStage { get; private set; }
        public AttackTimingGrade PendingAttackGrade { get; private set; }
        public Vector2 AttackAimInput { get; private set; }
        public float AttackElapsed => CurrentAttackStage == AttackStage.Ready ? 0 : Time.time - attackStartedAt;
        public float ChargeAmount => CurrentAttackStage == AttackStage.Ready
            ? 0
            : CurrentAttackStage == AttackStage.Charging
                ? AttackMechanics.Charge(AttackElapsed, Settings)
                : releasedCharge;
        public bool IsAttackInProgress => CurrentAttackStage != AttackStage.Ready;
        public bool IsAttackJumpActive { get; private set; }
        public int SuccessfulHits { get; private set; }
        public bool UsePlayerServeControls { get; set; }

        void Awake()
        {
            motor = GetComponent<CharacterMotor>();
            UsePlayerServeControls = motor.Team == TeamId.Human;
        }

        public bool BeginAimedAction(ActionType action, Vector2 aim)
        {
            if (!ControlAimMechanics.IsFinite(aim) || (action != ActionType.Receive && action != ActionType.Set) || !CanBegin(action)) return false;
            AimedAction = action;
            AimedActionInput = Vector2.ClampMagnitude(aim, 1f);
            return true;
        }

        public void HoldAimedAction(ActionType action, Vector2 aim)
        {
            if (AimedAction == action && ControlAimMechanics.IsFinite(aim)) AimedActionInput = Vector2.ClampMagnitude(aim, 1f);
        }

        public bool ReleaseAimedAction(ActionType action, Vector2 aim)
        {
            if (AimedAction != action || !ControlAimMechanics.IsFinite(aim)) return false;
            CancelAimedAction();
            if (!Request(action)) return false;
            AimedActionInput = Vector2.ClampMagnitude(aim, 1f);
            return true;
        }

        public void CancelAimedAction()
        {
            AimedAction = null;
            AimedActionInput = Vector2.zero;
        }

        public bool Request(ActionType action)
        {
            if (action == ActionType.Attack)
            {
                return motor.Team == TeamId.Cpu
                    ? BeginAutomatedAttack(Vector2.zero)
                    : BeginAttack(GetComponent<VolleyballCharacterController>().AimInput);
            }

            if (!CanBegin(action)) return false;
            bool serve = action == ActionType.Serve;
            if (serve && UsePlayerServeControls)
            {
                if (CurrentServeStage == ServeStage.Ready)
                {
                    if (!Interaction.TryToss(motor)) return false;
                    ServeAimInput = GetComponent<VolleyballCharacterController>().AimInput;
                    tossedAt = Time.time;
                    CurrentServeStage = ServeStage.Tossed;
                    Match.NotifyServeToss();
                    return true;
                }

                if (CurrentServeStage != ServeStage.Tossed) return false;
                ServeTimingGrade grade = ServeMechanics.EvaluateTiming(TossElapsed, Match.Ball.Body.position.y, Settings);
                if (grade == ServeTimingGrade.None) return false;
                PendingServeGrade = grade;
                ServeAimInput = GetComponent<VolleyballCharacterController>().AimInput;
                CurrentServeStage = ServeStage.StrikeQueued;
            }

            AimedActionInput = Vector2.zero;
            current = action;
            IsActive = true;
            expiresAt = Time.time + Settings.actionWindow;
            return true;
        }

        public bool BeginAttack(Vector2 aimDirection)
        {
            if (!CanBegin(ActionType.Attack)) return false;
            current = ActionType.Attack;
            IsActive = true;
            attackStartedAt = Time.time;
            expiresAt = Time.time + Settings.jumpDuration;
            releasedCharge = 0;
            AttackAimInput = Vector2.ClampMagnitude(aimDirection, 1f);
            PendingAttackGrade = AttackTimingGrade.Miss;
            CurrentAttackStage = AttackStage.Charging;
            IsAttackJumpActive = true;
            if (!motor.BeginJump())
            {
                IsAttackJumpActive = false;
                ResetAttack();
                IsActive = false;
                return false;
            }

            Match.NotifyAttackCharge();
            return true;
        }

        public void HoldAttack(Vector2 aimDirection)
        {
            if (CurrentAttackStage != AttackStage.Charging) return;
            AttackAimInput = Vector2.ClampMagnitude(aimDirection, 1f);
        }

        public bool ReleaseAttack(Vector2 aimDirection)
        {
            if (CurrentAttackStage != AttackStage.Charging || !IsActive) return false;
            AttackAimInput = Vector2.ClampMagnitude(aimDirection, 1f);
            releasedCharge = ChargeAmount;
            Vector3 ballOffset = Match.Ball.Body.position - motor.transform.position;
            PendingAttackGrade = AttackMechanics.EvaluateTiming(
                AttackElapsed,
                motor.JumpOffset,
                ballOffset,
                motor.IsJumping,
                Settings);
            CurrentAttackStage = AttackStage.Released;
            Match.NotifyAttackRelease(PendingAttackGrade);

            if (PendingAttackGrade == AttackTimingGrade.Miss)
            {
                FinishAttack(false);
            }

            return true;
        }

        public bool BeginAutomatedAttack(Vector2 aimDirection)
        {
            if (!CanBegin(ActionType.Attack)) return false;
            current = ActionType.Attack;
            IsActive = true;
            attackStartedAt = Time.time;
            expiresAt = Time.time + Settings.jumpDuration;
            releasedCharge = 1f;
            AttackAimInput = Vector2.ClampMagnitude(aimDirection, 1f);
            PendingAttackGrade = AttackTimingGrade.Perfect;
            CurrentAttackStage = AttackStage.CpuQueued;
            IsAttackJumpActive = true;
            if (!motor.BeginJump())
            {
                IsAttackJumpActive = false;
                ResetAttack();
                IsActive = false;
                return false;
            }
            return true;
        }

        bool CanBegin(ActionType action)
        {
            if (!Match || Match.Rules == null || Time.time < readyAt || IsActive || AimedAction.HasValue || motor.IsJumping) return false;
            bool serve = action == ActionType.Serve;
            return serve
                ? Match.Rules.State == MatchState.ServePreparation && Match.Rules.Server == motor.Team
                : Match.Rules.State == MatchState.Playing;
        }

        void FixedUpdate()
        {
            if (IsAttackJumpActive && !motor.IsJumping) IsAttackJumpActive = false;

            if (UsePlayerServeControls && CurrentServeStage == ServeStage.Tossed &&
                (TossElapsed > Settings.serveTossTimeout || !Match.Ball.IsLive))
            {
                Match.ResetServePreparation(motor.Team);
                ResetServe();
            }

            if (AimedAction.HasValue && Match.Rules.State != MatchState.Playing) CancelAimedAction();
            if (!IsActive) return;
            if (current == ActionType.Attack)
            {
                ProcessAttack();
                return;
            }

            if (Time.time > expiresAt)
            {
                IsActive = false;
                readyAt = Time.time + 0.08f;
                return;
            }

            if (Interaction.TryHit(motor, current))
            {
                IsActive = false;
                readyAt = Time.time + Settings.hitCooldown;
                SuccessfulHits++;
                if (current == ActionType.Serve) ResetServe();
                motor.Face(Match.Ball.transform.position);
            }
        }

        void ProcessAttack()
        {
            if (CurrentAttackStage == AttackStage.Charging)
            {
                if (Time.time > expiresAt || !motor.IsJumping)
                {
                    PendingAttackGrade = AttackTimingGrade.Miss;
                    Match.NotifyAttackRelease(PendingAttackGrade);
                    FinishAttack(false);
                }
                return;
            }

            if (CurrentAttackStage == AttackStage.CpuQueued)
            {
                if (Time.time > expiresAt)
                {
                    FinishAttack(false);
                    return;
                }

                if (motor.JumpOffset < Settings.attackMinimumJumpHeight) return;
                if (Interaction.TryHit(motor, ActionType.Attack)) FinishAttack(true);
                return;
            }

            bool hit = Interaction.TryHit(motor, ActionType.Attack);
            FinishAttack(hit);
        }

        void FinishAttack(bool hit)
        {
            if (hit)
            {
                SuccessfulHits++;
                motor.Face(Match.Ball.transform.position);
            }
            IsActive = false;
            readyAt = Time.time + Settings.hitCooldown;
            ResetAttack();
        }

        public void CancelAttack()
        {
            if (current == ActionType.Attack) IsActive = false;
            IsAttackJumpActive = false;
            ResetAttack();
        }

        public void Cancel()
        {
            CancelAimedAction();
            IsActive = false;
            IsAttackJumpActive = false;
            expiresAt = readyAt = 0;
            ResetServe();
            ResetAttack();
        }

        void ResetServe()
        {
            CurrentServeStage = ServeStage.Ready;
            PendingServeGrade = ServeTimingGrade.None;
            ServeAimInput = Vector2.zero;
            tossedAt = 0;
        }

        void ResetAttack()
        {
            CurrentAttackStage = AttackStage.Ready;
            PendingAttackGrade = AttackTimingGrade.Miss;
            AttackAimInput = Vector2.zero;
            attackStartedAt = 0;
            releasedCharge = 0;
        }

        void OnDrawGizmosSelected()
        {
            if (!Settings) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(
                transform.position + Vector3.up * Settings.attackContactHeight,
                Settings.hitRadius);
        }
    }
}
