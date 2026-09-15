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
        public bool HasMatchAuthority { get; private set; } = true;
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
            if(!HasMatchAuthority || Rules == null) return;
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
            if(!HasMatchAuthority || Rules==null || Rules.State!=MatchState.ServePreparation || Rules.Server!=team) return false;
            var server=team==TeamId.Human?Human:Cpu;
            Ball.Hold(server.transform.position+new Vector3(0,1.5f,CourtDefinition.Direction(team)*0.7f));
            LastMessage=team==TeamId.Human?"Toss expired - SPACE / B to retry":"CPU preparing serve";
            Changed?.Invoke();
            return true;
        }
        public bool TryStartServe(TeamId team)
        {
            if(!HasMatchAuthority || Rules==null || !Rules.Serve(team)) return false;
            LastMessage="Rally"; Changed?.Invoke();return true;
        }
        void OnLanded(Vector3 point)
        {
            if(!HasMatchAuthority || Rules==null) return;
            var winner=Rules.LandingWinner(point,Settings.halfWidth,Settings.halfLength);
            FinishPoint(winner);
        }
        void OnFault()
        {
            if(HasMatchAuthority && Rules!=null) FinishPoint(MatchRules.Opponent(Rules.LastTouch));
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
            if(!HasMatchAuthority || Rules==null) return;
            Rules.Reset(); PrepareRally();
        }
        public void SetNetworkAuthority(bool authoritative)
        {
            HasMatchAuthority=authoritative;
        }
        public void PauseOnline(string message)
        {
            if(!HasMatchAuthority || Rules==null) return;
            Rules.Wait();
            Human.CancelActions(); Cpu.CancelActions();
            Ball.Hold(Ball.transform.position);
            LastMessage=message;
            Changed?.Invoke();
        }
        public void BeginOnlineMatch()
        {
            if(!HasMatchAuthority || Rules==null) return;
            Rules.Reset();
            PrepareRally();
        }
        public MatchNetworkState CaptureNetworkState()
        {
            bool winner=Rules!=null && Rules.HasWinner;
            return new MatchNetworkState
            {
                State=Rules?.State??MatchState.Waiting,
                Server=Rules?.Server??TeamId.Human,
                LastTouch=Rules?.LastTouch??TeamId.Human,
                TouchCount=Rules?.TouchCount??0,
                TeamAScore=Rules?.HumanScore??0,
                TeamBScore=Rules?.CpuScore??0,
                TargetScore=Rules?.TargetScore??Settings.winningScore,
                HasWinner=winner,
                Winner=winner?Rules.Winner:TeamId.Human,
                Message=new Unity.Collections.FixedString128Bytes(LastMessage??string.Empty)
            };
        }
        public void ApplyNetworkState(MatchNetworkState snapshot)
        {
            if(HasMatchAuthority || Rules==null) return;
            Rules.Synchronize(snapshot);
            LastMessage=snapshot.Message.ToString();
            Changed?.Invoke();
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
