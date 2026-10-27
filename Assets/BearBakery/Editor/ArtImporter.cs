// Applies the ORIGINAL sprite metadata (pivot, 9-slice border, PPU) to derived PNG copies.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BB.EditorTools
{
    public class ArtImporter : AssetPostprocessor
    {
        [Serializable] class Item { public string path; public float[] pivot; public float[] border; public float ppu; }
        [Serializable] class List { public Item[] items; }
        static Dictionary<string, Item> map;

        static Dictionary<string, Item> Map()
        {
            if (map != null) return map;
            map = new Dictionary<string, Item>();
            var p = "Assets/BearBakery/Editor/import_settings.json";
            if (File.Exists(p))
                foreach (var it in JsonUtility.FromJson<List>(File.ReadAllText(p)).items) map[it.path] = it;
            return map;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/BearBakery/Resources/Art/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.npotScale = TextureImporterNPOTScale.None;
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            s.spriteGenerateFallbackPhysicsShape = false;
            if (Map().TryGetValue(assetPath, out var it))
            {
                s.spritePixelsPerUnit = it.ppu > 0 ? it.ppu : 100;
                s.spriteAlignment = (int)SpriteAlignment.Custom;
                s.spritePivot = new Vector2(it.pivot[0], it.pivot[1]);
                if (it.border != null && it.border.Length == 4) s.spriteBorder = new Vector4(it.border[0], it.border[1], it.border[2], it.border[3]);
            }
            else s.spritePixelsPerUnit = 100;
            ti.SetTextureSettings(s);
        }
    }
}
