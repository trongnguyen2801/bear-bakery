// Balance/config data. Loaded from Resources/config.json (single shared config table).
// All numbers are playtest starting points for this prototype, NOT values from the original game.
using System;

namespace BB.Model
{
    [Serializable] public class RecipeCfg
    {
        public int level;            // 1..6 (merge chain position)
        public string id;            // semantic art key, e.g. "DinnerRolls"
        public string nameVi;
        public float bakeSeconds;    // base bake time
        public int pieces;           // pieces produced per bake
        public int price;            // sell price per piece
    }

    [Serializable] public class CustomerCfg
    {
        public string id;            // Cat, Giraffe ... (art key)
        public string nameVi;
        public string descVi;
        public int[] likes;          // ordered preference list (recipe levels)
        public int unlockLevel;      // shop level needed before this guest visits
    }

    [Serializable] public class ChefCfg
    {
        public float[] intervalByLevel;      // seconds per dough at upgrade level 1..n
        public float[] bonusChanceByLevel;   // chance that a fresh dough is level 2
        public float helpFraction;           // progress added per help tap (fraction of a cycle)
        public int helpMaxPerCycle;          // help taps that count per dough cycle
        public int manualTapsPerDough;       // taps needed when the chef is away
    }

    [Serializable] public class CashierCfg
    {
        public float checkoutSeconds;
        public float helpFraction;
        public int helpMaxPerCheckout;
        public float manualTapFraction;      // when cashier is away
    }

    [Serializable] public class PromoterCfg
    {
        public float spawnMultiplier;        // spawn interval multiplier while promoting
        public float shoutCooldown;
    }

    [Serializable] public class EnergyCfg
    {
        public float chefPerDough;
        public float cashierPerCheckout;
        public float promoterPerSecond;
        public float tiredThreshold;
        public float tiredSpeedMult;         // speed multiplier when tired (floor)
        public float restThreshold;          // go rest at/below this
        public float restDoneAt;
        public float transitSeconds;
        public float inPlaceRecovery;        // per second, when no rest spot is free
        public float maxRecovery;            // cap per second
        public float maxDrainReduction;      // cap on furniture drain reduction
    }

    [Serializable] public class UpgradeCfg
    {
        public string id;                    // chef | oven | capacity
        public string nameVi;
        public string descVi;
        public int maxLevel;
        public int[] costs;                  // costs[i] = price to go from level i+1 to i+2
        public float[] values;               // chef: unused (see ChefCfg); oven: bake time mult; capacity: shelf cap
        public float[] values2;              // capacity: storage cap
    }

    [Serializable] public class ExpansionCfg
    {
        public int start;
        public int max;
        public int[] costs;                  // cost of the (start+i)-th unit
        public int[] levelReq;               // shop level required for the (start+i)-th unit
    }

    [Serializable] public class FurnitureCfg
    {
        public string id;                    // art id suffix (furn.<id>)
        public string nameVi;
        public string descVi;
        public string effect;                // rest | drain | restBoost | tips | spawn
        public float[] values;               // effect value per level
        public int[] costs;                  // [buy, ->lv2, ->lv3]
        public int levelReq;
        public bool restSpot;
        public bool startOwned;
    }

    [Serializable] public class QuestCfg
    {
        public string id;
        public string type;                  // event or state type
        public int target;
        public int reward;
        public string textVi;
        public string hintVi;
        public bool tutorial;
    }

    [Serializable] public class FlowCfg
    {
        public float spawnMin, spawnMax;
        public int maxCustomers;
        public float sadTrickleSeconds;
        public float pickSeconds;
        public float walkSpeed;              // world units / second (view + layout)
        public float tipBase, tipPerHeart, tipCap;
        public int affectionBuy, affectionFav;
        public int[] heartThresholds;
        public float firstSpawnDelay;
    }

    [Serializable] public class EconomyCfg
    {
        public int startCoins;
        public int autoRestockCost;
        public string autoRestockAfterQuest;
        public float autosaveSeconds;
    }

    [Serializable] public class OfflineCfg
    {
        public float maxSeconds;             // offline cap (default 2h)
        public float saleInterval;           // one offline sale per N seconds (stock-limited)
        public float energyRecoveryPerSecond;
        public float minSecondsForReport;
        public float resumeGapSeconds;       // gaps longer than this during a session use the offline path
    }

    [Serializable] public class BurnCfg
    {
        public bool burnEnabled;             // prototype option; OFF by default
        public float burnGraceSeconds;
        public float warnSeconds;
    }

    [Serializable] public class BoostCfg { public float duration; public float cooldown; public float multiplier; }

    [Serializable] public class MarketingCfg
    {
        public float awayMult;              // bar speed while Tweetie rests
        public float trashChance;           // chance a paying customer drops trash
        public float trashPenalty, trashPenaltyCap;
        public int cleanReward;
        public int cleanerQuestTarget, cleanerQuestReward;
        public float cleanerWalkSeconds, cleanerSweepSeconds;
    }

    [Serializable] public class DeliveryCfg
    {
        public string unlockAfterQuest;
        public float orderInterval, travelSeconds, skipSeconds, firstDelay;
        public int qtyMin, qtyMax;
        public float rewardMult; public int rewardBonus;
    }

    [Serializable] public class TutorialCfg
    {
        public float handIdleSeconds;
    }

    [Serializable] public class GameConfig
    {
        public int version;
        public RecipeCfg[] recipes;
        public CustomerCfg[] customers;
        public ChefCfg chef;
        public CashierCfg cashier;
        public PromoterCfg promoter;
        public EnergyCfg energy;
        public UpgradeCfg[] upgrades;
        public ExpansionCfg board;
        public ExpansionCfg ovens;
        public ExpansionCfg shelves;
        public FurnitureCfg[] furniture;
        public QuestCfg[] quests;
        public FlowCfg flow;
        public EconomyCfg economy;
        public OfflineCfg offline;
        public BurnCfg burn;
        public TutorialCfg tutorial;
        public BoostCfg boost;
        public MarketingCfg marketing;
        public DeliveryCfg delivery;
        public int[] levelXp;                // xp thresholds per shop level (index 0 = level 1)

        public int MaxRecipe => recipes.Length;
        public RecipeCfg Recipe(int level) => recipes[level - 1];
        public UpgradeCfg Upgrade(string id) { foreach (var u in upgrades) if (u.id == id) return u; return null; }
        public int UpgradeIndex(string id) { for (int i = 0; i < upgrades.Length; i++) if (upgrades[i].id == id) return i; return -1; }
        public FurnitureCfg Furniture(string id) { foreach (var f in furniture) if (f.id == id) return f; return null; }
        public CustomerCfg Customer(string id) { foreach (var c in customers) if (c.id == id) return c; return null; }
    }
}
