using System;
using System.Collections.Generic;
using System.Linq;

namespace BB.Model
{
    public partial class GameModel
    {
        // ------------------------------------------------------------------ customers
        void UpdateSpawn(float dt)
        {
            if (ActiveCustomers >= C.flow.maxCustomers) return;
            int avail = TotalAvailable();
            if (avail > 0)
            {
                sadTimer = 0;
                spawnTimer -= dt;
                if (spawnTimer <= 0)
                {
                    SpawnCustomer();
                    spawnTimer = Rand(C.flow.spawnMin, C.flow.spawnMax) * SpawnMult;
                }
            }
            else
            {
                if (spawnTimer < 2f) spawnTimer = 2f;
                if (OwnedShelves > 0 && S.stats.stocked > 0)
                {
                    sadTimer += dt;
                    if (sadTimer >= C.flow.sadTrickleSeconds) { sadTimer = 0; SpawnSad(); }
                }
            }
        }

        string PickCustomerType()
        {
            int lv = ShopLevel;
            var pool = C.customers.Where(c => c.unlockLevel <= lv).Select(c => c.id).ToList();
            if (pool.Count == 0) pool.Add(C.customers[0].id);
            var notPresent = pool.Where(id => !Customers.Any(c => c.type == id && c.phase != CPhase.Done)).ToList();
            var from = notPresent.Count > 0 ? notPresent : pool;
            return from[rng.Next(from.Count)];
        }

        public Customer SpawnCustomer(string forcedType = null)
        {
            var avail = Enumerable.Range(0, ShelfSlots).Where(i => Avail(i) > 0).ToList();
            if (avail.Count == 0) return null;
            string type = forcedType ?? PickCustomerType();
            int fav = FavoriteOf(type);
            var favShelves = avail.Where(i => S.shelfStock[i].recipe == fav).ToList();
            int shelf = favShelves.Count > 0 ? favShelves[rng.Next(favShelves.Count)] : avail[rng.Next(avail.Count)];
            Reserved[shelf]++;                                  // reservation prevents double-selling the last unit
            var c = new Customer
            {
                id = NewId(), type = type, phase = CPhase.Enter, t = 0, dur = Layout.EnterToShelf(shelf),
                shelf = shelf, recipe = S.shelfStock[shelf].recipe, favRecipe = fav
            };
            c.fav = c.recipe == fav;
            Customers.Add(c);
            var aff = Aff(type);
            if (!aff.met) { aff.met = true; E(Ev.CustomerMet, 0, 0, 0, c.id, 0, type); }
            E(Ev.CustomerSpawned, shelf, c.recipe, c.fav ? 1 : 0, c.id, 0, type);
            return c;
        }

        void SpawnSad()
        {
            string type = PickCustomerType();
            var c = new Customer { id = NewId(), type = type, phase = CPhase.SadVisit, t = 0, dur = Layout.SadVisit(), favRecipe = FavoriteOf(type) };
            Customers.Add(c);
            var aff = Aff(type);
            if (!aff.met) { aff.met = true; E(Ev.CustomerMet, 0, 0, 0, c.id, 0, type); }
            E(Ev.CustomerSad, 0, 0, 0, c.id, 0, type);
        }

        void UpdateCustomers(float dt)
        {
            for (int k = 0; k < Customers.Count; k++)
            {
                var c = Customers[k];
                c.t += dt;
                switch (c.phase)
                {
                    case CPhase.Enter:
                        if (c.t >= c.dur) { c.phase = CPhase.Pick; c.t = 0; c.dur = C.flow.pickSeconds; E(Ev.CustomerPicking, c.shelf, c.recipe, 0, c.id); }
                        break;
                    case CPhase.Pick:
                        if (c.t >= c.dur) PickUp(c);
                        break;
                    case CPhase.ToQueue:
                        if (c.t >= c.dur) { c.phase = CPhase.Queue; E(Ev.CustomerQueued, Queue.IndexOf(c.id), 0, 0, c.id); }
                        break;
                    case CPhase.Leave:
                    case CPhase.SadVisit:
                        if (c.t >= c.dur) { c.phase = CPhase.Done; E(Ev.CustomerLeft, 0, 0, 0, c.id); }
                        break;
                }
            }
            Customers.RemoveAll(c => c.phase == CPhase.Done);
            for (int i = 0; i < Queue.Count; i++) { var c = FindCustomer(Queue[i]); if (c != null) c.queueIndex = i; }
        }

        void PickUp(Customer c)
        {
            var sh = S.shelfStock[c.shelf];
            Reserved[c.shelf] = Math.Max(0, Reserved[c.shelf] - 1);
            if (sh.stock <= 0)
            {   // defensive: should never happen thanks to reservations
                c.phase = CPhase.Leave; c.t = 0; c.dur = C.flow.pickSeconds + Layout.QueueToExit();
                E(Ev.CustomerSad, 1, 0, 0, c.id, 0, c.type);
                return;
            }
            sh.stock--;
            c.carrying = true;
            c.price = C.Recipe(c.recipe).price;
            c.txId = "S" + NewId();
            Queue.Add(c.id);
            c.queueIndex = Queue.Count - 1;
            c.phase = CPhase.ToQueue; c.t = 0; c.dur = Layout.ShelfToQueue(c.shelf, c.queueIndex);
            E(Ev.CustomerPicked, c.shelf, c.recipe, c.fav ? 1 : 0, c.id);
            AutoRestock(true);
        }

        public Customer CurrentPaying()
        {
            if (Queue.Count == 0) return null;
            var c = FindCustomer(Queue[0]);
            return c != null && c.phase == CPhase.Paying ? c : null;
        }

        public Customer FrontReady()
        {
            if (Queue.Count == 0) return null;
            var c = FindCustomer(Queue[0]);
            return c != null && (c.phase == CPhase.Queue || c.phase == CPhase.Paying) ? c : null;
        }

        void UpdateCashier(float dt)
        {
            var st = Staff("cashier");
            if (IsAway(st)) return;
            var c = FrontReady();
            if (c == null) return;
            if (c.phase == CPhase.Queue) { c.phase = CPhase.Paying; st.progress = 0; st.helpUsed = 0; E(Ev.CustomerPaying, 0, 0, 0, c.id); }
            st.progress += dt * StaffSpeed(st) * BoostMult("cashier") / C.cashier.checkoutSeconds;
            if (st.progress >= 1f)
            {
                st.progress = 0;
                CommitSale(c);
                st.energy = Math.Max(0, st.energy - C.energy.cashierPerCheckout * DrainMult);
            }
        }

        public bool HelpCashier()
        {
            var st = Staff("cashier");
            if (IsAway(st)) return ManualCheckout();
            var c = CurrentPaying();
            if (c == null) return false;
            if (st.helpUsed >= C.cashier.helpMaxPerCheckout) { E(Ev.HelpLimit, 1); return false; }
            st.helpUsed++;
            S.stats.helpTaps++;
            st.progress += C.cashier.helpFraction;
            E(Ev.CashierHelped, st.helpUsed);
            if (st.progress >= 1f) { st.progress = 0; CommitSale(c); st.energy = Math.Max(0, st.energy - C.energy.cashierPerCheckout * DrainMult); }
            return true;
        }

        public bool ManualCheckout()
        {
            var st = Staff("cashier");
            if (!IsAway(st)) return false;
            var c = FrontReady();
            if (c == null) return false;
            if (c.phase == CPhase.Queue) { c.phase = CPhase.Paying; E(Ev.CustomerPaying, 0, 0, 0, c.id); }
            manualCheckout += C.cashier.manualTapFraction;
            S.stats.helpTaps++;
            E(Ev.ManualCheckout, (int)(manualCheckout * 100));
            if (manualCheckout >= 0.999f) { manualCheckout = 0; CommitSale(c); }
            return true;
        }

        int TipFor(string type, int price)
        {
            float rate = Math.Min(C.flow.tipCap, C.flow.tipBase + C.flow.tipPerHeart * Hearts(type)) * (1f + TipBonus);
            return Math.Max(1, (int)Math.Round(price * rate));
        }

        void CommitSale(Customer c)
        {
            if (c.txId == null) c.txId = "S" + NewId();
            if (!S.committedTx.Contains(c.txId))
            {
                int tip = c.fav ? TipFor(c.type, c.price) : 0;
                Earn(c.price, true);
                CommitTx(c.txId);
                if (tip > 0) { S.tipJar += tip; E(Ev.TipAdded, tip, 0, 0, c.id, S.tipJar); }
                ApplyAffection(c.type, c.fav);
                S.stats.sold++;
                if (S.stats.firstSaleAt < 0) S.stats.firstSaleAt = (float)S.playSeconds;
                E(Ev.SaleCommitted, c.recipe, c.price, tip, c.id, c.price, c.type);
                MaybeDropTrash();
            }
            c.carrying = false;
            Queue.Remove(c.id);
            c.queueIndex = -1;
            c.phase = CPhase.Leave; c.t = 0; c.dur = Layout.QueueToExit();
            for (int i = 0; i < Queue.Count; i++) { var q = FindCustomer(Queue[i]); if (q != null) q.queueIndex = i; }
        }

        void ApplyAffection(string type, bool fav)
        {
            var a = Aff(type);
            int before = Hearts(type);
            a.affection += fav ? C.flow.affectionFav : C.flow.affectionBuy;
            a.purchases++;
            int after = Hearts(type);
            if (after > before) E(Ev.HeartUp, after, 0, 0, 0, 0, type);
        }

        public long ClaimTips()
        {
            long amt = S.tipJar;
            if (amt <= 0) return 0;
            S.tipJar = 0;                       // drain first: rapid taps cannot claim twice
            Earn(amt, false);
            E(Ev.TipsClaimed, 0, 0, 0, 0, amt);
            return amt;
        }

        void UpdatePromoter(float dt)
        {
            var st = Staff("promoter");
            if (promoterCooldown > 0) promoterCooldown -= dt;
            if (IsAway(st)) return;
            if (TotalAvailable() > 0 || ActiveCustomers > 0)
                st.energy = Math.Max(0, st.energy - C.energy.promoterPerSecond * dt * DrainMult);
        }

        public bool Shout()
        {
            var st = Staff("promoter");
            if (IsAway(st) || promoterCooldown > 0) return false;
            promoterCooldown = C.promoter.shoutCooldown;
            S.stats.helpTaps++;
            bool attract = TotalAvailable() > 0 && ActiveCustomers < C.flow.maxCustomers;
            if (attract) spawnTimer = Math.Min(spawnTimer, 0.6f);
            E(Ev.PromoterShout, attract ? 1 : 0);
            return true;
        }

        // ------------------------------------------------------------------ purchases
        public int UpgradeCost(string id)
        {
            var u = C.Upgrade(id); int lv = UpLevel(id);
            return lv >= u.maxLevel ? -1 : u.costs[lv - 1];
        }

        public bool BuyUpgrade(string id)
        {
            int idx = C.UpgradeIndex(id);
            if (idx < 0) return false;
            int cost = UpgradeCost(id);
            if (cost < 0) return false;
            if (!Spend(cost)) return false;
            S.upgrades[idx]++;
            CountPurchase();
            E(Ev.UpgradeBought, idx, S.upgrades[idx], 0, 0, cost, id);
            AutoRestock(true);
            return true;
        }

        void CountPurchase()
        {
            S.stats.upgradesBought++;
            if (S.stats.firstUpgradeAt < 0) S.stats.firstUpgradeAt = (float)S.playSeconds;
        }

        // kind: 0 board slot, 1 oven, 2 shelf
        public int NextExpansionIndex(int kind)
        {
            if (kind == 0) return S.boardUnlocked < C.board.max ? S.boardUnlocked : -1;
            if (kind == 1) { for (int i = 0; i < Math.Min(OvenSlots, C.ovens.max); i++) if (!S.ovenJobs[i].owned) return i; return -1; }
            for (int i = 0; i < Math.Min(ShelfSlots, C.shelves.max); i++) if (!S.shelfStock[i].owned) return i; return -1;
        }

        ExpansionCfg Exp(int kind) => kind == 0 ? C.board : kind == 1 ? C.ovens : C.shelves;

        public int ExpansionCost(int kind, out int levelReq)
        {
            levelReq = 1;
            int idx = NextExpansionIndex(kind);
            if (idx < 0) return -1;
            var e = Exp(kind);
            int k = idx - e.start;
            if (k < 0 || k >= e.costs.Length) return -1;
            levelReq = e.levelReq != null && k < e.levelReq.Length ? e.levelReq[k] : 1;
            return e.costs[k];
        }

        public bool BuyExpansion(int kind)
        {
            int idx = NextExpansionIndex(kind);
            int cost = ExpansionCost(kind, out int req);
            if (idx < 0 || cost < 0) return false;
            if (ShopLevel < req) { E(Ev.LevelTooLow, req); return false; }
            if (!Spend(cost)) return false;
            if (kind == 0) S.boardUnlocked++;
            else if (kind == 1) { S.ovenJobs[idx].owned = true; S.ovenJobs[idx].state = 0; if (idx == 1 && S.stats.oven2At < 0) S.stats.oven2At = (float)S.playSeconds; }
            else { S.shelfStock[idx].owned = true; S.shelfStock[idx].auto = S.autoRestockUnlocked; }
            CountPurchase();
            E(Ev.ExpansionBought, kind, idx, 0, 0, cost);
            return true;
        }

        public int FurnitureCost(string id)
        {
            var f = C.Furniture(id); var d = Furn(id);
            if (f == null || d == null) return -1;
            int k = d.owned ? d.level : 0;
            return k < f.costs.Length ? f.costs[k] : -1;
        }

        public bool BuyFurniture(string id)
        {
            var f = C.Furniture(id); var d = Furn(id);
            if (f == null || d == null) return false;
            int cost = FurnitureCost(id);
            if (cost < 0) return false;
            if (!d.owned && ShopLevel < f.levelReq) { E(Ev.LevelTooLow, f.levelReq); return false; }
            if (!Spend(cost)) return false;
            if (!d.owned) { d.owned = true; d.level = 1; S.stats.furnitureBought++; E(Ev.FurnitureBought, 0, 1, 0, 0, cost, id); }
            else { d.level++; E(Ev.FurnitureUpgraded, 0, d.level, 0, 0, cost, id); }
            return true;
        }

        public void MarkBreakRoomVisited()
        {
            if (S.visitedBreakRoom) return;
            S.visitedBreakRoom = true;
            CheckQuests();
        }

        // ------------------------------------------------------------------ quests (state based, sequential)
        public int QuestValue(string type)
        {
            var st = S.stats;
            switch (type)
            {
                case "dough": return st.doughMade;
                case "merge": return st.merges;
                case "bake": return st.bakes;
                case "collect": return st.collects;
                case "stock": return st.stocked;
                case "sell": return st.sold;
                case "buy": return st.upgradesBought;
                case "visit_break": return S.visitedBreakRoom ? 1 : 0;
                case "ovens": return OwnedOvens;
                case "recipe": return MaxUnlockedRecipe;
                case "furniture": return st.furnitureBought;
                case "hearts": return C.customers.Max(c => Hearts(c.id));
                case "slots": return S.boardUnlocked;
                case "shelves": return OwnedShelves;
                case "earn": return (int)Math.Min(int.MaxValue, S.totalEarned);
                case "deliver": return st.deliveries;
                case "trash": return st.trashCleaned + st.trashCleanedByCleaner;
                case "boost": return st.boostsUsed;
            }
            return 0;
        }

        public int ActiveQuestIndex { get { for (int i = 0; i < S.quests.Count; i++) if (!S.quests[i].completed) return i; return -1; } }

        public void CheckQuests()
        {
            for (int guard = 0; guard < 32; guard++)
            {
                int i = ActiveQuestIndex;
                if (i < 0) { S.tutorialStep = S.quests.Count; return; }
                S.tutorialStep = i;
                var q = C.quests[i]; var d = S.quests[i];
                int v = Math.Min(q.target, QuestValue(q.type));
                if (v != d.progress) { d.progress = v; E(Ev.QuestProgress, i, v, q.target, 0, 0, q.id); }
                if (v >= q.target) { d.completed = true; E(Ev.QuestCompleted, i, 0, 0, 0, q.reward, q.id); continue; }
                return;
            }
        }

        public bool ClaimQuest(string id)
        {
            int i = S.quests.FindIndex(x => x.id == id);
            if (i < 0) return false;
            var d = S.quests[i];
            if (!d.completed || d.claimed || S.claimedRewards.Contains(id)) return false;
            d.claimed = true;                      // flag first: no double claim
            S.claimedRewards.Add(id);
            Earn(C.quests[i].reward, false);
            E(Ev.QuestClaimed, i, 0, 0, 0, C.quests[i].reward, id);
            return true;
        }

        public int ClaimableQuestCount => S.quests.Count(q => q.completed && !q.claimed);

        // ------------------------------------------------------------------ elapsed / offline (single path for load AND resume)
        public OfflineRewardData ApplyElapsed(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) seconds = 0;   // clock moved back => 0
            bool capped = seconds > C.offline.maxSeconds;
            float s = (float)Math.Min(seconds, C.offline.maxSeconds);
            if (s < 0.5f) return null;
            int baked = 0;
            foreach (var o in S.ovenJobs)
            {
                if (!o.owned || o.state != (int)OvenState.Baking) continue;
                o.remaining -= s;
                if (o.remaining <= 0) { o.remaining = 0; o.state = (int)OvenState.Ready; o.readyFor = 0; baked++; }
            }
            foreach (var st in S.staffEnergy)
            {
                st.energy = Math.Min(100f, st.energy + s * C.offline.energyRecoveryPerSecond);
                if (IsAway(st) && st.energy >= C.energy.restThreshold) { st.state = (int)StaffState.Working; st.restSpot = ""; st.timer = 0; }
            }
            long coins = 0; int sold = 0;
            int n = (int)(s / Math.Max(1f, C.offline.saleInterval));
            for (int k = 0; k < n; k++)
            {
                AutoRestock(false);   // only if unlocked and enabled; takes from inventory, never creates goods
                int best = -1;
                for (int i = 0; i < ShelfSlots; i++) if (Avail(i) > 0 && (best < 0 || S.shelfStock[i].stock > S.shelfStock[best].stock)) best = i;
                if (best < 0) break;
                var sh = S.shelfStock[best];
                sh.stock--;
                coins += C.Recipe(sh.recipe).price;
                sold++;
            }
            AutoRestock(false);
            S.stats.sold += sold;
            coins += ApplyElapsedExtra(s);
            if (sold > 0 || baked > 0 || coins > 0)
            {
                var p = S.pendingOffline;
                if (string.IsNullOrEmpty(p.id)) { p.id = "OFF" + NewId(); p.coins = 0; p.sold = 0; p.bakedDone = 0; p.seconds = 0; p.capped = false; }
                p.coins += coins; p.sold += sold; p.bakedDone += baked; p.seconds += s; p.capped |= capped;
                E(Ev.OfflineReport, sold, baked, 0, 0, coins, p.id);
            }
            CheckQuests();
            return S.pendingOffline;
        }

        public long ClaimOffline()
        {
            var p = S.pendingOffline;
            if (p == null || string.IsNullOrEmpty(p.id)) return 0;
            string id = p.id; long coins = p.coins;
            S.pendingOffline = new OfflineRewardData();      // cleared before paying
            if (S.claimedRewards.Contains(id)) return 0;
            S.claimedRewards.Add(id);
            Earn(coins, true);
            E(Ev.OfflineClaimed, 0, 0, 0, 0, coins, id);
            return coins;
        }

        /// <summary>Called once after load: customers who had already taken bread finish paying (rule documented in README).</summary>
        public int SettlePendingSales()
        {
            int n = 0; long coins = 0;
            foreach (var p in S.pendingSales)
            {
                if (string.IsNullOrEmpty(p.txId) || S.committedTx.Contains(p.txId)) continue;
                Earn(p.price, true);
                if (p.tip > 0) S.tipJar += p.tip;
                CommitTx(p.txId);
                if (C.Customer(p.customer) != null) ApplyAffection(p.customer, p.favorite);
                S.stats.sold++;
                n++; coins += p.price;
            }
            S.pendingSales.Clear();
            if (n > 0) E(Ev.PendingSalesSettled, n, 0, 0, 0, coins);
            return n;
        }

        /// <summary>Consistent snapshot: shelf stock already excludes picked bread; unpaid carried bread becomes pending sales; reservations are released.</summary>
        public SaveData Snapshot(double unixNow)
        {
            S.pendingSales.Clear();
            foreach (var c in Customers)
            {
                if (!c.carrying || c.txId == null || S.committedTx.Contains(c.txId)) continue;
                S.pendingSales.Add(new PendingSaleData
                {
                    txId = c.txId, customer = c.type, recipe = c.recipe, price = c.price,
                    tip = c.fav ? TipFor(c.type, c.price) : 0, favorite = c.fav
                });
            }
            S.lastSavedAt = unixNow;
            S.lastSimAt = unixNow;
            S.saveVersion = SaveData.CurrentVersion;
            return S;
        }

        /// <summary>Debug/testing helper.</summary>
        public void DebugAddCoins(long n) { S.currency += n; }
    }
}
