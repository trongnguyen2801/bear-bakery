// Zone camera (portrait frame, side-arrow navigation) and pointer routing (single primary pointer, drag & drop, taps).
using System;
using BB.Model;
using BB.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BB.View
{
    /// <summary>
    /// Zone camera: each area (kitchen, shop, break room) is framed to fit the screen width between HUD and bottom bar.
    /// Areas switch with the side arrows (horizontal slide). The shop is taller than one screen: it scrolls vertically
    /// (drag / wheel / hint pill) from the selling floor down to the marketing street.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public static CameraRig I;
        public Camera cam;
        public Rect frame;              // portrait frame in screen pixels
        public float HalfH => cam.orthographicSize;
        public bool Dragging => scrollDrag;
        public const float RefW = 390f, RefH = 844f;

        public static readonly string[] ZoneIds = { "kitchen", "shop", "break" };
        public static readonly string[] ZoneNames = { "Bếp", "Tiệm", "Phòng nghỉ" };
        static readonly float[] ZoneTop = { WL.KitchenTop, WL.ShopTop, WL.BreakTop };
        static readonly float[] ZoneBottom = { WL.KitchenBottom, WL.WorldBottom, WL.BreakBottom };
        static readonly float[] FitBottom = { WL.KitchenBottom, WL.FacadeBottom + 0.4f, WL.BreakBottom };   // must fit; the rest scrolls
        public int ZoneIndex { get; private set; } = 0;
        public bool Transitioning => slideT >= 0;
        public event Action<int> OnZoneChanged;

        int fromZone, toZone; float slideT = -1f; int slideDir;
        const float SlideTime = 0.36f;
        readonly float[] scroll = new float[3];     // 0 = top of the zone, 1 = bottom
        bool scrollDrag; float scrollVel; float scrollAnimTo = -1f;

        public void Init()
        {
            I = this;
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(214, 176, 140, 255);
            Apply();
            Place(ZoneIndex, 0);
        }

        public static Rect ComputeFrame(int w, int h)
        {
            float aspect = (float)w / Mathf.Max(1, h);
            if (aspect > 0.62f) { float fw = h * (RefW / RefH); return new Rect((w - fw) / 2f, 0, fw, h); }
            return new Rect(0, 0, w, h);
        }

        public void Apply()
        {
            frame = ComputeFrame(Screen.width, Screen.height);
            cam.rect = new Rect(frame.x / Screen.width, frame.y / Screen.height, frame.width / Screen.width, frame.height / Screen.height);
        }

        float TopPx => UIRoot.I != null ? UIRoot.I.TopInsetPx : 120f;
        float BotPx => UIRoot.I != null ? UIRoot.I.BottomInsetPx : 96f;

        /// <summary>Ortho size that fits the zone width (7.2u) and the must-fit height inside the free area.</summary>
        float OrthoFor(int z)
        {
            float aspect = frame.width / Mathf.Max(1f, frame.height);
            float widthFit = WL.Width / aspect / 2f;
            float freeFrac = Mathf.Clamp(1f - (TopPx + BotPx) / Mathf.Max(1f, frame.height), 0.3f, 1f);
            float h = ZoneTop[z] - FitBottom[z];
            float heightFit = h / 2f / freeFrac;
            return Mathf.Max(widthFit, heightFit);
        }

        float Upp(float ortho) => 2f * ortho / Mathf.Max(1f, frame.height);
        float CamYTop(int z, float ortho) => ZoneTop[z] - ortho + TopPx * Upp(ortho);
        float CamYBottom(int z, float ortho) => ZoneBottom[z] + ortho - BotPx * Upp(ortho);

        /// <summary>World units the zone can scroll (0 = fits on screen).</summary>
        public float ScrollRange(int z) { float o = OrthoFor(z); return Mathf.Max(0f, CamYTop(z, o) - CamYBottom(z, o)); }
        public bool CanScroll => !Transitioning && ScrollRange(ZoneIndex) > 0.05f;
        public float ScrollFrac => scroll[ZoneIndex];
        /// <summary>Shop sub-area under the camera: selling floor or marketing street.</summary>
        public string ShopSub() => ZoneIndex == 1 && scroll[1] > 0.5f ? "marketing" : "selling";

        float CamY(int z, float ortho)
        {
            float top = CamYTop(z, ortho), bot = CamYBottom(z, ortho);
            if (bot >= top) return top;            // fits: hug the HUD
            return Mathf.Lerp(top, bot, scroll[z]);
        }

        void Place(int z, float xOffset)
        {
            cam.orthographicSize = OrthoFor(z);
            cam.transform.position = new Vector3(xOffset, CamY(z, cam.orthographicSize), -10);
        }

        public bool CanGo(int dir) => !Transitioning && ZoneIndex + dir >= 0 && ZoneIndex + dir < ZoneIds.Length;

        public void Step(int dir) { if (CanGo(dir)) GoToZone(ZoneIndex + dir); }

        public void GoToZone(int z, bool instant = false)
        {
            z = Mathf.Clamp(z, 0, ZoneIds.Length - 1);
            scrollVel = 0; scrollDrag = false;
            if (instant || z == ZoneIndex && !Transitioning)
            {
                slideT = -1; ZoneIndex = z; Place(z, 0);
                OnZoneChanged?.Invoke(z);
                return;
            }
            if (Transitioning) return;
            fromZone = ZoneIndex; toZone = z; slideDir = z > ZoneIndex ? 1 : -1; slideT = 0;
            AudioMan.Play("se_Etc_Click", 0.4f);
        }

        /// <summary>Focus a world y: switches zone, and in the shop scrolls to the selling floor or the marketing street.</summary>
        public void Focus(float worldY, bool instant = false)
        {
            int z = ZoneOfY(worldY);
            if (z == 1)
            {
                float target = worldY < -27.2f ? 1f : 0f;
                if (z != ZoneIndex || instant) scroll[1] = target; else ScrollTo(target);
            }
            if (z != ZoneIndex || instant) GoToZone(z, instant);
        }
        public static int ZoneOfY(float y) => y > WL.KitchenTop ? 2 : y > WL.ShopTop ? 0 : 1;

        public float ViewCenterY() => cam.transform.position.y;
        public string Zone() => ZoneIds[ZoneIndex];

        // ---------------------------------------------------------------- vertical scrolling (shop)
        public void ScrollTo(float frac, bool instant = false)
        {
            frac = Mathf.Clamp01(frac);
            scrollVel = 0;
            if (instant) { scroll[ZoneIndex] = frac; scrollAnimTo = -1; } else scrollAnimTo = frac;
        }

        public void BeginScrollDrag() { scrollDrag = true; scrollVel = 0; scrollAnimTo = -1; }

        /// <summary>Finger moved by dyPx screen pixels (up = positive): content follows the finger.</summary>
        public void DragScroll(float dyPx)
        {
            float range = ScrollRange(ZoneIndex); if (range <= 0.05f) return;
            float d = dyPx * Upp(cam.orthographicSize) / range;
            scroll[ZoneIndex] = Mathf.Clamp01(scroll[ZoneIndex] + d);
            float v = d / Mathf.Max(0.001f, Time.unscaledDeltaTime);
            scrollVel = Mathf.Lerp(scrollVel, v, 0.5f);
        }

        public void EndScrollDrag() { scrollDrag = false; }

        void UpdateScroll()
        {
            if (Transitioning) return;
            int z = ZoneIndex;
            if (ScrollRange(z) <= 0.05f) { scroll[z] = 0; return; }
            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.01f && (UIRoot.I == null || !UIRoot.I.SheetOpen)) { scrollAnimTo = -1; scrollVel = 0; scroll[z] = Mathf.Clamp01(scroll[z] - wheel * 0.12f); }
            if (scrollAnimTo >= 0)
            {
                scroll[z] = Mathf.MoveTowards(scroll[z], scrollAnimTo, Time.unscaledDeltaTime * 2.6f);
                if (Mathf.Abs(scroll[z] - scrollAnimTo) < 0.001f) scrollAnimTo = -1;
            }
            else if (!scrollDrag && Mathf.Abs(scrollVel) > 0.01f)
            {   // inertia
                scroll[z] = Mathf.Clamp01(scroll[z] + scrollVel * Time.unscaledDeltaTime);
                scrollVel *= Mathf.Exp(-Time.unscaledDeltaTime * 5f);
                if (scroll[z] <= 0f || scroll[z] >= 1f) scrollVel = 0;
            }
        }

        void LateUpdate()
        {
            var fr = ComputeFrame(Screen.width, Screen.height);
            if (Screen.width > 0 && (frame.width <= 0 || Mathf.Abs(fr.width - frame.width) > 0.5f || Mathf.Abs(fr.height - frame.height) > 0.5f)) Apply();
            UpdateScroll();
            if (slideT >= 0)
            {
                // slide out to one side, cut to the new zone, slide in from the other side
                slideT += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(slideT / SlideTime);
                float span = 2f * cam.orthographicSize * frame.width / Mathf.Max(1f, frame.height) + 0.6f;
                if (k < 0.5f)
                {
                    float u = Ease.InQuad(k * 2f);
                    Place(fromZone, slideDir * span * u);
                }
                else
                {
                    if (ZoneIndex != toZone) { ZoneIndex = toZone; OnZoneChanged?.Invoke(toZone); }
                    float u = Ease.OutCubic((k - 0.5f) * 2f);
                    float span2 = 2f * OrthoFor(toZone) * frame.width / Mathf.Max(1f, frame.height) + 0.6f;
                    Place(toZone, -slideDir * span2 * (1f - u));
                }
                if (k >= 1f) { slideT = -1; Place(ZoneIndex, 0); }
            }
            else Place(ZoneIndex, 0);
            UIRoot.I?.UpdateFollows();
        }

        public Vector2 ScreenToWorld(Vector2 sp) => cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, 10));
        public Vector2 WorldToScreen(Vector3 wp) => cam.WorldToScreenPoint(wp);
    }

    public class InputRouter : MonoBehaviour
    {
        public static InputRouter I;
        enum Mode { None, UI, Tap, DragPending, Dragging, Camera }
        Mode mode;
        int pointerId = int.MinValue;
        Vector2 downPos, lastPos;
        float downTime, camVel;
        Hit downHit;
        int dragSlot = -1;
        SpriteRenderer proxy, proxyShadow;
        public bool IsDragging => mode == Mode.Dragging || mode == Mode.DragPending;
        public int DragSlot => IsDragging ? dragSlot : -1;
        public float LastInputTime { get; private set; }
        public static Func<bool> Blocked;   // e.g. modal open

        // synthetic pointer for automated tests (AutoPilot)
        bool synthActive, synthDown; Vector2 synthPos; bool synthUsed;
        public void SynthDown(Vector2 sp) { synthActive = true; synthDown = true; synthPos = sp; synthUsed = false; }
        public void SynthMove(Vector2 sp) { synthPos = sp; }
        public void SynthUp() { synthDown = false; }

        void Awake()
        {
            I = this;
            var go = new GameObject("DragProxy");
            proxy = go.AddComponent<SpriteRenderer>(); proxy.sortingOrder = 30001; proxy.enabled = false;
            var sh = new GameObject("DragShadow"); sh.transform.SetParent(go.transform, false);
            proxyShadow = sh.AddComponent<SpriteRenderer>(); proxyShadow.sprite = Art.S("char.shadow"); proxyShadow.sortingOrder = 30000;
            proxyShadow.color = new Color(1, 1, 1, 0.45f); proxyShadow.transform.localPosition = new Vector3(0, -0.45f, 0); proxyShadow.transform.localScale = new Vector3(0.9f, 0.9f, 1);
            proxyShadow.enabled = false;
            LastInputTime = Time.time;
        }

        bool ReadPointer(out Vector2 pos, out int phase)   // phase: 0 none, 1 down, 2 move, 3 up
        {
            pos = Vector2.zero; phase = 0;
            if (synthActive)
            {
                pos = synthPos;
                if (synthDown && pointerId == int.MinValue) { pointerId = -99; phase = 1; return true; }
                if (synthDown && pointerId == -99) { phase = 2; return true; }
                if (!synthDown && pointerId == -99) { phase = 3; synthActive = false; return true; }
                return false;
            }
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    if (pointerId == int.MinValue && t.phase == TouchPhase.Began) { pointerId = t.fingerId; pos = t.position; phase = 1; return true; }
                    if (t.fingerId != pointerId) continue;           // extra fingers are ignored (multi-touch safe)
                    pos = t.position;
                    phase = (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) ? 3 : 2;
                    return true;
                }
                if (pointerId != int.MinValue && pointerId >= 0) { phase = 3; pos = lastPos; return true; }   // finger vanished
                return false;
            }
            if (pointerId >= 0) { phase = 3; pos = lastPos; return true; }
            pos = Input.mousePosition;
            if (Input.GetMouseButtonDown(0) && pointerId == int.MinValue) { pointerId = -1; phase = 1; return true; }
            if (pointerId == -1) { phase = Input.GetMouseButton(0) ? 2 : 3; return true; }
            return false;
        }

        bool OverUI(int id)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            if (id == -99) return UIRoot.I != null && UIRoot.I.HitsUI(synthPos);
            return id >= 0 ? es.IsPointerOverGameObject(id) : es.IsPointerOverGameObject();
        }

        void Update()
        {
            if (!ReadPointer(out var pos, out int phase)) return;
            var cam = CameraRig.I;
            if (phase == 1)
            {
                LastInputTime = Time.time;
                AudioMan.UnlockByUser();
                UIRoot.I?.OnUserInput();
                downPos = lastPos = pos; downTime = Time.time; camVel = 0;
                if (OverUI(pointerId) || (Blocked != null && Blocked()) || cam.Transitioning) { mode = Mode.UI; return; }
                Vector2 wp = cam.ScreenToWorld(pos);
                if (!cam.frame.Contains(pos)) { mode = Mode.UI; return; }
                var cust = World.I.CustomerAt(wp);
                downHit = cust != null ? null : World.I.HitAt(wp);
                if (cust != null) { mode = Mode.Tap; tapCustomer = cust; return; }
                tapCustomer = null;
                if (downHit != null && downHit.kind == "slot" && downHit.index < World.I.M.S.boardUnlocked && World.I.M.S.boardSlots[downHit.index].level > 0)
                {
                    mode = Mode.DragPending; dragSlot = downHit.index; BeginLift(dragSlot, wp);
                }
                else if (downHit != null) mode = Mode.Tap;
                else if (cam.CanScroll) { mode = Mode.Camera; cam.BeginScrollDrag(); }   // shop: drag scrolls to the marketing street
                else mode = Mode.None;   // fixed zones: nothing (areas switch with the side arrows)
                return;
            }
            if (phase == 2)
            {
                Vector2 delta = pos - lastPos;
                if (delta.sqrMagnitude > 0) LastInputTime = Time.time;
                float moved = (pos - downPos).magnitude;
                Vector2 wp = cam.ScreenToWorld(pos);
                switch (mode)
                {
                    case Mode.DragPending:
                    case Mode.Dragging:
                        if (moved > 6f) mode = Mode.Dragging;
                        MoveProxy(wp);
                        World.I.SetDropPreview(World.I.HitAt(wp), dragSlot);
                        break;
                    case Mode.Tap:
                        if (moved > 14f)
                        {   // a drag that started on an object scrolls the shop (never taps it)
                            if (cam.CanScroll) { mode = Mode.Camera; cam.BeginScrollDrag(); cam.DragScroll(pos.y - downPos.y); }
                            else mode = Mode.None;
                        }
                        break;
                    case Mode.Camera:
                        cam.DragScroll(delta.y);
                        break;
                }
                lastPos = pos;
                return;
            }
            if (phase == 3)
            {
                Vector2 wp = cam.ScreenToWorld(pos);
                switch (mode)
                {
                    case Mode.DragPending:
                    case Mode.Dragging:
                        EndDrag(wp, mode == Mode.DragPending || (pos - downPos).magnitude < 6f);
                        break;
                    case Mode.Tap:
                        if (tapCustomer != null) World.I.TapCustomer(tapCustomer);
                        else { var h = World.I.HitAt(wp); if (h != null && h == downHit) World.I.Tap(h); }
                        break;
                }
                if (mode == Mode.Camera) cam.EndScrollDrag();
                mode = Mode.None; pointerId = int.MinValue; downHit = null; tapCustomer = null;
                World.I.SetDropPreview(null, -1);
            }
        }

        CustomerView tapCustomer;

        void BeginLift(int slot, Vector2 wp)
        {
            var sv = World.I.slots[slot];
            sv.hiddenForDrag = true;
            proxy.sprite = Art.S(Art.Dough(World.I.M.S.boardSlots[slot].level));
            proxy.enabled = true; proxyShadow.enabled = true;
            proxy.transform.position = sv.DoughPos;
            proxy.transform.localScale = Vector3.one * World.DoughScale;
            Tw.Kill(proxy.transform);
            Tw.Run(proxy.transform, 0.12f, t => proxy.transform.localScale = Vector3.one * World.DoughScale * Mathf.Lerp(1f, 1.18f, t), Ease.OutBack);
            World.I.SetDragHighlights(World.I.M.S.boardSlots[slot].level, slot);
        }

        void MoveProxy(Vector2 wp)
        {
            var target = wp + new Vector2(0, 0.35f);
            proxy.transform.position = Vector3.Lerp(proxy.transform.position, target, 0.6f);
        }

        /// <summary>Resolve the drop. The model is only touched here (commit); cancelled drags never change state.</summary>
        void EndDrag(Vector2 wp, bool wasTap)
        {
            var w = World.I; var m = w.M;
            int from = dragSlot; dragSlot = -1;
            w.ClearDragHighlights();
            var sv = w.slots[from];
            void Finish(Vector2 to, float dur, float endScale = World.DoughScale)
            {
                Tw.Kill(proxy.transform);
                Vector3 p0 = proxy.transform.position; Vector3 s0 = proxy.transform.localScale;
                Tw.Run(proxy.transform, dur, t =>
                {
                    proxy.transform.position = Vector3.Lerp(p0, to, t);
                    proxy.transform.localScale = Vector3.Lerp(s0, Vector3.one * endScale, t);
                }, Ease.OutQuad, () => { proxy.enabled = false; proxyShadow.enabled = false; sv.hiddenForDrag = false; });
            }
            if (wasTap)
            {
                sv.hiddenForDrag = false; proxy.enabled = false; proxyShadow.enabled = false;
                w.TapDough(from);
                return;
            }
            var h = w.HitAt(wp);
            if (h != null && h.kind == "slot" && h.index != from && h.index < m.S.boardUnlocked)
            {
                var r = m.DropDough(from, h.index);
                if (r == DropResult.Merged || r == DropResult.Moved) { sv.hiddenForDrag = false; Finish(w.slots[h.index].DoughPos, r == DropResult.Merged ? 0.12f : 0.1f); return; }
            }
            else if (h != null && h.kind == "oven" && m.S.ovenJobs[h.index].owned)
            {
                if (m.Bake(from, h.index))
                {
                    sv.hiddenForDrag = false;
                    Finish(w.ovens[h.index].Mouth, 0.2f, 0.35f);
                    return;
                }
            }
            // invalid / outside board: back to the original slot
            Finish(sv.DoughPos, 0.18f);
            AudioMan.Play("se_Etc_UseItem", 0.4f);
        }

        void OnApplicationFocus(bool focus)
        {
            if (!focus && IsDragging)
            {   // cancel any uncommitted drag
                var w = World.I; if (w == null) return;
                int from = dragSlot; dragSlot = -1; mode = Mode.None; pointerId = int.MinValue;
                if (from >= 0) { w.slots[from].hiddenForDrag = false; }
                proxy.enabled = false; proxyShadow.enabled = false; w.ClearDragHighlights();
            }
        }
    }
}
