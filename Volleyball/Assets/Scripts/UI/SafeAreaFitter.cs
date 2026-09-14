using UnityEngine;

namespace Volleyball
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        RectTransform rectTransform;
        Rect lastSafeArea;
        Vector2Int lastScreenSize;

        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            Apply();
        }

        void Update()
        {
            if (lastSafeArea != Screen.safeArea ||
                lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height)
            {
                Apply();
            }
        }

        void Apply()
        {
            Rect safeArea = Screen.safeArea;
            Rect normalizedArea = Normalize(safeArea, new Vector2Int(Screen.width, Screen.height));
            rectTransform.anchorMin = normalizedArea.min;
            rectTransform.anchorMax = normalizedArea.max;
            rectTransform.offsetMin = rectTransform.offsetMax = Vector2.zero;
            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        }

        public static Rect Normalize(Rect safeArea, Vector2Int screenSize)
        {
            float width = Mathf.Max(1, screenSize.x);
            float height = Mathf.Max(1, screenSize.y);
            float xMin = Mathf.Clamp01(safeArea.xMin / width);
            float yMin = Mathf.Clamp01(safeArea.yMin / height);
            float xMax = Mathf.Clamp(safeArea.xMax / width, xMin, 1f);
            float yMax = Mathf.Clamp(safeArea.yMax / height, yMin, 1f);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
    }
}
