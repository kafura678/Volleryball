using UnityEngine;
using UnityEngine.UI;

namespace Volleyball
{
    public sealed class OnlineMenuInstaller : MonoBehaviour
    {
        public OnlineSessionController Controller;

        GameObject panel;
        Button openButton;
        Button hostButton;
        Button joinButton;
        Button disconnectButton;
        InputField joinCodeInput;
        Text joinCodeText;
        Text statusText;

        void Start()
        {
            BuildUi();
            ResolveController();
            if (Controller) Controller.Changed += Refresh;
            Refresh();
        }

        void ResolveController()
        {
            if (!Controller) Controller = OnlineSessionController.Instance;
            if (!Controller)
            {
                OnlineSessionController[] controllers = Resources.FindObjectsOfTypeAll<OnlineSessionController>();
                if (controllers.Length > 0) Controller = controllers[0];
            }
        }

        void BuildUi()
        {
            if (GameObject.Find("OnlineMenuCanvas")) return;
            var canvasObject = new GameObject("OnlineMenuCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            openButton = CreateButton("OnlineOpenButton", "ONLINE", canvasObject.transform,
                new Vector2(0.5f, 1), new Vector2(0, -38), new Vector2(150, 54));
            openButton.onClick.AddListener(TogglePanel);

            panel = CreateImage("OnlinePanel", canvasObject.transform, new Vector2(0.5f, 1),
                new Vector2(0, -210), new Vector2(430, 275), new Color(0.04f, 0.08f, 0.14f, 0.92f));
            hostButton = CreateButton("HostMatchButton", "HOST MATCH", panel.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0, 92), new Vector2(185, 46));
            hostButton.onClick.AddListener(Host);

            joinCodeInput = CreateInputField(panel.transform, new Vector2(-64, 34), new Vector2(210, 43));
            joinButton = CreateButton("JoinMatchButton", "JOIN", panel.transform,
                new Vector2(0.5f, 0.5f), new Vector2(130, 34), new Vector2(105, 43));
            joinButton.onClick.AddListener(Join);

            joinCodeText = CreateText("JoinCodeText", panel.transform, "Join Code: -",
                new Vector2(0, -25), new Vector2(390, 38), 22);
            statusText = CreateText("ConnectionStatus", panel.transform, "Offline CPU Match",
                new Vector2(0, -67), new Vector2(390, 48), 17);
            disconnectButton = CreateButton("DisconnectButton", "DISCONNECT", panel.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0, -112), new Vector2(185, 42));
            disconnectButton.onClick.AddListener(Disconnect);
            panel.SetActive(false);
        }

        void TogglePanel()
        {
            panel.SetActive(!panel.activeSelf);
            Refresh();
        }

        async void Host()
        {
            ResolveController();
            if (Controller) await Controller.HostMatchAsync();
        }

        async void Join()
        {
            ResolveController();
            if (Controller) await Controller.JoinMatchAsync(joinCodeInput.text);
        }

        async void Disconnect()
        {
            ResolveController();
            if (Controller) await Controller.DisconnectAsync();
        }

        void Refresh()
        {
            if (!Controller) ResolveController();
            if (!Controller || !statusText) return;
            statusText.text = Controller.Status + (Controller.ConnectedPlayerCount > 0
                ? "\nPlayers: " + Controller.ConnectedPlayerCount + " / 2"
                : string.Empty);
            joinCodeText.text = string.IsNullOrEmpty(Controller.JoinCode)
                ? "Join Code: -"
                : "Join Code: " + Controller.JoinCode;
            bool busy = Controller.IsBusy;
            hostButton.interactable = !busy && Controller.Mode == OnlineMode.Offline;
            joinButton.interactable = !busy && Controller.Mode == OnlineMode.Offline;
            joinCodeInput.interactable = !busy && Controller.Mode == OnlineMode.Offline;
            disconnectButton.interactable = !busy && Controller.Mode != OnlineMode.Offline;
        }

        static GameObject CreateImage(string name, Transform parent, Vector2 anchor, Vector2 position,
            Vector2 size, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            gameObject.GetComponent<Image>().color = color;
            return gameObject;
        }

        static Button CreateButton(string name, string label, Transform parent, Vector2 anchor,
            Vector2 position, Vector2 size)
        {
            GameObject gameObject = CreateImage(name, parent, anchor, position, size,
                new Color(0.08f, 0.55f, 0.9f, 0.9f));
            Button button = gameObject.AddComponent<Button>();
            CreateText("Label", gameObject.transform, label, Vector2.zero, size, 18);
            return button;
        }

        static InputField CreateInputField(Transform parent, Vector2 position, Vector2 size)
        {
            GameObject gameObject = CreateImage("JoinCodeInput", parent, Vector2.one * 0.5f,
                position, size, new Color(1, 1, 1, 0.96f));
            Text text = CreateText("Text", gameObject.transform, string.Empty, Vector2.zero,
                size - new Vector2(18, 6), 20);
            text.color = Color.black;
            Text placeholder = CreateText("Placeholder", gameObject.transform, "JOIN CODE",
                Vector2.zero, size - new Vector2(18, 6), 17);
            placeholder.color = new Color(0.3f, 0.3f, 0.3f, 0.65f);
            InputField field = gameObject.AddComponent<InputField>();
            field.textComponent = text;
            field.placeholder = placeholder;
            field.characterLimit = 12;
            field.lineType = InputField.LineType.SingleLine;
            return field;
        }

        static Text CreateText(string name, Transform parent, string value, Vector2 position,
            Vector2 size, int fontSize)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Text text = gameObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = value;
            text.raycastTarget = false;
            return text;
        }

        void OnDestroy()
        {
            if (Controller) Controller.Changed -= Refresh;
        }
    }
}
