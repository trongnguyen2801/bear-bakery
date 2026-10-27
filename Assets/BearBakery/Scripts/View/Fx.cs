// Small sprite effects built from the provided VFX sprites (dust, stars, twinkles, smoke). Short and non-blocking.
using UnityEngine;

namespace BB.View
{
    public static class Fx
    {
        public static Transform Root;
        const int FxOrder = 25000;

        public static SpriteRenderer Sprite(string id, Vector2 pos, int order = FxOrder, float scale = 1f, Transform parent = null)
        {
            var go = new GameObject("fx." + id);
            go.transform.SetParent(parent != null ? parent : Root, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Art.S(id);
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>Flour dust burst (Img_MergeDust01): a few puffs expanding and fading ~0.35s.</summary>
        public static void Dust(Vector2 pos, int count = 5, float radius = 0.45f, float scale = 0.6f, Color? tint = null)
        {
            for (int i = 0; i < count; i++)
            {
                float a = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.6f);
                var sr = Sprite("fx.mergeDust", pos, FxOrder, scale * Random.Range(0.6f, 1f));
                sr.color = tint ?? new Color(1, 1, 1, 0.95f);
                var tr = sr.transform; var start = pos; float s0 = tr.localScale.x;
                Tw.Run(sr, Random.Range(0.3f, 0.42f), t =>
                {
                    tr.position = start + dir * radius * t;
                    tr.localScale = Vector3.one * s0 * (1f + 0.6f * t);
                    var c = sr.color; c.a = 1f - t; sr.color = c;
                }, Ease.OutQuad, () => Object.Destroy(sr.gameObject));
            }
        }

        public static void Stars(Vector2 pos, int count = 4, float radius = 0.7f, string id = "fx.star", float scale = 0.45f)
        {
            for (int i = 0; i < count; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var sr = Sprite(i % 2 == 0 ? id : "fx.twinkle2", pos, FxOrder + 1, scale);
                var tr = sr.transform; var start = pos;
                Tw.Run(sr, 0.5f, t =>
                {
                    tr.position = start + dir * radius * t + Vector2.up * 0.2f * t;
                    tr.localScale = Vector3.one * scale * (1f - 0.7f * t);
                    tr.rotation = Quaternion.Euler(0, 0, 180f * t);
                    var c = sr.color; c.a = 1f - t * t; sr.color = c;
                }, Ease.OutCubic, () => Object.Destroy(sr.gameObject));
            }
        }

        /// <summary>Oven smoke puff (timings from Oven_On.anim: rise 72->115px, scale 1.1->1.4, fade over 2s).</summary>
        public static void Smoke(Vector2 pos, int order)
        {
            var sr = Sprite("equip.ovenSmoke", pos, order, 1.1f);
            var tr = sr.transform;
            Tw.Run(sr, 2f, t =>
            {
                tr.position = pos + new Vector2(Mathf.Sin(t * 6f) * 0.04f, 0.43f * t);
                float s = Mathf.Lerp(1.1f, 1.4f, t);
                tr.localScale = new Vector3(s, s * Mathf.Clamp01(t * 3f + 0.1f), 1);
                var c = sr.color; c.a = t < 0.5f ? 1f : 1f - (t - 0.5f) * 2f; sr.color = c;
            }, Ease.Linear, () => Object.Destroy(sr.gameObject));
        }

        public static void Heat(Vector2 pos, int order)
        {
            var sr = Sprite("fx.glow", pos, order, 1.6f);
            sr.color = new Color(1f, 0.75f, 0.35f, 0f);
            var tr = sr.transform;
            Tw.Run(sr, 0.9f, t =>
            {
                tr.position = pos + Vector2.up * 0.3f * t;
                var c = sr.color; c.a = Mathf.Sin(t * Mathf.PI) * 0.65f; sr.color = c;
            }, Ease.Linear, () => Object.Destroy(sr.gameObject));
        }

        /// <summary>Flying sprite along an arc (dough from chef hands, bread from oven to storage, etc.).</summary>
        public static void Fly(string id, Vector2 from, Vector2 to, float dur, float arc, float scale, System.Action done = null, int order = FxOrder + 5)
        {
            var sr = Sprite(id, from, order, scale);
            var tr = sr.transform;
            Tw.Run(sr, dur, t =>
            {
                var p = Vector2.LerpUnclamped(from, to, t) + Vector2.up * arc * Mathf.Sin(t * Mathf.PI);
                tr.position = p;
            }, Ease.InOutQuad, () => { Object.Destroy(sr.gameObject); done?.Invoke(); });
        }
    }
}
