#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class MapEditorTilesetImportSmokeTest
{
    [MenuItem("Tools/Map Editor/Run Tileset Import Smoke Test")]
    public static void Run()
    {
        string sourcePath = Path.Combine(Application.temporaryCachePath, "mapeditor_tileset_import_test.png");
        string exportPath = Path.Combine(Application.temporaryCachePath, "mapeditor_animated_tile_export_test.json");
        Texture2D source = CreateSourceTexture();
        File.WriteAllBytes(sourcePath, source.EncodeToPNG());
        Object.DestroyImmediate(source);

        MapEditorTilesetLibraryService library = new MapEditorTilesetLibraryService();
        MapEditorTilesetDefinition definition = null;

        try
        {
            Require(library.Import(
                sourcePath,
                "Smoke Test Tileset",
                16,
                16,
                1,
                1,
                MapEditorLayerType.Ground,
                false,
                out definition,
                out string error), error);

            Require(definition.columns == 2 && definition.rows == 1, "Tileset dimensions were not detected correctly.");
            Require(definition.atlasGridSize == 16, "Tileset atlas grid was not normalized to 16x16.");
            Require(File.Exists(definition.atlasPath), "Normalized tileset atlas was not created.");
            Require(library.ConfigureAnimation(
                    definition.id,
                    "Water",
                    0,
                    2,
                    6f,
                    true,
                    out string animationError),
                animationError);
            Require(definition.animations != null && definition.animations.Length == 1,
                "Animated tileset definition was not created.");
            Require(definition.animations[0].GetFrameTileId(0) == 15 * definition.atlasGridSize
                && definition.animations[0].GetFrameTileId(1) == 15 * definition.atlasGridSize + 1,
                "Source animation frames were not mapped to normalized atlas tiles.");

            MapEditorPngTilesetService tiles = new MapEditorPngTilesetService();
            int redIndex = MapEditorPngTilesetService.EncodePaletteTileIndex(definition.atlasGridSize, 15 * definition.atlasGridSize);
            int greenIndex = MapEditorPngTilesetService.EncodePaletteTileIndex(definition.atlasGridSize, 15 * definition.atlasGridSize + 1);
            Sprite red = tiles.GetTileSprite(definition.atlasPath, redIndex);
            Sprite green = tiles.GetTileSprite(definition.atlasPath, greenIndex);

            Require(red != null && green != null, "Imported tileset sprites were not created.");
            Require(IsApproximately(red.texture.GetPixel(4, red.texture.height - 8), Color.red), "First imported tile color changed.");
            Require(IsApproximately(green.texture.GetPixel(24, green.texture.height - 8), Color.green), "Second imported tile color changed.");
            Require(red.texture.GetPixel(8, red.texture.height - 8).a < 0.01f, "Transparent tileset pixels became opaque during import.");

            MapSaveData saveData = new MapSaveData(1, 1)
            {
                importedTilesets = new[] { definition }
            };
            MapSaveData restored = JsonUtility.FromJson<MapSaveData>(JsonUtility.ToJson(saveData));
            Require(restored.importedTilesets != null && restored.importedTilesets.Length == 1, "Tileset definition was not saved in editable map data.");
            Require(restored.importedTilesets[0].tileWidth == 16 && restored.importedTilesets[0].spacing == 1, "Tileset slicing settings changed during save/load.");
            Require(restored.importedTilesets[0].animations != null
                && restored.importedTilesets[0].animations.Length == 1
                && Mathf.Abs(restored.importedTilesets[0].animations[0].framesPerSecond - 6f) < 0.01f,
                "Animation settings changed during save/load.");

            MapData mapData = new MapData(1, 1);
            mapData.SetTileOnLayer(0, 0, MapEditorLayerType.Ground, MapEditorManager.CustomImageTileId,
                Color.white, definition.atlasPath, redIndex, 0, false, false);
            MapEditorSpawnPointData[] spawns = { new MapEditorSpawnPointData("SpawnPoint_1", 0, 0, "Any") };
            Require(new MapEditorPixelChromaExportService().Export(mapData, exportPath, "animated_smoke", 16, 0, 0, spawns),
                "Animated map export failed.");
            PixelChromaMapExportData exported = JsonUtility.FromJson<PixelChromaMapExportData>(File.ReadAllText(exportPath));
            PixelChromaTileExportData exportedTile = exported.layers[0].tiles[0];
            Require(exportedTile.kind == PixelChromaExportContract.AnimatedPixelTileKind,
                "Animated tile was exported as a static tile.");
            Require(exportedTile.animationFrames != null && exportedTile.animationFrames.Length == 2,
                "Animated tile frames were not embedded in the game map.");
            Require(Mathf.Abs(exportedTile.animationFps - 6f) < 0.01f && exportedTile.animationLoop,
                "Animated tile playback settings were not exported.");

            Debug.Log("Tileset import smoke test passed: margin, spacing, atlas slicing, and sprite colors are valid.");
        }
        finally
        {
            if (definition != null)
            {
                library.Remove(definition.id);
                if (File.Exists(definition.atlasPath))
                {
                    File.Delete(definition.atlasPath);
                }
            }

            if (File.Exists(sourcePath))
            {
                File.Delete(sourcePath);
            }

            if (File.Exists(exportPath))
            {
                File.Delete(exportPath);
            }
        }
    }

    private static Texture2D CreateSourceTexture()
    {
        Texture2D texture = new Texture2D(35, 18, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[texture.width * texture.height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }
        texture.SetPixels(pixels);

        Fill(texture, 1, 1, 16, 16, Color.red);
        Fill(texture, 18, 1, 16, 16, Color.green);
        texture.SetPixel(9, 9, Color.clear);
        texture.Apply(false, false);
        return texture;
    }

    private static void Fill(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        texture.SetPixels(x, y, width, height, pixels);
    }

    private static bool IsApproximately(Color actual, Color expected)
    {
        return Mathf.Abs(actual.r - expected.r) < 0.02f
            && Mathf.Abs(actual.g - expected.g) < 0.02f
            && Mathf.Abs(actual.b - expected.b) < 0.02f
            && Mathf.Abs(actual.a - expected.a) < 0.02f;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new System.InvalidOperationException(message);
        }
    }
}
#endif
