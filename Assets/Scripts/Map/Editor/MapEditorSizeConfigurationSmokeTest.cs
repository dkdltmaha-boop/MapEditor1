using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MapEditorSizeConfigurationSmokeTest
{
    [MenuItem("Tools/MapEditor/Run Size Configuration Smoke Test")]
    public static void Run()
    {
        string texturePath = Path.Combine(Path.GetTempPath(), "MapEditorPaletteGridSmoke.png");
        Texture2D texture = null;
        GameObject testCellObject = null;
        GameObject collisionLineRoot = null;
        GameObject nestedCanvasObject = null;

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
            RectTransform toolbarRect = toolbar.GetComponent<RectTransform>();
            Require(toolbarRect != null && toolbarRect.anchoredPosition.x <= -42f,
                "Tool toolbar overlaps the fixed top-right quit-button area.");

            nestedCanvasObject = new GameObject("MapEditor_PlaytestHud_Smoke", typeof(RectTransform), typeof(Canvas));
            nestedCanvasObject.transform.SetParent(canvas.transform, false);
            nestedCanvasObject.GetComponent<Canvas>().overrideSorting = true;
            Require(MapEditorSceneUiBuilder.FindEditorCanvas() == canvas,
                "A nested playtest canvas replaced the editor's root canvas.");
            MapEditorToolbarBuilder.Ensure(manager, manager.toolToolbarOffset, Array.Empty<string>());
            Require(nestedCanvasObject.transform.Find("MapEditor_Toolbar") == null,
                "The tool toolbar was duplicated inside the playtest HUD canvas.");
            UnityEngine.Object.DestroyImmediate(nestedCanvasObject);
            nestedCanvasObject = null;

            Transform recentScrollTransform = FindDescendant(toolbar, "RecentResourceScroll");
            ScrollRect recentScroll = recentScrollTransform == null
                ? null
                : recentScrollTransform.GetComponent<ScrollRect>();
            Require(recentScroll != null
                && recentScroll.vertical
                && !recentScroll.horizontal
                && recentScroll.verticalScrollbar != null
                && recentScroll.verticalScrollbarVisibility == ScrollRect.ScrollbarVisibility.Permanent,
                "Recent Resources is missing its vertical scroll bar.");
            Require(recentScroll.viewport != null
                && recentScroll.viewport.GetComponent<RectMask2D>() != null
                && recentScroll.content != null
                && recentScroll.content.name == "RecentResourceContent",
                "Recent Resources scroll viewport or masked content is not configured.");
            RequireToolbarButton(toolbar, "BrushToolButton", "브러시 · 바닥", MapEditorToolbarAction.Brush);
            RequireToolbarButton(toolbar, "BrushRoleMenuButton", "▼", MapEditorToolbarAction.ToggleBrushRoleMenu);
            Require(FindDescendant(toolbar, "WallToolButton") == null,
                "Legacy standalone Wall tool is still visible.");
            RequireToolbarButton(toolbar, "TilesetsButton", "타일셋", MapEditorToolbarAction.OpenTilesetLibrary);
            RequireToolbarButton(toolbar, "AnimationTileButton", "애니메이션 타일", MapEditorToolbarAction.OpenAnimationTileEditor);
            Require(FindDescendant(toolbar, "AudioSettingsButton") == null,
                "Removed audio settings button is still visible.");
            RequireToolbarButton(toolbar, "MovingRegionButton", "이동 경로", MapEditorToolbarAction.MovingRegion);
            RequireToolbarButton(toolbar, "PlaytestButton", "맵 테스트", MapEditorToolbarAction.Playtest);
            MapData originalMapData = manager.CurrentMapData;
            MapData playtestMapData = new MapData(2, 2);
            playtestMapData.SetTileOnLayer(
                0, 0, MapEditorLayerType.Ground, MapEditorManager.CustomColorTileId,
                Color.white, string.Empty, -1, 0, false, false);
            manager.SetCurrentMapDataForLoad(playtestMapData);
            Require(manager.HasPlaytestGroundAt(0, 0),
                "Playtest does not recognize a painted Ground cell as walkable ground.");
            Require(!manager.HasPlaytestCollisionAt(0, 0),
                "Playtest reports collision on an empty collision layer.");
            playtestMapData.SetTileOnLayer(
                0, 0, MapEditorLayerType.WallCollision, MapEditorManager.WallTileId,
                Color.clear, string.Empty, -1, 0, false, false);
            Require(manager.HasPlaytestCollisionAt(0, 0),
                "Playtest does not recognize the WallCollision layer.");
            manager.SetCurrentMapDataForLoad(originalMapData);
            RequireToolbarButton(toolbar, "SpawnToolButton", "플레이어 시작", MapEditorToolbarAction.SetSpawn);
            RequireToolbarButton(toolbar, "SeekerSpawnToolButton", "술래 시작", MapEditorToolbarAction.SetSeekerSpawn);
            Require(toolbar.Find("ValidateButton") == null,
                "Standalone Validate Map button is still visible.");
            Require(toolbar.Find("PNGOutButton") == null,
                "Removed PNG export button is still visible.");
            RequireToolbarButton(toolbar, "WorkshopButton", "검사 후 창작마당 업로드", MapEditorToolbarAction.UploadWorkshop);
            Require(toolbar.Find("WorkshopUploadButton") == null,
                "A duplicate Workshop upload button is still visible.");
            RequireToolbarButton(toolbar, "HelpButton", "도움말", MapEditorToolbarAction.PackageGuide);
            Require(toolbar.Find("ClearButton") == null,
                "Legacy current-layer clear button is still visible.");
            Require(toolbar.Find("ClearAllButton")?.GetComponent<MapEditorToolbarButton>()?.action
                    == MapEditorToolbarAction.ClearAll,
                "Clear All button is missing or connected to the wrong action.");
            Require(toolbar.Find("CharacterTestToolButton") == null, "Removed character test tool is still visible.");

            MapEditorAnimationTileWindow animationWindow = MapEditorAnimationTileWindow.Open(manager);
            Transform animationRoot = canvas.transform.Find("MapEditor_AnimationTileWindow");
            Require(animationWindow != null && animationRoot != null,
                "Runtime animation tile editor was not created.");
            Require(FindDescendant(animationRoot, "SaveButton")?.GetComponent<Button>() != null
                && FindDescendant(animationRoot, "DeleteButton")?.GetComponent<Button>() != null
                && FindDescendant(animationRoot, "ImportTilesetButton")?.GetComponent<Button>() != null,
                "Animation tile editor actions are missing.");
            Require(FindDescendant(animationRoot, "AnimationListContent") != null
                && FindDescendant(animationRoot, "AnimationPreviewImage")?.GetComponent<Image>() != null
                && FindDescendant(animationRoot, "AnimationPreviewImage")?.GetComponent<MapEditorAnimatedTilePlayer>() != null,
                "Animation tile editor list or playback preview is missing.");
            Require(FindDescendant(animationRoot, "UseAnimationBrushButton")?.GetComponent<Button>() != null,
                "Animation tile editor is missing the Use as Brush action.");
            MapEditorObjectUtility.DestroyObject(animationRoot.gameObject);

            MapEditorTileCreatorWindow tileCreator = MapEditorTileCreatorWindow.Open(manager);
            Transform tileCreatorRoot = canvas.transform.Find("MapEditor_TileCreator");
            Require(tileCreator != null && tileCreatorRoot != null,
                "Direct tile creator was not created.");
            Require(FindDescendant(tileCreatorRoot, "AddFrameButton")?.GetComponent<Button>() != null
                && FindDescendant(tileCreatorRoot, "DuplicateFrameButton")?.GetComponent<Button>() != null
                && FindDescendant(tileCreatorRoot, "DeleteFrameButton")?.GetComponent<Button>() != null
                && FindDescendant(tileCreatorRoot, "SaveAnimationButton")?.GetComponent<Button>() != null,
                "Direct tile creator is missing frame-by-frame animation controls.");
            Require(FindDescendant(tileCreatorRoot, "NewTileSlot")?.GetComponent<Button>() != null,
                "Direct tile creator is missing the blank new-tile slot.");
            MapEditorObjectUtility.DestroyObject(tileCreatorRoot.gameObject);

            MapEditorLayerPanelBuilder.Ensure(manager, manager.toolToolbarOffset);
            MapEditorWorkshopPreviewPanelBuilder.Ensure(manager, manager.toolToolbarOffset);
            MapEditorMapInfoPanelBuilder.Ensure(canvas.transform, manager);
            Transform layerPanel = canvas.transform.Find("MapEditor_LayerPanel");
            Require(layerPanel != null, "Layer panel was not created.");
            Require(FindDescendant(layerPanel, "CanvasLayer_0")?.GetComponent<MapEditorToolbarButton>()?.action
                    == MapEditorToolbarAction.SetCanvas,
                "The base canvas layer does not have a selection button.");
            Require(FindDescendant(layerPanel, "CanvasLayer_0")?.GetComponentInParent<MapEditorCanvasLayerDragHandle>() != null,
                "The base canvas layer cannot be reordered by dragging.");
            Require(FindDescendant(layerPanel, "LayerAction_AddCanvas_Ground") != null,
                "Canvas-layer add button is missing.");
            Require(FindDescendant(layerPanel, "LayerAction_DeleteCanvas_Ground") != null,
                "Canvas-layer delete button is missing.");
            Transform previewPanel = canvas.transform.Find("MapEditor_WorkshopPreviewPanel");
            Require(previewPanel != null && FindDescendant(previewPanel, "AddPreviewButton")?.GetComponent<Button>() != null,
                "Workshop preview panel or capture button is missing below the layer panel.");
            Require(canvas.transform.Find("MapEditor_MapInfoPanel/Stats")?.GetComponent<Text>() != null,
                "Map statistics panel was not created below the tilemap workspace.");
            Require(FindDescendant(toolbar, "PreviewRegionToolButton") == null,
                "Preview capture is still present in the tool toolbar instead of its own panel.");

            manager.SetBrushLayerRole(MapEditorLayerType.Object);
            Require(manager.ActiveLayer == MapEditorLayerType.Ground,
                "Unsupported Object brush role did not fall back to Ground.");
            manager.ToggleBrushRoleMenu();
            toolbar = canvas.transform.Find("MapEditor_Toolbar");
            Transform collisionBrushTransform = FindDescendant(toolbar, "BrushRole_WallCollision");
            MapEditorToolbarButton collisionBrushHandler =
                collisionBrushTransform?.GetComponent<MapEditorToolbarButton>();
            Button collisionBrushButton = collisionBrushTransform?.GetComponent<Button>();
            Require(collisionBrushButton != null
                    && collisionBrushHandler != null
                    && collisionBrushHandler.action == MapEditorToolbarAction.SetBrushRole,
                "Collision brush role button was not created after opening the brush menu.");
            collisionBrushButton.onClick.Invoke();
            Require(manager.ActiveLayer == MapEditorLayerType.WallCollision,
                "Clicking the Collision brush role did not activate the collision layer.");
            Require(EditorToolController.Instance.CurrentTool == EditorToolType.Brush,
                "Collision brush role incorrectly switched away from the unified brush tool.");
            Require(!manager.BrushRoleMenuOpen,
                "Collision brush role menu did not close after selection.");
            Require(FindDescendant(toolbar, "BrushRole_WallCollision") == null,
                "Collision brush role menu remained visible after selection.");
            manager.SetSelectionTool();
            Require(manager.ActiveLayer == MapEditorLayerType.Ground,
                "Selection tool did not restore the last drawable canvas slot after Collision.");
            manager.SetBrushLayerRole(MapEditorLayerType.Object);
            manager.SetSpawnTool();
            Require(manager.ActiveLayer == MapEditorLayerType.Spawn,
                "Start Point tool did not activate the Spawn layer.");
            manager.SetSelectionTool();
            Require(manager.ActiveLayer == MapEditorLayerType.Ground,
                "Selection tool did not restore the last normal layer after Start Point.");
            manager.SetSpawnTool();
            Require(manager.SelectedSpawnRole == "Runner",
                "Runner spawn tool did not select the Runner role.");
            manager.SetSpawnTool("Seeker");
            Require(manager.SelectedSpawnRole == "Seeker",
                "Seeker spawn tool did not select the Seeker role.");
            manager.SetBrushTool();
            Require(manager.ActiveLayer == MapEditorLayerType.Ground,
                "Brush tool did not restore the last normal paint layer after Start Point.");

            manager.brushSize = 1;
            manager.ChangeBrushSize(1);
            manager.ChangeBrushSize(1);
            manager.ChangeBrushSize(1);
            manager.ChangeBrushSize(1);
            Require(manager.brushSize == 8, "Brush size exceeded the maximum size of 8.");
            manager.ChangeBrushSize(-1);
            manager.ChangeBrushSize(-1);
            manager.ChangeBrushSize(-1);
            manager.ChangeBrushSize(-1);
            Require(manager.brushSize == 1, "Brush size went below the minimum size of 1.");

            Require(MapEditorLayerUtility.CanvasLayerCount == 32,
                "Canvas layer limit is not 32.");
            Require(MapEditorLayerUtility.GroundOptionalLayers.Length == 31
                && MapEditorLayerUtility.ObjectOptionalLayers.Length == 31
                && MapEditorLayerUtility.WallOptionalLayers.Length == 31,
                "The 32 canvas layers do not have matching Ground/Object/Wall slots.");
            Require(MapEditorLayerUtility.GetCanvasLayer(31, MapEditorLayerType.Ground) == MapEditorLayerType.GroundExtra31
                && MapEditorLayerUtility.GetCanvasLayer(31, MapEditorLayerType.Object) == MapEditorLayerType.ObjectExtra31
                && MapEditorLayerUtility.GetCanvasLayer(31, MapEditorLayerType.WallVisual) == MapEditorLayerType.WallVisualExtra31,
                "The final canvas layer is mapped to the wrong internal slots.");

            manager.AddCanvasLayer();
            Require(manager.IsLayerEnabled(MapEditorLayerType.GroundExtra)
                && manager.IsLayerEnabled(MapEditorLayerType.ObjectExtra)
                && manager.IsLayerEnabled(MapEditorLayerType.WallVisualExtra),
                "Adding Layer 2 did not enable all of its internal drawing slots.");
            Require(manager.ActiveCanvasIndex == 1 && manager.ActiveLayer == MapEditorLayerType.GroundExtra,
                "Layer 2 was not selected with the Ground brush role.");
            manager.CurrentMapData.SetTileOnLayer(
                0, 0, MapEditorLayerType.ObjectExtra, MapEditorManager.CustomColorTileId,
                Color.magenta, string.Empty, -1, 0, false, false);
            manager.AddCanvasLayer();
            Require(manager.ActiveCanvasIndex == 2 && manager.ActiveLayer == MapEditorLayerType.GroundExtra2,
                "Layer 3 was not selected.");
            manager.CurrentMapData.SetTileOnLayer(
                1, 0, MapEditorLayerType.ObjectExtra2, MapEditorManager.CustomColorTileId,
                Color.cyan, string.Empty, -1, 0, false, false);
            MapEditorLayerPanelBuilder.Ensure(manager, manager.toolToolbarOffset);
            Canvas.ForceUpdateCanvases();
            MapEditorCanvasLayerDragHandle sourceDragHandle =
                FindDescendant(layerPanel, "CanvasLayerRow_1")?.GetComponent<MapEditorCanvasLayerDragHandle>();
            MapEditorCanvasLayerDragHandle targetDragHandle =
                FindDescendant(layerPanel, "CanvasLayerRow_2")?.GetComponent<MapEditorCanvasLayerDragHandle>();
            Require(sourceDragHandle != null && targetDragHandle != null,
                "Layer drag rows were not rebuilt after adding layers.");
            EventSystem eventSystem = EventSystem.current != null
                ? EventSystem.current
                : UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            Require(eventSystem != null, "SampleScene is missing EventSystem for layer dragging.");
            var dragEvent = new PointerEventData(eventSystem);
            RectTransform targetRowRect = targetDragHandle.transform as RectTransform;
            dragEvent.position = RectTransformUtility.WorldToScreenPoint(
                null,
                targetRowRect.TransformPoint(targetRowRect.rect.center));
            sourceDragHandle.OnBeginDrag(dragEvent);
            CanvasGroup listCanvasGroup = sourceDragHandle.transform.parent.GetComponent<CanvasGroup>();
            Require(listCanvasGroup == null || listCanvasGroup.blocksRaycasts,
                "Dragging one layer disabled raycasts for the entire layer list.");
            sourceDragHandle.OnEndDrag(dragEvent);
            Require(manager.CurrentMapData.GetTile(0, 0, MapEditorLayerType.ObjectExtra2) == MapEditorManager.CustomColorTileId
                && manager.CurrentMapData.GetColor(0, 0, MapEditorLayerType.ObjectExtra2) == Color.magenta,
                "Dragging Layer 2 below Layer 3 did not move its tile data.");
            Require(manager.CurrentMapData.GetTile(1, 0, MapEditorLayerType.ObjectExtra) == MapEditorManager.CustomColorTileId
                && manager.CurrentMapData.GetColor(1, 0, MapEditorLayerType.ObjectExtra) == Color.cyan,
                "Dragging a layer damaged the displaced layer data.");
            manager.MoveCanvasLayer(2, 1);
            manager.DeleteActiveCanvasLayer();
            Require(!manager.IsCanvasEnabled(2), "Deleted Layer 3 is still enabled.");
            Require(manager.CurrentMapData.GetTile(0, 0, MapEditorLayerType.ObjectExtra) == MapEditorManager.CustomColorTileId,
                "Deleting Layer 3 damaged Layer 2.");
            manager.SetActiveCanvas(1);
            manager.DeleteActiveCanvasLayer();
            Require(!manager.IsCanvasEnabled(1), "Deleted Layer 2 is still enabled.");
            Require(manager.CurrentMapData.GetTile(0, 0, MapEditorLayerType.ObjectExtra) == -1,
                "Deleting a canvas layer left its tile data behind.");
            Require(manager.ActiveCanvasIndex == 0 && manager.ActiveLayer == MapEditorLayerType.Ground,
                "Deleting a canvas layer did not return to Layer 1 with the current brush role.");

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

            MapEditorTilesetLibraryWindow runtimeTilesetWindow = MapEditorTilesetLibraryWindow.Open(manager);
            Require(runtimeTilesetWindow != null
                && canvas.transform.Find("MapEditor_TilesetLibraryWindow/Panel/ImportCollectionButton")?.GetComponent<Button>() != null
                && canvas.transform.Find("MapEditor_TilesetLibraryWindow/Panel/LibraryList")?.GetComponent<ScrollRect>() != null,
                "The runtime tileset library UI was not created for builds.");
            MapEditorObjectUtility.DestroyObject(runtimeTilesetWindow.gameObject);

            collisionLineRoot = new GameObject("CollisionLineRegressionCells", typeof(RectTransform));
            manager.ClearRegisteredCells();
            GridCell collisionLineStart = null;
            GridCell collisionLineEnd = null;
            for (int x = 0; x < manager.CurrentMapData.width; x++)
            {
                GameObject cellObject = new GameObject(
                    "CollisionLineCell_" + x,
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(GridCell));
                cellObject.transform.SetParent(collisionLineRoot.transform, false);
                GridCell generatedCell = cellObject.GetComponent<GridCell>();
                cellObject.SendMessage("Awake");
                generatedCell.Init(x, 1);
                manager.RegisterCell(generatedCell);
                if (x == 0) collisionLineStart = generatedCell;
                if (x == manager.CurrentMapData.width - 1) collisionLineEnd = generatedCell;
            }

            Require(collisionLineStart != null && collisionLineEnd != null,
                "Could not find map cells for the collision line regression test.");
            manager.brushSize = 1;
            manager.SetBrushLayerRole(MapEditorLayerType.WallCollision);
            manager.SetLineTool();
            manager.BeginPointerDrag(collisionLineStart);
            manager.UpdatePointerDrag(collisionLineEnd);
            Transform collisionLinePreview = gridGenerator.gridParent.parent.Find("MapEditor_BrushCursorPreview");
            Require(collisionLinePreview != null
                && collisionLinePreview.GetComponentsInChildren<Image>(false).Length >= manager.CurrentMapData.width,
                "Collision line preview did not render the dragged tile path.");
            manager.EndPointerDrag(collisionLineEnd);
            manager.CommitEditTransaction();
            for (int x = 0; x < manager.CurrentMapData.width; x++)
            {
                Require(
                    manager.CurrentMapData.GetTile(x, 1, MapEditorLayerType.WallCollision) == MapEditorManager.WallTileId,
                    "Collision line painting did not store a wall collision tile at x=" + x + ".");
            }
            manager.ClearActiveLayer();
            manager.SetBrushLayerRole(MapEditorLayerType.Ground);

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
            firstCell.SetSpawnMarkerRole("Runner");
            Text spawnMarker = firstCell.transform.Find("SpawnMarker")?.GetComponent<Text>();
            Require(spawnMarker != null && spawnMarker.text == "P" && spawnMarker.color.b > spawnMarker.color.r,
                "Runner spawn marker is not shown as a cyan P.");
            firstCell.SetSpawnMarkerRole("Seeker");
            Require(spawnMarker.text == "S" && spawnMarker.color.r > spawnMarker.color.b,
                "Seeker spawn marker is not shown as a red S.");
            firstCell.SetSpawnMarkerRole(string.Empty);
            firstCell.SetWallCollisionOutline(false, false, false, false, false);
            Require(firstCell.transform.Find("WallCollisionOverlay") == null,
                "An empty cell allocated a wall collision overlay during refresh.");
            manager.CurrentMapData.SetTileOnLayer(
                0, 0, MapEditorLayerType.WallCollision, MapEditorManager.WallTileId,
                Color.black, string.Empty, -1, 0, false, false);
            manager.RefreshCell(firstCell);
            Image wallOverlay = firstCell.transform.Find("WallCollisionOverlay")?.GetComponent<Image>();
            Require(wallOverlay != null && wallOverlay.gameObject.activeSelf && wallOverlay.color.a >= 0.35f,
                "Wall collision overlay is not clearly visible over painted tiles.");

            manager.pixelChromaSpawnPoints.Clear();
            manager.pixelChromaSpawnPoints.Add(new MapEditorSpawnPointData("RunnerSpawn", 1, 1, "Runner"));
            manager.CurrentMapData.SetTileOnLayer(
                1, 1, MapEditorLayerType.Ground, MapEditorManager.CustomColorTileId,
                Color.green, string.Empty, -1, 0, false, false);
            manager.CurrentMapData.SetTileOnLayer(
                1, 1, MapEditorLayerType.Object, MapEditorManager.CustomColorTileId,
                Color.yellow, string.Empty, -1, 0, false, false);
            manager.ClearMap();
            Require(manager.CurrentMapData.GetTile(1, 1, MapEditorLayerType.Ground) == -1
                && manager.CurrentMapData.GetTile(1, 1, MapEditorLayerType.Object) == -1
                && manager.pixelChromaSpawnPoints.Count == 0,
                "Clear All did not clear overlapping layers and start points.");
            manager.Undo();
            Require(manager.CurrentMapData.GetTile(1, 1, MapEditorLayerType.Ground) == MapEditorManager.CustomColorTileId
                && manager.CurrentMapData.GetTile(1, 1, MapEditorLayerType.Object) == MapEditorManager.CustomColorTileId
                && manager.pixelChromaSpawnPoints.Count == 1,
                "Undo did not restore every layer and the start point after Clear All.");
            manager.Redo();
            Require(manager.CurrentMapData.GetTile(1, 1, MapEditorLayerType.Ground) == -1
                && manager.CurrentMapData.GetTile(1, 1, MapEditorLayerType.Object) == -1
                && manager.pixelChromaSpawnPoints.Count == 0,
                "Redo did not clear the map again after Clear All undo.");

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
            Text failedValidationText = failedValidationModal.Find("Panel/Viewport/Content")?.GetComponent<Text>();
            Require(failedValidationText != null && failedValidationText.text.Contains("창작마당 합격 조건"),
                "Workshop validation did not explain its pass requirements.");
            Require(failedValidationText != null
                && failedValidationText.text.Contains("PixelChroma Workshop 콘텐츠 정책")
                && failedValidationText.text.Contains("타 게임에서 무단 추출한 에셋의 업로드를 금지합니다."),
                "Workshop validation did not show the content policy notice.");
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
            Require(hexRow != null && Mathf.Abs(hexRow.anchoredPosition.y + 218f) < 0.1f,
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
            Require(Mathf.Abs(paletteViewport.rect.width - 200f) < 0.1f && Mathf.Abs(paletteViewport.rect.height - 200f) < 0.1f,
                "PNG palette viewport is not using the full display size.");
            Require(paletteGrid != null && paletteGrid.padding.horizontal == 0 && paletteGrid.padding.vertical == 0,
                "PNG palette still has an outer border padding.");
            Require(Mathf.Abs(paletteViewport.rect.width - paletteContentRect.rect.width) < 0.1f
                && Mathf.Abs(paletteViewport.rect.height - paletteContentRect.rect.height) < 0.1f,
                "PNG palette does not fit its viewport after loading.");

            int originalMovingRegionCount = manager.MovingRegionCount;
            manager.movingRegions.Add(new MapEditorMovingRegionData
            {
                id = "moving_path_smoke",
                displayName = "Smoke Path",
                canvasLayerIndex = 2,
                x = 1,
                y = 1,
                width = 2,
                height = 2,
                tilesPerSecond = 3.5f,
                path = new[]
                {
                    new MapEditorPathPointData(2, 2),
                    new MapEditorPathPointData(4, 2)
                }
            });
            picker.RefreshMovingPathList();
            Transform movingPathList = picker.transform.Find("MovingPathList");
            ScrollRect movingPathScroll = movingPathList?.Find("ScrollView")?.GetComponent<ScrollRect>();
            Require(movingPathList != null && movingPathList.gameObject.activeSelf,
                "The saved moving-path list was not created below the palette.");
            Require(movingPathScroll != null && movingPathScroll.vertical && !movingPathScroll.horizontal,
                "The saved moving-path list is not vertically scrollable.");
            Require(movingPathScroll.content != null
                && movingPathScroll.content.childCount == originalMovingRegionCount + 1,
                "The saved moving-path list does not match the moving-region data.");
            manager.FocusMovingRegion(originalMovingRegionCount);
            InputField selectedPathSpeed = movingPathList.Find("SelectedPathControls/Speed")?.GetComponent<InputField>();
            Require(selectedPathSpeed != null && selectedPathSpeed.gameObject.activeInHierarchy && selectedPathSpeed.interactable,
                "The selected moving path speed input is not visible or editable.");
            Require(selectedPathSpeed.text == "3.5",
                "The selected moving path speed input does not show the saved speed.");
            MapEditorMovingPathListView reloadedMovingPathView = new MapEditorMovingPathListView(manager);
            reloadedMovingPathView.EnsureArea(picker.transform);
            Transform reloadedMovingPathContent = movingPathList.Find("ScrollView/Viewport/Content");
            InputField reloadedPathSpeed = movingPathList.Find("SelectedPathControls/Speed")?.GetComponent<InputField>();
            Require(reloadedMovingPathContent != null
                && reloadedMovingPathContent.childCount == originalMovingRegionCount + 1
                && reloadedPathSpeed != null
                && reloadedPathSpeed.interactable,
                "The moving-path list or speed input was lost after reconnecting existing scene UI.");
            Transform movingPathRow = reloadedMovingPathContent.Find("MovingPath_" + originalMovingRegionCount);
            Button movingPathVisibilityToggle = movingPathRow?.Find("Visibility")?.GetComponent<Button>();
            Text movingPathVisibilityLabel = movingPathRow?.Find("Visibility/Text")?.GetComponent<Text>();
            Require(movingPathVisibilityToggle != null && movingPathVisibilityLabel != null
                && manager.IsMovingPathGuideVisible(originalMovingRegionCount) && movingPathVisibilityLabel.text == "ON",
                "The per-path visibility toggle was not created in its enabled state.");
            movingPathVisibilityToggle.onClick.Invoke();
            movingPathVisibilityLabel = movingPathList.Find("ScrollView/Viewport/Content/MovingPath_"
                + originalMovingRegionCount + "/Visibility/Text")?.GetComponent<Text>();
            Require(!manager.IsMovingPathGuideVisible(originalMovingRegionCount)
                && movingPathVisibilityLabel != null && movingPathVisibilityLabel.text == "OFF"
                && manager.SelectedMovingRegionIndex == originalMovingRegionCount,
                "Hiding one moving-path guide changed the selected path or did not update its toggle.");
            movingPathVisibilityToggle = movingPathList.Find("ScrollView/Viewport/Content/MovingPath_"
                + originalMovingRegionCount + "/Visibility")?.GetComponent<Button>();
            movingPathVisibilityToggle.onClick.Invoke();
            movingPathVisibilityLabel = movingPathList.Find("ScrollView/Viewport/Content/MovingPath_"
                + originalMovingRegionCount + "/Visibility/Text")?.GetComponent<Text>();
            Require(manager.IsMovingPathGuideVisible(originalMovingRegionCount)
                && movingPathVisibilityLabel != null && movingPathVisibilityLabel.text == "ON"
                && manager.SelectedMovingRegionIndex == originalMovingRegionCount,
                "Showing one moving-path guide did not restore its visibility state.");
            int secondMovingRegionIndex = manager.MovingRegionCount;
            manager.movingRegions.Add(new MapEditorMovingRegionData
            {
                id = "moving_path_smoke_second",
                displayName = "Second Smoke Path",
                canvasLayerIndex = 1,
                x = 5,
                y = 5,
                width = 1,
                height = 1,
                tilesPerSecond = 2f,
                path = new[]
                {
                    new MapEditorPathPointData(5, 5),
                    new MapEditorPathPointData(6, 5)
                }
            });
            manager.SetMovingPathGuideVisible(originalMovingRegionCount, false);
            Require(!manager.IsMovingPathGuideVisible(originalMovingRegionCount)
                && manager.IsMovingPathGuideVisible(secondMovingRegionIndex),
                "Hiding one moving path also hid another path.");
            manager.SetMovingPathGuideVisible(originalMovingRegionCount, true);
            manager.DeleteMovingRegion(secondMovingRegionIndex);
            manager.SetMovingRegionSpeed(originalMovingRegionCount, 6.25f);
            Require(Mathf.Abs(manager.GetMovingRegionAt(originalMovingRegionCount).tilesPerSecond - 6.25f) < 0.001f,
                "Editing a saved moving path speed did not update its data.");
            manager.RenameMovingRegion(originalMovingRegionCount, "Renamed Path");
            Require(manager.GetMovingRegionAt(originalMovingRegionCount)?.displayName == "Renamed Path",
                "A saved moving path could not be renamed.");
            Require(manager.SelectedMovingRegionIndex == originalMovingRegionCount,
                "A saved moving path could not be focused on the map.");
            manager.DeleteMovingRegion(originalMovingRegionCount);
            Require(manager.MovingRegionCount == originalMovingRegionCount
                && manager.SelectedMovingRegionIndex == -1,
                "A saved moving path could not be deleted.");

            MapSaveData extendedSave = new MapSaveData(4, 4)
            {
                spawnPoints = new[]
                {
                    new MapEditorSpawnPointData("RunnerSpawn", 1, 1, "Runner"),
                    new MapEditorSpawnPointData("SeekerSpawn", 2, 2, "Seeker")
                },
                movingRegions = new[]
                {
                    new MapEditorMovingRegionData
                    {
                        id = "platform_1",
                        canvasLayerIndex = 3,
                        x = 0,
                        y = 0,
                        width = 2,
                        height = 2,
                        path = new[]
                        {
                            new MapEditorPathPointData(1, 1),
                            new MapEditorPathPointData(3, 1)
                        },
                        tilesPerSecond = 2f,
                        pingPong = true
                    }
                }
            };
            MapSaveData extendedRoundTrip = JsonUtility.FromJson<MapSaveData>(JsonUtility.ToJson(extendedSave));
            Require(extendedRoundTrip != null && extendedRoundTrip.formatVersion == 7,
                "Extended edit-map contract did not preserve format version 7.");
            Require(extendedRoundTrip.spawnPoints?.Length == 2
                && extendedRoundTrip.spawnPoints[0].role == "Runner"
                && extendedRoundTrip.spawnPoints[1].role == "Seeker",
                "Runner and Seeker spawn roles were not preserved by JSON round-trip.");
            Require(extendedRoundTrip.movingRegions?.Length == 1
                && extendedRoundTrip.movingRegions[0].path?.Length == 2
                && extendedRoundTrip.movingRegions[0].pingPong
                && extendedRoundTrip.movingRegions[0].canvasLayerIndex == 3
                && Mathf.Abs(extendedRoundTrip.movingRegions[0].tilesPerSecond - 2f) < 0.001f,
                "Moving-region path data was not preserved by JSON round-trip.");

            MapData renderContractMap = new MapData(2, 1);
            renderContractMap.SetTileOnLayer(0, 0, MapEditorLayerType.Ground,
                MapEditorManager.CustomColorTileId, Color.red, string.Empty, -1, 0, false, false);
            renderContractMap.SetTileOnLayer(0, 0, MapEditorLayerType.GroundExtra,
                MapEditorManager.CustomColorTileId, Color.green, string.Empty, -1, 0, false, false);
            MapEditorCellRenderService renderContract = new MapEditorCellRenderService(null, _ => true);
            Color32[] movingLayerPixel = new Color32[1];
            Color32[] remainingLayerPixel = new Color32[1];
            Color32[] emptyMovingPixel = new Color32[1];
            renderContract.WriteCanvasCellPixels(renderContractMap, 0, 0, 1, 1, movingLayerPixel, 1, 0, 0);
            renderContract.WriteCompositeCellPixelsExcludingCanvas(renderContractMap, 0, 0, 1, 1, remainingLayerPixel, 1, 0, 0);
            renderContract.WriteCanvasCellPixels(renderContractMap, 1, 0, 1, 1, emptyMovingPixel, 1, 0, 0);
            Require(ColorDistance(movingLayerPixel[0], Color.green) < 0.02f,
                "Moving-region rendering did not isolate the selected canvas layer.");
            Require(ColorDistance(remainingLayerPixel[0], Color.red) < 0.02f,
                "The moving-region source did not preserve the remaining canvas layers.");
            Require(emptyMovingPixel[0].a == 0,
                "An empty moving-layer cell was rendered as an opaque white tile.");

            Color32[] sourceCoverPixels = { new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255) };
            Color32[] movingPixels = { new Color32(255, 0, 0, 255), new Color32(0, 0, 0, 0) };
            System.Reflection.MethodInfo applyMovingMask = typeof(MapEditorPlaytestController).GetMethod(
                "ApplyMovingLayerMask",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Require(applyMovingMask != null, "The moving-region source mask helper is missing.");
            applyMovingMask.Invoke(null, new object[] { sourceCoverPixels, movingPixels });
            Require(sourceCoverPixels[0].a == 255 && sourceCoverPixels[1].a == 0,
                "The moving-region source cover still paints empty selection pixels white.");

            Debug.Log("MapEditor size configuration smoke test passed.");
        }
        finally
        {
            if (nestedCanvasObject != null)
            {
                UnityEngine.Object.DestroyImmediate(nestedCanvasObject);
            }

            if (texture != null)
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }

            if (testCellObject != null)
            {
                UnityEngine.Object.DestroyImmediate(testCellObject);
            }

            if (collisionLineRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(collisionLineRoot);
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
        Transform button = FindDescendant(toolbar, objectName);
        Require(button != null, objectName + " is missing.");
        Require(button.Find("Text")?.GetComponent<Text>()?.text == label, objectName + " label was not localized.");
        Require(button.GetComponent<MapEditorToolbarButton>()?.action == action, objectName + " action mapping changed.");
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < descendants.Length; i++)
        {
            if (descendants[i].name == objectName) return descendants[i];
        }

        return null;
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
