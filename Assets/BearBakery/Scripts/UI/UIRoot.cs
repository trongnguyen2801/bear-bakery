
// Screen-space UI: portrait frame + safe area, HUD, bottom bar (5 floating tiles + speed-up), zone arrows,
// world-anchored labels, toasts, coin fly.
using System;
using System.Collections.Generic;
using System.Linq;
using BB.Model;
using BB.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BB.UI
{
    public enum SheetKind { None, Kitchen, Shop, Furniture, Notebook, ShelfPick, Inventory, Settings, Delivery, Cleaner }

    public partial class UIRoot : MonoBehaviour
    {
        public static UIRoot I;
        public GameModel M;
        Canvas canvas; CanvasScaler scaler;
        public RectTransform Root, Frame, Safe, WorldLayer, HudLayer, BarLayer, NavLayer, SheetLayer, PopupLayer, ToastLayer, FxLayer, HandLayer;
        public const float HudH = 56f, ChipH = 50f, BarH = 96f;
        public float Scale { get; private set; } = 1f;
        public float TopInsetPx { get; private set; }
        public float BottomInsetPx { get; private set; }

        struct Follower { public RectTransform rt; public Func<Vector3> world; public Vector2 offset; public Bob bob; }
        readonly List<Follower> followers = new List<Follower>();

        // HUD refs
        Text coinText, levelText, goalText, goalProgress, hintText, tipText; Bar goalBar; Image xpRing;
        RectTransform coinIcon, goalChip, claimBtn, hintBox, questBtn, clearSticker, tipPill; Text claimText; Image questDot;
        double shownCoins; long lastCoins;
        float hintHiddenStep = -1;

        public void Build(GameModel m)
        {
            I = this; M = m;
            gameObject.layer = 5;
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            gameObject.AddComponent<GraphicRaycaster>();
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                DontDestroyOnLoad(es);
            }
            Root = (RectTransform)transform;
            Frame = K.Stretch(Root, "PortraitFrame");
            Frame.gameObject.AddComponent<RectMask2D>();
            WorldLayer = K.Stretch(Frame, "WorldLabels");     // below HUD/bar
            Safe = K.Stretch(Frame, "SafeArea");
            NavLayer = K.Stretch(Safe, "ZoneArrows");
            HudLayer = K.Stretch(Safe, "HUD");
            BarLayer = K.Stretch(Safe, "BottomBar");
            HandLayer = K.Stretch(Frame, "Hand");
            SheetLayer = K.Stretch(Frame, "Sheets");
            PopupLayer = K.Stretch(Frame, "Popups");
            ToastLayer = K.Stretch(Frame, "Toasts");
            FxLayer = K.Stretch(Frame, "UIFx");
            ApplyFrame();
            BuildHud();
            BuildBar();
            BuildNav();
            BuildHand();
            shownCoins = M.S.currency; lastCoins = M.S.currency;
            M.OnEvent += OnModelEvent;
        }

        void OnDestroy() { if (M != null) M.OnEvent -= OnModelEvent; }

        Rect lastScreen;
        void ApplyFrame()
        {
            var fr = CameraRig.ComputeFrame(Screen.width, Screen.height);
            float W = Mathf.Max(1, Screen.width), H = Mathf.Max(1, Screen.height);
            Scale = fr.width / CameraRig.RefW;
            scaler.scaleFactor = Scale;
            Frame.anchorMin = new Vector2(fr.x / W, fr.y / H); Frame.anchorMax = new Vector2((fr.x + fr.width) / W, (fr.y + fr.height) / H);
            Frame.offsetMin = Frame.offsetMax = Vector2.zero;
            var sa = Screen.safeArea;
            // safe area intersected with the frame, expressed in frame-relative anchors
            float x0 = Mathf.Max(sa.xMin, fr.xMin), x1 = Mathf.Min(sa.xMax, fr.xMax), y0 = Mathf.Max(sa.yMin, fr.yMin), y1 = Mathf.Min(sa.yMax, fr.yMax);
            Safe.anchorMin = new Vector2((x0 - fr.x) / fr.width, (y0 - fr.y) / fr.height);
            Safe.anchorMax = new Vector2((x1 - fr.x) / fr.width, (y1 - fr.y) / fr.height);
            Safe.offsetMin = Safe.offsetMax = Vector2.zero;
            TopInsetPx = (fr.yMax - y1) + (HudH + ChipH + 4) * Scale;
            BottomInsetPx = (y0 - fr.yMin) + BarH * Scale;
            lastScreen = new Rect(0, 0, Screen.width, Screen.height);
        }

        public bool HitsUI(Vector2 screenPos)
        {
            var es = EventSystem.current; if (es == null) return false;
            var data = new PointerEventData(es) { position = screenPos };
            var res = new List<RaycastResult>();
            es.RaycastAll(data, res);
            return res.Count > 0;
        }

        // ================================================================== HUD (reference layout: level badge, coin pill, tip pill, menu; quest icon column)
        void BuildHud()
        {
            var strip = K.Rect(HudLayer, "Strip", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 80), new Vector2(40, HudH + 84));
            K.Img(strip, "bg.white", new Color(1f, 0.95f, 0.88f, 0.9f), false, false, false);
            var shade = K.Rect(HudLayer, "StripShade", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -HudH + 4), new Vector2(40, 4));
            K.Img(shade, "bg.white", new Color(0.55f, 0.35f, 0.2f, 0.25f), false, false, false);
            var top = K.Rect(HudLayer, "TopRow", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -2), new Vector2(-12, HudH - 4));
            // level badge: ring = xp progress
            var lv = K.Rect(top, "Level", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(52, 52));
            K.Img(lv, "ui.whiteCircle", Pal.Brown, false, false, false);
            var ringRt = K.Stretch(lv, "Xp", 3, 3, 3, 3);
            xpRing = K.Img(ringRt, "ui.whiteCircle", Pal.Orange, false, false, false);
            xpRing.type = Image.Type.Filled; xpRing.fillMethod = Image.FillMethod.Radial360; xpRing.fillOrigin = (int)Image.Origin360.Top; xpRing.fillClockwise = true;
            var inner = K.Stretch(lv, "Inner", 8, 8, 8, 8); K.Img(inner, "ui.whiteCircle", Pal.Cream, false, false, false);
            K.Pic(lv, "Icon", "ui.level", new Vector2(0.5f, 0.5f), new Vector2(0, 5), new Vector2(30, 27));
            var lvPill = K.At(lv, "LvPill", new Vector2(0.5f, 0), new Vector2(0, 2), new Vector2(46, 18));
            K.Img(lvPill, "ui.btnBasic", Pal.Brown, true);
            levelText = K.Txt(lvPill, "T", "Lv.1", 12, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(46, 18), true);
            var lvBtn = lv.gameObject.AddComponent<Button>(); lvBtn.transition = Selectable.Transition.None;
            lv.GetComponent<Image>().raycastTarget = true; lvBtn.targetGraphic = lv.GetComponent<Image>();
            lvBtn.onClick.AddListener(() => { AudioMan.Click(); Toast(M.XpForNextLevel < 0 ? $"Tiệm cấp {M.ShopLevel} (tối đa)" : $"Tiệm cấp {M.ShopLevel}: còn {M.XpForNextLevel - M.S.xp} kinh nghiệm để lên cấp (bán bánh để nhận)."); });
            // coin pill
            var coin = K.Rect(top, "Coins", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(64, 0), new Vector2(132, 38));
            K.Img(coin, "ui.btnBasic", Color.white, true);
            coinIcon = K.At(coin, "CoinIcon", new Vector2(0, 0.5f), new Vector2(6, 1), new Vector2(40, 40));
            K.Img(coinIcon, "ui.coin");
            coinText = K.Txt(coin, "Amount", "0", 21, Pal.Brown, TextAnchor.MiddleCenter, new Vector2(0, 0.5f), new Vector2(80, 0), new Vector2(92, 36), false, true);
            // tip pill (piggy bank)
            tipPill = K.Rect(top, "Tips", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(206, 0), new Vector2(96, 38));
            K.Btn(tipPill, "ui.btnBasic", () => { if (M.ClaimTips() <= 0) Toast("Heo đất trống. Khách mua đúng món thích sẽ để lại tip."); }, Color.white, true);
            K.Pic(tipPill, "Piggy", "ui.piggyIng", new Vector2(0, 0.5f), new Vector2(8, 1), new Vector2(42, 35));
            tipText = K.Txt(tipPill, "Amount", "0", 18, Pal.Brown, TextAnchor.MiddleCenter, new Vector2(0, 0.5f), new Vector2(64, 0), new Vector2(60, 34), false, true);
            // menu
            var opt = K.Rect(top, "Settings", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 0), new Vector2(46, 46));
            K.Btn(opt, "ui.option", () => OpenSheet(SheetKind.Settings));
            // quest icon (left column) + goal pill
            questBtn = K.Rect(HudLayer, "Quest", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(32, -HudH - 24), new Vector2(54, 50));
            K.Btn(questBtn, "ui.quest", ClaimActiveQuest);
            clearSticker = K.At(questBtn, "Clear", new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(62, 26));
            K.Img(clearSticker, "ui.clearLabel");
            questDot = K.Pic(questBtn, "Dot", "ui.redDot", new Vector2(1, 1), new Vector2(-4, -4), new Vector2(16, 15));
            goalChip = K.Rect(HudLayer, "GoalChip", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(64, -HudH - 4), new Vector2(-72, 40));
            K.Btn(goalChip, "ui.btnBasic", () => OpenSheet(SheetKind.Notebook, 0), new Color(1, 1, 1, 0.93f), true);
            goalText = K.Txt(goalChip, "Goal", "", 13, Pal.Brown, TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(118, 7), new Vector2(220, 20), true);
            goalText.horizontalOverflow = HorizontalWrapMode.Wrap; goalText.verticalOverflow = VerticalWrapMode.Truncate;
            goalText.resizeTextForBestFit = true; goalText.resizeTextMinSize = 10; goalText.resizeTextMaxSize = 13;
            goalBar = Bar.Make(goalChip, "GoalBar", new Vector2(0, 0.5f), new Vector2(78, -9), new Vector2(140, 7), Pal.Green);
            goalProgress = K.Txt(goalChip, "Prog", "", 11, Pal.BrownSoft, TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(190, -9), new Vector2(70, 16));
            claimBtn = K.At(goalChip, "Claim", new Vector2(1, 0.5f), new Vector2(-44, 0), new Vector2(84, 38));
            K.Btn(claimBtn, "ui.btnBasic", ClaimActiveQuest, Pal.Green, true);
            claimText = K.Txt(claimBtn, "T", "Nhận", 14, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(84, 36), true, false, true);
            // tutorial hint (overlays the top of the zone)
            hintBox = K.Rect(HudLayer, "Hint", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -HudH - ChipH - 2), new Vector2(-24, 54));
            K.Img(hintBox, "ui.bubble", new Color(1, 1, 1, 0.95f), false, false, false);
            K.Pic(hintBox, "Bear", "illust.Bear", new Vector2(0, 0.5f), new Vector2(28, 0), new Vector2(50, 44));
            hintText = K.Txt(hintBox, "Text", "", 14, Pal.Brown, TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(190, 2), new Vector2(270, 50));
            var hclose = K.At(hintBox, "Close", new Vector2(1, 0.5f), new Vector2(-22, 0), new Vector2(44, 44));
            K.Btn(hclose, null, () => { hintHiddenStep = M.S.tutorialStep; hintBox.gameObject.SetActive(false); });
            K.Txt(hclose, "X", "×", 22, Pal.BrownSoft, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40));
        }

        void ClaimActiveQuest()
        {
            for (int i = 0; i < M.S.quests.Count; i++)
            {
                var q = M.S.quests[i];
                if (q.completed && !q.claimed) { M.ClaimQuest(q.id); return; }
            }
            OpenSheet(SheetKind.Notebook, 0);
        }

        // ================================================================== bottom bar: 4 floating tiles + big speed-up button in the centre
        class Tile { public RectTransform rt; public Image icon; public Text label; public Image dot; }
        Tile tUpgrade, tBook, tFurniture, tQuests;
        RectTransform boostTile, boostX2; Image boostIcon, boostGauge, boostInner; Text boostLabel, boostTime;
        public RectTransform BoostTileRect => boostTile;
        public RectTransform UpgradeTileRect => tUpgrade.rt;
        public RectTransform FurnitureTileRect => tFurniture.rt;

        Tile MakeTile(Transform bar, string name, float ax, string icon, string label, Action onClick)
        {
            var t = new Tile();
            t.rt = K.Rect(bar, name, new Vector2(ax, 0), new Vector2(ax, 0), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(70, 82));
            K.Btn(t.rt, null, onClick);
            var bg = K.At(t.rt, "Bg", new Vector2(0.5f, 1), new Vector2(0, -33), new Vector2(62, 62));
            K.Img(bg, "ui.btnBasic", Color.white, true);
            t.icon = K.Pic(t.rt, "Icon", icon, new Vector2(0.5f, 1), new Vector2(0, -32), new Vector2(48, 44));
            var pill = K.At(t.rt, "Label", new Vector2(0.5f, 0), new Vector2(0, 11), new Vector2(72, 20));
            K.Img(pill, "ui.btnBasic", Pal.Brown, true);
            t.label = K.Txt(pill, "T", label, 11, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(76, 20), true);
            t.dot = K.Pic(t.rt, "Dot", "ui.redDot", new Vector2(1, 1), new Vector2(-8, -4), new Vector2(18, 17));
            return t;
        }

        void BuildBar()
        {
            var bar = K.Rect(BarLayer, "Bar", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, BarH));
            var band = K.Rect(bar, "Band", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, -60), new Vector2(40, 60 + 58));
            K.Img(band, "bg.white", new Color(1f, 0.95f, 0.88f, 0.92f), false, true, false);   // also blocks world taps behind the bar
            var line = K.Rect(bar, "Line", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 58), new Vector2(40, 3));
            K.Img(line, "bg.white", new Color(0.55f, 0.35f, 0.2f, 0.3f), false, false, false);
            tUpgrade = MakeTile(bar, "TileUpgrade", 0.105f, "ui.tab.kitchen", "Nâng cấp", OnUpgradeTile);
            tBook = MakeTile(bar, "TileBook", 0.29f, "ui.store", "Công thức", OnBookTile);
            tFurniture = MakeTile(bar, "TileFurniture", 0.71f, "ui.tab.furniture", "Nội thất", OnFurnitureTile);
            tQuests = MakeTile(bar, "TileQuests", 0.895f, "ui.tab.book", "Nhiệm vụ", () => OpenSheet(SheetKind.Notebook, 0));
            // speed-up (raised, round)
            boostTile = K.Rect(bar, "Boost", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 4), new Vector2(104, 108));
            K.Btn(boostTile, null, OnBoostTile);
            K.Pic(boostTile, "Ring", "ui.whiteCircle", new Vector2(0.5f, 1), new Vector2(0, -45), new Vector2(88, 88), Pal.Brown);
            boostGauge = K.Pic(boostTile, "Gauge", "ui.whiteCircle", new Vector2(0.5f, 1), new Vector2(0, -45), new Vector2(82, 82), Pal.Orange);
            boostGauge.type = Image.Type.Filled; boostGauge.fillMethod = Image.FillMethod.Radial360; boostGauge.fillOrigin = (int)Image.Origin360.Top; boostGauge.fillClockwise = false;
            boostInner = K.Pic(boostTile, "Inner", "ui.whiteCircle", new Vector2(0.5f, 1), new Vector2(0, -45), new Vector2(70, 70), Pal.Cream);
            boostIcon = K.Pic(boostTile, "Icon", "ui.boost.chef", new Vector2(0.5f, 1), new Vector2(0, -46), new Vector2(62, 54));
            boostTime = K.Txt(boostTile, "Time", "", 22, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, -46), new Vector2(80, 40), true, true, true);
            boostX2 = K.At(boostTile, "X2", new Vector2(1, 1), new Vector2(-14, -10), new Vector2(40, 24));
            K.Img(boostX2, "ui.btnBasic", Pal.Red, true);
            K.Txt(boostX2, "T", "x2", 15, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(40, 24), true, false, true);
            var pill = K.At(boostTile, "Label", new Vector2(0.5f, 0), new Vector2(0, 9), new Vector2(110, 22));
            K.Img(pill, "ui.btnBasic", Pal.Brown, true);
            boostLabel = K.Txt(pill, "T", "Tăng tốc", 12, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(110, 22), true);
            // shop: hint pill to scroll between the selling floor and the marketing street
            BuildScrollHint(bar);
        }

        // ---------------------------------------------------------------- speed-up
        public string BoostIdForView()
        {
            var cam = CameraRig.I; if (cam == null) return "chef";
            switch (cam.Zone())
            {
                case "kitchen": return "chef";
                case "shop": return cam.ShopSub() == "marketing" ? "promoter" : "cashier";
                default: return "rest";
            }
        }

        public static string BoostName(string id) => id == "chef" ? "Nhồi bột" : id == "cashier" ? "Bán hàng" : id == "promoter" ? "Quảng cáo" : "Nghỉ ngơi";
        static string BoostWho(string id) => id == "chef" ? "Gấu nhồi bột" : id == "cashier" ? "Amber tính tiền" : id == "promoter" ? "Tweetie mời khách" : "Nhân viên hồi sức";
        static string BoostIcon(string id, bool alt) => id == "rest" ? "ui.buff.furniture" : "ui.boost." + id + (alt ? "2" : "");

        void OnBoostTile()
        {
            string id = BoostIdForView();
            if (M.StartBoost(id))
            {
                Tw.Pop(boostTile, 0.8f, 0.3f);
                Toast($"{BoostWho(id)} nhanh gấp đôi trong {M.C.boost.duration:0} giây!");
            }
        }

        public void PressBoost() => OnBoostTile();
        public void PulseBoost() { if (boostTile != null) Tw.Squash(boostTile, 0.12f, 0.35f, Vector3.one); }

        void UpdateBoostTile()
        {
            string id = BoostIdForView();
            var b = M.Boost(id);
            bool active = b.remaining > 0, cooling = !active && b.cooldown > 0;
            boostIcon.sprite = Art.S(BoostIcon(id, active && Mathf.Repeat(Time.time, 0.5f) > 0.25f));
            boostIcon.color = cooling ? new Color(0.62f, 0.58f, 0.55f, 1f) : Color.white;
            boostX2.gameObject.SetActive(!cooling);
            if (active)
            {
                boostGauge.fillAmount = b.remaining / Mathf.Max(0.01f, M.C.boost.duration);
                boostGauge.color = Pal.Green;
                boostLabel.text = $"{BoostName(id)} x2 · {Mathf.CeilToInt(b.remaining)}s";
                boostTime.text = "";
                boostTile.localScale = Vector3.one;
            }
            else if (cooling)
            {
                boostGauge.fillAmount = 1f - b.cooldown / Mathf.Max(0.01f, M.C.boost.cooldown);
                boostGauge.color = Pal.Grey;
                boostLabel.text = "Chờ " + Mathf.CeilToInt(b.cooldown) + "s";
                boostTime.text = Mathf.CeilToInt(b.cooldown).ToString();
                boostTile.localScale = Vector3.one;
            }
            else
            {
                boostGauge.fillAmount = 1f; boostGauge.color = Pal.Orange;
                boostLabel.text = "Tăng tốc " + BoostName(id).ToLower();
                boostTime.text = "";
                float s = 1f + 0.035f * Mathf.Sin(Time.time * 5f);
                if (!Tw.Busy(boostTile)) boostTile.localScale = new Vector3(s, s, 1);
            }
        }

        // ---------------------------------------------------------------- tiles
        void OnUpgradeTile()
        {
            switch (CameraRig.I.Zone())
            {
                case "kitchen": OpenSheet(SheetKind.Kitchen); break;
                case "shop": OpenSheet(SheetKind.Shop); break;
                default: OpenSheet(SheetKind.Furniture); break;
            }
        }

        void OnBookTile() => OpenSheet(SheetKind.Notebook, CameraRig.I.Zone() == "kitchen" ? 1 : 2);

        void OnFurnitureTile()
        {
            var cam = CameraRig.I;
            if (cam.Zone() != "break") cam.GoToZone(2);
            M.MarkBreakRoomVisited();
            OpenSheet(SheetKind.Furniture);
        }

        void UpdateTiles()
        {
            string zone = CameraRig.I.Zone();
            tUpgrade.icon.sprite = Art.S(zone == "kitchen" ? "ui.tab.kitchen" : zone == "shop" ? "ui.tab.shop" : "ui.tab.furniture");
            tBook.icon.sprite = Art.S(zone == "kitchen" ? "ui.store" : "ui.qc.Cat");
            tBook.label.text = zone == "kitchen" ? "Công thức" : "Khách quen";
            tUpgrade.dot.enabled = AnyAffordable(zone == "kitchen" ? 0 : zone == "shop" ? 1 : 2);
            tBook.dot.enabled = false;
            tFurniture.dot.enabled = zone != "break" && AnyAffordable(2);
            tQuests.dot.enabled = M.ClaimableQuestCount > 0;
        }

        // ================================================================== side arrows (zone navigation)
        class NavArrow { public RectTransform rt; public Image arrow, shadow; public Text label; public RectTransform badge; public Text badgeText; public int dir; }
        readonly NavArrow[] nav = new NavArrow[2];

        void BuildNav()
        {
            for (int k = 0; k < 2; k++)
            {
                int dir = k == 0 ? -1 : 1;
                var a = new NavArrow { dir = dir };
                var anchor = new Vector2(k, 0.5f);
                a.rt = K.Rect(NavLayer, dir < 0 ? "ArrowLeft" : "ArrowRight", anchor, anchor, new Vector2(k, 0.5f), Vector2.zero, new Vector2(64, 96));
                K.Btn(a.rt, null, () => CameraRig.I.Step(dir));            // invisible 64x96 hit area (>= 44x44)
                float x = dir < 0 ? 26 : -26;
                a.shadow = K.Pic(a.rt, "Shadow", "ui.arrow", new Vector2(k, 0.5f), new Vector2(x, 9), new Vector2(34, 62), new Color(0.35f, 0.2f, 0.1f, 0.35f));
                a.arrow = K.Pic(a.rt, "Arrow", "ui.arrow", new Vector2(k, 0.5f), new Vector2(x, 13), new Vector2(34, 62));
                if (dir < 0) { a.arrow.rectTransform.localScale = new Vector3(-1, 1, 1); a.shadow.rectTransform.localScale = new Vector3(-1, 1, 1); }
                var pill = K.Rect(a.rt, "Pill", new Vector2(k, 0.5f), new Vector2(k, 0.5f), new Vector2(k, 0.5f), new Vector2(dir < 0 ? 2 : -2, -32), new Vector2(70, 20));
                K.Img(pill, "ui.btnBasic", new Color(0.47f, 0.29f, 0.18f, 0.92f), true);
                a.label = K.Txt(pill, "T", "", 11, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(70, 20), true);
                a.badge = K.At(a.rt, "Badge", new Vector2(k, 0.5f), new Vector2(x + (dir < 0 ? 14 : -14), 40), new Vector2(22, 22));
                K.Img(a.badge, "ui.whiteCircle", Pal.Red, false, false, false);
                a.badgeText = K.Txt(a.badge, "T", "", 12, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(22, 22), true, true);
                nav[k] = a;
            }
        }

        /// <summary>Things waiting in a zone (red badge on the arrow that leads there).</summary>
        int ZoneAttention(int z)
        {
            int n = 0;
            if (z == 0) { foreach (var o in M.S.ovenJobs) if (o.owned && (o.state == (int)OvenState.Ready || o.state == (int)OvenState.Burnt)) n++; }
            else if (z == 1)
            {
                if (M.CanPackDelivery) n++;
                if (M.CleanerQuestDone) n++;
                if (M.StorageUsed > 0) for (int i = 0; i < GameModel.ShelfSlots; i++) if (M.S.shelfStock[i].owned && M.S.shelfStock[i].stock == 0) { n++; break; }
            }
            else foreach (var st in M.S.staffEnergy) if (st.state == (int)StaffState.RestInPlace) n++;
            return n;
        }

        void UpdateNav()
        {
            var cam = CameraRig.I;
            if (cam == null) return;
            int z = cam.ZoneIndex;
            float wy = z == 1 ? Mathf.Lerp(WL.NavArrowY[1], WL.NavArrowYMarketing, cam.ScrollFrac) : WL.NavArrowY[z];
            Vector2 sp = cam.WorldToScreen(new Vector3(0, wy, 0));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(NavLayer, sp, null, out var lp);
            float nudge = 3f * Mathf.Sin(Time.time * 3.2f);
            foreach (var a in nav)
            {
                bool show = cam.CanGo(a.dir) && !SheetOpen && !ModalOpen;
                if (a.rt.gameObject.activeSelf != show) a.rt.gameObject.SetActive(show);
                if (!show) continue;
                a.rt.anchoredPosition = new Vector2(a.dir * (2f + Mathf.Max(0, nudge)), lp.y);
                a.label.text = CameraRig.ZoneNames[z + a.dir];
                int att = ZoneAttention(z + a.dir);
                a.badge.gameObject.SetActive(att > 0);
                a.badgeText.text = att.ToString();
            }
        }

        // ---------------------------------------------------------------- shop scroll hint (selling floor <-> marketing street)
        RectTransform scrollHint; Text scrollHintText; Image scrollHintArrow, scrollHintDot;

        void BuildScrollHint(Transform bar)
        {
            scrollHint = K.Rect(bar, "ShopScroll", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(158, 30));
            K.Btn(scrollHint, "ui.btnBasic", () => { var c = CameraRig.I; c.ScrollTo(c.ScrollFrac > 0.5f ? 0f : 1f); }, new Color(1f, 0.98f, 0.93f, 0.95f), true);
            scrollHintArrow = K.Pic(scrollHint, "Arrow", "ui.arrow", new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(12, 22));
            scrollHintText = K.Txt(scrollHint, "T", "", 12, Pal.Brown, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(8, 1), new Vector2(130, 26), true);
            scrollHintDot = K.Pic(scrollHint, "Dot", "ui.redDot", new Vector2(1, 1), new Vector2(-4, -4), new Vector2(15, 14));
        }

        void UpdateScrollHint()
        {
            var cam = CameraRig.I;
            bool show = cam.Zone() == "shop" && cam.CanScroll && !SheetOpen && !ModalOpen;
            if (scrollHint.gameObject.activeSelf != show) scrollHint.gameObject.SetActive(show);
            if (!show) return;
            bool down = cam.ScrollFrac < 0.5f;
            scrollHintText.text = down ? "Quảng cáo & dọn rác" : "Quầy bán & giao hàng";
            scrollHintArrow.rectTransform.localEulerAngles = new Vector3(0, 0, down ? -90 : 90);
            scrollHintArrow.rectTransform.anchoredPosition = new Vector2(18, (down ? -2 : 2) * Mathf.Sin(Time.time * 5f));
            scrollHintDot.enabled = down ? (M.S.trash.Count > 0 && !M.S.cleanerHired) || M.CleanerQuestDone : M.CanPackDelivery;
        }

        // ================================================================== per-frame
        float refreshT;
        void Update()
        {
            if (M == null) return;
            if (Screen.width != (int)lastScreen.width || Screen.height != (int)lastScreen.height) ApplyFrame();
            // coins (count-up display of the committed model value)
            long cur = M.S.currency;
            shownCoins = Mathf.Abs((float)(cur - shownCoins)) < 1 ? cur : shownCoins + (cur - shownCoins) * Mathf.Min(1f, Time.deltaTime * 10f);
            coinText.text = Fmt((long)Math.Round(shownCoins));
            if (cur > lastCoins) Tw.Pop(coinIcon, 0.8f, 0.2f);
            lastCoins = cur;
            int lv = M.ShopLevel;
            levelText.text = "Lv." + lv;
            long need = M.XpForNextLevel; long prev = M.C.levelXp[Mathf.Min(lv - 1, M.C.levelXp.Length - 1)];
            xpRing.fillAmount = need < 0 ? 1f : Mathf.Clamp01((float)(M.S.xp - prev) / Mathf.Max(1, need - prev));
            tipText.text = Fmt(M.S.tipJar);
            UpdateGoal();
            string zone = CameraRig.I.Zone();
            if (zone == "break" && !CameraRig.I.Transitioning) M.MarkBreakRoomVisited();
            UpdateBoostTile();
            refreshT -= Time.deltaTime;
            if (refreshT <= 0)
            {
                refreshT = 0.3f;
                UpdateTiles();
                RefreshSheet();
            }
            UpdateNav();
            UpdateScrollHint();
            UpdateHand();
            UpdateToasts();
        }

        public static string Fmt(long v) => v >= 100000 ? (v / 1000) + "K" : v.ToString("N0").Replace(",", ".");

        bool AnyAffordable(int area)
        {
            long c = M.S.currency;
            if (area == 0)
            {
                foreach (var id in new[] { "chef", "oven" }) { int k = M.UpgradeCost(id); if (k >= 0 && k <= c) return true; }
                int e0 = M.ExpansionCost(0, out int r0); if (e0 >= 0 && e0 <= c && M.ShopLevel >= r0) return true;
                int e1 = M.ExpansionCost(1, out int r1); if (e1 >= 0 && e1 <= c && M.ShopLevel >= r1) return true;
                return false;
            }
            if (area == 1)
            {
                int k = M.UpgradeCost("capacity"); if (k >= 0 && k <= c) return true;
                int e2 = M.ExpansionCost(2, out int r2); if (e2 >= 0 && e2 <= c && M.ShopLevel >= r2) return true;
                if (M.CanUnlockAutoRestock && M.C.economy.autoRestockCost <= c) return true;
                return false;
            }
            foreach (var f in M.C.furniture)
            {
                var d = M.Furn(f.id); int k = M.FurnitureCost(f.id);
                if (k >= 0 && k <= c && (d.owned || M.ShopLevel >= f.levelReq) && !d.owned) return true;
            }
            return false;
        }

        void UpdateGoal()
        {
            int ai = M.ActiveQuestIndex;
            int claimable = -1;
            for (int i = 0; i < M.S.quests.Count; i++) if (M.S.quests[i].completed && !M.S.quests[i].claimed) { claimable = i; break; }
            claimBtn.gameObject.SetActive(claimable >= 0);
            clearSticker.gameObject.SetActive(claimable >= 0);
            questDot.enabled = claimable >= 0;
            var gtr = goalText.rectTransform;
            if (claimable >= 0)
            {
                var qc = M.C.quests[claimable];
                goalText.text = "Xong: " + qc.textVi;
                gtr.sizeDelta = new Vector2(150, 20); gtr.anchoredPosition = new Vector2(88, 7);
                goalBar.Set(1); goalProgress.text = "";
                goalBar.root.sizeDelta = new Vector2(130, 7);
                claimText.text = "Nhận +" + qc.reward;
                float s = 1f + 0.05f * Mathf.Sin(Time.time * 6f);
                claimBtn.localScale = new Vector3(s, s, 1);
                clearSticker.localScale = new Vector3(s, s, 1);
            }
            else
            {
                gtr.sizeDelta = new Vector2(220, 20); gtr.anchoredPosition = new Vector2(122, 7);
                goalBar.root.sizeDelta = new Vector2(140, 7);
                if (ai >= 0)
                {
                    var q = M.C.quests[ai]; var d = M.S.quests[ai];
                    goalText.text = q.textVi;
                    goalBar.Set(d.progress / (float)Mathf.Max(1, q.target));
                    goalProgress.text = d.progress + "/" + q.target;
                }
                else
                {
                    goalText.text = NextBigGoal();
                    goalBar.Set(1); goalProgress.text = "";
                }
            }
            // tutorial hint only for tutorial quests
            bool showHint = ai >= 0 && M.C.quests[ai].tutorial && hintHiddenStep != M.S.tutorialStep && !SheetOpen;
            hintBox.gameObject.SetActive(showHint);
            if (showHint)
            {
                var q = M.C.quests[ai];
                string h = q.hintVi;
                if (q.type == "collect" && !M.S.ovenJobs.Any(o => o.state == (int)OvenState.Ready)) h = "Bánh đang nướng trong lò. Khi bong bóng hiện dấu tích xanh, chạm vào lò để lấy bánh.";
                else if (q.type == "bake" && !M.S.boardSlots.Any(b => b.level > 0)) h = "Chờ Gấu nhồi bột, rồi kéo bột thả vào lò để nướng.";
                else if (q.type == "sell" && M.TotalAvailable() == 0 && M.StorageUsed == 0) h = "Nướng thêm bánh và bày lên kệ để khách mua.";
                hintText.text = h;
            }
        }

        /// <summary>"Bán N bánh để mua ..." style next goal once quests are done.</summary>
        public string NextBigGoal()
        {
            int c1 = M.ExpansionCost(1, out _);
            int avg = 0; foreach (var r in M.S.unlockedRecipes) avg = Mathf.Max(avg, M.C.Recipe(r).price);
            if (c1 > 0 && M.S.currency < c1) return $"Bán ~{Mathf.CeilToInt((c1 - M.S.currency) / (float)Mathf.Max(1, avg))} bánh để mua lò mới";
            return "Mở rộng tiệm và chăm khách quen!";
        }

        // ================================================================== world-anchored labels
        public void Follow(RectTransform rt, Func<Vector3> world, Vector2 offset)
        {
            rt.SetParent(WorldLayer, false);
            followers.Add(new Follower { rt = rt, world = world, offset = offset, bob = rt.GetComponent<Bob>() });
        }

        public void Unfollow(RectTransform rt, bool destroy)
        {
            followers.RemoveAll(f => f.rt == rt);
            if (destroy && rt != null) Destroy(rt.gameObject);
        }

        /// <summary>Called by the camera after it moved this frame.</summary>
        public void UpdateFollows()
        {
            var cam = CameraRig.I; if (cam == null) return;
            for (int i = followers.Count - 1; i >= 0; i--)
            {
                var f = followers[i];
                if (f.rt == null) { followers.RemoveAt(i); continue; }
                if (!f.rt.gameObject.activeSelf) continue;
                Vector3 wp; try { wp = f.world(); } catch { continue; }
                Vector2 sp = cam.WorldToScreen(wp);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(WorldLayer, sp, null, out var lp);
                f.rt.anchoredPosition = lp + f.offset + (f.bob != null ? Vector2.up * f.bob.amp * Mathf.Sin(Time.time * f.bob.speed + f.bob.ph) : Vector2.zero);
            }
        }


        public RectTransform MakeLabel(string name, out Text text, string icon)
        {
            var rt = K.Rect(WorldLayer, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(icon != null ? 96 : 70, 26));
            K.Img(rt, "ui.btnBasic", new Color(1, 0.98f, 0.93f, 0.95f), true);
            if (icon != null) K.Pic(rt, "Icon", icon, new Vector2(0, 0.5f), new Vector2(14, 1), new Vector2(24, 24));
            text = K.Txt(rt, "T", "", 13, Pal.Brown, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(icon != null ? 10 : 0, 1), new Vector2(icon != null ? 76 : 70, 24), true);
            return rt;
        }

        public Image AttachIcon(RectTransform parent, string id, Vector2 pos, float size)
            => K.Pic(parent, "AttachedIcon", id, new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));

        public RectTransform MakeTag(string name, out Text text, out Image icon)
        {
            var rt = K.Rect(WorldLayer, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(78, 30));
            K.Img(rt, "ui.btnBasic", new Color(1, 0.98f, 0.93f, 0.97f), true);
            icon = K.Pic(rt, "Icon", "ui.coin", new Vector2(0, 0.5f), new Vector2(14, 1), new Vector2(24, 24));
            text = K.Txt(rt, "T", "", 14, Pal.Brown, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(9, 1), new Vector2(58, 26), true);
            return rt;
        }

        public void SetTag(Text t, Image icon, string label, bool affordableStyle, bool lockOnly)
        {
            if (lockOnly) { t.text = ""; if (icon != null) { icon.sprite = Art.S("ui.lock"); icon.rectTransform.anchoredPosition = new Vector2(39, 1); } return; }
            t.text = label;
            bool isLevel = label.StartsWith("Cấp");
            if (icon != null) { icon.sprite = Art.S(isLevel ? "ui.lock" : "ui.coin"); icon.rectTransform.anchoredPosition = new Vector2(14, 1); }
            bool can = !isLevel && long.TryParse(label, out long cost) && cost <= M.S.currency;
            t.color = can ? Pal.GreenDark : Pal.Brown;
        }

        public RectTransform MakeBadge(string name, out Text text)
        {
            var rt = K.Rect(WorldLayer, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20, 20));
            K.Img(rt, "ui.whiteCircle", new Color(0.55f, 0.33f, 0.2f, 0.9f), false, false, false);
            text = K.Txt(rt, "T", "1", 12, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(20, 20), false, true);
            return rt;
        }

        public RectTransform MakePlusHint(string name)
        {
            var rt = K.Rect(WorldLayer, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34, 34));
            K.Img(rt, "ui.whiteCircle", new Color(1, 1, 1, 0.9f), false, false, false);
            K.Pic(rt, "Plus", "ui.plus", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22, 22));
            rt.gameObject.AddComponent<Bob>();
            return rt;
        }

        public RectTransform MakeOvenBubble(string name, out Image bg, out Image gauge, out Image icon, out Text time, out Image check)
        {
            var rt = K.Rect(WorldLayer, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52, 52));
            bg = K.Img(rt, "ui.whiteCircle", new Color(1, 1, 1, 0.95f), false, false, false);
            var g = K.Stretch(rt, "Gauge", 2, 2, 2, 2);
            gauge = K.Img(g, "ui.whiteCircle", new Color(0.98f, 0.66f, 0.32f, 0.85f), false, false, false);
            gauge.type = Image.Type.Filled; gauge.fillMethod = Image.FillMethod.Radial360; gauge.fillOrigin = (int)Image.Origin360.Top; gauge.fillClockwise = true;
            var inner = K.Stretch(rt, "Inner", 6, 6, 6, 6);
            K.Img(inner, "ui.whiteCircle", Color.white, false, false, false);
            icon = K.Pic(rt, "Icon", "bread.1", new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(34, 30));
            check = K.Pic(rt, "Check", "ui.check", new Vector2(1, 0), new Vector2(-4, 6), new Vector2(22, 22));
            var tbg = K.At(rt, "TimeBg", new Vector2(0.5f, 0), new Vector2(0, -8), new Vector2(64, 18));
            K.Img(tbg, "ui.btnBasic", new Color(0.45f, 0.28f, 0.18f, 0.85f), true);
            time = K.Txt(tbg, "Time", "", 12, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110, 18), true);
            return rt;
        }

        public RectTransform MakeBarLabel(string name, out Bar bar, string label)
        {
            var rt = K.Rect(WorldLayer, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110, 40));
            K.Img(rt, "ui.btnBasic", new Color(1, 0.98f, 0.93f, 0.95f), true);
            K.Txt(rt, "T", label, 12, Pal.Brown, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, -11), new Vector2(110, 18), true);
            bar = Bar.Make(rt, "Bar", new Vector2(0.5f, 0), new Vector2(0, 11), new Vector2(90, 9), Pal.Orange);
            return rt;
        }

        public RectTransform MakeEnergy(string name, out Bar bar, out Image mood)
        {
            var rt = K.Rect(WorldLayer, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 14));
            bar = Bar.Make(rt, "Bar", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52, 8), Pal.Green);
            mood = K.Pic(rt, "Mood", "bubble.sad", new Vector2(1, 0.5f), new Vector2(4, 12), new Vector2(26, 28));
            return rt;
        }

        public RectTransform MakeZzz(string name)
        {
            var rt = K.Rect(WorldLayer, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28, 32));
            K.Img(rt, "fx.zzz", Color.white);
            rt.gameObject.AddComponent<Bob>().amp = 6;
            return rt;
        }

        public RectTransform MakeCharBubble(string bubble, string inner)
        {
            var rt = K.Rect(WorldLayer, "CharBubble", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0), Vector2.zero, new Vector2(46, 50));
            if (bubble != null) K.Img(rt, bubble, Color.white);
            else K.Img(rt, "ui.whiteCircle", new Color(1, 1, 1, 0.95f), false, false, false);
            if (inner != null)
            {
                var ii = K.Pic(rt, "Inner", inner, new Vector2(0.5f, 0.5f), new Vector2(0, bubble != null ? 4 : 0), new Vector2(30, 26));
                if (bubble != null) { ii.rectTransform.anchoredPosition = new Vector2(16, 18); ii.rectTransform.sizeDelta = new Vector2(26, 22); }
            }
            Tw.Pop(rt, 0.3f, 0.25f);
            return rt;
        }

        public void ZoneTitle(string title, Vector3 world)
        {
            var rt = K.Rect(WorldLayer, "Zone_" + title, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 1), Vector2.zero, new Vector2(120, 26));
            K.Img(rt, "ui.btnBasic", new Color(0.47f, 0.29f, 0.18f, 0.85f), true);
            K.Txt(rt, "T", title, 14, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(120, 24), true);
            Follow(rt, () => world, Vector2.zero);
        }

        // ================================================================== toasts / float text / coin fly
        class ToastItem { public RectTransform rt; public float until; }
        readonly List<ToastItem> toasts = new List<ToastItem>();
        string lastToast; float lastToastAt;

        public void Toast(string msg, float dur = 2.2f)
        {
            if (msg == lastToast && Time.time - lastToastAt < 1.2f) return;
            lastToast = msg; lastToastAt = Time.time;
            foreach (var t in toasts) if (t.rt != null) Destroy(t.rt.gameObject);
            toasts.Clear();
            var rt = K.Rect(ToastLayer, "Toast", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, BottomInsetPx / Scale + 14), new Vector2(350, 46));
            K.Img(rt, "ui.btnBasic", new Color(0.36f, 0.22f, 0.14f, 0.92f), true);
            var tx = K.Txt(rt, "T", msg, 14, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(330, 44), true);
            Tw.Pop(rt, 0.8f, 0.2f);
            toasts.Add(new ToastItem { rt = rt, until = Time.time + dur });
        }

        void UpdateToasts()
        {
            for (int i = toasts.Count - 1; i >= 0; i--)
                if (Time.time > toasts[i].until) { if (toasts[i].rt) Destroy(toasts[i].rt.gameObject); toasts.RemoveAt(i); }
        }

        public void FloatText(Vector3 world, string text, Color color)
        {
            var rt = K.Rect(FxLayer, "Float", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160, 30));
            var t = K.Txt(rt, "T", text, 16, color, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160, 30), true, false, true);
            Vector2 sp = CameraRig.I.WorldToScreen(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(FxLayer, sp, null, out var lp);
            rt.anchoredPosition = lp;
            Tw.Run(rt, 1.0f, k => { if (!rt) return; rt.anchoredPosition = lp + Vector2.up * 40 * k; var c = t.color; c.a = 1 - k * k; t.color = c; }, Ease.OutQuad, () => { if (rt) Destroy(rt.gameObject); });
        }

        /// <summary>Visual only: the model already committed the money.</summary>
        public void CoinFly(Vector3 world, long amount)
        {
            int n = Mathf.Clamp((int)(amount / 10) + 2, 2, 6);
            Vector2 sp = CameraRig.I.WorldToScreen(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(FxLayer, sp, null, out var from);
            Vector2 tsp = RectTransformUtility.WorldToScreenPoint(null, coinIcon.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(FxLayer, tsp, null, out var to);
            for (int i = 0; i < n; i++)
            {
                var rt = K.Rect(FxLayer, "Coin", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), from, new Vector2(26, 26));
                K.Img(rt, "ui.coin");
                var jitter = new Vector2(UnityEngine.Random.Range(-30f, 30f), UnityEngine.Random.Range(10f, 50f));
                var mid = from + jitter;
                bool last = i == n - 1;
                Tw.Run(rt, 0.6f, k =>
                {
                    if (!rt) return;
                    var a = Vector2.Lerp(from, mid, Mathf.Min(1, k * 2.5f));
                    rt.anchoredPosition = Vector2.Lerp(a, to, Ease.InQuad(k));
                    rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.7f, k);
                }, Ease.Linear, () => { if (rt) Destroy(rt.gameObject); if (last) AudioMan.Play("se_Etc_GetCoin", 0.5f, 0.1f); }, i * 0.05f);
            }
        }

        // ================================================================== events
        void OnModelEvent(GameEvent e)
        {
            OnExtraEvent(e);
            switch (e.type)
            {
                case Ev.NotEnoughCoins: Toast($"Chưa đủ xu (thiếu {e.amount})."); break;
                case Ev.LevelTooLow: Toast($"Cần đạt cấp tiệm {e.a}."); break;
                case Ev.LevelUp: ShowBanner($"Tiệm lên cấp {e.a}!", "ui.level", "Mở thêm khách mới và thiết bị."); AudioMan.Play("se_Store_Fanfare", 0.8f); break;
                case Ev.RecipeUnlocked:
                    if (e.a > 1) { ShowBanner("Công thức mới!", Art.BreadBig(e.a), $"{M.C.Recipe(e.a).nameVi} (cấp {e.a}) — giá {M.C.Recipe(e.a).price} xu/chiếc"); AudioMan.Play("se_Etc_GetReward", 0.8f); }
                    break;
                case Ev.QuestCompleted: Tw.Pop(goalChip, 0.85f, 0.3f); AudioMan.Play("se_Etc_GetTicket", 0.6f); break;
                case Ev.QuestClaimed:
                    CoinFlyFromUI(claimBtn, e.amount); AudioMan.Play("se_Etc_GetReward", 0.8f); break;
                case Ev.HeartUp: Toast($"{M.C.Customer(e.s).nameVi} thân thiết hơn: {e.a}/3 tim!"); break;
                case Ev.CustomerMet: Toast($"Khách mới ghé tiệm: {M.C.Customer(e.s).nameVi}"); break;
                case Ev.AutoRestockUnlocked: Toast("Đã mở Tự bổ sung kệ! Bật/tắt trên từng kệ."); break;
                case Ev.UpgradeBought:
                case Ev.ExpansionBought:
                case Ev.FurnitureBought:
                case Ev.FurnitureUpgraded:
                    RebuildSheet(); break;
                case Ev.PendingSalesSettled: Toast($"{e.a} khách đã thanh toán trước khi tiệm đóng: +{e.amount} xu"); break;
            }
        }

        void CoinFlyFromUI(RectTransform src, long amount)
        {
            if (src == null) return;
            Vector2 sp = RectTransformUtility.WorldToScreenPoint(null, src.position);
            var w = CameraRig.I.cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, 10));
            CoinFly(w, amount);
        }

        public void OnUserInput() { handShownFor = -1; if (hand != null) hand.gameObject.SetActive(false); }
    }

    /// <summary>Gentle vertical bob for hint icons.</summary>
    public class Bob : MonoBehaviour
    {
        public float amp = 4f, speed = 3f, ph;
        void Awake() { ph = UnityEngine.Random.value * 6f; }
    }
}
