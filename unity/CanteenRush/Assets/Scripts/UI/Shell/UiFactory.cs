using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
namespace CanteenRush.UI
{
    /// Builds the shell's world-space canvases in code (MASTER_SPEC §12): Render Mode World Space, scale 0.001
    /// (1 canvas unit = 1 mm), TrackedDeviceGraphicRaycaster so the XR ray can click, one font (the same
    /// built-in LegacyRuntime font P2's order UI uses), high contrast, no text under 24 units.
    /// Building in code keeps the prefabs trivial (no hand-merged UI YAML) and the layout readable in one file.
    public static class UiFactory
    {
        public const float CanvasScale = 0.001f;
        public const int BodySize = 44, TitleSize = 96, SmallSize = 32;   // >= 24 at canvas scale; 44 mm body ~ 3 cm cap height
        public static readonly Color Panel = new Color(0.07f, 0.08f, 0.11f, 0.92f);
        public static readonly Color Ink = new Color(0.97f, 0.97f, 0.95f);
        public static readonly Color Accent = new Color(1f, 0.72f, 0.18f);       // warm canteen amber
        public static readonly Color ButtonColor = new Color(0.16f, 0.42f, 0.78f);
        public static readonly Color ButtonHover = new Color(0.25f, 0.56f, 0.95f);

        static Font _font;
        public static Font Font => _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        public static Canvas Canvas(Transform parent, string name, Vector2 sizeMm)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 0;
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = sizeMm;
            rt.localScale = Vector3.one * CanvasScale;
            go.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 4;
            go.AddComponent<TrackedDeviceGraphicRaycaster>();
            var bg = go.AddComponent<Image>();
            bg.color = Panel;
            return canvas;
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin = default, Vector2 offsetMax = default)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            return rt;
        }

        public static Text Label(Transform parent, string name, string text, int size, TextAnchor align, Vector2 anchorMin, Vector2 anchorMax, Color? color = null)
        {
            var rt = Rect(parent, name, anchorMin, anchorMax);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font; t.fontSize = Mathf.Max(24, size); t.alignment = align;
            t.color = color ?? Ink; t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button Button(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick)
        {
            var rt = Rect(parent, name, anchorMin, anchorMax);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Color.white;
            var b = rt.gameObject.AddComponent<Button>();
            var colors = b.colors;
            colors.normalColor = ButtonColor; colors.highlightedColor = ButtonHover; colors.selectedColor = ButtonColor;
            colors.pressedColor = Accent; colors.fadeDuration = 0.05f;
            b.colors = colors;
            b.targetGraphic = img;
            if (onClick != null) b.onClick.AddListener(onClick);
            rt.gameObject.AddComponent<UiSoundHooks>();
            Label(rt, "Label", label, BodySize, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            return b;
        }

        public static void SetLabel(Button b, string text) { var t = b.GetComponentInChildren<Text>(); if (t != null) t.text = text; }

        /// One EventSystem with the XR UI Input Module (it also handles mouse/keyboard, so the desktop works too).
        public static void EnsureEventSystem()
        {
            var es = Object.FindFirstObjectByType<EventSystem>();
            if (es == null) { es = new GameObject("EventSystem").AddComponent<EventSystem>(); }
            if (es.GetComponent<XRUIInputModule>() != null) return;
            foreach (var m in es.GetComponents<BaseInputModule>()) m.enabled = false;
            es.gameObject.AddComponent<XRUIInputModule>();
        }
    }
}
