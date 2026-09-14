using UnityEngine;

namespace Volleyball
{
    public sealed class CpuController : MonoBehaviour
    {
        public VolleyballCharacterController Character;
        public MatchController Match;
        public PrototypeSettings Settings;

        float nextThink;
        float serveAt;
        MatchState previous;
        Vector3 destination;

        void Update()
        {
            if (!Match || Match.Rules == null) return;
            MatchState state = Match.Rules.State;
            if (state != previous)
            {
                previous = state;
                serveAt = Time.time + Settings.cpuServeDelay;
            }

            if (state == MatchState.ServePreparation)
            {
                Character.Move(Vector3.zero);
                if (Match.Rules.Server == Character.Team && Time.time >= serveAt)
                    Character.RequestAction(ActionType.Serve);
                return;
            }

            if (state != MatchState.Playing)
            {
                Character.Move(Vector3.zero);
                return;
            }

            Vector3 position = Match.Ball.Body.position;
            Vector3 velocity = Match.Ball.Velocity;
            if (Time.time >= nextThink)
            {
                nextThink = Time.time + Settings.cpuReaction;
                float time = BallTrajectory.DescendingTime(position, velocity, Settings.gravity, 3.1f);
                if (time < 0) time = BallTrajectory.DescendingTime(position, velocity, Settings.gravity, 1.5f);
                Vector3 predicted = time >= 0
                    ? BallTrajectory.AtTime(position, velocity, Settings.gravity, time)
                    : position;
                destination = Match.Court.OnSide(predicted, Character.Team)
                    ? Match.Court.Clamp(predicted, Character.Team)
                    : Match.Court.Spawn(Character.Team);
            }

            Vector3 delta = destination - transform.position;
            delta.y = 0;
            Character.Move(delta.magnitude > 0.15f ? Vector3.ClampMagnitude(delta / 0.3f, 1) : Vector3.zero);
            Vector3 ballDelta = position - transform.position;
            ballDelta.y = 0;
            if (!Match.Court.OnSide(position, Character.Team) || ballDelta.magnitude > Settings.hitRadius - 0.15f) return;
            float arrival = BallTrajectory.DescendingTime(
                position,
                velocity,
                Settings.gravity,
                Settings.attackContactHeight + Settings.jumpHeight * 0.8f);
            if (arrival >= 0 && arrival < Settings.jumpDuration * 0.48f && position.y > 2.5f)
                Character.AutomatedAttack(Vector2.zero);
            else if (position.y < 2.45f && velocity.y < 0)
                Character.RequestAction(ActionType.Receive);
        }

        void OnDisable()
        {
            if (Character) Character.Move(Vector3.zero);
        }
    }
}
