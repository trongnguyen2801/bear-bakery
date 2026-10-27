using System;

namespace BB.Model
{
    public enum Ev
    {
        DoughSpawned, DoughMerged, DoughMoved, DropRejected, MaxLevel, BoardFull, BoardFreed,
        BakeStarted, OvenReady, OvenCollected, InventoryFull, OvenBurnWarn, OvenBurnt, OvenCleared, OvenBusy,
        ShelfStocked, StockFailed, AutoRestocked,
        CustomerSpawned, CustomerPicking, CustomerPicked, CustomerQueued, CustomerPaying, SaleCommitted, CustomerLeft, CustomerSad,
        TipAdded, TipsClaimed,
        UpgradeBought, ExpansionBought, FurnitureBought, FurnitureUpgraded, AutoRestockUnlocked, AutoToggled,
        RecipeUnlocked, QuestProgress, QuestCompleted, QuestClaimed, LevelUp,
        StaffState, ChefHelped, HelpLimit, CashierHelped, PromoterShout, ManualKnead, ManualCheckout,
        BoostStarted, BoostEnded, BoostBusy, MarketingAttract, TrashSpawned, TrashCleaned, CleanerHired,
        DeliveryOrder, DeliveryPacked, DeliveryDone, DeliverySkipped,
        OfflineReport, OfflineClaimed, NotEnoughCoins, LevelTooLow, HeartUp, CustomerMet, PendingSalesSettled
    }

    public struct GameEvent
    {
        public Ev type;
        public int a, b, c;
        public long id;
        public long amount;
        public string s;
        public override string ToString() => $"{type}({a},{b},{c},id={id},amt={amount},{s})";
    }

    /// <summary>Walking durations come from the renderer's real paths; tests use DefaultLayout.</summary>
    public interface IShopLayout
    {
        float EnterToShelf(int shelf);
        float ShelfToQueue(int shelf, int queueIndex);
        float QueueToExit();
        float SadVisit();
    }

    public class DefaultLayout : IShopLayout
    {
        public float EnterToShelf(int shelf) => 5.0f + 0.4f * (shelf / 2);
        public float ShelfToQueue(int shelf, int queueIndex) => 2.2f + 0.3f * queueIndex;
        public float QueueToExit() => 5.0f;
        public float SadVisit() => 9.0f;
    }

    public enum CPhase { Enter = 0, Pick = 1, ToQueue = 2, Queue = 3, Paying = 4, Leave = 5, SadVisit = 6, Done = 7 }

    /// <summary>Runtime customer (not persisted except as PendingSaleData when carrying unpaid bread).</summary>
    public class Customer
    {
        public long id;
        public string type;
        public CPhase phase;
        public float t, dur;
        public int shelf = -1;
        public int recipe;
        public int favRecipe;
        public bool fav;
        public int price;
        public bool carrying;
        public string txId;
        public int queueIndex = -1;
    }

    public enum DropResult { Invalid, Moved, Merged, MaxLevel, Same }
    public enum CollectResult { Collected, NotReady, StorageFull, Cleared }
}
