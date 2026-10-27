// Persistent state (JSON). Only committed transactions live here; drags in progress never do.
using System;
using System.Collections.Generic;

namespace BB.Model
{
    [Serializable] public class SlotData { public long id; public int level; }            // level 0 = empty

    public enum OvenState { Empty = 0, Baking = 1, Ready = 2, Burnt = 3 }

    [Serializable] public class OvenData
    {
        public bool owned;
        public long jobId;
        public int recipe;
        public float remaining;
        public float total;
        public int state;          // OvenState
        public float readyFor;     // seconds spent ready during ACTIVE play (burn mode only)
    }

    [Serializable] public class ShelfData { public bool owned; public int recipe; public int stock; public bool auto; }
    [Serializable] public class FurnitureData { public string id; public bool owned; public int level; }

    public enum StaffState { Working = 0, IdleBlocked = 1, ToRest = 2, Resting = 3, Returning = 4, RestInPlace = 5 }

    [Serializable] public class StaffData
    {
        public string id;          // chef | cashier | promoter
        public float energy = 100;
        public int state;          // StaffState
        public float timer;
        public string restSpot = "";
        public float progress;     // work progress (chef: dough cycle, cashier: checkout)
        public int helpUsed;
    }

    [Serializable] public class BoostData { public string id; public float remaining; public float cooldown; }
    [Serializable] public class TrashData { public long id; public int spot; public int kind; }
    public enum DeliveryState { None = 0, Open = 1, Out = 2 }
    [Serializable] public class DeliveryData { public int state; public string orderId = ""; public int recipe; public int qty; public long reward; public float timer = 20; }

    [Serializable] public class AffectionData { public string id; public bool met; public int affection; public int purchases; }
    [Serializable] public class QuestData { public string id; public int progress; public bool completed; public bool claimed; }

    [Serializable] public class OfflineRewardData
    {
        public string id = "";     // empty = nothing pending
        public long coins;
        public int sold;
        public int bakedDone;
        public float seconds;
        public bool capped;
    }

    [Serializable] public class PendingSaleData
    {
        public string txId;
        public string customer;
        public int recipe;
        public int price;
        public int tip;
        public bool favorite;
    }

    [Serializable] public class StatsData
    {
        public int doughMade, merges, bakes, collects, stocked, sold, upgradesBought, helpTaps, furnitureBought, boostsUsed, trashCleaned, trashCleanedByCleaner, deliveries;
        public float firstSaleAt = -1, firstUpgradeAt = -1, oven2At = -1, firstMergeAt = -1, firstBakeAt = -1;
        public float[] recipeUnlockAt = new float[6] { 0, -1, -1, -1, -1, -1 };
    }

    [Serializable] public class SaveData
    {
        public const int CurrentVersion = 3;
        public int saveVersion = CurrentVersion;
        public double lastSavedAt;          // unix seconds (UTC)
        public double lastSimAt;            // unix seconds when the model was last advanced
        public double playSeconds;          // active play time
        public long currency;
        public long totalEarned;
        public long xp;
        public List<int> unlockedRecipes = new List<int>();
        public int boardUnlocked;
        public List<SlotData> boardSlots = new List<SlotData>();
        public List<OvenData> ovenJobs = new List<OvenData>();
        public List<int> inventory = new List<int>();          // pieces per recipe (index = level-1)
        public List<ShelfData> shelfStock = new List<ShelfData>();
        public List<int> upgrades = new List<int>();           // level per upgrade (config order)
        public List<FurnitureData> furniture = new List<FurnitureData>();
        public List<StaffData> staffEnergy = new List<StaffData>();
        public List<AffectionData> customerAffection = new List<AffectionData>();
        public List<QuestData> quests = new List<QuestData>();
        public List<string> claimedRewards = new List<string>();
        public int tutorialStep;
        public long nextId = 1;
        public List<string> committedTx = new List<string>();  // recent committed transaction ids (dedupe)
        public long tipJar;
        public OfflineRewardData pendingOffline = new OfflineRewardData();
        public List<PendingSaleData> pendingSales = new List<PendingSaleData>();
        public bool autoRestockUnlocked;
        public bool soundOn = true;
        public bool handHints = true;
        public bool visitedBreakRoom;
        public List<BoostData> boosts = new List<BoostData>();
        public List<TrashData> trash = new List<TrashData>();
        public bool cleanerHired;
        public DeliveryData delivery = new DeliveryData();
        public StatsData stats = new StatsData();
    }
}
