
// Builds the portrait world from real sprites and keeps it in sync with the model.
using System;
using System.Collections.Generic;
using System.Linq;
using BB.Model;
using BB.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace BB.View
{
    public class Hit
    {
        public Rect rect; public int prio; public string kind; public int index; public string sid;
        public Func<bool> enabled;
        public bool Contains(Vector2 p) => (enabled == null || enabled()) && rect.Contains(p);
    }

    public partial class World : MonoBehaviour
    {
        public static World I;
        public const float DoughScale = 1.2f;
        public GameModel M;
        public readonly List<Hit> hits = new List<Hit>();
        public readonly SlotView[] slots = new SlotView[GameModel.BoardMax];
        public readonly OvenView[] ovens = new OvenView[GameModel.OvenSlots];
        public readonly ShelfView[] shelves = new ShelfView[GameModel.ShelfSlots];
        public readonly Dictionary<string, FurnView> furn = new Dictionary<string, FurnView>();
        public readonly Dictionary<string, StaffView> staff = new Dictionary<string, StaffView>();
        public readonly Dictionary<long, CustomerView> customers = new Dictionary<long, CustomerView>();
        public ShopPaths paths;
        Transform bgRoot, objRoot, actorRoot;
        SpriteRenderer storage, kitchenTable, counter, piggy, stool;
        RectTransform storageLabel; Text storageText;
        RectTransform chefHint; Text chefHintText;
        RectTransform knead; Bar kneadBar;
        RectTransform checkout; Bar checkoutBar;
        // aggregated sale popup
        long saleAgg; float saleAggT; RectTransform salePop; Text salePopText;

        public void Build(GameModel m)
        {
            I = this; M = m;
            Frames.Init();
            bgRoot = new GameObject("BG").transform; bgRoot.SetParent(transform, false);
            objRoot = new GameObject("Objects").transform; objRoot.SetParent(transform, false);
            actorRoot = new GameObject("Actors").transform; actorRoot.SetParent(transform, false);
            Fx.Root = new GameObject("FX").transform; Fx.Root.SetParent(transform, false);
            paths = new ShopPaths(M.C.flow.walkSpeed);
            M.Layout = paths;
            BuildBreakRoom();
            BuildKitchen();
            BuildShop();
            BuildStaff();
            BuildStreet();
            M.OnEvent += OnModelEvent;
            SyncAll(true);
        }

        void OnDestroy() { if (M != null) M.OnEvent -= OnModelEvent; }

        Hit AddHit(Rect r, int prio, string kind, int index = -1, string sid = null, Func<bool> enabled = null)
        {
            var h = new Hit { rect = r, prio = prio, kind = kind, index = index, sid = sid, enabled = enabled };
            hits.Add(h); return h;
        }

        static Rect Box(Vector2 c, Vector2 size) => new Rect(c.x - size.x / 2, c.y - size.y / 2, size.x, size.y);

        // ================================================================== static scene
        void BuildBreakRoom()
        {
            float wallH = WL.BreakTop - WL.BreakWallBottom, floorH = WL.BreakWallBottom - WL.BreakBottom;
            SR.Tiled(bgRoot, "BreakWall", "bg.break.wall", new Vector2(0, WL.BreakTop - wallH / 2), new Vector2(WL.BgWidth, wallH), -1900);
            SR.Tiled(bgRoot, "BreakFloor", "bg.break.floor", new Vector2(0, WL.BreakWallBottom - floorH / 2), new Vector2(WL.BgWidth, floorH), -1950);
            var line = SR.Make(bgRoot, "BreakLine", "bg.line", new Vector2(0, WL.BreakWallBottom), -1890);
            line.transform.localScale = new Vector3(2.0f, 1.4f, 1); line.color = new Color(0.75f, 0.55f, 0.4f, 0.9f);
            SR.Bottom(objRoot, "StaffDoor", "bg.break.door", WL.StaffDoor, -1800);
            AddHit(Box(WL.StaffDoor + new Vector2(0, 0.7f), new Vector2(1.1f, 1.4f)), 1, "staffdoor");
            foreach (var f in M.C.furniture)
            {
                if (!WL.FurniturePos.TryGetValue(f.id, out var p)) continue;
                var v = new FurnView { id = f.id };
                bool flat = f.id == "rug";
                v.sr = flat ? SR.Make(objRoot, "Furn_" + f.id, "furn." + f.id, p, -1700) : SR.Bottom(objRoot, "Furn_" + f.id, "furn." + f.id, p);
                furn[f.id] = v;
                var b = v.sr.bounds;
                AddHit(new Rect(b.min.x, b.min.y, b.size.x, b.size.y), flat ? 1 : 3, "furniture", -1, f.id);
                v.tag = UIRoot.I.MakeTag("Tag_" + f.id, out v.tagText, out v.tagIcon);
                var center = (Vector3)(Vector2)b.center;
                UIRoot.I.Follow(v.tag, () => center, Vector2.zero);
            }
        }

        void BuildKitchen()
        {
            float wallH = WL.KitchenTop - WL.KitchenWallBottom, floorH = WL.KitchenWallBottom - WL.KitchenBottom;
            SR.Tiled(bgRoot, "KitchenWall", "bg.kitchen.wall", new Vector2(0, WL.KitchenTop - wallH / 2), new Vector2(WL.BgWidth, wallH), -1900);
            SR.Tiled(bgRoot, "KitchenFloor", "bg.kitchen.floor", new Vector2(0, WL.KitchenWallBottom - floorH / 2), new Vector2(WL.BgWidth, floorH), -1950);
            var line = SR.Make(bgRoot, "KitchenLine", "bg.line", new Vector2(0, WL.KitchenWallBottom), -1890);
            line.transform.localScale = new Vector3(2.0f, 1.4f, 1); line.color = new Color(0.55f, 0.45f, 0.35f, 0.9f);
            SR.Make(objRoot, "Window", "bg.kitchen.window", WL.WindowPos, -1850);
            var sun = SR.Make(objRoot, "Sunlight", "bg.kitchen.sunlight", WL.WindowPos + new Vector2(0.9f, -1.8f), -1840, 0.9f);
            sun.color = new Color(1, 1, 1, 0.28f);
            SR.Bottom(objRoot, "Fridge", "bg.kitchen.fridge", WL.FridgePos);
            storage = SR.Bottom(objRoot, "Storage", "bg.kitchen.storage", WL.StoragePos);
            var sb = storage.bounds;
            AddHit(new Rect(sb.min.x, sb.min.y, sb.size.x, sb.size.y), 4, "storage");
            kitchenTable = SR.Make(objRoot, "KitchenTable", "bg.kitchen.table", WL.KitchenTablePos, WL.SortOrder(WL.KitchenTablePos.y - 0.7f));
            AddHit(Box(WL.KitchenTablePos + new Vector2(0, 0.15f), new Vector2(3.1f, 1.2f)), 2, "table");
            storageLabel = UIRoot.I.MakeLabel("StorageLabel", out storageText, "ui.inventory");
            UIRoot.I.Follow(storageLabel, () => new Vector3(WL.StoragePos.x + 0.1f, WL.StoragePos.y + 1.5f), Vector2.zero);
            for (int i = 0; i < GameModel.OvenSlots; i++) BuildOven(i);
            for (int i = 0; i < GameModel.BoardMax; i++) BuildSlot(i);
        }

        void BuildOven(int i)
        {
            var v = new OvenView { index = i };
            v.root = new GameObject("Oven" + i).transform; v.root.SetParent(objRoot, false);
            v.root.position = v.Pos;
            int ord = WL.SortOrder(v.Pos.y - 0.95f);
            v.body = SR.Make(v.root, "Body", "equip.oven", v.Pos, ord);
            v.fire = SR.Make(v.root, "Fire", "equip.ovenFire", v.Pos + new Vector2(0.006f, -0.778f), ord + 1);
            v.doorOpen = SR.Make(v.root, "DoorOpen", "equip.ovenDoorOpen", v.Pos + new Vector2(-0.188f, -0.307f), ord + 2, 1.35f);
            v.doorClosed = SR.Make(v.root, "DoorClosed", "equip.ovenDoorClosed", v.Pos + new Vector2(-0.32f, -0.307f), ord + 2, 1.5f);
            ovens[i] = v;
            int idx = i;
            AddHit(Box(v.Pos, WL.OvenSize), 5, "oven", i, null, () => M.S.ovenJobs[idx].owned || M.NextExpansionIndex(1) == idx);
            v.bubble = UIRoot.I.MakeOvenBubble("OvenBubble" + i, out v.bubbleBg, out v.gauge, out v.icon, out v.time, out v.check);
            UIRoot.I.Follow(v.bubble, () => (Vector3)(v.Pos + new Vector2(0, 0.62f)), Vector2.zero);
            v.tag = UIRoot.I.MakeBuySpot("OvenBuy" + i, out v.tagText, out v.tagIcon);
            UIRoot.I.Follow(v.tag, () => (Vector3)v.Pos, Vector2.zero);
            v.warn = UIRoot.I.MakeLabel("OvenWarn" + i, out v.warnText, "ui.issue");
            UIRoot.I.Follow(v.warn, () => (Vector3)(v.Pos + new Vector2(0, -1.05f)), Vector2.zero);
            v.warn.gameObject.SetActive(false);
        }

        void BuildSlot(int i)
        {
            var v = new SlotView { index = i };
            v.table = SR.Make(objRoot, "Slot" + i, "equip.boardSlot", v.Pos, WL.SortOrder(v.Pos.y - 0.65f));
            v.dough = SR.Make(objRoot, "Dough" + i, null, v.DoughPos, WL.SortOrder(v.Pos.y - 0.65f) + 3);
            v.dough.transform.localScale = Vector3.one * DoughScale;
            slots[i] = v;
            int idx = i;
            AddHit(Box(v.Pos, WL.SlotSize), 6, "slot", i, null, () => idx < M.S.boardUnlocked || M.NextExpansionIndex(0) == idx);
            v.badge = UIRoot.I.MakeBadge("Badge" + i, out v.badgeText);
            UIRoot.I.Follow(v.badge, () => (Vector3)(v.DoughPos + new Vector2(0.38f, -0.22f)), Vector2.zero);
            v.buy = UIRoot.I.MakeBuySpot("SlotBuy" + i, out v.buyText, out v.buyIcon);
            UIRoot.I.Follow(v.buy, () => (Vector3)v.Pos, Vector2.zero);
        }

        void BuildShop()
        {
            float wallH = WL.ShopTop - WL.ShopWallBottom;
            SR.Tiled(bgRoot, "ShopWall", "bg.shop.wall", new Vector2(0, WL.ShopTop - wallH / 2), new Vector2(WL.BgWidth, wallH), -1900);
            float floorTop = WL.ShopWallBottom, floorBot = WL.FacadeTop;
            SR.Tiled(bgRoot, "ShopFloor", "bg.shop.floor", new Vector2(0, (floorTop + floorBot) / 2), new Vector2(WL.BgWidth, floorTop - floorBot), -1950);
            // storefront
            var fac = SR.Make(objRoot, "Facade", "bg.shop.facade", new Vector2(0, WL.FacadeCenterY), WL.SortOrder(WL.FacadeBottom));
            fac.sortingOrder = WL.SortOrder(WL.FacadeBottom);
            // counter wall decor + delivery corner (sign with the dog face, left of the counter)
            SR.Make(objRoot, "Menu", "bg.shop.menu", new Vector2(0, -19.25f), -1850);
            SR.Make(objRoot, "Light1", "bg.shop.light", new Vector2(-1.3f, -19.1f), -1840);
            SR.Make(objRoot, "Light2", "bg.shop.light", new Vector2(1.3f, -19.1f), -1840);
            var sign = SR.Make(objRoot, "DeliverySign", "bg.shop.deliverySign", WL.DeliverySignPos, -1845, 1.1f);
            SR.Make(objRoot, "DeliverySignDog", "bg.shop.deliveryDog", WL.DeliverySignPos + new Vector2(0, 0.06f), -1844, 0.62f);
            AddHit(new Rect(sign.bounds.min.x, sign.bounds.min.y, sign.bounds.size.x, sign.bounds.size.y), 4, "dog");
            SR.Make(objRoot, "MilkFridge", "bg.shop.milkfridge", WL.MilkFridgePos, WL.SortOrder(WL.MilkFridgePos.y - 0.7f));
            for (int k = 0; k < 3; k++) SR.Make(objRoot, "Milk" + k, "bg.shop.milk", WL.MilkFridgePos + new Vector2(-0.38f + 0.38f * k, 0.18f), WL.SortOrder(WL.MilkFridgePos.y - 0.7f) + 1);
            counter = SR.Make(objRoot, "Counter", "bg.shop.counter", WL.CounterPos, WL.SortOrder(WL.CounterPos.y - 0.65f));
            SR.Make(objRoot, "Calculator", "bg.shop.calculator", new Vector2(1.0f, -20.89f), counter.sortingOrder + 2);
            SR.Make(objRoot, "MenuSmall", "bg.shop.menuSmall", new Vector2(0.55f, -20.9f), counter.sortingOrder + 2, 0.8f);
            piggy = SR.Make(objRoot, "Piggy", "ui.piggyEmpty", WL.PiggyPos, counter.sortingOrder + 3, 0.62f);
            AddHit(Box(WL.PiggyPos, new Vector2(0.9f, 0.9f)), 8, "piggy");
            AddHit(Box(WL.CounterPos + new Vector2(0, 0.5f), new Vector2(2.6f, 1.6f)), 3, "counter");
            for (int i = 0; i < GameModel.ShelfSlots; i++) BuildShelf(i);
            salePop = UIRoot.I.MakeLabel("SalePop", out salePopText, "ui.coin");
            UIRoot.I.Follow(salePop, () => (Vector3)(WL.CounterPos + new Vector2(0.5f, 1.65f)), Vector2.zero);
            salePop.gameObject.SetActive(false);
        }

        void BuildShelf(int i)
        {
            var v = new ShelfView { index = i };
            int ord = WL.SortOrder(v.Pos.y - 0.65f);
            v.stand = SR.Make(objRoot, "Shelf" + i, "equip.shelf", v.Pos, ord);
            for (int k = 0; k < WL.BreadOnShelf.Length; k++)
                v.breads.Add(SR.Make(v.stand.transform, "Bread" + k, null, v.Pos + WL.BreadOnShelf[k], ord + 1 + k));
            shelves[i] = v;
            int idx = i;
            AddHit(Box(v.Pos, WL.ShelfSize + new Vector2(0.0f, 0.1f)), 6, "shelf", i, null, () => M.S.shelfStock[idx].owned || M.NextExpansionIndex(2) == idx);
            v.label = UIRoot.I.MakeLabel("ShelfLabel" + i, out v.labelText, null);
            v.label.sizeDelta = new Vector2(58, 24);
            UIRoot.I.Follow(v.label, () => (Vector3)(v.Pos + new Vector2(0, -0.78f)), Vector2.zero);
            v.autoIcon = UIRoot.I.AttachIcon(v.label, "ui.auto", new Vector2(-26, 0), 20);
            v.plus = UIRoot.I.MakePlusHint("ShelfPlus" + i);
            UIRoot.I.Follow(v.plus, () => (Vector3)(v.Pos + new Vector2(0, 0.25f)), Vector2.zero);
            v.tag = UIRoot.I.MakeBuySpot("ShelfBuy" + i, out v.tagText, out v.tagIcon);
            UIRoot.I.Follow(v.tag, () => (Vector3)v.Pos, Vector2.zero);
        }

        // ================================================================== staff
        void BuildStaff()
        {
            MakeStaff("chef", 0, WL.ChefPos, "chef", kitchenTable.sortingOrder - 3);
            MakeStaff("cashier", 1, WL.CashierPos, "cashier", counter.sortingOrder - 3);
            stool = SR.Bottom(objRoot, "Stool", "bg.street.chair", WL.StoolPos);
            MakeStaff("promoter", 2, WL.PromoterPos, "promoter", stool.sortingOrder + 2);
            var cash = staff["cashier"];
            cash.extra = SR.Make(cash.root, "Basket", "cashier.Bread", WL.CashierPos + new Vector2(-0.34f, -0.25f), 2);
            // characters are not tapped to work faster any more (bottom-bar speed-up button); a tap explains that
            AddHit(Box(WL.ChefPos + new Vector2(0, 0.85f), new Vector2(1.5f, 1.5f)), 7, "chef");
            AddHit(Box(WL.CashierPos + new Vector2(0, 0.6f), new Vector2(1.1f, 1.1f)), 7, "cashier");
            AddHit(Box(WL.PromoterPos + new Vector2(0, 0.35f), new Vector2(1.0f, 1.2f)), 7, "promoter");
            chefHint = UIRoot.I.MakeLabel("ChefHint", out chefHintText, "ui.issue");
            UIRoot.I.Follow(chefHint, () => (Vector3)(WL.ChefPos + new Vector2(0.9f, 1.55f)), Vector2.zero);
            chefHint.gameObject.SetActive(false);
            knead = UIRoot.I.MakeBarLabel("Knead", out kneadBar, "Tự nhồi bột");
            UIRoot.I.Follow(knead, () => (Vector3)(WL.KitchenTablePos + new Vector2(0, 0.9f)), Vector2.zero);
            checkout = UIRoot.I.MakeBarLabel("Checkout", out checkoutBar, "Tự tính tiền");
            UIRoot.I.Follow(checkout, () => (Vector3)(WL.CashierPos + new Vector2(0, 1.3f)), Vector2.zero);
        }

        void MakeStaff(string id, int idx, Vector2 station, string prefix, int order)
        {
            var v = new StaffView { id = id, index = idx, station = station, idleKey = prefix + ".idle", workKey = prefix + ".work", fastKey = prefix + ".fast", restKey = prefix + ".rest" };
            v.root = new GameObject("Staff_" + id).transform; v.root.SetParent(actorRoot, false);
            v.root.position = station;
            v.group = v.root.gameObject.AddComponent<SortingGroup>();
            v.group.sortingOrder = order;
            var body = SR.Make(v.root, "Body", null, station, 1);
            v.anim = body.gameObject.AddComponent<FrameAnim>();
            v.anim.Play(v.idleKey, true, UnityEngine.Random.value);
            if (id == "promoter") body.flipX = true;
            staff[id] = v;
            v.energy = UIRoot.I.MakeEnergy("Energy_" + id, out v.energyBar, out v.moodIcon);
            UIRoot.I.Follow(v.energy, () => StaffHead(v), Vector2.zero);
            v.zzz = UIRoot.I.MakeZzz("Zzz_" + id);
            UIRoot.I.Follow(v.zzz, () => (Vector3)(v.root.position + new Vector3(0.5f, 1.1f)), Vector2.zero);
            v.zzz.gameObject.SetActive(false);
            v.boostTag = UIRoot.I.MakeBoostTag("Boost_" + id);
            UIRoot.I.Follow(v.boostTag, () => StaffHead(v) + new Vector3(0.55f, 0.1f), Vector2.zero);
            v.boostTag.gameObject.SetActive(false);
        }

        Vector3 StaffHead(StaffView v)
        {
            float h = v.id == "chef" ? 1.75f : v.id == "cashier" ? 1.25f : 0.85f;
            if (v.inBreakRoom) h = 1.25f;
            return v.root.position + new Vector3(0, h, 0);
        }

        // ================================================================== sync
        public void SyncAll(bool instant)
        {
            for (int i = 0; i < slots.Length; i++) SyncSlot(slots[i], instant);
            foreach (var o in ovens) SyncOven(o, instant);
            foreach (var s in shelves) SyncShelf(s, instant);
            foreach (var f in furn.Values) SyncFurniture(f, instant);
            SyncTrash(instant);
        }

        /// <summary>Owned items are drawn normally; only the next purchasable one is shown (ghosted, big "+" and price).</summary>
        void SyncSlot(SlotView v, bool instant)
        {
            var d = M.S.boardSlots[v.index];
            bool unlocked = v.index < M.S.boardUnlocked;
            bool nextBuy = !unlocked && M.NextExpansionIndex(0) == v.index;
            v.table.enabled = unlocked || nextBuy;
            if (!unlocked) v.table.color = new Color(1f, 0.97f, 0.92f, 0.42f);
            else if (hlLevel < 0) v.table.color = Color.white;
            v.buy.gameObject.SetActive(nextBuy);
            if (nextBuy) { int cost = M.ExpansionCost(0, out int req); UIRoot.I.SetBuySpot(v.buy, v.buyText, v.buyIcon, cost, req); }
            if (v.hiddenForDrag) { v.dough.enabled = false; v.badge.gameObject.SetActive(false); return; }
            if (d.level == 0 || !unlocked) { v.dough.enabled = false; v.shownId = 0; v.shownLevel = 0; v.badge.gameObject.SetActive(false); return; }
            v.dough.enabled = true;
            if (d.id != v.shownId || d.level != v.shownLevel)
            {
                v.dough.sprite = Art.S(Art.Dough(d.level));
                v.dough.transform.position = v.DoughPos;
                v.dough.transform.localScale = Vector3.one * DoughScale;
                if (!instant) Tw.Pop(v.dough.transform, 0.4f, 0.28f, Vector3.one * DoughScale);
                v.shownId = d.id; v.shownLevel = d.level;
            }
            v.badge.gameObject.SetActive(true);
            v.badgeText.text = d.level.ToString();
        }

        void SyncOven(OvenView v, bool instant)
        {
            var o = M.S.ovenJobs[v.index];
            bool owned = o.owned;
            bool nextBuy = !owned && M.NextExpansionIndex(1) == v.index;
            if (owned != v.shownOwned || instant)
            {
                v.body.color = owned ? Color.white : new Color(1, 1, 1, 0.33f);
                v.doorClosed.enabled = owned; v.doorOpen.enabled = owned; v.fire.enabled = owned;
                if (owned && !instant && v.shownOwned == false && v.shownState != -1) Install(v.root, v.Pos, "se_Kitchen_OvenConstructionCompletion");
                v.shownOwned = owned;
            }
            v.body.enabled = owned || nextBuy;
            v.tag.gameObject.SetActive(nextBuy);
            if (!owned)
            {
                if (nextBuy) { int cost = M.ExpansionCost(1, out int req); UIRoot.I.SetBuySpot(v.tag, v.tagText, v.tagIcon, cost, req); }
                v.doorClosed.enabled = v.doorOpen.enabled = v.fire.enabled = false;
                v.bubble.gameObject.SetActive(false); v.warn.gameObject.SetActive(false);
                v.shownState = 0;
                return;
            }
            var st = (OvenState)o.state;
            v.doorClosed.enabled = st == OvenState.Baking;
            v.doorOpen.enabled = st != OvenState.Baking;
            v.fire.enabled = st == OvenState.Baking;
            if (st == OvenState.Baking) v.fire.color = new Color(1, 1, 1, Mathf.Lerp(0.78f, 1f, 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI)));
            if (hlLevel < 0) v.body.color = st == OvenState.Burnt ? new Color(0.55f, 0.45f, 0.42f) : Color.white;
            v.bubble.gameObject.SetActive(st != OvenState.Empty);
            if (st == OvenState.Baking)
            {
                v.gauge.fillAmount = 1f - o.remaining / Mathf.Max(0.01f, o.total);
                v.icon.sprite = Art.S(Art.Bread(o.recipe)); v.icon.color = new Color(1, 1, 1, 0.55f);
                v.time.text = Mathf.CeilToInt(o.remaining) + "s";
                v.check.enabled = false;
                v.bubble.localScale = Vector3.one;
            }
            else if (st == OvenState.Ready)
            {
                v.gauge.fillAmount = 1; v.icon.sprite = Art.S(Art.Bread(o.recipe)); v.icon.color = Color.white;
                v.check.enabled = true;
                float left = M.BurnLeft(v.index);
                v.time.text = left >= 0 && left < M.C.burn.warnSeconds ? $"Cháy sau {Mathf.CeilToInt(left)}s" : "Xong!";
                float b = 1f + 0.08f * Mathf.Abs(Mathf.Sin(Time.time * 4f + v.index));
                v.bubble.localScale = new Vector3(b, b, 1);
            }
            else if (st == OvenState.Burnt)
            {
                v.gauge.fillAmount = 0; v.icon.sprite = Art.S(Art.Bread(Mathf.Max(1, o.recipe))); v.icon.color = new Color(0.2f, 0.15f, 0.12f);
                v.check.enabled = false; v.time.text = "Cháy! Chạm để dọn";
            }
            if (st == OvenState.Baking)
            {
                v.smokeT -= Time.deltaTime;
                if (v.smokeT <= 0) { v.smokeT = 0.9f; Fx.Smoke(v.Chimney, v.body.sortingOrder + 5); }
            }
            if (st != OvenState.Ready && v.warn.gameObject.activeSelf && st != OvenState.Burnt) v.warn.gameObject.SetActive(false);
            v.shownState = o.state;
        }

        void SyncShelf(ShelfView v, bool instant)
        {
            var s = M.S.shelfStock[v.index];
            bool nextBuy = !s.owned && M.NextExpansionIndex(2) == v.index;
            if (s.owned != v.shownOwned || instant)
            {
                v.stand.color = s.owned ? Color.white : new Color(1f, 0.97f, 0.92f, 0.4f);
                if (s.owned && !instant && !v.shownOwned && v.shownStock != -1) Install(v.stand.transform, v.Pos, "se_Store_StandConstructionCompletion");
                v.shownOwned = s.owned;
            }
            v.stand.enabled = s.owned || nextBuy;
            v.tag.gameObject.SetActive(nextBuy);
            if (!s.owned)
            {
                if (nextBuy) { int cost = M.ExpansionCost(2, out int req); UIRoot.I.SetBuySpot(v.tag, v.tagText, v.tagIcon, cost, req); }
                foreach (var b in v.breads) b.enabled = false;
                v.label.gameObject.SetActive(false); v.plus.gameObject.SetActive(false);
                v.shownStock = 0;
                return;
            }
            int show = Mathf.Min(s.stock, v.breads.Count);
            for (int k = 0; k < v.breads.Count; k++)
            {
                bool on = k < show;
                if (on && (!v.breads[k].enabled || v.shownRecipe != s.recipe)) { v.breads[k].sprite = Art.S(Art.Bread(Mathf.Max(1, s.recipe))); if (!instant && v.shownStock >= 0 && k >= v.shownStock) Tw.Pop(v.breads[k].transform, 0.3f, 0.25f); }
                v.breads[k].enabled = on;
            }
            v.label.gameObject.SetActive(s.stock > 0 || s.recipe > 0);
            v.labelText.text = s.stock > 0 ? $"{s.stock}/{M.ShelfCap}" : "Hết";
            v.labelText.color = s.stock > 0 ? Pal.Brown : Pal.Red;
            v.autoIcon.enabled = M.S.autoRestockUnlocked && s.auto;
            bool canStock = M.StorageUsed > 0 && (s.stock == 0 ? true : M.S.inventory[s.recipe - 1] > 0 && s.stock < M.ShelfCap);
            v.plus.gameObject.SetActive(canStock && !(s.auto && M.S.autoRestockUnlocked && s.stock > 0));
            v.shownStock = s.stock; v.shownRecipe = s.recipe;
        }

        void SyncFurniture(FurnView v, bool instant)
        {
            var d = M.Furn(v.id); var cfg = M.C.Furniture(v.id);
            if (d.owned != v.shownOwned || d.level != v.shownLevel || instant)
            {
                v.sr.color = d.owned ? Color.white : new Color(1, 1, 1, 0.33f);
                if (!instant && d.owned && !v.shownOwned) Install(v.sr.transform, (Vector2)v.sr.bounds.center, "se_Etc_SetFurniture");
                else if (!instant && d.level > v.shownLevel && v.shownOwned) { Tw.Squash(v.sr.transform, 0.12f, 0.3f, Vector3.one); Fx.Stars(v.sr.bounds.center, 5); AudioMan.Play("se_Etc_Upgrade"); }
                v.shownOwned = d.owned; v.shownLevel = d.level;
            }
            int cost = M.FurnitureCost(v.id);
            v.tag.gameObject.SetActive(!d.owned);
            if (!d.owned) UIRoot.I.SetTag(v.tagText, v.tagIcon, M.ShopLevel < cfg.levelReq ? $"Cấp {cfg.levelReq}" : cost.ToString(), M.ShopLevel >= cfg.levelReq, false);
            if (d.owned && v.id == "coffee")
            {
                v.steamT -= Time.deltaTime;
                if (v.steamT <= 0) { v.steamT = 1.6f; Fx.Smoke((Vector2)v.sr.bounds.center + new Vector2(-0.05f, 0.35f), v.sr.sortingOrder + 1); }
            }
        }

        /// <summary>Installation animation: drop + bounce (Furniture.anim squash pattern) + dust.</summary>
        public void Install(Transform tr, Vector2 at, string sfx)
        {
            Vector3 baseScale = tr.localScale; Vector3 basePos = tr.position;
            tr.position = basePos + Vector3.up * 0.8f;
            Tw.Run(tr, 0.28f, t => { if (tr) tr.position = Vector3.LerpUnclamped(basePos + Vector3.up * 0.8f, basePos, t); }, Ease.InQuad, () =>
            {
                if (!tr) return;
                tr.position = basePos;
                Tw.Run(tr, 0.45f, t =>
                {
                    float sx = 1f + 0.07f * Mathf.Sin(t * Mathf.PI * 2f) * (1 - t);
                    float sy = 1f - 0.07f * Mathf.Sin(t * Mathf.PI * 2f) * (1 - t);
                    if (tr) tr.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, 1);
                }, Ease.Linear, () => { if (tr) tr.localScale = baseScale; });
                Fx.Dust(at + Vector2.down * 0.5f, 7, 0.8f, 0.8f);
                Fx.Stars(at, 4, 0.9f);
                AudioMan.Play(sfx);
            });
        }
    }
}
