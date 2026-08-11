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
        string saveFileName = "mapeditor_animated_tile_save_test_" + System.Guid.NewGuid().ToString("N") + ".json";
        string savePath = Path.Combine(Application.persistentDataPath, saveFileName);
        string packagePath = Path.Combine(Application.temporaryCachePath, "mapeditor_animated_workshop_" + System.Guid.NewGuid().ToString("N"));
        string restoredAtlasPath = string.Empty;
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

            Require(definition.columns == 4 && definition.rows == 1, "Tileset dimensions were not detected correctly.");
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
            MapEditorTilesetAnimationDefinition waterAnimation = definition.animations[0];

            Require(library.AddAnimation(
                    definition.id,
                    "Lava",
                    new[] { 2, 3 },
                    10f,
                    true,
                    out MapEditorTilesetAnimationDefinition lavaAnimation,
                    out string addAnimationError),
                addAnimationError);
            Require(definition.animations.Length == 2 && lavaAnimation.id != waterAnimation.id,
                "A second animation was not added to the tileset.");
            Require(!library.AddAnimation(
                    definition.id,
                    "Overlap",
                    new[] { 1, 2 },
                    8f,
                    true,
                    out _,
                    out _),
                "Overlapping animation frames were accepted.");
            Require(library.UpdateAnimation(
                    definition.id,
                    lavaAnimation.id,
                    "Lava Reverse",
                    new[] { 3, 2 },
                    12f,
                    false,
                    out string updateAnimationError),
                updateAnimationError);
            lavaAnimation = library.FindAnimation(definition.id, lavaAnimation.id);
            Require(lavaAnimation != null
                && lavaAnimation.displayName == "Lava Reverse"
                && lavaAnimation.GetFrameTileId(0) == 15 * definition.atlasGridSize + 3
                && !lavaAnimation.loop,
                "Animation edits were not applied in frame order.");

            int lavaPaletteIndex = MapEditorPngTilesetService.EncodePaletteTileIndex(
                definition.atlasGridSize,
                15 * definition.atlasGridSize + 3);
            Require(MapEditorTilesetLibraryService.TryGetAnimation(
                    definition.atlasPath,
                    lavaPaletteIndex,
                    out _,
                    out MapEditorTilesetAnimationDefinition foundLava)
                && foundLava.id == lavaAnimation.id,
                "The second animation could not be resolved from its palette tile.");

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
                && restored.importedTilesets[0].animations.Length == 2
                && Mathf.Abs(restored.importedTilesets[0].animations[0].framesPerSecond - 6f) < 0.01f,
                "Animation settings changed during save/load.");
            Require(restored.importedTilesets[0].animations[1].displayName == "Lava Reverse"
                && restored.importedTilesets[0].animations[1].frameTileIds.Length == 2
                && restored.importedTilesets[0].animations[1].frameTileIds[0] == 15 * definition.atlasGridSize + 3,
                "Multiple animations or their frame order changed during save/load.");

            restored.importedTilesets[0].animations[0].id = string.Empty;
            restored.importedTilesets[0].animations[0].frameTileIds = null;
            library.ReplaceDefinitions(restored.importedTilesets);
            definition = library.FindById(definition.id);
            waterAnimation = definition.animations[0];
            lavaAnimation = definition.animations[1];
            Require(!string.IsNullOrEmpty(waterAnimation.id)
                && waterAnimation.frameTileIds.Length == 2
                && waterAnimation.GetFrameTileId(0) == 15 * definition.atlasGridSize,
                "Legacy animation data was not upgraded without losing its frames.");

            MapData mapData = new MapData(1, 1);
            mapData.SetTileOnLayer(0, 0, MapEditorLayerType.Ground, MapEditorManager.CustomImageTileId,
                Color.white, definition.atlasPath, redIndex, 0, false, false);
            mapData.SetTileOnLayer(0, 0, MapEditorLayerType.GroundExtra, MapEditorManager.CustomColorTileId,
                new Color(0f, 0f, 1f, 0.25f), string.Empty, -1, 0, false, false);
            MapEditorSpawnPointData[] spawns = { new MapEditorSpawnPointData("SpawnPoint_1", 0, 0, "Any") };
            PixelChromaMapValidationReport validation = MapEditorPixelChromaValidationService.Validate(mapData, 0, 0, spawns);
            Require(validation.isValid
                && validation.animatedTileCount == 1
                && validation.animationDefinitionCount == 1
                && validation.invalidAnimationCount == 0,
                "Map validation did not recognize the animated tile.");

            int originalSecondFrame = waterAnimation.frameTileIds[1];
            waterAnimation.frameTileIds[1] = definition.atlasGridSize * definition.atlasGridSize;
            library.ReplaceDefinitions(new[] { definition });
            PixelChromaMapValidationReport invalidAnimationReport = MapEditorPixelChromaValidationService.Validate(mapData, 0, 0, spawns);
            Require(!invalidAnimationReport.isValid && invalidAnimationReport.invalidAnimationCount == 1,
                "Map validation accepted an animation frame outside the tileset.");
            waterAnimation.frameTileIds[1] = originalSecondFrame;
            library.ReplaceDefinitions(new[] { definition });

            Require(new MapEditorPixelChromaExportService().Export(mapData, exportPath, "animated_smoke", 16, 0, 0, spawns),
                "Animated map export failed.");
            PixelChromaMapExportData exported = JsonUtility.FromJson<PixelChromaMapExportData>(File.ReadAllText(exportPath));
            PixelChromaTileExportData exportedTile = exported.layers[0].tiles[0];
            Require(exportedTile.kind == PixelChromaExportContract.AnimatedPixelTileKind,
                "Animated tile became static when it overlapped another editable layer.");
            Require(exportedTile.animationFrames != null && exportedTile.animationFrames.Length == 2,
                "Animated tile frames were not embedded in the game map.");
            Require(Mathf.Abs(exportedTile.animationFps - 6f) < 0.01f && exportedTile.animationLoop,
                "Animated tile playback settings were not exported.");

            MapEditorWorkshopExportService workshopExport = new MapEditorWorkshopExportService(tiles.GetTileSprite);
            Require(workshopExport.Export(
                    mapData,
                    packagePath,
                    "animated_smoke",
                    "Animated Smoke",
                    "MapEditor",
                    "Animated workshop export test.",
                    "1.0.0",
                    "Private",
                    "Map,Animation",
                    0,
                    0,
                    spawns,
                    16,
                    false),
                "Workshop package with an animated tile failed validation.");
            PixelChromaWorkshopPackageReport packageReport = JsonUtility.FromJson<PixelChromaWorkshopPackageReport>(
                File.ReadAllText(Path.Combine(packagePath, "package_report.json")));
            Require(packageReport.isValid
                && packageReport.animatedTileCount == 1
                && packageReport.animationDefinitionCount == 1
                && packageReport.invalidAnimationCount == 0,
                "Workshop package report lost animated tile validation data.");

            MapEditorMapSaveService saveService = new MapEditorMapSaveService(MapEditorManager.MaxMapSize);
            saveService.SetImportedTilesets(library.GetDefinitionsForSave());
            Require(saveService.Save(mapData, definition.atlasPath, 0, 0, spawns, saveFileName),
                "Editable map with an animated tile could not be saved.");
            File.Delete(definition.atlasPath);
            Require(saveService.Load(saveFileName, out MapSaveData loadedSave, out _),
                "Editable map could not restore its embedded animated tileset.");
            Require(loadedSave.importedTilesets != null && loadedSave.importedTilesets.Length == 1,
                "Animated tileset definition disappeared while loading the editable map.");
            restoredAtlasPath = loadedSave.importedTilesets[0].atlasPath;
            Require(File.Exists(restoredAtlasPath),
                "Embedded animated tileset PNG was not restored after the original PNG was removed.");

            library.ReplaceDefinitions(loadedSave.importedTilesets);
            definition = library.FindById(definition.id);
            waterAnimation = definition.animations[0];
            lavaAnimation = definition.animations[1];
            MapData loadedMap = MapData.FromSaveData(loadedSave);
            string loadedImagePath = loadedMap.GetImagePath(0, 0, MapEditorLayerType.Ground);
            int loadedImageIndex = loadedMap.GetImageIndex(0, 0, MapEditorLayerType.Ground);
            Require(string.Equals(loadedImagePath, restoredAtlasPath, System.StringComparison.OrdinalIgnoreCase)
                && MapEditorTilesetLibraryService.TryGetAnimation(loadedImagePath, loadedImageIndex, out _, out _),
                "Loaded map tile no longer resolves to its restored animation.");

            Require(library.RemoveAnimation(definition.id, lavaAnimation.id)
                && definition.animations.Length == 1
                && library.FindAnimation(definition.id, lavaAnimation.id) == null,
                "Animation removal did not update the tileset library.");

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

            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }

            if (!string.IsNullOrEmpty(restoredAtlasPath) && File.Exists(restoredAtlasPath))
            {
                File.Delete(restoredAtlasPath);
            }

            if (Directory.Exists(packagePath))
            {
                Directory.Delete(packagePath, true);
            }
        }
    }

    private static Texture2D CreateSourceTexture()
    {
        Texture2D texture = new Texture2D(69, 18, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[texture.width * texture.height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }
        texture.SetPixels(pixels);

        Fill(texture, 1, 1, 16, 16, Color.red);
        Fill(texture, 18, 1, 16, 16, Color.green);
        Fill(texture, 35, 1, 16, 16, Color.blue);
        Fill(texture, 52, 1, 16, 16, Color.yellow);
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
