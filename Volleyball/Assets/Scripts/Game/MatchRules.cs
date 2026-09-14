using System;
using UnityEngine;
namespace Volleyball
{
    public enum TeamId { Human, Cpu }
    public enum MatchState { Waiting, ServePreparation, Playing, PointFinished, MatchFinished }
    public enum ActionType { Receive, Set, Attack, Serve }
    public enum ServeStage { Ready, Tossed, StrikeQueued }
    public enum ServeTimingGrade { None, Good, Perfect }
    public enum AttackStage { Ready, Charging, Released, CpuQueued }
    public enum AttackTimingGrade { Miss, Good, Perfect }

    public sealed class MatchRules
    {
        public MatchState State { get; private set; }
        public TeamId Server { get; private set; }
        public TeamId LastTouch { get; private set; }
        public int TouchCount { get; private set; }
        public int HumanScore { get; private set; }
        public int CpuScore { get; private set; }
        public int TargetScore { get; }
        public static TeamId Opponent(TeamId team) => team == TeamId.Human ? TeamId.Cpu : TeamId.Human;
        public MatchRules(int target) { TargetScore = Math.Max(1, target); Reset(); }
        public void Reset()
        {
            HumanScore = CpuScore = TouchCount = 0;
            Server = LastTouch = TeamId.Human;
            State = MatchState.Waiting;
        }
        public bool Prepare()
        {
            if (State != MatchState.Waiting && State != MatchState.PointFinished) return false;
            State = MatchState.ServePreparation; LastTouch = Server; TouchCount = 0; return true;
        }
        public bool Serve(TeamId team)
        {
            if (State != MatchState.ServePreparation || team != Server) return false;
            State = MatchState.Playing; Touch(team); return true;
        }
        public bool Touch(TeamId team)
        {
            if (State != MatchState.Playing) return false;
            TouchCount = team == LastTouch ? TouchCount + 1 : 1;
            LastTouch = team; return true;
        }
        public bool Award(TeamId winner)
        {
            if (State != MatchState.Playing) return false;
            if (winner == TeamId.Human) HumanScore++; else CpuScore++;
            Server = winner;
            State = HumanScore >= TargetScore || CpuScore >= TargetScore ? MatchState.MatchFinished : MatchState.PointFinished;
            return true;
        }
        public TeamId LandingWinner(Vector3 point, float halfWidth, float halfLength)
        {
            if (Mathf.Abs(point.x) > halfWidth || Mathf.Abs(point.z) > halfLength) return Opponent(LastTouch);
            return point.z < 0 ? TeamId.Cpu : TeamId.Human;
        }
    }
}
