// Pure gameplay model: no UnityEngine dependency. The renderer only reads state and listens to events.
// Every command validates, mutates atomically and returns a result; money only moves in Commit*/Spend.
using System;
using System.Collections.Generic;
using System.Linq;

namespace BB.Model
{
    public partial class GameModel
    {
        public const int BoardMax = 12;
        public const int OvenSlots = 3;
        public const int ShelfSlots = 5;
        public static readonly string[] StaffIds = { "chef", "cashier", "promoter" };

        public readonly GameConfig C;
        public SaveData S { get; private set; }
        public IShopLayout Layout = new DefaultLayout();
        public event Action<GameEvent> OnEvent;

        public readonly List<Customer> Customers = new List<Customer>();
        public readonly List<long> Queue = new List<long>();
        public readonly int[] Reserved = new int[ShelfSlots];
        public double SessionTime;            // active seconds this session

        readonly Random rng;
        float spawnTimer, sadTimer, autoTimer, promoterCooldown;
        float manualKnead, manualCheckout;
        readonly bool[] burnWarned = new bool[OvenSlots];
        int lastLevel;

        public GameModel(GameConfig cfg, SaveData save, int seed = 12345)
        {
            C = cfg;
            S = save ?? NewGame(cfg);
            Normalize();
            rng = new Random(seed);
            spawnTimer = C.flow.firstSpawnDelay;
            marketingCycle = C.flow.firstSpawnDelay;
            lastLevel = ShopLevel;
        }

        // ------------------------------------------------------------------ setup
        public static SaveData NewGame(GameConfig C)
        {
            var s = new SaveData { currency = C.economy.startCoins };
            s.delivery.timer = C.delivery.firstDelay;
            s.unlockedRecipes.Add(1);
            s.boardUnlocked = C.board.start;
            return s;
        }

        /// <summary>Repairs list sizes and impossible values (older saves / config changes). Never drops valid progress.</summary>
        public void Normalize()
        {
            var s = S;
            if (s.unlockedRecipes == null || s.unlockedRecipes.Count == 0) s.unlockedRecipes = new List<int> { 1 };
            while (s.boardSlots.Count < BoardMax) s.boardSlots.Add(new SlotData());
            for (int i = 0; i < BoardMax; i++) if (s.boardSlots[i] == null) s.boardSlots[i] = new SlotData();
            s.boardUnlocked = Math.Max(C.board.start, Math.Min(C.board.max, s.boardUnlocked));
            while (s.ovenJobs.Count < OvenSlots) s.ovenJobs.Add(new OvenData { owned = s.ovenJobs.Count < C.ovens.start });
            for (int i = 0; i < C.ovens.start; i++) s.ovenJobs[i].owned = true;
            while (s.inventory.Count < C.MaxRecipe) s.inventory.Add(0);
            for (int i = 0; i < s.inventory.Count; i++) if (s.inventory[i] < 0) s.inventory[i] = 0;
            while (s.shelfStock.Count < ShelfSlots) s.shelfStock.Add(new ShelfData { owned = s.shelfStock.Count < C.shelves.start });
            while (s.shelfStock.Count > ShelfSlots)
            {   // older layout had 6 stands: move the extra stand's bread back to storage
                var x = s.shelfStock[s.shelfStock.Count - 1];
                if (x.stock > 0 && x.recipe >= 1 && x.recipe <= s.inventory.Count) s.inventory[x.recipe - 1] += x.stock;
                s.shelfStock.RemoveAt(s.shelfStock.Count - 1);
            }
            for (int i = 0; i < C.shelves.start; i++) s.shelfStock[i].owned = true;
            foreach (var sh in s.shelfStock) { if (sh.stock < 0) sh.stock = 0; if (sh.stock == 0 && sh.recipe < 0) sh.recipe = 0; }
            while (s.upgrades.Count < C.upgrades.Length) s.upgrades.Add(1);
            for (int i = 0; i < s.upgrades.Count && i < C.upgrades.Length; i++) s.upgrades[i] = Math.Max(1, Math.Min(C.upgrades[i].maxLevel, s.upgrades[i]));
            foreach (var f in C.furniture)
                if (!s.furniture.Any(x => x.id == f.id)) s.furniture.Add(new FurnitureData { id = f.id, owned = f.startOwned, level = f.startOwned ? 1 : 0 });
            foreach (var id in StaffIds)
                if (!s.staffEnergy.Any(x => x.id == id)) s.staffEnergy.Add(new StaffData { id = id, energy = 100 });
            foreach (var st in s.staffEnergy) { st.energy = Clamp(st.energy, 0, 100); }
            foreach (var c in C.customers)
                if (!s.customerAffection.Any(x => x.id == c.id)) s.customerAffection.Add(new AffectionData { id = c.id });
            foreach (var q in C.quests)
                if (!s.quests.Any(x => x.id == q.id)) s.quests.Add(new QuestData { id = q.id });
            // quest list in config order
            s.quests = C.quests.Select(q => s.quests.First(x => x.id == q.id)).ToList();
            if (s.currency < 0) s.currency = 0;
            if (s.tipJar < 0) s.tipJar = 0;
            if (s.pendingOffline == null) s.pendingOffline = new OfflineRewardData();
            if (s.boosts == null) s.boosts = new List<BoostData>();
            foreach (var id in BoostIds) if (!s.boosts.Any(b => b.id == id)) s.boosts.Add(new BoostData { id = id });
            foreach (var b in s.boosts) { b.remaining = Clamp(b.remaining, 0, C.boost.duration); b.cooldown = Clamp(b.cooldown, 0, C.boost.cooldown); }
            if (s.trash == null) s.trash = new List<TrashData>();
            s.trash.RemoveAll(t => t == null || t.spot < 0 || t.spot >= TrashSpots);
            if (s.delivery == null) s.delivery = new DeliveryData { timer = C.delivery.firstDelay };
            if (s.delivery.state < 0 || s.delivery.state > 2) s.delivery.state = 0;
            if (s.delivery.state != 0 && (s.delivery.recipe < 1 || s.delivery.recipe > C.MaxRecipe)) { s.delivery.state = 0; s.delivery.orderId = ""; }
            if (s.stats == null) s.stats = new StatsData();
            if (s.stats.recipeUnlockAt == null || s.stats.recipeUnlockAt.Length < 6) s.stats.recipeUnlockAt = new float[6] { 0, -1, -1, -1, -1, -1 };
            if (s.committedTx == null) s.committedTx = new List<string>();
            if (s.pendingSales == null) s.pendingSales = new List<PendingSaleData>();
            if (s.claimedRewards == null) s.claimedRewards = new List<string>();
            // board items at max level only; no locked-slot items
            for (int i = 0; i < BoardMax; i++)
            {
                var b = s.boardSlots[i];
                if (b.level < 0 || b.level > C.MaxRecipe) b.level = 0;
                if (b.level > 0 && i >= s.boardUnlocked)
                {   // relocate instead of deleting
                    int e = FirstEmptySlot();
                    if (e >= 0) { s.boardSlots[e].id = b.id; s.boardSlots[e].level = b.level; }
                    b.level = 0; b.id = 0;
                }
            }
            foreach (var o in s.ovenJobs) { if (o.state < 0 || o.state > 3) o.state = 0; if (o.remaining < 0) o.remaining = 0; }
        }

        // ------------------------------------------------------------------ helpers
        static float Clamp(float v, float a, float b) => v < a ? a : (v > b ? b : v);
        void E(Ev t, int a = 0, int b = 0, int c = 0, long id = 0, long amount = 0, string s = null)
            => OnEvent?.Invoke(new GameEvent { type = t, a = a, b = b, c = c, id = id, amount = amount, s = s });
        long NewId() => S.nextId++;
        float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        public int ShopLevel
        {
            get { int lv = 1; for (int i = 1; i < C.levelXp.Length; i++) if (S.xp >= C.levelXp[i]) lv = i + 1; return lv; }
        }
        public long XpForNextLevel => ShopLevel < C.levelXp.Length ? C.levelXp[ShopLevel] : -1;
        public int UpLevel(string id) => S.upgrades[C.UpgradeIndex(id)];
        public float ChefInterval => C.chef.intervalByLevel[Math.Min(C.chef.intervalByLevel.Length, UpLevel("chef")) - 1];
        public float ChefBonusChance => C.chef.bonusChanceByLevel[Math.Min(C.chef.bonusChanceByLevel.Length, UpLevel("chef")) - 1];
        public float OvenMult => C.Upgrade("oven").values[UpLevel("oven") - 1];
        public int ShelfCap => (int)C.Upgrade("capacity").values[UpLevel("capacity") - 1];
        public int StorageCap => (int)C.Upgrade("capacity").values2[UpLevel("capacity") - 1];
        public int StorageUsed { get { int n = 0; foreach (var v in S.inventory) n += v; return n; } }
        public StaffData Staff(string id) => S.staffEnergy.First(x => x.id == id);
        public bool IsAway(StaffData st) => st.state == (int)StaffState.ToRest || st.state == (int)StaffState.Resting
                                           || st.state == (int)StaffState.Returning || st.state == (int)StaffState.RestInPlace;
        public bool IsTired(StaffData st) => st.energy < C.energy.tiredThreshold;
        public float StaffSpeed(StaffData st) => IsTired(st) ? C.energy.tiredSpeedMult : 1f;
        public FurnitureData Furn(string id) => S.furniture.FirstOrDefault(x => x.id == id);
        float FurnValue(FurnitureCfg f) { var d = Furn(f.id); if (d == null || !d.owned || d.level <= 0) return 0; return f.values[Math.Min(f.values.Length, d.level) - 1]; }
        float SumEffect(string effect) { float v = 0; foreach (var f in C.furniture) if (f.effect == effect) v += FurnValue(f); return v; }
        public float DrainMult => 1f - Math.Min(C.energy.maxDrainReduction, SumEffect("drain"));
        public float RestBoost => SumEffect("restBoost");
        public float TipBonus => SumEffect("tips");
        public float RestRate(string spot)
        {
            var f = C.Furniture(spot);
            if (f == null) return C.energy.inPlaceRecovery;
            return Math.Min(C.energy.maxRecovery * BoostMult("rest"), FurnValue(f) * (1f + RestBoost) * BoostMult("rest"));
        }
        public bool PromoterActive { get { var p = Staff("promoter"); return !IsAway(p) && TotalAvailable() > 0; } }
        public float SpawnMult => (PromoterActive ? C.promoter.spawnMultiplier : 1f) * (1f - Math.Min(0.3f, SumEffect("spawn")));
        public float PromoterCooldown => promoterCooldown;
        public int Avail(int shelf) => S.shelfStock[shelf].owned ? S.shelfStock[shelf].stock - Reserved[shelf] : 0;
        public int TotalAvailable() { int n = 0; for (int i = 0; i < ShelfSlots; i++) n += Math.Max(0, Avail(i)); return n; }
        public int OwnedOvens => S.ovenJobs.Count(o => o.owned);
        public int OwnedShelves => S.shelfStock.Count(o => o.owned);
        public int MaxUnlockedRecipe => S.unlockedRecipes.Max();
        public bool IsUnlocked(int recipe) => S.unlockedRecipes.Contains(recipe);
        public AffectionData Aff(string id) => S.customerAffection.First(x => x.id == id);
        public int Hearts(string id) { int a = Aff(id).affection, h = 0; foreach (var t in C.flow.heartThresholds) if (a >= t) h++; return h; }
        public int FirstEmptySlot() { for (int i = 0; i < S.boardUnlocked; i++) if (S.boardSlots[i].level == 0) return i; return -1; }
        public int ActiveCustomers => Customers.Count(c => c.phase != CPhase.Done);
        public Customer FindCustomer(long id) { foreach (var c in Customers) if (c.id == id) return c; return null; }
        public float ManualKneadProgress => manualKnead;
        public float ManualCheckoutProgress => manualCheckout;

        public int FavoriteOf(string type)
        {
            var cfg = C.Customer(type);
            foreach (var r in cfg.likes) if (IsUnlocked(r)) return r;
            return S.unlockedRecipes.Min();
        }

        bool Spend(long cost)
        {
            if (cost < 0) return false;
            if (S.currency < cost) { E(Ev.NotEnoughCoins, amount: cost - S.currency); return false; }
            S.currency -= cost;
            return true;
        }

        void Earn(long amount, bool xp)
        {
            if (amount <= 0) return;
            S.currency += amount;
            S.totalEarned += amount;
            if (xp) S.xp += amount;
            int lv = ShopLevel;
            if (lv > lastLevel) { lastLevel = lv; E(Ev.LevelUp, lv); }
        }

        void CommitTx(string tx)
        {
            S.committedTx.Add(tx);
            if (S.committedTx.Count > 400) S.committedTx.RemoveRange(0, S.committedTx.Count - 400);
        }

        void UnlockRecipe(int level)
        {
            if (level < 1 || level > C.MaxRecipe || IsUnlocked(level)) return;
            S.unlockedRecipes.Add(level);
            S.unlockedRecipes.Sort();
            if (S.stats.recipeUnlockAt[level - 1] < 0) S.stats.recipeUnlockAt[level - 1] = (float)S.playSeconds;
            E(Ev.RecipeUnlocked, level);
        }

        // ------------------------------------------------------------------ tick
        public void Tick(float dt)
        {
            if (dt <= 0 || float.IsNaN(dt)) return;
            if (dt > 0.5f) dt = 0.5f;               // big gaps go through ApplyElapsed
            SessionTime += dt;
            S.playSeconds += dt;
            foreach (var st in S.staffEnergy) UpdateStaffRest(st, dt);
            UpdateChef(dt);
            UpdateOvens(dt);
            autoTimer -= dt;
            if (autoTimer <= 0) { AutoRestock(true); autoTimer = 0.5f; }
            UpdateBoosts(dt);
            UpdateMarketing(dt);
            UpdateCustomers(dt);
            UpdateCashier(dt);
            UpdatePromoter(dt);
            UpdateCleaner(dt);
            UpdateDelivery(dt);
            CheckQuests();
        }

        // ------------------------------------------------------------------ staff energy / rest
        string FindFreeRestSpot(StaffData me)
        {
            string best = null; float bestRate = -1;
            foreach (var f in C.furniture)
            {
                if (!f.restSpot) continue;
                var d = Furn(f.id);
                if (d == null || !d.owned) continue;
                bool used = S.staffEnergy.Any(o => o != me && o.restSpot == f.id &&
                    (o.state == (int)StaffState.ToRest || o.state == (int)StaffState.Resting));
                if (used) continue;
                float r = RestRate(f.id);
                if (r > bestRate) { bestRate = r; best = f.id; }
            }
            return best;
        }

        void SetStaff(StaffData st, StaffState s, int idx)
        {
            st.state = (int)s;
            E(Ev.StaffState, idx, (int)s, 0, 0, 0, st.id);
        }

        void UpdateStaffRest(StaffData st, float dt)
        {
            int idx = S.staffEnergy.IndexOf(st);
            var E2 = C.energy;
            switch ((StaffState)st.state)
            {
                case StaffState.Working:
                case StaffState.IdleBlocked:
                    if (st.energy <= E2.restThreshold)
                    {
                        // cashier finishes the current checkout first
                        if (st.id == "cashier" && CurrentPaying() != null) break;
                        var spot = FindFreeRestSpot(st);
                        if (spot != null) { st.restSpot = spot; st.timer = E2.transitSeconds; SetStaff(st, StaffState.ToRest, idx); }
                        else { st.restSpot = ""; SetStaff(st, StaffState.RestInPlace, idx); }
                    }
                    break;
                case StaffState.ToRest:
                    st.timer -= dt;
                    if (st.timer <= 0) SetStaff(st, StaffState.Resting, idx);
                    break;
                case StaffState.Resting:
                    st.energy = Math.Min(100f, st.energy + RestRate(st.restSpot) * dt);
                    if (st.energy >= E2.restDoneAt) { st.timer = E2.transitSeconds; SetStaff(st, StaffState.Returning, idx); }
                    break;
                case StaffState.Returning:
                    st.timer -= dt;
                    if (st.timer <= 0) { st.restSpot = ""; SetStaff(st, StaffState.Working, idx); }
                    break;
                case StaffState.RestInPlace:
                    st.energy = Math.Min(100f, st.energy + E2.inPlaceRecovery * dt);
                    var free = FindFreeRestSpot(st);
                    if (free != null && st.energy < E2.restDoneAt - 20) { st.restSpot = free; st.timer = E2.transitSeconds; SetStaff(st, StaffState.ToRest, idx); }
                    else if (st.energy >= E2.restDoneAt) SetStaff(st, StaffState.Working, idx);
                    break;
            }
        }

        /// <summary>Player sends a tired staff member to rest early (only if a spot is free).</summary>
        public bool SendToRest(string staffId)
        {
            var st = Staff(staffId);
            if (IsAway(st) || st.energy >= C.energy.restDoneAt - 5) return false;
            if (st.id == "cashier" && CurrentPaying() != null) return false;
            var spot = FindFreeRestSpot(st);
            if (spot == null) return false;
            st.restSpot = spot; st.timer = C.energy.transitSeconds;
            SetStaff(st, StaffState.ToRest, S.staffEnergy.IndexOf(st));
            return true;
        }

        // ------------------------------------------------------------------ chef & board
        void UpdateChef(float dt)
        {
            var st = Staff("chef");
            if (IsAway(st)) return;
            int empty = FirstEmptySlot();
            if (empty < 0)
            {
                if (st.state != (int)StaffState.IdleBlocked) { SetStaff(st, StaffState.IdleBlocked, 0); E(Ev.BoardFull); }
                return;
            }
            if (st.state == (int)StaffState.IdleBlocked) { SetStaff(st, StaffState.Working, 0); E(Ev.BoardFreed); }
            st.progress += dt * StaffSpeed(st) * BoostMult("chef") / ChefInterval;
            if (st.progress >= 1f) ChefPlaceDough(st);
        }

        void ChefPlaceDough(StaffData st)
        {
            int empty = FirstEmptySlot();
            if (empty < 0) { st.progress = 1f; return; }
            int lvl = rng.NextDouble() < ChefBonusChance ? 2 : 1;
            SpawnDough(empty, lvl, true);
            st.progress = 0; st.helpUsed = 0;
            st.energy = Math.Max(0, st.energy - C.energy.chefPerDough * DrainMult);
        }

        void SpawnDough(int slot, int level, bool byChef)
        {
            var b = S.boardSlots[slot];
            b.id = NewId(); b.level = level;
            S.stats.doughMade++;
            UnlockRecipe(level);
            E(Ev.DoughSpawned, slot, level, byChef ? 1 : 0, b.id);
        }

        /// <summary>Tap the chef: adds limited progress (a few taps per cycle).</summary>
        public bool HelpChef()
        {
            var st = Staff("chef");
            if (IsAway(st)) return ManualKnead();
            if (FirstEmptySlot() < 0) { E(Ev.BoardFull); return false; }
            if (st.helpUsed >= C.chef.helpMaxPerCycle) { E(Ev.HelpLimit, 0); return false; }
            st.helpUsed++;
            S.stats.helpTaps++;
            st.progress += C.chef.helpFraction;
            E(Ev.ChefHelped, st.helpUsed);
            if (st.progress >= 1f) ChefPlaceDough(st);
            return true;
        }

        /// <summary>When the chef is resting, the player can knead at the table by tapping.</summary>
        public bool ManualKnead()
        {
            var st = Staff("chef");
            if (!IsAway(st)) return false;
            int empty = FirstEmptySlot();
            if (empty < 0) { E(Ev.BoardFull); return false; }
            manualKnead += 1f / Math.Max(1, C.chef.manualTapsPerDough);
            S.stats.helpTaps++;
            E(Ev.ManualKnead, (int)(manualKnead * 100));
            if (manualKnead >= 0.999f) { manualKnead = 0; SpawnDough(empty, 1, false); }
            return true;
        }

        public DropResult DropDough(int from, int to)
        {
            if (from < 0 || from >= S.boardUnlocked || to < 0 || to >= S.boardUnlocked) { E(Ev.DropRejected, from); return DropResult.Invalid; }
            if (from == to) return DropResult.Same;
            var a = S.boardSlots[from]; var b = S.boardSlots[to];
            if (a.level == 0) { E(Ev.DropRejected, from); return DropResult.Invalid; }
            if (b.level == 0)
            {
                b.id = a.id; b.level = a.level; a.id = 0; a.level = 0;
                E(Ev.DoughMoved, from, to, b.level, b.id);
                return DropResult.Moved;
            }
            if (b.level != a.level) { E(Ev.DropRejected, from, to); return DropResult.Invalid; }
            if (a.level >= C.MaxRecipe) { E(Ev.MaxLevel, from, to); return DropResult.MaxLevel; }
            int nl = a.level + 1;
            long idA = a.id, idB = b.id;
            a.id = 0; a.level = 0;                       // both inputs consumed exactly once
            b.id = NewId(); b.level = nl;
            S.stats.merges++;
            if (S.stats.firstMergeAt < 0) S.stats.firstMergeAt = (float)S.playSeconds;
            UnlockRecipe(nl);
            E(Ev.DoughMerged, from, to, nl, b.id, idA ^ idB);
            return DropResult.Merged;
        }

        // ------------------------------------------------------------------ ovens
        public bool Bake(int slot, int oven)
        {
            if (slot < 0 || slot >= S.boardUnlocked || oven < 0 || oven >= OvenSlots) return false;
            var o = S.ovenJobs[oven]; var b = S.boardSlots[slot];
            if (!o.owned || b.level == 0) return false;
            if (o.state != (int)OvenState.Empty) { E(Ev.OvenBusy, oven); return false; }
            o.jobId = NewId(); o.recipe = b.level;
            o.total = C.Recipe(b.level).bakeSeconds * OvenMult;
            o.remaining = o.total; o.state = (int)OvenState.Baking; o.readyFor = 0; burnWarned[oven] = false;
            int lvl = b.level;
            b.id = 0; b.level = 0;
            S.stats.bakes++;
            if (S.stats.firstBakeAt < 0) S.stats.firstBakeAt = (float)S.playSeconds;
            E(Ev.BakeStarted, oven, lvl, slot, o.jobId);
            return true;
        }

        void UpdateOvens(float dt)
        {
            for (int i = 0; i < OvenSlots; i++)
            {
                var o = S.ovenJobs[i];
                if (!o.owned) continue;
                if (o.state == (int)OvenState.Baking)
                {
                    o.remaining -= dt;
                    if (o.remaining <= 0) { o.remaining = 0; o.state = (int)OvenState.Ready; o.readyFor = 0; E(Ev.OvenReady, i, o.recipe, 0, o.jobId); }
                }
                else if (o.state == (int)OvenState.Ready && C.burn.burnEnabled)
                {
                    o.readyFor += dt;   // only during active play (ApplyElapsed never touches this)
                    if (!burnWarned[i] && o.readyFor >= C.burn.burnGraceSeconds - C.burn.warnSeconds) { burnWarned[i] = true; E(Ev.OvenBurnWarn, i); }
                    if (o.readyFor >= C.burn.burnGraceSeconds) { o.state = (int)OvenState.Burnt; E(Ev.OvenBurnt, i, o.recipe, 0, o.jobId); }
                }
            }
        }

        public float BurnLeft(int oven)
        {
            var o = S.ovenJobs[oven];
            if (!C.burn.burnEnabled || o.state != (int)OvenState.Ready) return -1;
            return Math.Max(0, C.burn.burnGraceSeconds - o.readyFor);
        }

        public CollectResult Collect(int oven)
        {
            if (oven < 0 || oven >= OvenSlots) return CollectResult.NotReady;
            var o = S.ovenJobs[oven];
            if (o.state == (int)OvenState.Burnt)
            {
                o.state = (int)OvenState.Empty; o.recipe = 0; o.jobId = 0; o.readyFor = 0;
                E(Ev.OvenCleared, oven);
                return CollectResult.Cleared;
            }
            if (o.state != (int)OvenState.Ready) return CollectResult.NotReady;
            int pieces = C.Recipe(o.recipe).pieces;
            if (StorageUsed + pieces > StorageCap) { E(Ev.InventoryFull, oven, o.recipe); return CollectResult.StorageFull; }
            int r = o.recipe;
            long job = o.jobId;
            o.state = (int)OvenState.Empty; o.recipe = 0; o.jobId = 0; o.readyFor = 0; o.remaining = 0;   // state flips before reward
            S.inventory[r - 1] += pieces;
            S.stats.collects++;
            E(Ev.OvenCollected, oven, r, pieces, job);
            AutoRestock(true);
            return CollectResult.Collected;
        }

        // ------------------------------------------------------------------ shelves
        public int StockShelf(int shelf, int recipe)
        {
            if (shelf < 0 || shelf >= ShelfSlots || recipe < 1 || recipe > C.MaxRecipe) return 0;
            var sh = S.shelfStock[shelf];
            if (!sh.owned) return 0;
            if (sh.stock > 0 && sh.recipe != recipe) { E(Ev.StockFailed, shelf, recipe, 1); return 0; }
            int space = ShelfCap - sh.stock;
            int n = Math.Min(space, S.inventory[recipe - 1]);
            if (n <= 0) { E(Ev.StockFailed, shelf, recipe, space <= 0 ? 2 : 3); return 0; }
            S.inventory[recipe - 1] -= n;
            sh.stock += n; sh.recipe = recipe;
            S.stats.stocked++;
            E(Ev.ShelfStocked, shelf, recipe, n);
            return n;
        }

        void AutoRestock(bool emit)
        {
            if (!S.autoRestockUnlocked) return;
            for (int i = 0; i < ShelfSlots; i++)
            {
                var sh = S.shelfStock[i];
                if (!sh.owned || !sh.auto || sh.recipe <= 0) continue;
                int n = Math.Min(ShelfCap - sh.stock, S.inventory[sh.recipe - 1]);
                if (n <= 0) continue;
                S.inventory[sh.recipe - 1] -= n;
                sh.stock += n;
                if (emit) E(Ev.AutoRestocked, i, sh.recipe, n);
            }
        }

        public bool SetAuto(int shelf, bool on)
        {
            if (!S.autoRestockUnlocked || shelf < 0 || shelf >= ShelfSlots || !S.shelfStock[shelf].owned) return false;
            S.shelfStock[shelf].auto = on;
            E(Ev.AutoToggled, shelf, on ? 1 : 0);
            if (on) AutoRestock(true);
            return true;
        }

        public bool CanUnlockAutoRestock
        {
            get
            {
                if (S.autoRestockUnlocked) return false;
                var q = S.quests.FirstOrDefault(x => x.id == C.economy.autoRestockAfterQuest);
                return q != null && q.completed;
            }
        }

        public bool UnlockAutoRestock()
        {
            if (!CanUnlockAutoRestock) return false;
            if (!Spend(C.economy.autoRestockCost)) return false;
            S.autoRestockUnlocked = true;
            foreach (var sh in S.shelfStock) if (sh.owned) sh.auto = true;
            E(Ev.AutoRestockUnlocked);
            AutoRestock(true);
            return true;
        }
    }
}
