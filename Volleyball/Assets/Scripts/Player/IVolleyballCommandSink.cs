using UnityEngine;

namespace Volleyball
{
    public interface IVolleyballCommandSink
    {
        ServeStage CurrentServeStage { get; }
        AttackStage CurrentAttackStage { get; }
        float CurrentAttackCharge { get; }
        void Move(Vector3 direction);
        void Aim(Vector2 input);
        bool RequestAction(ActionType action);
        bool AttackStarted(Vector2 aimDirection);
        void AttackHeld(Vector2 aimDirection);
        bool AttackReleased(Vector2 aimDirection);
    }
}
