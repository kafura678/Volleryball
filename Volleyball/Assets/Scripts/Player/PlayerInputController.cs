using UnityEngine;
using UnityEngine.InputSystem;

namespace Volleyball
{
    public sealed class PlayerInputController : MonoBehaviour
    {
        public InputActionAsset InputAsset;
        public Camera ViewCamera;
        public VolleyballCharacterController Character;

        static readonly ActionType[] PerformedActions =
        {
            ActionType.Serve
        };

        InputActionAsset ownedAsset;
        InputActionMap map;
        InputAction attackAction;

        public InputActionMap GameplayMap => map;

        void OnEnable()
        {
            if (!InputAsset) return;
            ownedAsset = Instantiate(InputAsset);
            map = ownedAsset.FindActionMap("Volleyball", true);
            foreach (ActionType action in PerformedActions)
            {
                map.FindAction(action.ToString()).performed += OnAction;
            }

            foreach (ActionType action in AimedActions)
            {
                map.FindAction(action.ToString()).started += OnAimedStarted;
                map.FindAction(action.ToString()).canceled += OnAimedReleased;
            }
            attackAction = map.FindAction(ActionType.Attack.ToString(), true);
            attackAction.started += OnAttackStarted;
            attackAction.canceled += OnAttackReleased;
            map.Enable();
        }

        void Update()
        {
            if (map == null || !Character) return;
            Vector2 move = ReadMove();
            Vector3 forward = ViewCamera ? ViewCamera.transform.forward : Vector3.forward;
            forward.y = 0;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Character.Move(forward * move.y + right * move.x);
            Character.Aim(move);

            foreach (ActionType action in AimedActions)
                if (map.FindAction(action.ToString()).IsPressed()) Character.AimedActionHeld(action, MobileInputMath.ScreenAimToCourtAim(move, Character.Team));

            if (attackAction != null && attackAction.IsPressed())
            {
                Character.AttackHeld(move);
            }
        }

        static readonly ActionType[] AimedActions = { ActionType.Receive, ActionType.Set };

        void OnAimedStarted(InputAction.CallbackContext context)
        {
            if (Character && System.Enum.TryParse(context.action.name, out ActionType action))
                Character.AimedActionStarted(action, MobileInputMath.ScreenAimToCourtAim(ReadMove(), Character.Team));
        }

        void OnAimedReleased(InputAction.CallbackContext context)
        {
            if (Character && System.Enum.TryParse(context.action.name, out ActionType action))
                Character.AimedActionReleased(action, MobileInputMath.ScreenAimToCourtAim(ReadMove(), Character.Team));
        }

        Vector2 ReadMove()
        {
            return map.FindAction("Move", true).ReadValue<Vector2>();
        }

        void OnAction(InputAction.CallbackContext context)
        {
            if (Character && System.Enum.TryParse(context.action.name, out ActionType action))
            {
                Character.RequestAction(action);
            }
        }

        void OnAttackStarted(InputAction.CallbackContext context)
        {
            if (Character) Character.AttackStarted(ReadMove());
        }

        void OnAttackReleased(InputAction.CallbackContext context)
        {
            if (Character) Character.AttackReleased(ReadMove());
        }

        void OnDisable()
        {
            if (map != null)
            {
                foreach (ActionType action in PerformedActions)
                {
                    map.FindAction(action.ToString()).performed -= OnAction;
                }

                if (attackAction != null)
                {
                    attackAction.started -= OnAttackStarted;
                    attackAction.canceled -= OnAttackReleased;
                }

                foreach (ActionType action in AimedActions)
                {
                    map.FindAction(action.ToString()).started -= OnAimedStarted;
                    map.FindAction(action.ToString()).canceled -= OnAimedReleased;
                }
                map.Disable();
            }

            map = null;
            attackAction = null;
            if (Character)
            {
                Character.Move(Vector3.zero);
                Character.CancelAttack();
                Character.Actions.CancelAimedAction();
            }
            if (ownedAsset) Destroy(ownedAsset);
        }
    }
}
