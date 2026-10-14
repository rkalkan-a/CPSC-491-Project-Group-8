using UnityEditor;
using UnityEngine;

namespace DungeonCrawler.EditorTools
{
    // any png dropped under Assets/_Game/Art gets imported as a crisp sprite automatically,
    // so nobody has to fix import settings by hand. pixels per unit depends on the folder
    public class GameArtImporter : AssetPostprocessor
    {
        private const string ArtRoot = "Assets/_Game/Art/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnitFor(assetPath);
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
        }

        private static float PixelsPerUnitFor(string path)
        {
            if (path.Contains("/Placeholders/")) return 32f;
            if (path.Contains("/Maps/"))
            {
                if (path.Contains("maze")) return 20f; // the maze is about 77 units wide so the corridors fit the player
                return path.Contains("dungeon") ? 64f : 48f; // dungeon rooms are 24 units wide, the forest is 32
            }
            if (path.Contains("/Player/")) return 800f;
            if (path.Contains("/Reference/")) return 1000f; // big concept images, not used in game
            if (path.Contains("/Chests/")) return 48f; // hand drawn at map scale, 44px = about 1 unit
            if (path.Contains("/Effects/")) return 48f;
            if (path.Contains("/Enemies/")) return 16f; // small pixel-art creature sprites, 16px = 1 unit
            return 800f;
        }
    }
}
