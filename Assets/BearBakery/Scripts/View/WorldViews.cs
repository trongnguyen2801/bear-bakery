// Per-entity renderers. They read model state every frame and play cosmetic animations on model events.
using System.Collections.Generic;
using BB.Model;
using BB.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace BB.View
{
    public static class SR
    {
        public static SpriteRenderer Make(Transform parent, string name, string id, Vector2 pos, int order, float scale = 1f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0);
            go.transform.localScale = new Vector3(scale, scale, 1);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Art.S(id);
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>Place a centre-pivot sprite so its bottom edge sits on 'bottom' (feet/ground anchoring).</summary>
        public static SpriteRenderer Bottom(Transform parent, string name, string id, Vector2 bottom, int? order = null, float scale = 1f)
        {
            var sr = Make(parent, name, id, bottom, 0, scale);
            float minY = sr.sprite.bounds.min.y * scale;
            sr.transform.position = new Vector3(bottom.x, bottom.y - minY, 0);
            sr.sortingOrder = order ?? WL.SortOrder(bottom.y);
            return sr;
        }

        public static SpriteRenderer Tiled(Transform parent, string name, string id, Vector2 center, Vector2 size, int order, Color? color = null)
        {
            var sr = Make(parent, name, id, center, order);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = size;
            // tiled area is laid out around the sprite pivot: compensate so 'center' is the real centre
            var sp = sr.sprite;
            var piv = new Vector2(sp.pivot.x / sp.rect.width, sp.pivot.y / sp.rect.height);
            sr.transform.position = new Vector3(center.x + (piv.x - 0.5f) * size.x, center.y + (piv.y - 0.5f) * size.y, 0);
            if (color.HasValue) sr.color = color.Value;
            return sr;
        }

        public static SpriteRenderer Stretched(Transform parent, string name, string id, Vector2 center, Vector2 size, int order, Color color)
        {
            var sr = Make(parent, name, id, center, order);
            var b = sr.sprite.bounds.size;
            sr.transform.localScale = new Vector3(size.x / b.x, size.y / b.y, 1);
            sr.color = color;
            return sr;
        }
    }

    // ------------------------------------------------------------------ board slot + dough
    public class SlotView
    {
        public int index;
        public SpriteRenderer table, dough;
        public long shownId; public int shownLevel;
        public bool hiddenForDrag;
        public RectTransform badge; public Text badgeText; public RectTransform buy; public Text buyText; public Image buyIcon;
        public Vector2 Pos => WL.Slot(index);
        public Vector2 DoughPos => WL.DoughOnSlot(index);
    }

    // ------------------------------------------------------------------ oven
    public class OvenView
    {
        public int index;
        public Transform root;
        public SpriteRenderer body, doorClosed, doorOpen, fire;
        public int shownState = -1; public bool shownOwned;
        public float smokeT;
        public RectTransform bubble; public Image bubbleBg, gauge, icon, check; public Text time;
        public RectTransform tag; public Text tagText; public Image tagIcon;
        public RectTransform warn; public Text warnText;
        public Vector2 Pos => WL.Ovens[index];
        public Vector2 Mouth => WL.Ovens[index] + new Vector2(0f, -0.3f);
        public Vector2 Chimney => WL.Ovens[index] + new Vector2(-0.013f, 0.83f);
    }

    // ------------------------------------------------------------------ shelf
    public class ShelfView
    {
        public int index;
        public SpriteRenderer stand;
        public readonly List<SpriteRenderer> breads = new List<SpriteRenderer>();
        public int shownStock = -1, shownRecipe = -1; public bool shownOwned;
        public RectTransform label; public Text labelText; public Image autoIcon;
        public RectTransform plus;
        public RectTransform tag; public Text tagText; public Image tagIcon;
        public Vector2 Pos => WL.Shelves[index];
    }

    // ------------------------------------------------------------------ furniture
    public class FurnView
    {
        public string id;
        public SpriteRenderer sr;
        public bool shownOwned; public int shownLevel;
        public RectTransform tag; public Text tagText; public Image tagIcon;
        public float steamT;
    }

    // ------------------------------------------------------------------ staff
    public class StaffView
    {
        public string id; public int index;
        public Transform root; public FrameAnim anim; public SortingGroup group;
        public SpriteRenderer shadow, extra;  // extra = bread basket for cashier
        public Vector2 station;
        public string idleKey, workKey, fastKey, restKey;
        public float fastUntil;
        public int shownState = -1;
        public bool inBreakRoom;
        public RectTransform energy; public Bar energyBar; public Image moodIcon;
        public RectTransform zzz;
        public RectTransform boostTag;
        public float hopT, sparkleT;
    }

    // ------------------------------------------------------------------ street / counter NPCs (delivery dog, cleaner raccoon)
    public class NpcView
    {
        public Transform root; public FrameAnim anim; public SortingGroup group;
        public SpriteRenderer shadow, carry;
        public Vector2 pos;
        public RectTransform bubble; public Image bubbleIcon; public Text bubbleText; public Image bubbleCheck;
        public RectTransform status; public Text statusText;
        public Hit hit;
        public float alpha = 1, dustT;
    }

    // ------------------------------------------------------------------ customer
    public class CustomerView
    {
        public long id; public string type;
        public Transform root; public FrameAnim anim; public SortingGroup group;
        public SpriteRenderer shadow, tray, trayBread, bag;
        public CPhase shownPhase = (CPhase)(-1);
        public WalkPath path; public float pathT;
        public Vector2 pos; public float holdH;
        public int shownQueue = -1; public Vector2 queueFrom, queueTo; public float queueMoveT = 1;
        public bool paid;
        public float alpha = 1;
        public RectTransform bubble;
        public float bubbleUntil;
    }
}
