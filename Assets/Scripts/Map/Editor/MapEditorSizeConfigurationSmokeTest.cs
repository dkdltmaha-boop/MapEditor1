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
            GridGenerator gridGenerator = manager.GetComponent<GridGenerator>();
            Require(gridGenerator != null && gridGenerator.gridParent != null, "SampleScene is missing the map grid.");
            gridGenerator.ApplyLayoutSize();
            Require(
                gridGenerator.gridParent.GetComponent<RectMask2D>() != null,
                "Map grid content is not clipped to the actual map bounds.");
            Require(
                typeof(MaskableGraphic).IsAssignableFrom(typeof(MapEditorGridLineOverlay)),
                "Map grid line overlay does not support UI masking.");

            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            Require(canvas != null, "SampleScene is missing Canvas.");
            MapEditorSceneUiBuilder.EnsureBackground();
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            Require(scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize,
                "Desktop editor UI is being resampled instead of rendered at a fixed pixel size.");
            Require(Mathf.Abs(scaler.scaleFactor - 1f) < 0.001f,
                "Canvas scale factor is not using one-to-one pixel rendering.");
            Require(canvas.pixelPerfect,
                "Canvas pixel snapping is disabled, which can blur text at fractional positions.");
            RawImage background = canvas.transform.Find("MapEditor_Background")?.GetComponent<RawImage>();
            RawImage logo = canvas.transform.Find("MapEditor_Logo")?.GetComponent<RawImage>();
            Require(background != null && background.texture != null
                && background.texture.name == "MapEditor_Title_20260727",
                "The new PixelChroma title background was not applied.");
            Require(background.GetComponent<AspectRatioFitter>()?.aspectMode == AspectRatioFitter.AspectMode.EnvelopeParent,
                "The title background is not preserving its aspect ratio.");
            Require(logo != null && logo.texture != null && logo.gameObject.activeSelf,
                "The PixelChroma logo was not created.");
            Button quitButton = canvas.transform.Find("MapEditor_QuitButton")?.GetComponent<Button>();
            Require(quitButton != null && quitButton.interactable,
                "The top-right application quit button was not created.");
            RectTransform quitRect = quitButton.GetComponent<RectTransform>();
            Require(quitRect.anchorMin == Vector2.one && quitRect.anchorMax == Vector2.one,
                "The quit button is not anchored to the top-right corner.");
            Require(quitButton.transform.GetSiblingIndex() == canvas.transform.childCount - 1,
                "The quit button is not rendered above the other UI panels.");
            MapEditorToolbarBuilder.Ensure(manager, manager.toolToolbarOffset, Array.Empty<string>());
            MapEditorSceneUiBuilder.BringQuitButtonToFront();
            Transform toolbar = canvas.transform.Find("MapEditor_Toolbar");
            Require(toolbar != null, "Tool toolbar was not created.");
            RequireToolbarButton(toolbar, "BrushToolButton", "브러시", MapEditorToolbarAction.Brush);
            RequireToolbarButton(toolbar, "WallToolButton", "벽", MapEditorToolbarAction.Wall);
            RequireToolbarButton(toolbar, "TilesetsButton", "타일셋", MapEditorToolbarAction.OpenTilesetLibrary);
            RequireToolbarButton(toolbar, "ValidateButton", "맵 검사", MapEditorToolbarAction.ValidateMap);
            RequireToolbarButton(toolbar, "WorkshopButton", "창작마당 내보내기", MapEditorToolbarAction.ExportWorkshop);
            RequireToolbarButton(toolbar, "HelpButton", "도움말", MapEditorToolbarAction.PackageGuide);
            Require(toolbar.Find("CharacterTestToolButton") == null, "Removed character test tool is still visible.");

            MapEditorMapSizePanelBuilder.Ensure(canvas.transform, manager, manager.toolToolbarOffset);
            Transform panel = canvas.transform.Find("MapEditor_MapSizePanel");
            Require(panel != null, "Map size panel was not created.");
            Require(panel.Find("PresetRow/Preset64 x 64Button") != null, "64 x 64 preset is missing.");
            Require(panel.Find("PresetRow/Preset128 x 128Button") != null, "128 x 128 preset is missing.");
            Require(panel.Find("LargePresetRow/Preset256 x 128Button") != null, "256 x 128 preset is missing.");
            Require(panel.Find("LargePresetRow/Preset256 x 256Button") != null, "256 x 256 preset is missing.");
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
            Require(picker.transform.Find("Title")?.GetComponent<Text>()?.text.StartsWith("색상") == true, "Color picker title was not localized.");
            Require(picker.transform.Find("WallTileSelector/WallTileLabel")?.GetComponent<Text>()?.text == "벽 타일", "Wall tile label was not localized.");
            Require(picker.transform.Find("ExportCellSizeSelector/DotSizeLabel")?.GetComponent<Text>()?.text == "그리기 크기", "Paint size label was not localized.");
            Require(picker.transform.Find("PngPaletteLabel")?.GetComponent<Text>()?.text.StartsWith("PNG 팔레트 ") == true, "PNG palette label was not localized.");
            Require(picker.transform.Find("HueBar") != null, "PixelChroma-style hue bar is missing.");
            Require(picker.transform.Find("HueWheel") == null, "Legacy circular hue wheel still exists.");

            RectTransform svRect = picker.transform.Find("SaturationValueSquare") as RectTransform;
            Require(svRect != null && Mathf.Abs(svRect.rect.width - 196f) < 0.1f && Mathf.Abs(svRect.rect.height - 140f) < 0.1f,
                "PixelChroma-style saturation/value area has the wrong size.");

            picker.SetHueFromLocalPoint(new Vector2(-98f, 0f));
            picker.SetSaturationValueFromLocalPoint(new Vector2(98f, 70f));
            Require(ColorDistance(manager.selectedColor, Color.red) < 0.01f, "HSV pointer mapping did not select red.");

            RectTransform hexRow = picker.transform.Find("HexColorInput") as RectTransform;
            InputField hexInput = picker.transform.Find("HexColorInput/Input")?.GetComponent<InputField>();
            Require(hexRow != null && Mathf.Abs(hexRow.anchoredPosition.y + 228f) < 0.1f,
                "HEX color input is not positioned below the color controls.");
            Require(hexInput != null, "HEX color input field is missing.");
            hexInput.text = "46F1F1";
            hexInput.onEndEdit.Invoke(hexInput.text);
            Require(ColorDistance(manager.selectedColor, new Color32(0x46, 0xF1, 0xF1, 0xFF)) < 0.02f,
                "HEX color search did not update the selected color.");

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

    private static void RequireToolbarButton(Transform toolbar, string objectName, string label, MapEditorToolbarAction action)
    {
        Transform button = toolbar.Find(objectName);
        Require(button != null, objectName + " is missing.");
        Require(button.Find("Text")?.GetComponent<Text>()?.text == label, objectName + " label was not localized.");
        Require(button.GetComponent<MapEditorToolbarButton>()?.action == action, objectName + " action mapping changed.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static float ColorDistance(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
    }
}
