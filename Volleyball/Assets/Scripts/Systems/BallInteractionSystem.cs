using UnityEngine;
namespace Volleyball
{
    public sealed class BallInteractionSystem : MonoBehaviour
    {
        public MatchController Match;
        public PrototypeSettings Settings;
        public CourtDefinition Court;
        public BallController Ball;
        float nextContactAt;
        public Vector3 LastServeTarget {get;private set;}
        public ServeTimingGrade LastServeGrade {get;private set;}
        public Vector3 LastAttackTarget { get; private set; }
        public AttackTimingGrade LastAttackGrade { get; private set; }
        public float LastAttackCharge { get; private set; }
        public Vector3 LastAimedActionTarget { get; private set; }
        public bool TryToss(CharacterMotor actor)
        {
            var rules=Match.Rules;
            if(rules==null || rules.State!=MatchState.ServePreparation ||
                rules.Server!=actor.Team || !actor.GetComponent<VolleyballActions>().UsePlayerServeControls || Ball.IsLive ||
                Vector3.Distance(actor.transform.position,Ball.Body.position)>3)
            {
                return false;
            }

            Ball.Launch(Vector3.up*Settings.serveTossSpeed);
            return true;
        }
        public bool TryHit(CharacterMotor actor,ActionType action)
        {
            if(actor.GetComponent<VolleyballActions>().ActiveAction!=action) return false;
            var rules=Match.Rules;
            if(rules==null) return false;
            Vector3 position=Ball.Body.position;
            float d=CourtDefinition.Direction(actor.Team);
            if(action==ActionType.Serve)
            {
                Vector3 serveOffset=position-actor.transform.position;
                if(rules.State!=MatchState.ServePreparation || actor.Team!=rules.Server ||
                    new Vector2(serveOffset.x,serveOffset.z).magnitude>3) return false;
                var actions=actor.GetComponent<VolleyballActions>();
                ServeTimingGrade grade=actions.UsePlayerServeControls?actions.PendingServeGrade:ServeTimingGrade.Perfect;
                if(actions.UsePlayerServeControls && (actions.CurrentServeStage!=ServeStage.StrikeQueued || !Ball.IsLive || grade==ServeTimingGrade.None)) return false;
                if(!Match.TryStartServe(actor.Team)) return false;
                nextContactAt=Time.time+Settings.hitCooldown;
                Vector2 aim=actions.UsePlayerServeControls?actions.ServeAimInput:Vector2.zero;
                Vector3 serveTarget=ServeMechanics.TargetForInput(aim,actor.Team,Settings,grade);
                LastServeTarget=serveTarget;
                LastServeGrade=grade;
                Ball.Launch(ServeMechanics.Velocity(position,serveTarget,grade,Settings));
                if(actions.UsePlayerServeControls) Match.NotifyServeStrike(grade); else Match.NotifyHit(actor.Team,action);
                return true;
            }
            if(rules.State!=MatchState.Playing || !Ball.IsLive || Time.time<nextContactAt || !Court.OnSide(position,actor.Team)) return false;
            Vector3 delta=position-actor.transform.position;
            if(new Vector2(delta.x,delta.z).magnitude>Settings.hitRadius) return false;
            float min=action==ActionType.Receive?0.25f:action==ActionType.Set?1.25f:Settings.attackContactHeight-Settings.attackHeightTolerance;
            float max=action==ActionType.Receive?2.5f:action==ActionType.Set?3.7f:Settings.attackContactHeight+Settings.attackHeightTolerance;
            if(delta.y<min || delta.y>max) return false;
            Vector3 target,velocity;
            if(action==ActionType.Attack)
            {
                if(!actor.IsJumping || actor.JumpOffset<0.25f) return false;
                var actions=actor.GetComponent<VolleyballActions>();
                bool validStage=actions.CurrentAttackStage==AttackStage.Released || actions.CurrentAttackStage==AttackStage.CpuQueued;
                if(!validStage || actions.PendingAttackGrade==AttackTimingGrade.Miss) return false;
                target=AttackMechanics.TargetForInput(
                    actions.AttackAimInput,actor.Team,Settings,actions.PendingAttackGrade);
                velocity=AttackMechanics.Velocity(
                    position,target,actions.ChargeAmount,actions.PendingAttackGrade,Settings);
                LastAttackTarget=target;
                LastAttackGrade=actions.PendingAttackGrade;
                LastAttackCharge=actions.ChargeAmount;
            }
            else
            {
                target=ControlAimMechanics.TargetForInput(
                    actor.transform.position, actor.GetComponent<VolleyballActions>().AimedActionInput,
                    actor.Team, action, Court);
                LastAimedActionTarget=target;
                velocity=BallTrajectory.Arc(position,target,Settings.gravity,action==ActionType.Set?Settings.setArc:Settings.receiveArc);
            }
            if(velocity.magnitude>Settings.maxBallSpeed) velocity=Vector3.ClampMagnitude(velocity,Settings.maxBallSpeed);
            rules.Touch(actor.Team);nextContactAt=Time.time+Settings.hitCooldown;
            Ball.Launch(velocity);
            if(action==ActionType.Attack) Match.NotifyAttackHit(actor.Team,LastAttackGrade);
            else Match.NotifyHit(actor.Team,action);
            return true;
        }
    }
}
