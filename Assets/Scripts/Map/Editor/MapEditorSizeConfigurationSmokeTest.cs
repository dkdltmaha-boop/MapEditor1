using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MapEditorSizeConfigurationSmokeTest
{
    [MenuItem("Tools/MapEditor/Run Size Configuration Smoke Test")]
    public static void Run()
    {
        string texturePath = Path.Combine(Path.GetTempPath(), "MapEditorPaletteGridSmoke.png");
        Texture2D texture = null;

        try
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            MapEditorManager manager = UnityEngine.Object.FindFirstObjectByType<MapEditorManager>();
            Require(manager != null, "SampleScene is missing MapEditorManager.");
            Require(manager.mapWidth == 64 && manager.mapHeight == 64, "Default map size is not 64 x 64.");

            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            Require(canvas != null, "SampleScene is missing Canvas.");
            MapEditorMapSizePanelBuilder.Ensure(canvas.transform, manager, manager.toolToolbarOffset);
            Transform panel = canvas.transform.Find("MapEditor_MapSizePanel");
            Require(panel != null, "Map size panel was not created.");
            Require(panel.Find("PresetRow/Preset64 x 64Button") != null, "64 x 64 preset is missing.");
            Require(panel.Find("PresetRow/Preset128 x 128Button") != null, "128 x 128 preset is missing.");
            Require(panel.Find("PresetRow/Preset16Button") == null && panel.Find("PresetRow/Preset32Button") == null, "Legacy map presets are still present.");

            foreach (int gridSize in MapEditorManager.PngPaletteGridSizeOptions)
            {
                manager.SetPngPaletteGridSize(gridSize);
                Require(manager.GetPngPaletteGridSize() == gridSize, "PNG palette grid size did not change to " + gridSize + ".");
            }

            texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            texture.SetPixels(CreateTestPixels(texture.width * texture.height));
            texture.Apply(false, false);
            File.WriteAllBytes(texturePath, texture.EncodeToPNG());

            var tilesets = new MapEditorPngTilesetService();
            foreach (int gridSize in MapEditorManager.PngPaletteGridSizeOptions)
            {
                int encodedIndex = MapEditorPngTilesetService.EncodePaletteTileIndex(gridSize, gridSize * gridSize - 1);
                Sprite sprite = tilesets.GetTileSprite(texturePath, encodedIndex);
                int expectedSize = 128 / gridSize;
                Require(sprite != null, "PNG tile was not restored for " + gridSize + " x " + gridSize + ".");
                Require(Mathf.RoundToInt(sprite.rect.width) == expectedSize && Mathf.RoundToInt(sprite.rect.height) == expectedSize,
                    "PNG tile dimensions are wrong for " + gridSize + " x " + gridSize + ".");
            }

            manager.SetPngPaletteGridSize(128);
            ColorWheelPickerWindow picker = ColorWheelPickerWindow.Create(manager, manager.colorPaletteOffset);
            Require(picker != null, "Color picker window was not created.");
            picker.SetPngPalette(texture, texturePath);

            Transform selector = picker.transform.Find("PngPaletteSizeSelector");
            Require(selector != null && selector.childCount == 4, "PNG palette size selector does not contain four options.");
            Transform paletteContent = picker.transform.Find("ColorPicker_PngTilesetViewport/ColorPicker_PngTilesetGrid");
            Require(paletteContent != null && paletteContent.childCount < 10, "128 x 128 palette created excessive UI objects.");
            RectTransform paletteViewport = picker.transform.Find("ColorPicker_PngTilesetViewport") as RectTransform;
            RectTransform paletteContentRect = paletteContent as RectTransform;
            GridLayoutGroup paletteGrid = paletteContent.GetComponent<GridLayoutGroup>();
            Require(paletteViewport != null && paletteContentRect != null, "PNG palette viewport layout is missing.");
            Require(Mathf.Abs(paletteViewport.rect.width - 176f) < 0.1f && Mathf.Abs(paletteViewport.rect.height - 176f) < 0.1f,
                "PNG palette viewport is not using the full display size.");
            Require(paletteGrid != null && paletteGrid.padding.horizontal == 0 && paletteGrid.padding.vertical == 0,
                "PNG palette still has an outer border padding.");
            Require(Mathf.Abs(paletteViewport.rect.width - paletteContentRect.rect.width) < 0.1f
                && Mathf.Abs(paletteViewport.rect.height - paletteContentRect.rect.height) < 0.1f,
                "PNG palette does not fit its viewport after loading.");

            Debug.Log("MapEditor size configuration smoke test passed.");
        }
        finally
        {
            if (texture != null)
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }

            if (File.Exists(texturePath))
            {
                File.Delete(texturePath);
            }
        }
    }

    private static Color[] CreateTestPixels(int count)
    {
        var pixels = new Color[count];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.HSVToRGB((i % 128) / 128f, 1f, 1f);
        }

        return pixels;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
