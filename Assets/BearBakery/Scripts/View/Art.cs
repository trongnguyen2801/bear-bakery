// Semantic sprite registry: gameplay/view code only uses ids like "dough.3"; the manifest maps them to real files.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BB.View
{
    public static class Art
    {
        [Serializable] class Item { public string id; public string res; public int w, h; public float[] pivot; public float[] border; }
        [Serializable] class Manifest { public Item[] items; }

        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, string> paths = new Dictionary<string, string>();
        public static readonly List<string> Missing = new List<string>();
        public static Font Vi, Num;
        static Sprite placeholder;
        static bool inited;

        public static void Init()
        {
            if (inited) return;
            inited = true;
            var ta = Resources.Load<TextAsset>("asset_manifest");
            var m = JsonUtility.FromJson<Manifest>(ta.text);
            foreach (var it in m.items) paths[it.id] = it.res;
            Vi = Resources.Load<Font>("Fonts/LiberationSans");
            Num = Resources.Load<Font>("Fonts/KATURI");
            if (Vi == null) Vi = Font.CreateDynamicFontFromOSFont("Arial", 16);
            if (Num == null) Num = Vi;
        }

        public static int Count => paths.Count;
        public static IEnumerable<string> Ids => paths.Keys;
        public static string PathOf(string id) => paths.TryGetValue(id, out var p) ? p : null;

        public static Sprite S(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (sprites.TryGetValue(id, out var s)) return s;
            Sprite sp = null;
            if (paths.TryGetValue(id, out var res)) sp = Resources.Load<Sprite>(res);
            if (sp == null)
            {
                if (!Missing.Contains(id)) { Missing.Add(id); Debug.LogWarning("[Art] missing sprite id " + id); }
                sp = Placeholder();
            }
            sprites[id] = sp;
            return sp;
        }

        public static bool Has(string id) => paths.ContainsKey(id);

        static Sprite Placeholder()
        {
            if (placeholder != null) return placeholder;
            var t = new Texture2D(8, 8);
            var px = new Color32[64];
            for (int i = 0; i < 64; i++) px[i] = new Color32(255, 0, 255, 255);
            t.SetPixels32(px); t.Apply();
            placeholder = Sprite.Create(t, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8);
            return placeholder;
        }

        public static string Dough(int lv) => "dough." + lv;
        public static string Bread(int lv) => "bread." + lv;
        public static string BreadBig(int lv) => "breadBig." + lv;
    }
}
