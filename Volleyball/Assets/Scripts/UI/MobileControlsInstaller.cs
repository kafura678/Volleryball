using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Volleyball
{
    public sealed class MobileControlsInstaller : MonoBehaviour
    {
        public MatchController Match;
        public MobileInputController Input;
        public bool ShowMobileControlsInEditor = true;

        GameObject mobileRoot;
        GameObject receiveButton;
        GameObject setButton;
        GameObject attackArea;
        GameObject serveArea;

        void Start()
        {
            if (!Match || !Input) return;
            bool show = WebPlatformPolicy.ShowControls(WebPlatformPolicy.IsWeb, WebPlatformPolicy.IsTouchBrowser,
                Application.isMobilePlatform, Application.isEditor, ShowMobileControlsInEditor);
            if (!show) return;
            EnsureInputSystemEventSystem();
            BuildUi();
            RefreshContext();
        }

        void Update()
        {
            if (mobileRoot) RefreshContext();
        }

        void EnsureInputSystemEventSystem()
        {
            EventSystem eventSystem = EventSystem.current;
            if (!eventSystem)
            {
                eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            }
            if (!eventSystem.GetComponent<InputSystemUIInputModule>())
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
        }

        void BuildUi()
        {
            mobileRoot = new GameObject(
                "MobileControlsCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Canvas canvas = mobileRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            CanvasScaler scaler = mobileRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject safeRoot = new GameObject("MobileSafeAreaRoot", typeof(RectTransform), typeof(SafeAreaFitter));
            safeRoot.transform.SetParent(mobileRoot.transform, false);
            RectTransform safeRect = safeRoot.GetComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.offsetMin = safeRect.offsetMax = Vector2.zero;

            CreateJoystick(safeRect);
            receiveButton = CreateTapButton("MobileReceiveButton", "RECEIVE", ActionType.Receive,
                safeRect, new Vector2(1, 0), new Vector2(-110, 280), new Vector2(170, 76), new Color(0.1f, 0.7f, 0.85f, 0.55f));
            setButton = CreateTapButton("MobileSetButton", "SET", ActionType.Set,
                safeRect, new Vector2(1, 0), new Vector2(-110, 370), new Vector2(170, 76), new Color(0.3f, 0.65f, 1f, 0.55f));
            attackArea = CreateSwipeArea("MobileAttackArea", "HOLD / SWIPE / RELEASE\nATTACK", MobileSwipeAction.Attack,
                safeRect, new Vector2(1, 0), new Vector2(-230, 120), new Vector2(290, 170), new Color(1f, 0.3f, 0.18f, 0.48f));
            serveArea = CreateSwipeArea("MobileServeArea", "HOLD / SWIPE / RELEASE\nSERVE", MobileSwipeAction.Serve,
                safeRect, new Vector2(1, 0), new Vector2(-230, 120), new Vector2(290, 170), new Color(1f, 0.65f, 0.12f, 0.52f));
        }

        void CreateJoystick(RectTransform parent)
        {
            GameObject background = CreateImage("MobileJoystick", parent, new Vector2(0, 0),
                new Vector2(145, 145), new Vector2(220, 220), new Color(0.15f, 0.75f, 0.8f, 0.28f));
            GameObject knob = CreateImage("MobileJoystickKnob", background.transform, Vector2.one * 0.5f,
                Vector2.zero, new Vector2(86, 86), new Color(0.25f, 0.95f, 1f, 0.62f));
            var joystick = background.AddComponent<MobileVirtualJoystick>();
            joystick.Input = Input;
            joystick.Background = background.GetComponent<RectTransform>();
            joystick.Knob = knob.GetComponent<RectTransform>();
            AddLabel("MOVE", background.transform, new Vector2(0, -132), new Vector2(180, 34), 19);
        }

        GameObject CreateTapButton(string name, string label, ActionType action, RectTransform parent,
            Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            GameObject button = CreateImage(name, parent, anchor, position, size, color);
            var tap = button.AddComponent<MobileTapButton>();
            tap.Input = Input;
            tap.Action = action;
            AddLabel(label, button.transform, Vector2.zero, size, 20);
            return button;
        }

        GameObject CreateSwipeArea(string name, string label, MobileSwipeAction action, RectTransform parent,
            Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            GameObject area = CreateImage(name, parent, anchor, position, size, color);
            var swipe = area.AddComponent<MobileSwipeArea>();
            swipe.Input = Input;
            swipe.Action = action;
            AddLabel(label, area.transform, Vector2.zero, size, 18);
            return area;
        }

        static GameObject CreateImage(string name, Transform parent, Vector2 anchor,
            Vector2 position, Vector2 size, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            gameObject.GetComponent<Image>().color = color;
            return gameObject;
        }

        static void AddLabel(string text, Transform parent, Vector2 position, Vector2 size, int fontSize)
        {
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(parent, false);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Text label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = text;
            label.raycastTarget = false;
        }

        void RefreshContext()
        {
            if (Match.Rules == null) return;
            bool playing = Match.Rules.State == MatchState.Playing;
            bool humanServe = Match.Rules.State == MatchState.ServePreparation && Match.Rules.Server == Match.Human.Team;
            receiveButton.SetActive(playing);
            setButton.SetActive(playing);
            attackArea.SetActive(playing);
            serveArea.SetActive(humanServe);
        }
    }
}
