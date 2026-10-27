// World layout (units = original art px / 100; screen width = 7.2 units like the source project).
// Zones (navigated with side arrows): kitchen | shop interior + storefront + marketing street | break room.
using System.Collections.Generic;
using BB.Model;
using UnityEngine;

namespace BB.View
{
    public static class WL
    {
        public const float Width = 7.2f;
        public const float BgWidth = 11f;     // backgrounds wider than the screen (zoomed-out zones never show a gap)
        // zone bounds (y). Zones are navigated with the side arrows: Kitchen | Shop (selling + marketing street) | Break room
        public const float BreakTop = 0f, BreakWallBottom = -2.7f, BreakBottom = -7.6f;
        public const float KitchenTop = -7.6f, KitchenWallBottom = -9.69f, KitchenBottom = -18.6f;
        public const float ShopTop = -18.6f, ShopWallBottom = -20.66f;
        public const float FacadeTop = -25.45f, FacadeCenterY = -26.76f, FacadeBottom = -28.07f, WorldBottom = -33.6f;
        // side-arrow anchor heights per zone (kitchen, shop-selling, break) + shop marketing view
        public static readonly float[] NavArrowY = { -11.55f, -22.75f, -4.4f };
        public const float NavArrowYMarketing = -28.85f;

        // camera focus helpers (kept for callers)
        public const float FocusBreak = -4.3f, FocusKitchen = -13.1f, FocusShop = -22.0f, FocusMarketing = -30.0f;

        // ---------------- kitchen (reference layout: bear + table on top, 5-column merge trays filled bottom-up, ovens at the bottom)
        public static readonly Vector2 ChefPos = new Vector2(0f, -10.1f);
        public static readonly Vector2 KitchenTablePos = new Vector2(0f, -10.49f);
        public static readonly Vector2 StoragePos = new Vector2(-2.6f, -10.15f);    // bottom-anchored
        public static readonly Vector2 FridgePos = new Vector2(2.6f, -9.98f);       // bottom-anchored
        public static readonly Vector2 WindowPos = new Vector2(0f, -8.5f);
        public static readonly float[] BoardX = { -2.34f, -1.17f, 0f, 1.17f, 2.34f };
        public static readonly float[] BoardY = { -15.45f, -14.15f, -12.85f };      // row 0 is the bottom row
        public const int BoardCols = 5;
        public static Vector2 Slot(int i) => new Vector2(BoardX[i % BoardCols], BoardY[i / BoardCols]);
        public static Vector2 DoughOnSlot(int i) => Slot(i) + new Vector2(0, 0.2f);
        public static readonly Vector2 SlotSize = new Vector2(1.17f, 1.3f);
        public static readonly Vector2[] Ovens = { new Vector2(-2.27f, -17.22f), new Vector2(-0.91f, -17.22f), new Vector2(0.45f, -17.22f) };
        public static readonly Vector2 OvenSize = new Vector2(1.33f, 1.9f);

        // ---------------- shop (counter on top, delivery dog left of it, one row of stands, storefront, street)
        public static readonly Vector2 CashierPos = new Vector2(0f, -20.74f);
        public static readonly Vector2 CounterPos = new Vector2(0f, -21.1f);
        public static readonly Vector2 PiggyPos = new Vector2(-1.2f, -20.62f);
        public static readonly Vector2 MilkFridgePos = new Vector2(2.6f, -20.95f);
        public static readonly Vector2 DogHome = new Vector2(-2.5f, -21.78f);
        public static readonly Vector2 DeliverySignPos = new Vector2(-2.45f, -19.75f);
        public static readonly Vector2 DogAway = new Vector2(-4.6f, -22.4f);
        public static readonly Vector2[] Shelves = {
            new Vector2(-1.665f, -24.0f), new Vector2(-0.495f, -24.0f), new Vector2(0.675f, -24.0f), new Vector2(1.845f, -24.0f), new Vector2(3.015f, -24.0f) };
        public static readonly Vector2 ShelfSize = new Vector2(1.17f, 1.3f);
        public static readonly Vector2[] BreadOnShelf = {   // StandSlot.prefab offsets (px/100) relative to stand centre
            new Vector2(0f, 0.25f), new Vector2(-0.2f, 0.10f), new Vector2(0.2f, 0.10f), new Vector2(-0.2f, 0.35f), new Vector2(0.2f, 0.35f) };
        public const float StandY = -24.95f, CorridorY = -25.0f, QueueLaneY = -22.62f, AisleX = -2.9f;
        public static Vector2 StandPoint(int shelf) => new Vector2(Shelves[shelf].x, StandY);
        public static readonly Vector2[] QueueSlots = {
            new Vector2(0f, -22.2f), new Vector2(0.75f, -22.2f), new Vector2(1.5f, -22.2f),
            new Vector2(2.25f, -22.2f), new Vector2(3.0f, -22.2f), new Vector2(3.0f, -22.85f) };
        public static Vector2 Queue(int i) => QueueSlots[Mathf.Clamp(i, 0, QueueSlots.Length - 1)];
        public static readonly Vector2 DoorIn = new Vector2(2.75f, -25.25f);
        public static readonly Vector2 DoorOut = new Vector2(2.75f, -28.45f);
        public static readonly Vector2 StreetIn = new Vector2(4.6f, -29.35f);
        public static readonly Vector2 StreetOut = new Vector2(-4.7f, -29.75f);
        public static readonly Vector2 SidewalkTurn = new Vector2(2.3f, -29.75f);   // leave along the kerb, in front of the sparrow and the raccoon
        // marketing street
        public static readonly Vector2 StoolPos = new Vector2(0.55f, -28.95f);       // bottom-anchored
        public static readonly Vector2 PromoterPos = new Vector2(0.55f, -28.3f);     // sitting on the stool
        public static readonly Vector2 MarketingBarPos = new Vector2(1.45f, -27.55f);
        public static readonly Vector2 RaccoonHome = new Vector2(-1.85f, -29.05f);
        public static readonly Vector2[] TrashSpots = {
            new Vector2(-2.9f, -30.25f), new Vector2(-1.15f, -30.95f), new Vector2(0.65f, -30.45f), new Vector2(2.35f, -30.85f),
            new Vector2(-2.2f, -32.05f), new Vector2(-0.35f, -32.4f), new Vector2(1.45f, -31.85f), new Vector2(2.95f, -32.65f) };
        public const float TeleportTime = 0.35f;

        // ---------------- break room
        public static readonly Vector2 StaffDoor = new Vector2(3.0f, -2.75f);
        public static readonly Dictionary<string, Vector2> FurniturePos = new Dictionary<string, Vector2> {
            { "bed", new Vector2(-2.3f, -3.45f) }, { "sofa", new Vector2(0.75f, -3.35f) }, { "lamp", new Vector2(-0.85f, -3.25f) },
            { "beanbag", new Vector2(-2.3f, -6.15f) }, { "coffee", new Vector2(2.55f, -5.75f) }, { "rug", new Vector2(0.35f, -5.6f) } };
        public static readonly Dictionary<string, Vector2> RestPos = new Dictionary<string, Vector2> {
            { "bed", new Vector2(-2.3f, -2.65f) }, { "sofa", new Vector2(0.75f, -2.85f) }, { "beanbag", new Vector2(-2.3f, -5.55f) } };

        public static float Sort(float y) => 1000 + (-y * 100f);
        public static int SortOrder(float y) => Mathf.Clamp((int)Sort(y), -30000, 30000);
    }

    /// <summary>A walk path: list of points; a NaN-x point marks a door teleport between its neighbours.</summary>
    public class WalkPath
    {
        public readonly List<Vector2> pts = new List<Vector2>();
        public readonly List<bool> teleport = new List<bool>();   // teleport[i] => segment i (pts[i]->pts[i+1]) is a door transition
        public readonly List<float> pauseAt = new List<float>();  // optional pause (seconds) at point i
        public float speed = 1.9f;

        public WalkPath Add(Vector2 p, bool teleportFromPrev = false, float pause = 0)
        {
            if (pts.Count > 0) teleport.Add(teleportFromPrev);
            pts.Add(p); pauseAt.Add(pause);
            return this;
        }

        float SegTime(int i) => teleport[i] ? WL.TeleportTime : Vector2.Distance(pts[i], pts[i + 1]) / speed;

        public float Duration
        {
            get { float d = 0; for (int i = 0; i < pts.Count - 1; i++) d += SegTime(i); foreach (var p in pauseAt) d += p; return d; }
        }

        /// <summary>Position, facing (dx), alpha and whether walking at time t (seconds since path start).</summary>
        public void Eval(float t, out Vector2 pos, out float dx, out float alpha, out bool walking)
        {
            alpha = 1; dx = 0; walking = false;
            if (pts.Count == 0) { pos = Vector2.zero; return; }
            if (pts.Count == 1) { pos = pts[0]; return; }
            for (int i = 0; i < pts.Count - 1; i++)
            {
                if (pauseAt[i] > 0) { if (t < pauseAt[i]) { pos = pts[i]; return; } t -= pauseAt[i]; }
                float st = SegTime(i);
                if (t <= st)
                {
                    float k = st <= 0 ? 1 : t / st;
                    if (teleport[i])
                    {
                        pos = k < 0.5f ? pts[i] : pts[i + 1];
                        alpha = Mathf.Abs(k - 0.5f) * 2f;
                        dx = 0; walking = true;
                        return;
                    }
                    pos = Vector2.Lerp(pts[i], pts[i + 1], k);
                    dx = pts[i + 1].x - pts[i].x;
                    walking = true;
                    return;
                }
                t -= st;
            }
            pos = pts[pts.Count - 1];
        }
    }

    /// <summary>Real path lengths for the model's customer phase durations (left aisle, corridor in front of the stands).</summary>
    public class ShopPaths : IShopLayout
    {
        public float speed = 1.9f;
        public ShopPaths(float speed) { this.speed = speed; }

        public WalkPath Enter(int shelf)
        {
            var p = new WalkPath { speed = speed };
            var stand = WL.StandPoint(shelf);
            p.Add(WL.StreetIn).Add(WL.DoorOut).Add(WL.DoorIn, true).Add(new Vector2(WL.DoorIn.x, WL.CorridorY)).Add(stand);
            return p;
        }

        public WalkPath ToQueue(int shelf, int q, Vector2 from)
        {
            var p = new WalkPath { speed = speed };
            var slot = WL.Queue(q);
            p.Add(from).Add(new Vector2(WL.AisleX, WL.CorridorY)).Add(new Vector2(WL.AisleX, WL.QueueLaneY))
             .Add(new Vector2(slot.x, WL.QueueLaneY)).Add(slot);
            return p;
        }

        public WalkPath Exit(Vector2 from)
        {
            var p = new WalkPath { speed = speed };
            p.Add(from).Add(new Vector2(from.x, WL.QueueLaneY)).Add(new Vector2(WL.AisleX, WL.QueueLaneY)).Add(new Vector2(WL.AisleX, WL.CorridorY))
             .Add(new Vector2(WL.DoorIn.x, WL.CorridorY)).Add(WL.DoorIn).Add(WL.DoorOut, true).Add(WL.SidewalkTurn).Add(WL.StreetOut);
            return p;
        }

        public WalkPath Sad()
        {
            var p = new WalkPath { speed = speed };
            p.Add(WL.StreetIn).Add(WL.DoorOut).Add(WL.DoorIn, true).Add(new Vector2(1.2f, WL.CorridorY))
             .Add(new Vector2(1.21f, WL.CorridorY), false, 1.6f).Add(WL.DoorIn).Add(WL.DoorOut, true).Add(WL.SidewalkTurn).Add(WL.StreetOut);
            return p;
        }

        public float EnterToShelf(int shelf) => Enter(shelf).Duration;
        public float ShelfToQueue(int shelf, int queueIndex) => ToQueue(shelf, queueIndex, WL.StandPoint(shelf)).Duration;
        public float QueueToExit() => Exit(WL.Queue(0)).Duration;
        public float SadVisit() => Sad().Duration;
    }
}
