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
        GameObject testCellObject = null;

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
            RawImage titleGround = canvas.transform.Find("MapEditor_TitleGround")?.GetComponent<RawImage>();
            RawImage titleClouds = canvas.transform.Find("MapEditor_TitleClouds")?.GetComponent<RawImage>();
            RawImage titleCharacters = canvas.transform.Find("MapEditor_TitleCharacters")?.GetComponent<RawImage>();
            RawImage logo = canvas.transform.Find("MapEditor_Logo")?.GetComponent<RawImage>();
            Require(background != null && background.texture != null
                && background.texture.name == "PixelChroma_TitleSky_20260803",
                "The latest PixelChroma title sky was not applied.");
            Require(background.GetComponent<AspectRatioFitter>() == null,
                "The title layers still use independent aspect fitters and can drift out of alignment.");
            Require(canvas.GetComponent<MapEditorTitleBackdropLayout>() != null,
                "The PixelChroma title layer layout controller was not created.");
            Require(titleGround != null && titleGround.texture != null
                && titleGround.GetComponent<MapEditorBackgroundRotator>() != null,
                "The rotating PixelChroma BG_2 layer was not created.");
            Require(titleClouds != null && titleClouds.texture != null,
                "The PixelChroma cloud layer was not created.");
            Require(titleCharacters != null && titleCharacters.texture != null,
                "The PixelChroma character layer was not created.");
            Require(titleClouds.transform.GetSiblingIndex() < titleGround.transform.GetSiblingIndex()
                && titleGround.transform.GetSiblingIndex() < titleCharacters.transform.GetSiblingIndex(),
                "The rotating ground is not layered between the clouds and characters.");
            Require(titleGround.rectTransform.anchorMin == new Vector2(0.5f, 0f)
                && titleGround.rectTransform.anchoredPosition.y < 0f,
                "The rotating ground is not positioned below the screen like PixelChroma.");
            Require(titleCharacters.rectTransform.anchorMin == Vector2.zero
                && titleCharacters.rectTransform.anchorMax == Vector2.one,
                "The PixelChroma character layer is not stretched with the title canvas.");
            Require(logo != null && logo.texture != null && logo.gameObject.activeSelf
                && logo.texture.name == "PixelChroma_TitleLogo_20260803",
                "The latest PixelChroma logo was not created.");
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
            Require(toolbar.Find("ValidateButton") == null,
                "Standalone Validate Map button is still visible.");
            Require(toolbar.Find("PNGOutButton") == null,
                "Removed PNG export button is still visible.");
            RequireToolbarButton(toolbar, "WorkshopButton", "창작마당 내보내기", MapEditorToolbarAction.ExportWorkshop);
            RequireToolbarButton(toolbar, "HelpButton", "도움말", MapEditorToolbarAction.PackageGuide);
            Require(toolbar.Find("ClearButton")?.GetComponent<MapEditorToolbarButton>()?.action
                    == MapEditorToolbarAction.Clear,
                "Clear Layer button is not connected to the current-layer clear action.");
            Require(toolbar.Find("ClearAllButton")?.GetComponent<MapEditorToolbarButton>()?.action
                    == MapEditorToolbarAction.ClearAll,
                "Clear All button is missing or connected to the wrong action.");
            Require(toolbar.Find("CharacterTestToolButton") == null, "Removed character test tool is still visible.");

            MapEditorLayerPanelBuilder.Ensure(manager, manager.toolToolbarOffset);
            Transform layerPanel = canvas.transform.Find("MapEditor_LayerPanel");
            Require(layerPanel != null, "Layer panel was not created.");
            Require(layerPanel.Find("LayerGrid/LayerRow_Ground/Layer_Ground")?.GetComponent<MapEditorToolbarButton>()?.action
                    == MapEditorToolbarAction.SetLayer,
                "Ground layer does not have an explicit selection button.");
            Require(layerPanel.Find("LayerManagement/LayerAction_AddLayer_Object") != null,
                "Object-layer add button is missing.");
            Require(layerPanel.Find("LayerManagement/LayerAction_DeleteLayer_Ground") != null,
                "Optional-layer delete button is missing.");

            manager.SetActiveLayer(MapEditorLayerType.Object);
            manager.SetWallTileTool();
            Require(manager.ActiveLayer == MapEditorLayerType.WallCollision,
                "Wall tool did not activate the collision layer.");
            manager.SetSelectionTool();
            Require(manager.ActiveLayer == MapEditorLayerType.Object,
                "Selection tool did not restore the last normal layer after Wall.");
            manager.SetWallTileTool();
            manager.SetBrushTool();
            Require(manager.ActiveLayer == MapEditorLayerType.Object,
                "Brush tool did not restore the last normal paint layer after Wall.");
            manager.SetSpawnTool();
            Require(manager.ActiveLayer == MapEditorLayerType.Spawn,
                "Start Point tool did not activate the Spawn layer.");
            manager.SetSelectionTool();
            Require(manager.ActiveLayer == MapEditorLayerType.Object,
                "Selection tool did not restore the last normal layer after Start Point.");
            manager.SetSpawnTool();
            manager.SetBrushTool();
            Require(manager.ActiveLayer == MapEditorLayerType.Object,
                "Brush tool did not restore the last normal paint layer after Start Point.");

            manager.AddUserLayer(MapEditorLayerType.Object);
            Require(manager.IsLayerEnabled(MapEditorLayerType.ObjectExtra), "Object user layer was not enabled.");
            Require(manager.ActiveLayer == MapEditorLayerType.ObjectExtra, "New Object user layer was not selected.");
            manager.CurrentMapData.SetTileOnLayer(
                0, 0, MapEditorLayerType.ObjectExtra, MapEditorManager.CustomColorTileId,
                Color.magenta, string.Empty, -1, 0, false, false);
            manager.AddUserLayer(MapEditorLayerType.Object);
            Require(manager.IsLayerEnabled(MapEditorLayerType.ObjectExtra2), "Second Object user layer was not enabled.");
            Require(manager.ActiveLayer == MapEditorLayerType.ObjectExtra2, "Second Object user layer was not selected.");
            manager.DeleteActiveUserLayer();
            Require(!manager.IsLayerEnabled(MapEditorLayerType.ObjectExtra2), "Deleted second Object user layer is still enabled.");
            Require(manager.CurrentMapData.GetTile(0, 0, MapEditorLayerType.ObjectExtra) == MapEditorManager.CustomColorTileId,
                "Deleting one user layer damaged another user layer.");
            manager.SetActiveLayer(MapEditorLayerType.ObjectExtra);
            manager.DeleteActiveUserLayer();
            Require(!manager.IsLayerEnabled(MapEditorLayerType.ObjectExtra), "Deleted Object user layer is still enabled.");
            Require(manager.CurrentMapData.GetTile(0, 0, MapEditorLayerType.ObjectExtra) == -1,
                "Deleting a user layer left its tile data behind.");
            Require(manager.ActiveLayer == MapEditorLayerType.Object, "Deleting a user layer did not select its protected base layer.");

            MapEditorMapSizePanelBuilder.Ensure(canvas.transform, manager, manager.toolToolbarOffset);
            Transform panel = canvas.transform.Find("MapEditor_MapSizePanel");
            Require(panel != null, "Map size panel was not created.");
            Require(panel.Find("PresetRow/Preset64 x 64Button") != null, "64 x 64 preset is missing.");
            Require(panel.Find("PresetRow/Preset128 x 128Button") != null, "128 x 128 preset is missing.");
            Require(panel.Find("LargePresetRow/Preset256 x 128Button") == null, "Removed 256 x 128 preset is still visible.");
            Require(panel.Find("LargePresetRow/Preset256 x 256Button") != null, "256 x 256 preset is missing.");
            Require(panel.Find("PresetRow/Preset16Button") == null && panel.Find("PresetRow/Preset32Button") == null, "Legacy map presets are still present.");
            InputField widthInput = panel.Find("WidthControl/WidthInputRow/ValueInput")?.GetComponent<InputField>();
            InputField heightInput = panel.Find("HeightControl/HeightInputRow/ValueInput")?.GetComponent<InputField>();
            MapEditorTabNavigation widthTab = widthInput?.GetComponent<MapEditorTabNavigation>();
            MapEditorTabNavigation heightTab = heightInput?.GetComponent<MapEditorTabNavigation>();
            Require(widthTab != null && widthTab.next == heightInput,
                "Width input does not move to Height input with Tab.");
            Require(heightTab != null && heightTab.next == widthInput,
                "Height input does not cycle back to Width input with Tab.");
            Require(panel.Find("PlayerScaleGuideButton")?.GetComponent<Button>() != null,
                "Player scale comparison button is missing.");

            manager.showPlayerScaleGuide = false;
            manager.TogglePlayerScaleGuide();
            RectTransform playerGuide = gridGenerator.gridParent.Find("MapEditor_PlayerScaleGuide") as RectTransform;
            Require(playerGuide != null && playerGuide.gameObject.activeSelf,
                "Player 1 x 1 tile scale guide was not created.");
            Require(Mathf.Abs(playerGuide.rect.width - gridGenerator.cellSize) < 0.1f
                && Mathf.Abs(playerGuide.rect.height - gridGenerator.cellSize) < 0.1f,
                "Player scale guide is not one map tile.");
            RawImage playerGuideImage = playerGuide.Find("PlayerSprite")?.GetComponent<RawImage>();
            Require(playerGuideImage != null && playerGuideImage.texture != null,
                "Player scale guide does not use the PixelChroma player sprite.");
            Require(playerGuide.GetComponent<MapEditorPlayerScaleGuideDragHandle>() != null,
                "Player scale guide cannot be dragged across the map.");
            manager.SetPlayerScaleGuidePosition(2, 3);
            Require(Vector2.Distance(
                    playerGuide.anchoredPosition,
                    new Vector2(2f * gridGenerator.cellSize, -3f * gridGenerator.cellSize)) < 0.1f,
                "Player scale guide did not move to the requested tile.");
            manager.TogglePlayerScaleGuide();

            testCellObject = new GameObject("Phase4WallOverlayTestCell", typeof(RectTransform), typeof(Image), typeof(GridCell));
            GridCell firstCell = testCellObject.GetComponent<GridCell>();
            testCellObject.SendMessage("Awake");
            firstCell.Init(0, 0);
            manager.CurrentMapData.SetTileOnLayer(
                0, 0, MapEditorLayerType.WallCollision, MapEditorManager.WallTileId,
                Color.black, string.Empty, -1, 0, false, false);
            manager.RefreshCell(firstCell);
            Image wallOverlay = firstCell.transform.Find("WallCollisionOverlay")?.GetComponent<Image>();
            Require(wallOverlay != null && wallOverlay.gameObject.activeSelf && wallOverlay.color.a >= 0.35f,
                "Wall collision overlay is not clearly visible over painted tiles.");

            manager.pixelChromaSpawnPoints.Clear();
            manager.pixelChromaSpawnPoints.Add(new MapEditorSpawnPointData("SpawnPoint_1", 1, 1, "Any"));
            manager.SetActiveLayer(MapEditorLayerType.Spawn);
            manager.ClearActiveLayer();
            Require(manager.pixelChromaSpawnPoints.Count == 0,
                "Clearing the Spawn layer did not remove start points.");
            PixelChromaMapValidationReport missingSpawnReport = MapEditorPixelChromaValidationService.Validate(
                manager.CurrentMapData, manager.pixelChromaSpawnX, manager.pixelChromaSpawnY, manager.pixelChromaSpawnPoints);
            Require(!missingSpawnReport.isValid,
                "Map validation accepted a map without a start point.");
            manager.ExportWorkshopPackage();
            Transform failedValidationModal = canvas.transform.Find("MapEditor_ModalPanel");
            Require(failedValidationModal != null,
                "Workshop export did not show validation before opening a save dialog.");
            Require(failedValidationModal.Find("Panel/FooterActionButton") == null,
                "Failed validation incorrectly allows Workshop export to continue.");

            PixelChromaMapValidationReport passedValidationReport = new PixelChromaMapValidationReport
            {
                isValid = true,
                paintedTileCount = 1,
                groundTileCount = 1,
                spawnPointCount = 1
            };
            passedValidationReport.passedChecks.Add("Smoke validation passed.");
            MapEditorModalPanel.ShowValidation(manager, passedValidationReport, () => { });
            Transform passedValidationModal = canvas.transform.Find("MapEditor_ModalPanel");
            Require(passedValidationModal?.Find("Panel/FooterActionButton")?.GetComponent<Button>() != null,
                "Passed validation does not provide a save-location action.");

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
            Require(picker.transform.Find("PngPaletteLabel")?.GetComponent<Text>()?.text.StartsWith("팔레트 ") == true, "Palette label was not localized.");
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

            if (testCellObject != null)
            {
                UnityEngine.Object.DestroyImmediate(testCellObject);
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
