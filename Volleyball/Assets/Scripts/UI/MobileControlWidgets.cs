using UnityEngine;
using UnityEngine.EventSystems;

namespace Volleyball
{
    public sealed class MobileVirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public MobileInputController Input;
        public RectTransform Background;
        public RectTransform Knob;

        int activePointer = int.MinValue;
        Canvas inputCanvas;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Input || activePointer != int.MinValue || !Input.BeginMove(eventData.pointerId)) return;
            activePointer = eventData.pointerId;
            UpdatePointer(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == activePointer) UpdatePointer(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointer) return;
            Input.EndMove(activePointer);
            activePointer = int.MinValue;
            if (Knob) Knob.anchoredPosition = Vector2.zero;
        }

        void UpdatePointer(PointerEventData eventData)
        {
            if (!Background) return;
            if (!inputCanvas) inputCanvas = Background.GetComponentInParent<Canvas>();
            Camera eventCamera = inputCanvas && inputCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? inputCanvas.worldCamera
                : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    Background, eventData.position, eventCamera, out Vector2 localPoint)) return;

            float radius = InputRadius(Background.rect);
            Vector2 normalized = NormalizedInput(Background.rect, localPoint);
            Input.SetMove(activePointer, normalized);
            if (Knob) Knob.anchoredPosition = normalized * radius;
        }

        public static Vector2 NormalizedInput(Rect backgroundRect, Vector2 localPoint)
        {
            Vector2 fromCenter = localPoint - backgroundRect.center;
            return Vector2.ClampMagnitude(fromCenter / InputRadius(backgroundRect), 1f);
        }

        static float InputRadius(Rect backgroundRect)
        {
            return Mathf.Max(1f, Mathf.Min(backgroundRect.width, backgroundRect.height) * 0.42f);
        }
    }

    // Keep the existing component and button layout; Receive / Set now use the shared swipe lifecycle.
    public sealed class MobileTapButton : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public MobileInputController Input;
        public ActionType Action;
        int activePointer = int.MinValue;
        MobileSwipeAction SwipeAction => Action == ActionType.Receive ? MobileSwipeAction.Receive : MobileSwipeAction.Set;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Input || activePointer != int.MinValue ||
                !Input.BeginSwipe(eventData.pointerId, SwipeAction, eventData.position)) return;
            activePointer = eventData.pointerId;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == activePointer) Input.HoldSwipe(activePointer, eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointer) return;
            Input.EndSwipe(activePointer, eventData.position);
            activePointer = int.MinValue;
        }

        void OnDisable()
        {
            if (Input && activePointer != int.MinValue) Input.CancelSwipe(activePointer);
            activePointer = int.MinValue;
        }
    }

    public sealed class MobileSwipeArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public MobileInputController Input;
        public MobileSwipeAction Action;

        int activePointer = int.MinValue;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Input || activePointer != int.MinValue ||
                !Input.BeginSwipe(eventData.pointerId, Action, eventData.position)) return;
            activePointer = eventData.pointerId;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == activePointer) Input.HoldSwipe(activePointer, eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointer) return;
            Input.EndSwipe(activePointer, eventData.position);
            activePointer = int.MinValue;
        }
    }
}
