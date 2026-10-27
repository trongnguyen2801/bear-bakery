// Code-built uGUI helpers (logical px, reference width 390).
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BB.View;

namespace BB.UI
{
    public static class Pal
    {
        public static readonly Color Cream = new Color32(255, 248, 236, 255);
        public static readonly Color CreamDark = new Color32(246, 231, 205, 255);
        public static readonly Color Brown = new Color32(122, 74, 48, 255);
        public static readonly Color BrownSoft = new Color32(160, 110, 80, 255);
        public static readonly Color Orange = new Color32(246, 150, 84, 255);
        public static readonly Color Green = new Color32(113, 186, 99, 255);
        public static readonly Color GreenDark = new Color32(70, 140, 70, 255);
        public static readonly Color Red = new Color32(228, 92, 82, 255);
        public static readonly Color Grey = new Color32(170, 160, 150, 255);
        public static readonly Color Shade = new Color(0.18f, 0.1f, 0.05f, 0.45f);
        public static readonly Color White = Color.white;
        public static readonly Color Gold = new Color32(255, 205, 80, 255);
    }

    public static class K
    {
        public static RectTransform Rect(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(Transform parent, string name, float l = 0, float r = 0, float t = 0, float b = 0)
        {
            var rt = Rect(parent, name, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        public static RectTransform At(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
            => Rect(parent, name, anchor, anchor, pivot ?? new Vector2(0.5f, 0.5f), pos, size);

        public static Image Img(RectTransform rt, string sprite, Color? color = null, bool sliced = false, bool raycast = false, bool preserve = true)
        {
            var im = rt.gameObject.AddComponent<Image>();
            if (sprite != null) im.sprite = Art.S(sprite);
            im.color = color ?? Color.white;
            im.raycastTarget = raycast;
            if (sliced && im.sprite != null && im.sprite.border.sqrMagnitude > 0) { im.type = Image.Type.Sliced; im.pixelsPerUnitMultiplier = 1.4f; }
            else im.preserveAspect = preserve && sprite != null;
            return im;
        }

        public static Image Pic(Transform parent, string name, string sprite, Vector2 anchor, Vector2 pos, Vector2 size, Color? color = null, bool sliced = false)
            => Img(At(parent, name, anchor, pos, size), sprite, color, sliced);

        public static Text Txt(Transform parent, string name, string s, int size, Color color, TextAnchor align, Vector2 anchor, Vector2 pos, Vector2 box, bool bold = false, bool num = false, bool outline = false)
        {
            var rt = At(parent, name, anchor, pos, box);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = num ? Art.Num : Art.Vi;
            t.text = s; t.fontSize = size; t.color = color; t.alignment = align;
            t.fontStyle = bold && !num ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            if (outline) { var o = rt.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0.35f, 0.2f, 0.1f, 0.85f); o.effectDistance = new Vector2(1.4f, -1.4f); }
            return t;
        }

        public static Text TxtStretch(Transform parent, string s, int size, Color color, TextAnchor align, bool bold = false, float pad = 0)
        {
            var rt = Stretch(parent, "Text", pad, pad, 0, 0);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Art.Vi; t.text = s; t.fontSize = size; t.color = color; t.alignment = align;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false;
            return t;
        }

        /// <summary>Button: an Image (raycast) + Press feedback. Min touch size enforced by caller (>= 44).</summary>
        public static Button Btn(RectTransform rt, string sprite, Action onClick, Color? color = null, bool sliced = false)
        {
            var im = Img(rt, sprite, color, sliced, true, !sliced);
            if (sprite == null) im.color = new Color(1, 1, 1, 0.001f);
            var b = rt.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.targetGraphic = im;
            b.onClick.AddListener(() => { AudioMan.Click(); onClick?.Invoke(); });
            rt.gameObject.AddComponent<Press>();
            return b;
        }

        /// <summary>Pill-shaped text button with optional coin price.</summary>
        public static Button PillButton(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, string label, Color bg, Action onClick, out Text text, int fontSize = 16)
        {
            var rt = At(parent, name, anchor, pos, size);
            var b = Btn(rt, "ui.btnBasic", onClick, bg, true);
            text = Txt(rt, "Label", label, fontSize, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 1), size, true, false, true);
            return b;
        }
    }

    /// <summary>Small scale-down on press for every button.</summary>
    public class Press : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        Vector3 baseScale = Vector3.one; bool down;
        void Awake() { baseScale = transform.localScale; }
        public void OnPointerDown(PointerEventData e) { down = true; transform.localScale = baseScale * 0.92f; }
        public void OnPointerUp(PointerEventData e) { if (!down) return; down = false; Tw.Pop(transform, 0.92f, 0.18f, baseScale); }
        public void OnPointerExit(PointerEventData e) { if (!down) return; down = false; transform.localScale = baseScale; }
    }

    /// <summary>Generic progress bar built from the source gauge sprites.</summary>
    public class Bar
    {
        public RectTransform root; public Image fill; public Image bg;
        public static Bar Make(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color fillColor)
        {
            var b = new Bar();
            b.root = K.At(parent, name, anchor, pos, size);
            b.bg = K.Img(b.root, "ui.whiteRound", new Color(0.35f, 0.22f, 0.12f, 0.55f), false, false, false);
            var f = K.Stretch(b.root, "Fill", 2, 2, 2, 2);
            b.fill = K.Img(f, "ui.whiteRound", fillColor, false, false, false);
            b.fill.type = Image.Type.Filled; b.fill.fillMethod = Image.FillMethod.Horizontal; b.fill.fillOrigin = 0;
            return b;
        }
        public void Set(float v) { fill.fillAmount = Mathf.Clamp01(v); }
    }
}
