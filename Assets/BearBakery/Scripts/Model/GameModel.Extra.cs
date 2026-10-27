// Speed-up boosts (per zone character), marketing bar (customer attraction), trash + raccoon cleaner, delivery orders.
using System;
using System.Collections.Generic;
using System.Linq;

namespace BB.Model
{
    public enum CleanerState { Idle = 0, Walking = 1, Sweeping = 2, Returning = 3 }

    public partial class GameModel
    {
        public static readonly string[] BoostIds = { "chef", "cashier", "promoter", "rest" };
        public const int TrashSpots = 8;

        // ------------------------------------------------------------------ boosts
        public BoostData Boost(string id) => S.boosts.First(b => b.id == id);
        public bool BoostActive(string id) => Boost(id).remaining > 0;
        public float BoostMult(string id) => BoostActive(id) ? C.boost.multiplier : 1f;

        /// <summary>Speed-up button: x multiplier for duration, then cooldown. No resource cost (prototype; original uses ads).</summary>
        public bool StartBoost(string id)
        {
            var b = S.boosts.FirstOrDefault(x => x.id == id);
            if (b == null) return false;
            if (b.remaining > 0) { E(Ev.BoostBusy, 0, 0, 0, 0, 0, id); return false; }
            if (b.cooldown > 0) { E(Ev.BoostBusy, 1, 0, 0, 0, (long)Math.Ceiling(b.cooldown), id); return false; }
            b.remaining = C.boost.duration;
            S.stats.boostsUsed++;
            E(Ev.BoostStarted, 0, 0, 0, 0, 0, id);
            return true;
        }

        void UpdateBoosts(float dt)
        {
            foreach (var b in S.boosts)
            {
                if (b.remaining > 0)
                {
                    b.remaining -= dt;
                    if (b.remaining <= 0) { b.remaining = 0; b.cooldown = C.boost.cooldown; E(Ev.BoostEnded, 0, 0, 0, 0, 0, b.id); }
                }
                else if (b.cooldown > 0) b.cooldown = Math.Max(0, b.cooldown - dt);
            }
        }

        // ------------------------------------------------------------------ marketing (customer attraction bar)
        float marketingProgress, marketingCycle = 7f;
        public float MarketingProgress => marketingProgress;
        public float MarketingRate
        {
            get
            {
                var p = Staff("promoter");
                float r = IsAway(p) ? C.marketing.awayMult : 1f;
                r *= BoostMult("promoter");
                r *= 1f - Math.Min(C.marketing.trashPenaltyCap, C.marketing.trashPenalty * S.trash.Count);
                r *= 1f + Math.Min(0.3f, SumEffect("spawn"));
                return r;
            }
        }
        public float MarketingSecondsLeft => (1f - marketingProgress) * marketingCycle / Math.Max(0.01f, MarketingRate);
        public bool MarketingWaiting => marketingProgress >= 1f;   // bar full: waiting for stock / room

        void UpdateMarketing(float dt)
        {
            if (marketingProgress < 1f) marketingProgress = Math.Min(1f, marketingProgress + dt * MarketingRate / marketingCycle);
            if (marketingProgress >= 1f && TotalAvailable() > 0 && ActiveCustomers < C.flow.maxCustomers)
            {
                if (SpawnCustomer() != null)
                {
                    marketingProgress = 0;
                    marketingCycle = Rand(C.flow.spawnMin, C.flow.spawnMax);
                    E(Ev.MarketingAttract);
                }
            }
            // sad trickle when nothing is on the shelves (feedback, never takes stock)
            if (TotalAvailable() == 0 && OwnedShelves > 0 && S.stats.stocked > 0 && ActiveCustomers < C.flow.maxCustomers)
            {
                sadTimer += dt;
                if (sadTimer >= C.flow.sadTrickleSeconds) { sadTimer = 0; SpawnSad(); }
            }
            else sadTimer = 0;
        }

        // ------------------------------------------------------------------ trash + cleaner
        void MaybeDropTrash()
        {
            if (S.trash.Count >= TrashSpots || rng.NextDouble() >= C.marketing.trashChance) return;
            var free = Enumerable.Range(0, TrashSpots).Where(i => !S.trash.Any(t => t.spot == i)).ToList();
            if (free.Count == 0) return;
            var t = new TrashData { id = NewId(), spot = free[rng.Next(free.Count)], kind = rng.Next(4) };
            S.trash.Add(t);
            E(Ev.TrashSpawned, t.spot, t.kind, 0, t.id);
        }

        /// <summary>Player taps a trash item.</summary>
        public bool CleanTrash(long id)
        {
            int i = S.trash.FindIndex(t => t.id == id);
            if (i < 0) return false;
            var t = S.trash[i];
            if (cleanerTarget == id) return false;       // the raccoon is already sweeping it
            S.trash.RemoveAt(i);
            S.stats.trashCleaned++;
            Earn(C.marketing.cleanReward, false);
            E(Ev.TrashCleaned, t.spot, 0, 0, t.id, C.marketing.cleanReward);
            return true;
        }

        public int CleanerQuestProgress => Math.Min(C.marketing.cleanerQuestTarget, S.stats.trashCleaned);
        public bool CleanerQuestAvailable => !S.cleanerHired && (S.stats.trashCleaned > 0 || S.trash.Count > 0);
        public bool CleanerQuestDone => !S.cleanerHired && S.stats.trashCleaned >= C.marketing.cleanerQuestTarget;

        /// <summary>Side quest: clean N trash by hand -> the raccoon joins as cleaner (+coins).</summary>
        public bool ClaimCleanerQuest()
        {
            if (!CleanerQuestDone) return false;
            S.cleanerHired = true;
            Earn(C.marketing.cleanerQuestReward, false);
            E(Ev.CleanerHired, 0, 0, 0, 0, C.marketing.cleanerQuestReward);
            return true;
        }

        public CleanerState Cleaner { get; private set; }
        public float CleanerTimer { get; private set; }
        public float CleanerPhaseLength { get; private set; }
        long cleanerTarget;
        public int CleanerTargetSpot { get; private set; } = -1;

        void UpdateCleaner(float dt)
        {
            if (!S.cleanerHired) { Cleaner = CleanerState.Idle; return; }
            CleanerTimer += dt;
            switch (Cleaner)
            {
                case CleanerState.Idle:
                case CleanerState.Returning:
                    if (Cleaner == CleanerState.Returning && CleanerTimer < CleanerPhaseLength) break;
                    if (S.trash.Count > 0)
                    {
                        var t = S.trash[0];
                        cleanerTarget = t.id; CleanerTargetSpot = t.spot;
                        Cleaner = CleanerState.Walking; CleanerTimer = 0; CleanerPhaseLength = C.marketing.cleanerWalkSeconds;
                    }
                    else if (Cleaner == CleanerState.Returning) { Cleaner = CleanerState.Idle; CleanerTimer = 0; }
                    break;
                case CleanerState.Walking:
                    if (CleanerTimer >= CleanerPhaseLength) { Cleaner = CleanerState.Sweeping; CleanerTimer = 0; CleanerPhaseLength = C.marketing.cleanerSweepSeconds; }
                    break;
                case CleanerState.Sweeping:
                    if (CleanerTimer >= CleanerPhaseLength)
                    {
                        int i = S.trash.FindIndex(x => x.id == cleanerTarget);
                        if (i >= 0)
                        {
                            var t = S.trash[i]; S.trash.RemoveAt(i);
                            S.stats.trashCleanedByCleaner++;
                            E(Ev.TrashCleaned, t.spot, 1, 0, t.id, 0);
                        }
                        cleanerTarget = 0;
                        Cleaner = CleanerState.Returning; CleanerTimer = 0; CleanerPhaseLength = C.marketing.cleanerWalkSeconds;
                    }
                    break;
            }
        }

        // ------------------------------------------------------------------ delivery orders (dog next to the counter)
        public bool DeliveryUnlocked
        {
            get { var q = S.quests.FirstOrDefault(x => x.id == C.delivery.unlockAfterQuest); return q != null && q.completed; }
        }

        void UpdateDelivery(float dt)
        {
            var d = S.delivery;
            if (!DeliveryUnlocked) return;
            if (d.state == (int)DeliveryState.None)
            {
                d.timer -= dt;
                if (d.timer <= 0) NewOrder();
            }
            else if (d.state == (int)DeliveryState.Out)
            {
                d.timer -= dt;
                if (d.timer <= 0) FinishDelivery(false);
            }
        }

        void NewOrder()
        {
            var d = S.delivery;
            var pool = S.unlockedRecipes.ToList();
            // prefer the two highest unlocked recipes (worth packing), never a locked one
            pool.Sort(); if (pool.Count > 2) pool = pool.Skip(pool.Count - 2).ToList();
            int r = pool[rng.Next(pool.Count)];
            int qty = C.delivery.qtyMin + rng.Next(C.delivery.qtyMax - C.delivery.qtyMin + 1);
            d.orderId = "D" + NewId();
            d.recipe = r; d.qty = qty;
            d.reward = (long)Math.Round(C.Recipe(r).price * qty * C.delivery.rewardMult) + C.delivery.rewardBonus;
            d.state = (int)DeliveryState.Open; d.timer = 0;
            E(Ev.DeliveryOrder, r, qty, 0, 0, d.reward, d.orderId);
        }

        public bool CanPackDelivery => S.delivery.state == (int)DeliveryState.Open && S.inventory[S.delivery.recipe - 1] >= S.delivery.qty;

        /// <summary>The dog packs the order from storage and leaves; payment when it returns (one commit per orderId).</summary>
        public bool PackDelivery()
        {
            var d = S.delivery;
            if (d.state != (int)DeliveryState.Open) return false;
            if (S.inventory[d.recipe - 1] < d.qty) { E(Ev.StockFailed, -1, d.recipe, 3); return false; }
            S.inventory[d.recipe - 1] -= d.qty;
            d.state = (int)DeliveryState.Out; d.timer = C.delivery.travelSeconds;
            E(Ev.DeliveryPacked, d.recipe, d.qty, 0, 0, d.reward, d.orderId);
            return true;
        }

        public bool SkipDelivery()
        {
            var d = S.delivery;
            if (d.state != (int)DeliveryState.Open) return false;
            d.state = (int)DeliveryState.None; d.timer = C.delivery.skipSeconds; d.orderId = "";
            E(Ev.DeliverySkipped);
            return true;
        }

        /// <returns>coins credited (0 if already committed)</returns>
        long FinishDelivery(bool offline)
        {
            var d = S.delivery;
            long paid = 0;
            if (!string.IsNullOrEmpty(d.orderId) && !S.committedTx.Contains(d.orderId))
            {
                CommitTx(d.orderId);
                S.stats.deliveries++;
                paid = d.reward;
                if (!offline) Earn(d.reward, true);
                E(Ev.DeliveryDone, d.recipe, d.qty, offline ? 1 : 0, 0, d.reward, d.orderId);
            }
            d.state = (int)DeliveryState.None; d.timer = C.delivery.orderInterval; d.orderId = "";
            return paid;
        }

        /// <summary>Offline part for the new systems (called from ApplyElapsed). Returns coins for the pending offline reward.</summary>
        long ApplyElapsedExtra(float s)
        {
            foreach (var b in S.boosts)
            {
                if (b.remaining > 0)
                {
                    float r = Math.Min(b.remaining, s); b.remaining -= r;
                    if (b.remaining <= 0) { b.remaining = 0; b.cooldown = Math.Max(0, C.boost.cooldown - (s - r)); }
                }
                else b.cooldown = Math.Max(0, b.cooldown - s);
            }
            long coins = 0;
            var d = S.delivery;
            if (d.state == (int)DeliveryState.Out) { d.timer -= s; if (d.timer <= 0) coins += FinishDelivery(true); }
            else if (d.state == (int)DeliveryState.None && DeliveryUnlocked) { d.timer -= s; if (d.timer <= 0) NewOrder(); }
            if (S.cleanerHired && S.trash.Count > 0)
            {   // the hired raccoon keeps the street clean while you are away
                int n = Math.Min(S.trash.Count, (int)(s / (2 * C.marketing.cleanerWalkSeconds + C.marketing.cleanerSweepSeconds)));
                for (int i = 0; i < n; i++) { S.trash.RemoveAt(0); S.stats.trashCleanedByCleaner++; }
            }
            return coins;
        }
    }
}
