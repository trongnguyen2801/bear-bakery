// Frame-based animation driven by timings read from the original AnimationClips (see Docs/anim_timings.md).
using System.Collections.Generic;
using UnityEngine;

namespace BB.View
{
    public class FrameSet
    {
        public string name;
        public string[] ids;
        public float[] durs;
        public bool loop = true;
        public float squashY;       // optional per-loop y squash (from Walk.anim: 0.98 at half cycle)
        public FrameSet(string name, string[] ids, float[] durs, bool loop = true, float squashY = 0)
        { this.name = name; this.ids = ids; this.durs = durs; this.loop = loop; this.squashY = squashY; }
        public float Length { get { float s = 0; foreach (var d in durs) s += d; return s; } }
    }

    public static class Frames
    {
        static readonly Dictionary<string, FrameSet> sets = new Dictionary<string, FrameSet>();
        static FrameSet Def(string key, string[] ids, float[] durs, bool loop = true, float squash = 0)
        { var f = new FrameSet(key, ids, durs, loop, squash); sets[key] = f; return f; }

        public static FrameSet Get(string key) => sets.TryGetValue(key, out var f) ? f : null;

        public static void Init()
        {
            if (sets.Count > 0) return;
            // Mr. Bear (chef) — Bear_Idle/Bear_Work/Bear_Fast/Bear_Rest.anim
            Def("chef.idle", new[] { "chef.Idle01", "chef.Idle02" }, new[] { 1f, 1f });
            Def("chef.work", new[] { "chef.Work01", "chef.Work02", "chef.Work03" }, new[] { 0.45f, 0.4333f, 0.45f });
            Def("chef.fast", new[] { "chef.Fast02", "chef.Fast01" }, new[] { 0.3333f, 0.3333f });
            Def("chef.rest", new[] { "chef.Rest01", "chef.Rest02" }, new[] { 0.25f, 0.25f });
            // Amber (cashier) — LesserPanda_*.anim
            Def("cashier.idle", new[] { "cashier.Idle01", "cashier.Idle02" }, new[] { 1f, 1f });
            Def("cashier.work", new[] { "cashier.Work01", "cashier.Work02", "cashier.Work03", "cashier.Work04" }, new[] { 0.3f, 0.2833f, 0.3f, 0.2833f });
            Def("cashier.fast", new[] { "cashier.Fast01", "cashier.Fast02", "cashier.Fast03" }, new[] { 0.1667f, 0.1667f, 0.1667f });
            Def("cashier.rest", new[] { "cashier.Rest01", "cashier.Rest02" }, new[] { 0.25f, 0.25f });
            // Tweetie (promoter) — Sparrow_*.anim (Work3/4 in the clip have no sprite in the atlas: Work01/02 reused)
            Def("promoter.idle", new[] { "promoter.Idle01", "promoter.Idle02" }, new[] { 1f, 1f });
            Def("promoter.work", new[] { "promoter.Work01", "promoter.Work02" }, new[] { 0.5f, 0.5f });
            Def("promoter.fast", new[] { "promoter.Fast01", "promoter.Fast02" }, new[] { 0.25f, 0.25f });
            Def("promoter.rest", new[] { "promoter.Rest01", "promoter.Rest02" }, new[] { 0.25f, 0.25f });
            // delivery dog / raccoon cleaner (Man_Main_Dog_*, Man_Main_Raccoon_* sprites; frame timings approximated: idle 1 s like the staff, faster walk cycle for short trips)
            Def("npc.dog.idle", new[] { "npc.dog.Idle01", "npc.dog.Idle02" }, new[] { 1f, 1f });
            Def("npc.dog.walk", new[] { "npc.dog.Walk01", "npc.dog.Walk02" }, new[] { 0.3f, 0.3f }, true, 0.02f);
            Def("npc.raccoon.idle", new[] { "npc.raccoon.Idle01", "npc.raccoon.Idle02" }, new[] { 1f, 1f });
            Def("npc.raccoon.walk", new[] { "npc.raccoon.Walk01", "npc.raccoon.Walk02" }, new[] { 0.3f, 0.3f }, true, 0.02f);
            Def("npc.raccoon.sweep", new[] { "npc.raccoon.Walk02", "npc.raccoon.Idle01" }, new[] { 0.2f, 0.2f });
            // customers — Walk.anim / Chracters_Pickwalk.anim (0.5s per frame, y squash 0.98)
            foreach (var c in new[] { "Cat", "Giraffe", "Pig", "Rabbit", "Penguin", "Lion" })
            {
                Def($"cust.{c}.idle", new[] { $"cust.{c}.Idle01" }, new[] { 1f });
                Def($"cust.{c}.hold", new[] { $"cust.{c}.Hold01" }, new[] { 1f });
                Def($"cust.{c}.walk", new[] { $"cust.{c}.Walk01", $"cust.{c}.Walk02" }, new[] { 0.5f, 0.5f }, true, 0.02f);
                Def($"cust.{c}.pickwalk", new[] { $"cust.{c}.Pickwalk01", $"cust.{c}.Pickwalk02" }, new[] { 0.5f, 0.5f }, true, 0.02f);
            }
        }
    }

    [RequireComponent(typeof(SpriteRenderer))]
    public class FrameAnim : MonoBehaviour
    {
        public SpriteRenderer sr;
        public FrameSet cur;
        public float speed = 1f;
        float t;
        int idx = -1;
        Vector3 baseScale = Vector3.one;
        public bool squashEnabled = true;

        void Awake() { sr = GetComponent<SpriteRenderer>(); baseScale = transform.localScale; }

        public void SetBaseScale(Vector3 s) { baseScale = s; transform.localScale = s; }

        public void Play(string key, bool restart = false, float phase = 0)
        {
            var f = Frames.Get(key);
            if (f == null) return;
            if (f == cur && !restart) return;
            cur = f;
            t = phase * f.Length;
            idx = -1;
            Apply();
        }

        void Update()
        {
            if (cur == null) return;
            t += Time.deltaTime * speed;
            float len = cur.Length;
            if (cur.loop) { if (t >= len) t %= len; }
            else t = Mathf.Min(t, len - 0.0001f);
            Apply();
        }

        void Apply()
        {
            if (cur == null || sr == null) return;
            float acc = 0; int i = 0;
            for (; i < cur.durs.Length; i++) { acc += cur.durs[i]; if (t < acc) break; }
            if (i >= cur.ids.Length) i = cur.ids.Length - 1;
            if (i != idx) { idx = i; sr.sprite = Art.S(cur.ids[i]); }
            if (squashEnabled && cur.squashY > 0)
            {
                float len = cur.Length; float ph = (t % (len * 0.5f)) / (len * 0.5f);
                float sy = 1f - cur.squashY * Mathf.Sin(ph * Mathf.PI);
                transform.localScale = new Vector3(baseScale.x, baseScale.y * sy, baseScale.z);
            }
            else if (transform.localScale.y != baseScale.y && squashEnabled) transform.localScale = baseScale;
        }
    }
}
