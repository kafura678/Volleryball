using System.Collections.Generic;
using UnityEngine;

namespace Volleyball
{
    public enum MobileControlChannel { Move, Receive, Set, Attack, Serve }
    public enum MobileSwipeAction { Attack, Serve, Receive, Set }

    [DefaultExecutionOrder(100)]
    public sealed class MobileInputController : MonoBehaviour
    {
        const int NoPointer = int.MinValue;

        public VolleyballCharacterController Character;
        public MatchController Match;
        public Camera ViewCamera;
        public PrototypeSettings Settings;

        readonly Dictionary<int, MobileControlChannel> pointerOwners = new Dictionary<int, MobileControlChannel>();
        int movePointer = NoPointer;
        int swipePointer = NoPointer;
        Vector2 moveInput;
        Vector2 swipeStart;
        Vector2 swipeCurrent;
        MobileSwipeAction swipeAction;

        public Vector2 MoveInput => moveInput;
        public Vector2 CurrentAim => MobileInputMath.GameplayAim(
            swipeStart,
            swipeCurrent,
            Settings.mobileSwipeDeadZone,
            Settings.mobileSwipeFullScale,
            Character ? Character.Team : TeamId.Human);
        public bool HasActiveMove => movePointer != NoPointer;
        public bool HasActiveSwipe => swipePointer != NoPointer;

        public bool IsPointerClaimed(int pointerId)
        {
            return pointerOwners.ContainsKey(pointerId);
        }

        public bool BeginMove(int pointerId)
        {
            if (movePointer != NoPointer || !TryClaim(pointerId, MobileControlChannel.Move)) return false;
            movePointer = pointerId;
            moveInput = Vector2.zero;
            return true;
        }

        public void SetMove(int pointerId, Vector2 normalizedInput)
        {
            if (!Owns(pointerId, MobileControlChannel.Move)) return;
            moveInput = MobileInputMath.ApplyRadialDeadZone(
                Vector2.ClampMagnitude(normalizedInput, 1f), Settings.mobileJoystickDeadZone);
        }

        public void EndMove(int pointerId)
        {
            if (!Owns(pointerId, MobileControlChannel.Move)) return;
            moveInput = Vector2.zero;
            movePointer = NoPointer;
            Release(pointerId);
            if (Character) Character.Move(Vector3.zero);
        }

        public bool TapAction(int pointerId, ActionType action)
        {
            MobileControlChannel channel = action == ActionType.Receive
                ? MobileControlChannel.Receive
                : MobileControlChannel.Set;
            if ((action != ActionType.Receive && action != ActionType.Set) || !TryClaim(pointerId, channel)) return false;
            bool accepted = Character && Character.RequestAction(action);
            if (!accepted) Release(pointerId);
            return accepted;
        }

        public void EndTap(int pointerId, ActionType action)
        {
            MobileControlChannel channel = action == ActionType.Receive
                ? MobileControlChannel.Receive
                : MobileControlChannel.Set;
            if (Owns(pointerId, channel)) Release(pointerId);
        }

        public bool BeginSwipe(int pointerId, MobileSwipeAction action, Vector2 screenPosition)
        {
            MobileControlChannel channel = ChannelFor(action);
            if (swipePointer != NoPointer || !TryClaim(pointerId, channel)) return false;

            bool accepted;
            if (action == MobileSwipeAction.Attack)
            {
                accepted = Character && Character.AttackStarted(Vector2.zero);
            }
            else if (action == MobileSwipeAction.Receive || action == MobileSwipeAction.Set)
            {
                accepted = Character && Character.AimedActionStarted(ToAction(action), Vector2.zero);
            }
            else
            {
                accepted = BeginServe();
            }

            if (!accepted)
            {
                Release(pointerId);
                return false;
            }

            swipePointer = pointerId;
            swipeAction = action;
            swipeStart = swipeCurrent = screenPosition;
            return true;
        }

        public void HoldSwipe(int pointerId, Vector2 screenPosition)
        {
            if (!OwnsSwipe(pointerId)) return;
            swipeCurrent = screenPosition;
            Vector2 aim = CurrentAim;
            if (swipeAction == MobileSwipeAction.Attack) Character.AttackHeld(aim);
            else if (swipeAction == MobileSwipeAction.Receive || swipeAction == MobileSwipeAction.Set)
                Character.AimedActionHeld(ToAction(swipeAction), aim);
            else Character.Aim(aim);
        }

        public bool EndSwipe(int pointerId, Vector2 screenPosition)
        {
            if (!OwnsSwipe(pointerId)) return false;
            swipeCurrent = screenPosition;
            Vector2 aim = CurrentAim;
            bool accepted;
            if (swipeAction == MobileSwipeAction.Attack)
                accepted = Character.AttackReleased(aim);
            else if (swipeAction == MobileSwipeAction.Receive || swipeAction == MobileSwipeAction.Set)
                accepted = Character.AimedActionReleased(ToAction(swipeAction), aim);
            else
            {
                Character.Aim(aim);
                accepted = Character.RequestAction(ActionType.Serve);
            }

            swipePointer = NoPointer;
            Release(pointerId);
            return accepted;
        }

        public void CancelSwipe(int pointerId)
        {
            if (!OwnsSwipe(pointerId)) return;
            if (Character && (swipeAction == MobileSwipeAction.Receive || swipeAction == MobileSwipeAction.Set))
                Character.Actions.CancelAimedAction();
            swipePointer = NoPointer;
            Release(pointerId);
        }

        bool BeginServe()
        {
            if (!Character || !Match || Match.Rules == null ||
                Match.Rules.State != MatchState.ServePreparation ||
                Match.Rules.Server != Character.Team)
            {
                return false;
            }

            ServeStage stage = Character.CurrentServeStage;
            if (stage == ServeStage.Ready) return Character.RequestAction(ActionType.Serve);
            return stage == ServeStage.Tossed;
        }

        void Update()
        {
            if (!Character) return;
            if (movePointer != NoPointer) ApplyMovement();
            if (swipePointer == NoPointer) return;
            Vector2 aim = CurrentAim;
            if (swipeAction == MobileSwipeAction.Attack) Character.AttackHeld(aim);
            else if (swipeAction == MobileSwipeAction.Receive || swipeAction == MobileSwipeAction.Set)
                Character.AimedActionHeld(ToAction(swipeAction), aim);
            else Character.Aim(aim);
        }

        void ApplyMovement()
        {
            Vector3 forward = ViewCamera ? ViewCamera.transform.forward : Vector3.forward;
            forward.y = 0;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Character.Move(forward * moveInput.y + right * moveInput.x);
        }

        bool TryClaim(int pointerId, MobileControlChannel channel)
        {
            if (pointerOwners.ContainsKey(pointerId)) return false;
            pointerOwners.Add(pointerId, channel);
            return true;
        }

        bool Owns(int pointerId, MobileControlChannel channel)
        {
            return pointerOwners.TryGetValue(pointerId, out MobileControlChannel owner) && owner == channel;
        }

        bool OwnsSwipe(int pointerId)
        {
            MobileControlChannel channel = ChannelFor(swipeAction);
            return swipePointer == pointerId && Owns(pointerId, channel);
        }

        static ActionType ToAction(MobileSwipeAction action) => action == MobileSwipeAction.Receive ? ActionType.Receive : ActionType.Set;

        static MobileControlChannel ChannelFor(MobileSwipeAction action)
        {
            switch (action)
            {
                case MobileSwipeAction.Receive: return MobileControlChannel.Receive;
                case MobileSwipeAction.Set: return MobileControlChannel.Set;
                case MobileSwipeAction.Attack: return MobileControlChannel.Attack;
                default: return MobileControlChannel.Serve;
            }
        }

        void Release(int pointerId)
        {
            pointerOwners.Remove(pointerId);
        }

        void OnDisable()
        {
            if (Character)
            {
                Character.Move(Vector3.zero);
                Character.Actions.CancelAimedAction();
                if (swipePointer != NoPointer && swipeAction == MobileSwipeAction.Attack) Character.CancelAttack();
            }
            moveInput = Vector2.zero;
            movePointer = swipePointer = NoPointer;
            pointerOwners.Clear();
        }
    }
}
