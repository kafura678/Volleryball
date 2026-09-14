using System;
using UnityEngine;
namespace Volleyball
{
    public sealed class MatchController : MonoBehaviour
    {
        public PrototypeSettings Settings;
        public CourtDefinition Court;
        public BallController Ball;
        public VolleyballCharacterController Human;
        public VolleyballCharacterController Cpu;
        public MatchRules Rules { get; private set; }
        public event Action Changed;
        float transitionAt;
        public string LastMessage { get; private set; } = "";
        void Start()
        {
            Physics.IgnoreCollision(Ball.GetComponent<Collider>(),Human.GetComponent<Collider>());
            Physics.IgnoreCollision(Ball.GetComponent<Collider>(),Cpu.GetComponent<Collider>());
            Rules=new MatchRules(Settings.winningScore);
            Ball.Landed+=OnLanded;Ball.Faulted+=OnFault;
            var targetMarker=GetComponent<ServeTargetMarker>();
            if(!targetMarker) targetMarker=gameObject.AddComponent<ServeTargetMarker>();
            targetMarker.Match=this;
            var attackMarker=GetComponent<AttackTargetMarker>();
            if(!attackMarker) attackMarker=gameObject.AddComponent<AttackTargetMarker>();
            attackMarker.Match=this;
            PrepareRally();
        }
        void OnDestroy() {if(Ball) {Ball.Landed-=OnLanded;Ball.Faulted-=OnFault;}}
        void Update()
        {
            if(Rules == null) return;
            if(Rules.State==MatchState.PointFinished && Time.time>=transitionAt) PrepareRally();
        }
        void PrepareRally()
        {
            if(!Rules.Prepare()) return;
            Human.ResetForRally(Court.Spawn(TeamId.Human)); Cpu.ResetForRally(Court.Spawn(TeamId.Cpu));
            var server=Rules.Server==TeamId.Human?Human:Cpu;
            Ball.Hold(server.transform.position+new Vector3(0,1.5f,CourtDefinition.Direction(Rules.Server)*0.7f));
            LastMessage=Rules.Server==TeamId.Human?"Aim with MOVE - SPACE / B to toss":"CPU preparing serve";
            Changed?.Invoke();
        }
        public bool ResetServePreparation(TeamId team)
        {
            if(Rules==null || Rules.State!=MatchState.ServePreparation || Rules.Server!=team) return false;
            var server=team==TeamId.Human?Human:Cpu;
            Ball.Hold(server.transform.position+new Vector3(0,1.5f,CourtDefinition.Direction(team)*0.7f));
            LastMessage=team==TeamId.Human?"Toss expired - SPACE / B to retry":"CPU preparing serve";
            Changed?.Invoke();
            return true;
        }
        public bool TryStartServe(TeamId team)
        {
            if(Rules==null || !Rules.Serve(team)) return false;
            LastMessage="Rally"; Changed?.Invoke();return true;
        }
        void OnLanded(Vector3 point)
        {
            if(Rules==null) return;
            var winner=Rules.LandingWinner(point,Settings.halfWidth,Settings.halfLength);
            FinishPoint(winner);
        }
        void OnFault()
        {
            if(Rules!=null) FinishPoint(MatchRules.Opponent(Rules.LastTouch));
        }
        void FinishPoint(TeamId winner)
        {
            if(!Rules.Award(winner)) return;
            Ball.Hold(Ball.transform.position);
            Human.CancelActions();Cpu.CancelActions();
            LastMessage=(winner==TeamId.Human?"PLAYER":"CPU")+" scores";
            transitionAt=Time.time+Settings.pointDelay;Changed?.Invoke();
        }
        public void Restart()
        {
            if(Rules==null) return;
            Rules.Reset(); PrepareRally();
        }
        public void NotifyHit(TeamId team,ActionType action)
        {
            LastMessage=(team==TeamId.Human?"PLAYER ":"CPU ")+action.ToString().ToUpperInvariant();
            Changed?.Invoke();
        }
        public void NotifyServeToss()
        {
            LastMessage="Tossed - press SPACE / B near the top";
            Changed?.Invoke();
        }
        public void NotifyServeStrike(ServeTimingGrade grade)
        {
            LastMessage=grade==ServeTimingGrade.Perfect?"PERFECT SERVE":"GOOD SERVE";
            Changed?.Invoke();
        }
        public void NotifyAttackCharge()
        {
            LastMessage="SPIKE CHARGE - aim with MOVE, release L / X";
            Changed?.Invoke();
        }
        public void NotifyAttackRelease(AttackTimingGrade grade)
        {
            LastMessage=grade==AttackTimingGrade.Miss?"SPIKE MISS":grade.ToString().ToUpperInvariant()+" TIMING";
            Changed?.Invoke();
        }
        public void NotifyAttackHit(TeamId team,AttackTimingGrade grade)
        {
            LastMessage=(team==TeamId.Human?"PLAYER ":"CPU ")+grade.ToString().ToUpperInvariant()+" SPIKE";
            Changed?.Invoke();
        }
    }
}
