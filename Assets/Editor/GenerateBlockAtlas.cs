#if UNITY_EDITOR
// Assets/Editor/GenerateBlockAtlas.cs
// Generates a 16×28 pixel texture atlas where each row is the colour of one block type.
// Run once from: Deencraft → Generate Block Atlas
// Then assign the saved PNG to your WorldMaterial's Albedo texture.
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DeenCraft.Editor
{
    public static class GenerateBlockAtlas
    {
        private const int PixelsPerRow = 16;
        private const int TotalBlocks  = 28;
        private const string OutputPath = "Assets/Art/BlockAtlas.png";

        // One colour per BlockType index (0 = Air is transparent/unused)
        private static readonly Color32[] BlockColours = new Color32[TotalBlocks]
        {
            new Color32(0,   0,   0,   0),   // 0  Air          — transparent
            new Color32( 93, 138,  60, 255),  // 1  Grass
            new Color32(139,  94,  60, 255),  // 2  Dirt
            new Color32(136, 136, 136, 255),  // 3  Stone
            new Color32(194, 178, 128, 255),  // 4  Sand
            new Color32(238, 238, 255, 255),  // 5  Snow
            new Color32( 58, 107, 200, 255),  // 6  Water
            new Color32(139,  99,  64, 255),  // 7  Wood
            new Color32( 74, 122,  40, 255),  // 8  Leaves
            new Color32(212, 180, 131, 255),  // 9  Mosque (sandstone)
            new Color32(176, 212, 248, 255),  // 10 Ice
            new Color32( 90, 138,  64, 255),  // 11 Moss
            new Color32(212, 168,  64, 255),  // 12 Wheat
            new Color32( 74, 154,  64, 255),  // 13 Cactus
            new Color32(176, 120,  72, 255),  // 14 MudBrick
            new Color32(160, 120,  64, 255),  // 15 PalmWood
            new Color32(106, 138,  72, 255),  // 16 OliveLeaves
            new Color32(212,  64, 128, 255),  // 17 Flower (pink)
            new Color32(160,  88,  40, 255),  // 18 Boat
            new Color32(200, 168,  72, 255),  // 19 Thatch
            new Color32(232, 212, 168, 255),  // 20 Minaret
            new Color32( 64, 168, 112, 255),  // 21 Dome (Islamic green)
            new Color32(184, 168, 136, 255),  // 22 StoneArch
            new Color32(200, 168,   0, 255),  // 23 Crescent (gold)
            new Color32(232, 200,   0, 255),  // 24 StarBlock (yellow)
            new Color32(120,  64,  40, 255),  // 25 AppleWood
            new Color32( 40, 138,  40, 255),  // 26 AppleLeaves
            new Color32( 64, 184, 216, 255),  // 27 WaterSlide
        };

        [MenuItem("Deencraft/Generate Block Atlas")]
        public static void Generate()
        {
            // Ensure output directory exists
            string dir = Path.GetDirectoryName(OutputPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // Texture is PixelsPerRow wide, TotalBlocks tall
            // Each row = one block type; UV Y = blockId / TotalBlocks
            var tex = new Texture2D(PixelsPerRow, TotalBlocks, TextureFormat.RGBA32, mipChain: false);
            tex.filterMode = FilterMode.Point; // pixel-crisp

            for (int blockId = 0; blockId < TotalBlocks; blockId++)
            {
                Color32 col = BlockColours[blockId];
                for (int x = 0; x < PixelsPerRow; x++)
                    tex.SetPixel(x, blockId, col);
            }
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(OutputPath, png);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(OutputPath);

            // Set import settings: no compression, point filter, no mip maps
            var importer = (TextureImporter)AssetImporter.GetAtPath(OutputPath);
            if (importer != null)
            {
                importer.textureType        = TextureImporterType.Default;
                importer.mipmapEnabled      = false;
                importer.filterMode         = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            Debug.Log($"[DeenCraft] Block atlas saved to {OutputPath}. " +
                      "Assign it to WorldMaterial's Albedo slot.");
            EditorUtility.DisplayDialog(
                "Block Atlas Generated!",
                $"Saved to {OutputPath}\n\n" +
                "Next steps:\n" +
                "1. Click your WorldMaterial in the Project panel\n" +
                "2. Drag BlockAtlas.png onto the Albedo (Texture) slot\n" +
                "3. Press Play — blocks will now have colours!",
                "Got it");
        }
    }
}
#endif
