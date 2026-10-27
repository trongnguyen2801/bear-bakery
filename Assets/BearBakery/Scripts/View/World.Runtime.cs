using System.Collections.Generic;
using System.Linq;
using BB.Model;
using BB.UI;
using UnityEngine;
using UnityEngine.Rendering;

namespace BB.View
{
    public partial class World
    {
        readonly float[] slotHoldUntil = new float[GameModel.BoardMax];
        float helpLimitToastAt, fullToastAt;
        readonly List<long> toRemove = new List<long>();

        void Update()
        {
            if (M == null) return;
            for (int i = 0; i < slots.Length; i++) if (Time.time >= slotHoldUntil[i]) SyncSlot(slots[i], false);
            foreach (var o in ovens) SyncOven(o, false);
            foreach (var s in shelves) SyncShelf(s, false);
            foreach (var f in furn.Values) SyncFurniture(f, false);
            UpdateLabels();
            foreach (var v in staff.Values) UpdateStaff(v);
            UpdateCustomers();
            UpdateStreet();
            if (saleAggT > 0) { saleAggT -= Time.deltaTime; if (saleAggT <= 0) { salePop.gameObject.SetActive(false); saleAgg = 0; } }
        }

        void UpdateLabels()
        {
            storageText.text = $"Kho {M.StorageUsed}/{M.StorageCap}";
            storageText.color = M.StorageUsed >= M.StorageCap ? Pal.Red : Pal.Brown;
            long tip = M.S.tipJar;
            piggyLabel.gameObject.SetActive(tip > 0);
            piggyText.text = "+" + tip;
            piggy.sprite = Art.S(tip <= 0 ? "ui.piggyEmpty" : tip < 60 ? "ui.piggyIng" : "ui.piggyFull");
            var chef = M.Staff("chef");
            bool chefAway = M.IsAway(chef);
            chefHint.gameObject.SetActive(!chefAway && chef.state == (int)StaffState.IdleBlocked);
            chefHintText.text = "Bàn đầy!";
            knead.gameObject.SetActive(chefAway && M.FirstEmptySlot() >= 0);
            kneadBar.Set(M.ManualKneadProgress);
            var cash = M.Staff("cashier");
            bool cashAway = M.IsAway(cash);
            checkout.gameObject.SetActive(cashAway && M.FrontReady() != null);
            checkoutBar.Set(M.ManualCheckoutProgress);
        }

        // ================================================================== staff visuals
        void UpdateStaff(StaffView v)
        {
            var st = M.Staff(v.id);
            var state = (StaffState)st.state;
            Vector2 pos = v.station; float alpha = 1; bool inBreak = false; string anim;
            float transit = Mathf.Max(0.01f, M.C.energy.transitSeconds);
            Vector2 rest = WL.RestPos.TryGetValue(st.restSpot ?? "", out var rp) ? rp : WL.StaffDoor;
            switch (state)
            {
                case StaffState.ToRest:
                {
                    float k = 1f - st.timer / transit;
                    if (k < 0.5f) { alpha = 1f - k * 2f; pos = v.station + Vector2.up * 0.25f * Mathf.Sin(k * 2f * Mathf.PI); }
                    else { inBreak = true; float u = (k - 0.5f) * 2f; pos = Vector2.Lerp(WL.StaffDoor, rest, u) + Vector2.up * 0.15f * Mathf.Abs(Mathf.Sin(u * Mathf.PI * 3f)); alpha = Mathf.Min(1, u * 3f); }
                    anim = v.idleKey; break;
                }
                case StaffState.Resting: inBreak = true; pos = rest; anim = v.restKey; break;
                case StaffState.Returning:
                {
                    float k = 1f - st.timer / transit;
                    if (k < 0.5f) { inBreak = true; float u = k * 2f; pos = Vector2.Lerp(rest, WL.StaffDoor, u) + Vector2.up * 0.15f * Mathf.Abs(Mathf.Sin(u * Mathf.PI * 3f)); alpha = 1f - u * 0.8f; }
                    else { float u = (k - 0.5f) * 2f; alpha = u; pos = v.station + Vector2.up * 0.25f * Mathf.Sin(u * Mathf.PI); }
                    anim = v.idleKey; break;
                }
                case StaffState.RestInPlace: anim = v.restKey; break;
                default:
                {
                    bool working;
                    if (v.id == "chef") working = state != StaffState.IdleBlocked;
                    else if (v.id == "cashier") working = M.CurrentPaying() != null;
                    else working = M.PromoterActive && !M.MarketingWaiting;
                    anim = working ? v.workKey : v.idleKey;
                    // speed-up button: the zone character works in its fast animation (+ sparkles) while boosted
                    if ((working && M.BoostActive(v.id)) || Time.time < v.fastUntil) anim = v.fastKey;
                    if (working && M.BoostActive(v.id))
                    {
                        v.sparkleT -= Time.deltaTime;
                        if (v.sparkleT <= 0) { v.sparkleT = 0.45f; Fx.Stars((Vector2)StaffHead(v) + new Vector2(Random.Range(-0.4f, 0.4f), -0.3f), 1, 0.4f, "fx.twinkle1", 0.32f); }
                    }
                    break;
                }
            }
            v.inBreakRoom = inBreak;
            v.root.position = pos;
            // anchor sprites to root position (children were created at station)
            v.anim.transform.position = pos;
            if (v.shadow != null) { v.shadow.transform.position = pos + new Vector2(0, 0.02f); v.shadow.enabled = !inBreak; }
            if (v.id == "promoter" && stool != null) v.anim.sr.flipX = !inBreak;
            if (v.extra != null) { v.extra.transform.position = pos + new Vector2(-0.34f, -0.25f); v.extra.enabled = !inBreak && (anim == v.workKey || anim == v.fastKey); }
            v.anim.Play(anim);
            v.anim.speed = M.IsTired(st) && !M.IsAway(st) ? M.C.energy.tiredSpeedMult : 1f;
            var c = v.anim.sr.color; c.a = alpha; v.anim.sr.color = c;
            if (inBreak) v.group.sortingOrder = WL.SortOrder(pos.y - 0.9f);   // in front of the furniture it uses
            else v.group.sortingOrder = v.id == "chef" ? kitchenTable.sortingOrder - 3 : v.id == "cashier" ? counter.sortingOrder - 3 : stool.sortingOrder + 2;
            bool boostShown = !inBreak && alpha > 0.5f && M.BoostActive(v.id);
            if (v.boostTag.gameObject.activeSelf != boostShown) v.boostTag.gameObject.SetActive(boostShown);
            bool resting = state == StaffState.Resting || state == StaffState.RestInPlace;
            v.zzz.gameObject.SetActive(resting);
            bool showEnergy = st.energy < 60 || M.IsAway(st) || Time.time < v.fastUntil + 1.5f;
            v.energy.gameObject.SetActive(showEnergy && alpha > 0.5f);
            v.energyBar.Set(st.energy / 100f);
            v.energyBar.fill.color = st.energy < M.C.energy.tiredThreshold ? Pal.Red : st.energy < 60 ? Pal.Orange : Pal.Green;
            v.moodIcon.enabled = M.IsTired(st) && !resting;
        }

        public Vector3 StaffWorldPos(string id) => staff.TryGetValue(id, out var v) ? v.root.position : Vector3.zero;

        // ================================================================== customers
        void UpdateCustomers()
        {
            foreach (var c in M.Customers)
            {
                if (!customers.TryGetValue(c.id, out var v)) v = customers[c.id] = MakeCustomer(c);
                UpdateCustomer(v, c);
            }
            toRemove.Clear();
            foreach (var kv in customers) if (M.FindCustomer(kv.Key) == null) toRemove.Add(kv.Key);
            foreach (var id in toRemove)
            {
                var v = customers[id];
                if (v.bubble != null) UIRoot.I.Unfollow(v.bubble, true);
                Destroy(v.root.gameObject);
                customers.Remove(id);
            }
        }

        CustomerView MakeCustomer(Customer c)
        {
            var v = new CustomerView { id = c.id, type = c.type };
            v.root = new GameObject("Cust_" + c.type + "_" + c.id).transform; v.root.SetParent(actorRoot, false);
            v.group = v.root.gameObject.AddComponent<SortingGroup>();
            v.shadow = SR.Make(v.root, "Shadow", "char.shadow", Vector2.zero, 0, 1f); v.shadow.color = new Color(1, 1, 1, 0.5f);
            var body = SR.Make(v.root, "Body", $"cust.{c.type}.Idle01", Vector2.zero, 1);
            v.anim = body.gameObject.AddComponent<FrameAnim>();
            v.holdH = body.sprite.bounds.size.y * 0.36f;
            v.tray = SR.Make(v.root, "Tray", "prop.trayBread", Vector2.zero, 3, 0.9f);
            v.trayBread = SR.Make(v.root, "TrayBread", Art.Bread(Mathf.Max(1, c.recipe)), Vector2.zero, 4, 0.8f);
            v.bag = SR.Make(v.root, "Bag", "prop.bag", Vector2.zero, 3, 1f);
            v.tray.enabled = v.trayBread.enabled = v.bag.enabled = false;
            v.anim.Play($"cust.{c.type}.walk", true, Random.value);
            v.root.position = WL.StreetIn;
            v.pos = WL.StreetIn;
            return v;
        }

        void SetAlpha(CustomerView v, float a)
        {
            if (Mathf.Approximately(v.alpha, a)) return;
            v.alpha = a;
            foreach (var sr in new[] { v.anim.sr, v.shadow, v.tray, v.trayBread, v.bag })
            { var col = sr.color; col.a = sr == v.shadow ? 0.5f * a : a; sr.color = col; }
        }

        void UpdateCustomer(CustomerView v, Customer c)
        {
            if (c.phase != v.shownPhase)
            {
                OnCustomerPhase(v, c);
                v.shownPhase = c.phase;
            }
            Vector2 pos = v.pos; float dx = 0, alpha = 1; bool walking = false;
            switch (c.phase)
            {
                case CPhase.Enter:
                case CPhase.ToQueue:
                case CPhase.Leave:
                case CPhase.SadVisit:
                    if (v.path != null)
                    {
                        float scale = c.dur > 0 ? v.path.Duration / c.dur : 1f;
                        v.path.Eval(c.t * scale, out pos, out dx, out alpha, out walking);
                    }
                    break;
                case CPhase.Pick:
                    pos = WL.StandPoint(c.shelf);
                    break;
                case CPhase.Queue:
                case CPhase.Paying:
                    if (c.queueIndex != v.shownQueue)
                    {
                        v.queueFrom = v.pos; v.queueTo = WL.Queue(c.queueIndex); v.queueMoveT = 0; v.shownQueue = c.queueIndex;
                    }
                    if (v.queueMoveT < 1f)
                    {
                        float d = Mathf.Max(0.05f, Vector2.Distance(v.queueFrom, v.queueTo) / M.C.flow.walkSpeed);
                        v.queueMoveT = Mathf.Min(1f, v.queueMoveT + Time.deltaTime / d);
                        pos = Vector2.Lerp(v.queueFrom, v.queueTo, v.queueMoveT);
                        dx = v.queueTo.x - v.queueFrom.x; walking = v.queueMoveT < 1f;
                    }
                    else pos = v.queueTo;
                    break;
            }
            v.pos = pos;
            v.root.position = pos;
            SetAlpha(v, alpha);
            if (dx > 0.001f) v.anim.sr.flipX = true; else if (dx < -0.001f) v.anim.sr.flipX = false;
            bool flip = v.anim.sr.flipX;
            // animation selection reflects gameplay state
            string key;
            if (c.phase == CPhase.Pick) key = c.t > c.dur * 0.6f ? "hold" : "idle";
            else if (c.phase == CPhase.Queue || c.phase == CPhase.Paying) key = walking ? "pickwalk" : "hold";
            else if (c.phase == CPhase.ToQueue) key = walking ? "pickwalk" : "hold";
            else key = walking ? "walk" : "idle";
            v.anim.Play($"cust.{c.type}.{key}");
            bool carrying = c.carrying && (c.phase == CPhase.ToQueue || c.phase == CPhase.Queue || c.phase == CPhase.Paying || (c.phase == CPhase.Pick && c.t > c.dur * 0.6f));
            v.tray.enabled = carrying; v.trayBread.enabled = carrying;
            float hx = flip ? 0.12f : -0.12f;
            v.tray.transform.localPosition = new Vector3(hx, v.holdH, 0);
            v.trayBread.transform.localPosition = new Vector3(hx, v.holdH + 0.1f, 0);
            v.bag.enabled = c.phase == CPhase.Leave && v.paid;
            v.bag.transform.localPosition = new Vector3(flip ? 0.28f : -0.28f, v.holdH * 0.55f, 0);
            v.group.sortingOrder = WL.SortOrder(pos.y);
            if (v.bubble != null && Time.time > v.bubbleUntil) { UIRoot.I.Unfollow(v.bubble, true); v.bubble = null; }
        }

        void OnCustomerPhase(CustomerView v, Customer c)
        {
            switch (c.phase)
            {
                case CPhase.Enter: v.path = paths.Enter(c.shelf); v.trayBread.sprite = Art.S(Art.Bread(Mathf.Max(1, c.recipe))); break;
                case CPhase.Pick:
                    ShowCustomerBubble(v, c.fav ? "bubble.heart" : null, Art.Bread(c.recipe), 1.6f);
                    break;
                case CPhase.ToQueue: v.path = paths.ToQueue(c.shelf, c.queueIndex, WL.StandPoint(c.shelf)); v.shownQueue = c.queueIndex; break;
                case CPhase.Queue:
                case CPhase.Paying:
                    if (v.shownPhase != CPhase.Queue && v.shownPhase != CPhase.Paying) { v.queueTo = v.pos; v.queueMoveT = 1f; v.shownQueue = -2; }
                    break;
                case CPhase.Leave: v.path = paths.Exit(v.pos); break;
                case CPhase.SadVisit: v.path = paths.Sad(); Tw.Delay(v.root, 0.35f + 0.4f + (4.2f / M.C.flow.walkSpeed), () => { if (v.root) ShowCustomerBubble(v, "bubble.sad", null, 1.8f); }); break;
            }
        }

        public void ShowCustomerBubble(CustomerView v, string bubble, string inner, float dur)
        {
            if (v.bubble != null) UIRoot.I.Unfollow(v.bubble, true);
            v.bubble = UIRoot.I.MakeCharBubble(bubble, inner);
            var tr = v.root;
            float h = v.holdH / 0.36f + 0.15f;
            UIRoot.I.Follow(v.bubble, () => tr != null ? tr.position + new Vector3(0, h, 0) : Vector3.zero, Vector2.zero);
            v.bubbleUntil = Time.time + dur;
        }

        // ================================================================== model events -> animation/sfx
        void OnModelEvent(GameEvent e)
        {
            OnStreetEvent(e);
            switch (e.type)
            {
                case Ev.DoughSpawned:
                {
                    var v = slots[e.a];
                    if (e.c == 1)
                    {
                        slotHoldUntil[e.a] = Time.time + 0.32f;
                        Vector2 from = WL.ChefPos + new Vector2(0, 0.9f);
                        Fx.Fly(Art.Dough(e.b), from, v.DoughPos, 0.32f, 0.9f, DoughScale, () => { Tw.Squash(v.dough.transform, 0.2f, 0.2f, Vector3.one * DoughScale); AudioMan.Play("se_Kitchen_DoughSave", 0.5f); });
                    }
                    else { Fx.Dust(v.DoughPos, 5, 0.4f, 0.55f); AudioMan.Play("se_Kitchen_Dough", 0.6f); }
                    break;
                }
                case Ev.DoughMerged:
                {
                    int to = e.b; var v = slots[to];
                    slotHoldUntil[to] = Time.time + 0.12f;
                    Tw.Run(v.dough.transform, 0.12f, t => { if (v.dough) v.dough.transform.localScale = Vector3.one * DoughScale * (1f + 0.25f * t); }, Ease.OutQuad);
                    Tw.Delay(v.dough, 0.12f, () =>
                    {
                        v.dough.transform.localScale = Vector3.one * DoughScale;
                        SyncSlot(v, false);
                        Tw.Squash(v.dough.transform, 0.22f, 0.22f, Vector3.one * DoughScale);
                        Fx.Dust(v.DoughPos, 6, 0.55f, 0.65f);
                        Fx.Stars(v.DoughPos, 3, 0.6f);
                        AudioMan.Play("se_Etc_Bubble", 0.8f);
                    });
                    break;
                }
                case Ev.DoughMoved: slotHoldUntil[e.b] = Time.time + 0.1f; break;
                case Ev.MaxLevel: UIRoot.I.Toast("Bột đã đạt cấp cao nhất, hãy đưa vào lò!"); break;
                case Ev.BakeStarted:
                {
                    var o = ovens[e.a];
                    AudioMan.Play("se_Kitchen_OvenStart", 0.7f);
                    Tw.Squash(o.body.transform, 0.06f, 0.25f, Vector3.one);
                    Fx.Heat(o.Mouth, o.body.sortingOrder + 6);
                    break;
                }
                case Ev.OvenReady:
                {
                    var o = ovens[e.a];
                    AudioMan.Play("se_Kitchen_OvenDone", 0.8f);
                    Tw.Pop(o.bubble, 0.5f, 0.35f);
                    Fx.Stars(o.Pos + new Vector2(0, 0.6f), 5, 0.8f);
                    for (int k = 0; k < 2; k++) Fx.Smoke(o.Chimney + new Vector2(0.1f * k, 0), o.body.sortingOrder + 5);
                    break;
                }
                case Ev.OvenCollected:
                {
                    var o = ovens[e.a];
                    var target = new Vector2(WL.StoragePos.x, WL.StoragePos.y + 0.8f);   // bread flies onto the rack
                    for (int k = 0; k < e.c; k++)
                    {
                        int kk = k;
                        Tw.Delay(o.root, 0.07f * k, () => Fx.Fly(Art.Bread(e.b), o.Mouth, target + new Vector2(0.15f * (kk - 1), 0), 0.45f, 1.0f, 1f,
                            () => { if (kk == 0) { Tw.Squash(storage.transform, 0.08f, 0.2f, Vector3.one); Tw.Pop(storageLabel, 0.7f, 0.25f); } }));
                    }
                    AudioMan.Play("se_Shop_OpenInventory", 0.7f);
                    break;
                }
                case Ev.InventoryFull:
                {
                    var o = ovens[e.a];
                    o.warn.gameObject.SetActive(true); o.warnText.text = "Kho đầy!";
                    Tw.Kill(o.warn); Tw.Delay(o.warn, 2.2f, () => { if (o.warn) o.warn.gameObject.SetActive(false); });
                    if (Time.time > fullToastAt) { fullToastAt = Time.time + 3; UIRoot.I.Toast("Kho đầy! Bày bánh lên kệ hoặc nâng cấp Sức chứa."); }
                    break;
                }
                case Ev.OvenBurnWarn:
                {
                    var o = ovens[e.a]; o.warn.gameObject.SetActive(true); o.warnText.text = "Sắp cháy!";
                    break;
                }
                case Ev.OvenBurnt: { var o = ovens[e.a]; o.warn.gameObject.SetActive(true); o.warnText.text = "Cháy mất rồi"; Fx.Dust(o.Mouth, 6, 0.5f, 0.7f, new Color(0.3f, 0.25f, 0.25f, 0.9f)); break; }
                case Ev.OvenCleared: { var o = ovens[e.a]; o.warn.gameObject.SetActive(false); Fx.Dust(o.Mouth, 5, 0.4f, 0.6f); break; }
                case Ev.OvenBusy: UIRoot.I.Toast("Lò đang bận nướng."); break;
                case Ev.ShelfStocked:
                case Ev.AutoRestocked:
                {
                    var s = shelves[e.a];
                    for (int k = 0; k < Mathf.Min(e.c, 5); k++)
                    {
                        var p = s.Pos + WL.BreadOnShelf[k % WL.BreadOnShelf.Length];
                        int kk = k;
                        Tw.Delay(s.stand, 0.05f * k, () => Fx.Fly(Art.Bread(e.b), p + new Vector2(0, 0.9f), p, 0.22f, 0.1f, 1f));
                    }
                    Tw.Squash(s.stand.transform, 0.06f, 0.22f, Vector3.one);
                    if (e.type == Ev.ShelfStocked) AudioMan.Play("se_Shop_SetBreadToStand", 0.8f);
                    break;
                }
                case Ev.StockFailed:
                    UIRoot.I.Toast(e.c == 1 ? "Kệ đang bày loại khác — chờ bán hết hoặc chọn kệ trống." : e.c == 2 ? "Kệ đã đầy." : "Trong kho không còn loại bánh này.");
                    break;
                case Ev.CustomerPicked:
                {
                    var s = shelves[e.a];
                    if (customers.TryGetValue(e.id, out var cv)) Fx.Fly(Art.Bread(e.b), s.Pos + new Vector2(0, 0.3f), (Vector2)cv.root.position + new Vector2(0, cv.holdH + 0.1f), 0.25f, 0.3f, 0.9f);
                    break;
                }
                case Ev.SaleCommitted:
                {
                    var cash = staff["cashier"]; cash.fastUntil = Time.time + 0.55f;
                    if (customers.TryGetValue(e.id, out var cv))
                    {
                        cv.paid = true;
                        ShowCustomerBubble(cv, e.c > 0 ? "bubble.heart" : null, e.c > 0 ? null : "prop.bag", 1.2f);
                    }
                    AudioMan.Play("se_Shop_Sell", 0.7f);
                    UIRoot.I.CoinFly(WL.CounterPos + new Vector2(0, 0.6f), e.amount);
                    saleAgg += e.amount; saleAggT = 1.2f;
                    salePop.gameObject.SetActive(true); salePopText.text = "+" + saleAgg;
                    Tw.Pop(salePop, 0.7f, 0.2f);
                    break;
                }
                case Ev.TipAdded: Tw.Squash(piggy.transform, 0.15f, 0.25f, Vector3.one * 0.62f); break;
                case Ev.TipsClaimed:
                    UIRoot.I.CoinFly(WL.PiggyPos, e.amount);
                    Fx.Stars(WL.PiggyPos, 4, 0.6f);
                    AudioMan.Play("se_Etc_GetCoin", 0.8f);
                    break;
                case Ev.CustomerSad:
                    if (e.a == 1 && customers.TryGetValue(e.id, out var sv)) ShowCustomerBubble(sv, "bubble.sad", null, 1.5f);
                    break;
                case Ev.UpgradeBought:
                    AudioMan.Play("se_Etc_Upgrade", 0.8f);
                    if (e.s == "chef") { staff["chef"].fastUntil = Time.time + 1.2f; Fx.Stars(WL.ChefPos + new Vector2(0, 1f), 6, 1f); Tw.Squash(kitchenTable.transform, 0.06f, 0.3f, Vector3.one); }
                    else if (e.s == "oven") { foreach (var o in ovens) if (M.S.ovenJobs[o.index].owned) { Tw.Squash(o.body.transform, 0.1f, 0.35f, Vector3.one); Fx.Stars(o.Pos, 4, 0.8f); } }
                    else { foreach (var s in shelves) if (M.S.shelfStock[s.index].owned) { Tw.Squash(s.stand.transform, 0.1f, 0.35f, Vector3.one); Fx.Stars(s.Pos, 3, 0.7f); } Tw.Squash(storage.transform, 0.1f, 0.35f, Vector3.one); }
                    break;
                case Ev.ExpansionBought:
                    if (e.a == 0) { var sv2 = slots[e.b]; Install(sv2.table.transform, sv2.Pos, "se_Kitchen_TrayConstructionCompletion"); }
                    break;
                case Ev.ChefHelped:
                {
                    var c = staff["chef"]; c.fastUntil = Time.time + 0.67f;
                    Fx.Dust(WL.KitchenTablePos + new Vector2(Random.Range(-0.3f, 0.3f), 0.45f), 4, 0.35f, 0.45f);
                    AudioMan.Play("se_Kitchen_Dough", 0.6f, 0.12f);
                    if (e.a == 1) AudioMan.Play("se_Etc_Speak_Bear", 0.5f, 1f);
                    break;
                }
                case Ev.HelpLimit:
                    if (Time.time > helpLimitToastAt) { helpLimitToastAt = Time.time + 2.5f; UIRoot.I.Toast(e.a == 0 ? "Gấu đang dốc sức rồi! Đợi mẻ bột sau nhé." : "Amber đang tính tiền hết tốc lực!"); }
                    break;
                case Ev.ManualKnead:
                    Fx.Dust(WL.KitchenTablePos + new Vector2(Random.Range(-0.4f, 0.4f), 0.45f), 4, 0.35f, 0.45f);
                    AudioMan.Play("se_Kitchen_Dough", 0.6f, 0.1f);
                    Tw.Pop(knead, 0.85f, 0.15f);
                    break;
                case Ev.CashierHelped:
                case Ev.ManualCheckout:
                    staff["cashier"].fastUntil = Time.time + 0.4f;
                    Fx.Stars(WL.CashierPos + new Vector2(0, 0.9f), 2, 0.4f, "fx.twinkle1", 0.35f);
                    AudioMan.Play("se_Etc_Speak_Lesserpanda", 0.5f, 0.8f);
                    break;
                case Ev.PromoterShout:
                {
                    var p = staff["promoter"]; p.fastUntil = Time.time + 0.8f;
                    AudioMan.Play("se_Etc_Speak_Sparrow", 0.8f);
                    UIRoot.I.FloatText(WL.PromoterPos + new Vector2(0.4f, 1.2f), e.a == 1 ? "Mời ghé tiệm!" : "Hết bánh rồi...", Pal.Orange);
                    break;
                }
                case Ev.StaffState:
                    if (e.b == (int)StaffState.Working && staff.TryGetValue(e.s, out var sv3)) Fx.Stars((Vector2)sv3.station + Vector2.up * 0.8f, 3, 0.5f, "fx.twinkle1", 0.4f);
                    break;
            }
        }
    }
}
