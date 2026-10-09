using System.Runtime.InteropServices;
using UnityEngine;

namespace Volleyball
{
    public static class WebPlatformPolicy
    {
        public static bool IsWeb => Application.platform == RuntimePlatform.WebGLPlayer;
        public static bool IsTouchBrowser
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return VolleyballHasTouch() != 0;
#else
                return false;
#endif
            }
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int VolleyballHasTouch();
#endif

        public static bool ShowControls(bool web, bool touch, bool mobile, bool editor, bool editorControls)
            => web ? touch : mobile || (editor && editorControls);

        public static bool NeedsLandscape(bool touch, int width, int height) => touch && height > width;

        public static Rect SafeArea(Rect area, bool webTouch)
        {
            if (!webTouch) return area;
            // CSS safe-area insets protect the browser canvas; retain a small interior margin
            // when browsers report the entire canvas as Screen.safeArea.
            return Rect.MinMaxRect(Mathf.Max(area.xMin, .02f), Mathf.Max(area.yMin, .02f),
                Mathf.Min(area.xMax, .98f), Mathf.Min(area.yMax, .98f));
        }
    }
}
