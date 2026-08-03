using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class MapEditorManager : MonoBehaviour
{
    public const int CustomColorTileId = -2;
    public const int CustomImageTileId = -3;
    public const int WallTileId = -4;
    public const int MaxMapSize = 512;
    public const int MinExportCellPixels = 1;
    public const int MaxExportCellPixels = 16;
    public static readonly int[] ExportCellPixelOptions = { 1, 4, 8, 16 };
    public static readonly int[] PngPaletteGridSizeOptions = { 16, 32, 64, 128 };
    public static MapEditorManager Instance { get; private set; }

    [Header("맵")]
    public int mapWidth = 64;
    public int mapHeight = 64;

    [Header("PNG 팔레트")]
    public int pngPaletteGridSize = 16;

    [Header("그리기 색상")]
    public Color selectedColor = Color.red;
    public bool useSelectedColor = true;
    public bool useWallTileBrush;
    public Sprite selectedImageBrush;
    public string selectedImagePath = string.Empty;
    public int selectedImageIndex = -1;
    public int selectedImageRotation;
    public bool selectedImageFlipX;
    public bool selectedImageFlipY;
    public int brushSize = 1;

    [Header("레이어")]
    public MapEditorLayerType activeLayer = MapEditorLayerType.Ground;
    public bool showGroundLayer = true;
    public bool showObjectLayer = true;
    public bool showWallVisualLayer = true;
    public bool showWallCollisionLayer = true;
    public bool showZoneLayer = true;
    public List<MapEditorLayerSetting> layerSettings = new List<MapEditorLayerSetting>();

    [Header("도구")]
    public BrushTool brushTool;
    public EraserTool eraserTool;

    [Header("입력")]
    public bool enableKeyboardShortcuts = true;
    public KeyCode eyedropperKey = KeyCode.Space;

    [Header("도구 모음")]
    public bool createToolToolbar = true;
    public bool removeLegacyToolButtons = true;
    public Vector2 toolToolbarOffset = new Vector2(-12f, -12f);
    public int maxRecentPngFiles = 5;

    [Header("미니맵")]
    public bool createMinimap = false;
    public Vector2 minimapOffset = new Vector2(-198f, -152f);
    public Vector2 minimapSize = new Vector2(120f, 120f);

    [Header("맵 확대/축소")]
    public float mapZoomStep = 4f;
    public float maxMapCellSize = 96f;

    [Header("브러시 커서 미리보기")]
    public bool showBrushCursorPreview = true;
    public float brushCursorAlpha = 0.45f;

    [Header("내보내기")]
    public int exportCellPixels = 1;
    public bool paintWholeTile;
    public bool exportEmptyCellsTransparent = true;
    public string pixelChromaMapId = "map_01";
    public string workshopTitle = "새 PixelChroma 맵";
    public string workshopAuthor = "작성자 미상";
    [TextArea(2, 4)]
    public string workshopDescription = string.Empty;
    public string requiredPixelChromaVersion = "1.0.0";
    public string steamWorkshopVisibility = "Public";
    public string steamWorkshopTags = "Map";
    public uint steamAppId;
    public int pixelChromaSpawnX;
    public int pixelChromaSpawnY;
    public List<MapEditorSpawnPointData> pixelChromaSpawnPoints = new List<MapEditorSpawnPointData>();

    [Header("색상 창")]
    public bool createColorWheelWindow = true;
    public Vector2 colorPaletteOffset = new Vector2(12f, -12f);

    public MapData CurrentMapData { get; private set; }

    private readonly Dictionary<Vector2Int, GridCell> cells = new Dictionary<Vector2Int, GridCell>();
    private readonly MapEditorPngFileService pngFiles = new MapEditorPngFileService();
    private MapEditorTilesetLibraryService tilesetLibrary;
    private readonly MapEditorBrushCursorPreview brushCursorPreview = new MapEditorBrushCursorPreview();
    private readonly MapEditorMapSaveService mapSaveService = new MapEditorMapSaveService(MaxMapSize);
    private readonly MapEditorPixelChromaImportService pixelChromaImportService = new MapEditorPixelChromaImportService();
    private readonly MapEditorPixelChromaExportService pixelChromaExportService = new MapEditorPixelChromaExportService();
    private MapEditorWorkshopExportService workshopExportService;
    private readonly MapEditorToolbarStateService toolbarState = new MapEditorToolbarStateService();
    private readonly MapEditorMapSizeService mapSizeService = new MapEditorMapSizeService();
    private readonly MapEditorMapLoadApplyService mapLoadApplyService = new MapEditorMapLoadApplyService();
    private readonly MapEditorEyedropperService eyedropperService = new MapEditorEyedropperService();
    private MapEditorBrushSelectionService brushSelection;

    private GridGenerator gridGenerator;
    private MapEditorMapEditingService mapEditing;
    private MapEditorSelectionClipboardService selectionClipboard;
    private MapEditorViewportService viewportService;
    private MapEditorInputService inputService;
    private ColorWheelPickerWindow colorWheelWindow;
    private MapEditorMinimap minimap;
    private EditorToolController subscribedToolController;
    private GridCell hoveredCell;
    private int hoveredSubPixelX;
    private int hoveredSubPixelY;
    private int selectedRegionGridSize;
    private int selectedRegionStartX;
    private int selectedRegionStartYFromTop;
    private int selectedRegionWidth = 1;
    private int selectedRegionHeight = 1;
    private bool hasPaintStrokeSample;
    private Vector2Int lastPaintStrokePoint;
    private int lastPaintStrokeResolution;
    private EditorToolType lastPaintStrokeTool;
    private Vector2Int? previewDragStart;
    private RectInt? previewRegion;

    public GridGenerator GridGenerator => gridGenerator;
    public MapEditorLayerType ActiveLayer => activeLayer;
    private Vector2 lastCanvasSize = new Vector2(-1f, -1f);

    public void SetCurrentMapDataForLoad(MapData mapData)
    {
        if (mapData == null)
        {
            return;
        }

        CurrentMapData = mapData;
    }

    private void Awake()
    {
        Instance = this;
        EnsureLayerSettings();
        EnsureTilesetLibrary();
        gridGenerator = GetComponent<GridGenerator>();

        if (CurrentMapData == null)
        {
            CurrentMapData = new MapData(mapWidth, mapHeight);
        }
        else
        {
            CurrentMapData.EnsureInitialized();
            mapWidth = CurrentMapData.width;
            mapHeight = CurrentMapData.height;
        }

        EnsureMapEditingService();
        EnsureSelectionClipboardService();
        EnsureViewportService();
        EnsureInputService();
        EnsureBrushSelectionService();
    }

    private void OnEnable()
    {
        Instance = this;
        EnsureLayerSettings();
        EnsureTilesetLibrary();
        EnsureMapEditingService();
        EnsureSelectionClipboardService();
        EnsureViewportService();
        EnsureInputService();
        EnsureBrushSelectionService();
        SubscribeToolControllerChange();

        if (!Application.isPlaying)
        {
            EnsureSceneTools();
        }
    }

    private void OnDisable()
    {
        UnsubscribeToolControllerChange();
    }

    private void EnsureMapEditingService()
    {
        if (mapEditing != null)
        {
            return;
        }

        mapEditing = new MapEditorMapEditingService(() => CurrentMapData, () => activeLayer, IsLayerVisible, cells, GetPngTileSprite, RefreshMinimap);
    }

    private MapEditorTilesetLibraryService EnsureTilesetLibrary()
    {
        if (tilesetLibrary == null)
        {
            tilesetLibrary = new MapEditorTilesetLibraryService();
        }

        return tilesetLibrary;
    }

    private void EnsureSelectionClipboardService()
    {
        if (selectionClipboard != null)
        {
            return;
        }

        EnsureMapEditingService();
        selectionClipboard = new MapEditorSelectionClipboardService(
            mapEditing,
            () => CurrentMapData,
            () => hoveredCell,
            EnsureMapContainsRect,
            RefreshAllCells,
            ConfigureMapViewportVisual,
            UpdateBrushCursorPreview,
            RefreshMinimap
        );
    }

    private void EnsureViewportService()
    {
        if (viewportService != null)
        {
            return;
        }

        viewportService = new MapEditorViewportService(
            () => GridGenerator,
            () => mapWidth,
            () => mapHeight,
            () => mapZoomStep,
            () => maxMapCellSize,
            SyncMinimapView
        );
    }

    private void EnsureInputService()
    {
        if (inputService != null)
        {
            return;
        }

        inputService = new MapEditorInputService(this);
    }

    private void EnsureBrushSelectionService()
    {
        if (brushSelection != null)
        {
            return;
        }

        brushSelection = new MapEditorBrushSelectionService(GetPngTileSprite);
    }

    private void EnsureWorkshopExportService()
    {
        if (workshopExportService != null)
        {
            return;
        }

        workshopExportService = new MapEditorWorkshopExportService(GetPngTileSprite);
    }

    private void ConfigureWorkshopPreview()
    {
        EnsureWorkshopExportService();
        workshopExportService.SetPreviewRegion(previewRegion);
    }

    private void SubscribeToolControllerChange()
    {
        if (subscribedToolController == EditorToolController.Instance)
        {
            return;
        }

        UnsubscribeToolControllerChange();

        if (EditorToolController.Instance == null)
        {
            return;
        }

        subscribedToolController = EditorToolController.Instance;
        subscribedToolController.ToolChanged += HandleToolChanged;
    }

    private void UnsubscribeToolControllerChange()
    {
        if (subscribedToolController == null)
        {
            return;
        }

        subscribedToolController.ToolChanged -= HandleToolChanged;
        subscribedToolController = null;
    }

    private void HandleToolChanged(EditorToolType tool)
    {
        NormalizeToolStateForTool(tool);
        CancelTransientToolState();
        RefreshToolToolbarSelection();
        UpdateBrushPreview();
        UpdateBrushCursorPreview();
    }

    private void NormalizeToolStateForTool(EditorToolType tool)
    {
        useWallTileBrush = tool == EditorToolType.Wall;

        if (useWallTileBrush)
        {
            useSelectedColor = selectedImageBrush == null;
        }
    }

    private MapEditorPaintSelection GetPaintSelection()
    {
        return new MapEditorPaintSelection
        {
            useSelectedColor = useSelectedColor,
            useWallTileBrush = useWallTileBrush,
            selectedColor = selectedColor,
            selectedImageBrush = selectedImageBrush,
            selectedImagePath = selectedImagePath,
            selectedImageIndex = selectedImageIndex,
            selectedImageRotation = selectedImageRotation,
            selectedImageFlipX = selectedImageFlipX,
            selectedImageFlipY = selectedImageFlipY,
            brushSize = brushSize
        };
    }

    private void Start()
    {
        EnsureSceneTools();
    }

    private void EnsureSceneTools()
    {
        MapEditorSceneUiBuilder.EnsureBackground();
        MapEditorSceneSetupService.RemoveMinimapObjects();
        createMinimap = false;
        minimap = null;

        if (removeLegacyToolButtons)
        {
            MapEditorSceneSetupService.RemoveLegacyToolButtons();
        }

        if (createColorWheelWindow)
        {
            colorWheelWindow = ColorWheelPickerWindow.Create(this, colorPaletteOffset);
        }

        if (createToolToolbar)
        {
            CreateToolToolbar();
        }

        ConfigureMapViewportVisual();
        RefreshResponsiveLayout(true);
        MapEditorSceneUiBuilder.EnsureQuitButton(Object.FindFirstObjectByType<Canvas>());
        MapEditorFontProvider.ApplyToScene(gameObject.scene);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        RefreshResponsiveLayout(false);
        MapEditorSceneUiBuilder.BringQuitButtonToFront();
        inputService.Tick();
        UpdateBrushCursorPreview();
    }

    private void RefreshResponsiveLayout(bool force)
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        RectTransform canvasRect = canvas == null ? null : canvas.transform as RectTransform;

        if (canvasRect == null)
        {
            return;
        }

        Vector2 currentSize = canvasRect.rect.size;

        if (!force && (currentSize - lastCanvasSize).sqrMagnitude < 0.01f)
        {
            return;
        }

        lastCanvasSize = currentSize;
        MapEditorSceneUiBuilder.ConfigureCanvasScaler(canvas);
        MapEditorToolbarBuilder.RefreshLayout(toolToolbarOffset);
        MapEditorMapSizePanelBuilder.RefreshLayout(canvas.transform, toolToolbarOffset);
        MapEditorLayerPanelBuilder.RefreshLayout(toolToolbarOffset);
        ConfigureMapViewportVisual();
    }

    public void SetBrushTool()
    {
        CancelTransientToolState();
        useWallTileBrush = false;

        if (EditorToolController.Instance != null)
        {
            EditorToolController.Instance.SetBrushTool();
        }

        RefreshToolToolbarSelection();
        UpdateBrushPreview();
        UpdateBrushCursorPreview();
    }

    public void SetWallTileTool()
    {
        CancelTransientToolState();
        useWallTileBrush = true;
        useSelectedColor = true;
        activeLayer = MapEditorLayerType.WallCollision;

        if (EditorToolController.Instance != null)
        {
            EditorToolController.Instance.SetWallTool();
        }

        RefreshToolToolbarSelection();
        UpdateBrushPreview();
        UpdateBrushCursorPreview();
    }

    public void SetEraserTool()
    {
        CancelTransientToolState();
        useWallTileBrush = false;

        if (EditorToolController.Instance != null)
        {
            EditorToolController.Instance.SetEraserTool();
        }

        RefreshToolToolbarSelection();
    }

    public void SetSelectionTool()
    {
        CancelTransientToolState();
        useWallTileBrush = false;

        if (EditorToolController.Instance != null)
        {
            EditorToolController.Instance.SetSelectionTool();
        }

        RefreshToolToolbarSelection();
        UpdateBrushCursorPreview();
    }

    public void SetSpawnTool()
    {
        CancelTransientToolState();
        useWallTileBrush = false;
        activeLayer = MapEditorLayerType.Spawn;

        if (EditorToolController.Instance != null)
        {
            EditorToolController.Instance.SetSpawnTool();
        }

        RefreshToolToolbarSelection();
        UpdateBrushCursorPreview();
    }

    public void SetPreviewRegionTool()
    {
        CancelTransientToolState();
        useWallTileBrush = false;

        if (EditorToolController.Instance != null)
        {
            EditorToolController.Instance.SetPreviewRegionTool();
        }

        RefreshToolToolbarSelection();
        UpdateBrushCursorPreview();
    }

    public void SetActiveLayer(MapEditorLayerType layerType)
    {
        activeLayer = layerType;

        if (layerType == MapEditorLayerType.WallCollision)
        {
            SetWallTileTool();
        }
        else if (layerType == MapEditorLayerType.Spawn)
        {
            SetSpawnTool();
        }
        else
        {
            useWallTileBrush = false;

            if (EditorToolController.Instance != null
                && (EditorToolController.Instance.CurrentTool == EditorToolType.Wall
                    || EditorToolController.Instance.CurrentTool == EditorToolType.Spawn))
            {
                EditorToolController.Instance.SetBrushTool();
            }

            RefreshToolToolbarSelection();
            UpdateBrushPreview();
            UpdateBrushCursorPreview();
            toolbarState.RefreshLayerSelection();
        }

        Debug.Log("선택한 레이어: " + activeLayer);
    }

    public bool IsLayerVisible(MapEditorLayerType layerType)
    {
        EnsureLayerSettings();
        MapEditorLayerSetting setting = FindLayerSetting(layerType);

        if (setting != null)
        {
            return setting.visible;
        }

        return true;
    }

    public void ToggleLayerVisible(MapEditorLayerType layerType)
    {
        EnsureLayerSettings();
        MapEditorLayerSetting setting = FindLayerSetting(layerType);

        if (setting == null)
        {
            return;
        }

        setting.visible = !setting.visible;
        SyncLegacyLayerVisibility();
        RefreshAllCells();

        if (createToolToolbar)
        {
            CreateToolToolbar();
        }
        else
        {
            RefreshToolToolbarSelection();
        }

        Debug.Log("레이어 표시 상태: " + layerType + " = " + IsLayerVisible(layerType));
    }

    public string GetLayerDisplayName(MapEditorLayerType layerType)
    {
        EnsureLayerSettings();
        MapEditorLayerSetting setting = FindLayerSetting(layerType);
        return setting == null ? GetDefaultLayerName(layerType) : setting.displayName;
    }

    public void SetLayerDisplayName(MapEditorLayerType layerType, string displayName)
    {
        EnsureLayerSettings();
        MapEditorLayerSetting setting = FindLayerSetting(layerType);

        if (setting == null)
        {
            return;
        }

        string trimmedName = string.IsNullOrWhiteSpace(displayName)
            ? GetDefaultLayerName(layerType)
            : displayName.Trim();
        setting.displayName = trimmedName.Length > 18 ? trimmedName.Substring(0, 18) : trimmedName;

        if (createToolToolbar)
        {
            CreateToolToolbar();
        }
    }

    public MapEditorLayerSetting[] GetLayerSettingsForSave()
    {
        EnsureLayerSettings();
        MapEditorLayerSetting[] result = new MapEditorLayerSetting[layerSettings.Count];

        for (int i = 0; i < layerSettings.Count; i++)
        {
            result[i] = layerSettings[i].Clone();
        }

        return result;
    }

    private void ApplyLayerSettings(MapEditorLayerSetting[] savedSettings)
    {
        layerSettings.Clear();

        if (savedSettings == null || savedSettings.Length == 0)
        {
            showGroundLayer = true;
            showObjectLayer = true;
            showWallVisualLayer = true;
            showWallCollisionLayer = true;
            showZoneLayer = true;
        }
        else
        {
            for (int i = 0; i < savedSettings.Length; i++)
            {
                MapEditorLayerSetting setting = savedSettings[i];

                if (setting == null || !System.Enum.IsDefined(typeof(MapEditorLayerType), setting.layer))
                {
                    continue;
                }

                MapEditorLayerType layerType = (MapEditorLayerType)setting.layer;

                if (FindLayerSetting(layerType) != null)
                {
                    continue;
                }

                string name = string.IsNullOrWhiteSpace(setting.displayName)
                    ? GetDefaultLayerName(layerType)
                    : setting.displayName.Trim();
                layerSettings.Add(new MapEditorLayerSetting(layerType, name, setting.visible));
            }
        }

        EnsureLayerSettings();
        SyncLegacyLayerVisibility();
    }

    private void EnsureLayerSettings()
    {
        if (layerSettings == null)
        {
            layerSettings = new List<MapEditorLayerSetting>();
        }

        int expectedCount = System.Enum.GetValues(typeof(MapEditorLayerType)).Length;
        bool hasEveryLayer = layerSettings.Count == expectedCount;

        if (hasEveryLayer)
        {
            foreach (MapEditorLayerType layerType in System.Enum.GetValues(typeof(MapEditorLayerType)))
            {
                if (FindLayerSetting(layerType) == null)
                {
                    hasEveryLayer = false;
                    break;
                }
            }
        }

        if (hasEveryLayer)
        {
            return;
        }

        foreach (MapEditorLayerType layerType in System.Enum.GetValues(typeof(MapEditorLayerType)))
        {
            if (FindLayerSetting(layerType) == null)
            {
                layerSettings.Add(new MapEditorLayerSetting(layerType, GetDefaultLayerName(layerType), GetLegacyLayerVisibility(layerType)));
            }
        }

        layerSettings.Sort((left, right) => left.layer.CompareTo(right.layer));
    }

    private MapEditorLayerSetting FindLayerSetting(MapEditorLayerType layerType)
    {
        if (layerSettings == null)
        {
            return null;
        }

        int layerValue = (int)layerType;

        for (int i = 0; i < layerSettings.Count; i++)
        {
            if (layerSettings[i] != null && layerSettings[i].layer == layerValue)
            {
                return layerSettings[i];
            }
        }

        return null;
    }

    private bool GetLegacyLayerVisibility(MapEditorLayerType layerType)
    {
        switch (layerType)
        {
            case MapEditorLayerType.Object:
                return showObjectLayer;
            case MapEditorLayerType.WallVisual:
                return showWallVisualLayer;
            case MapEditorLayerType.WallCollision:
                return showWallCollisionLayer;
            case MapEditorLayerType.Zone:
                return showZoneLayer;
            default:
                return showGroundLayer;
        }
    }

    private void SyncLegacyLayerVisibility()
    {
        showGroundLayer = FindLayerSetting(MapEditorLayerType.Ground)?.visible ?? true;
        showObjectLayer = FindLayerSetting(MapEditorLayerType.Object)?.visible ?? true;
        showWallVisualLayer = FindLayerSetting(MapEditorLayerType.WallVisual)?.visible ?? true;
        showWallCollisionLayer = FindLayerSetting(MapEditorLayerType.WallCollision)?.visible ?? true;
        showZoneLayer = FindLayerSetting(MapEditorLayerType.Zone)?.visible ?? true;
    }

    private static string GetDefaultLayerName(MapEditorLayerType layerType)
    {
        switch (layerType)
        {
            case MapEditorLayerType.Ground:
                return "Ground";
            case MapEditorLayerType.Object:
                return "Object";
            case MapEditorLayerType.WallVisual:
                return "Wall";
            case MapEditorLayerType.WallCollision:
                return "Collision";
            case MapEditorLayerType.Spawn:
                return "Spawn";
            case MapEditorLayerType.Zone:
                return "Zone";
            default:
                return layerType.ToString();
        }
    }

    private void CancelTransientToolState()
    {
        ResetPaintStroke();
        mapEditing?.ClearPendingPaintGesture();
        selectionClipboard?.CancelActiveDrag();
        previewDragStart = null;
    }

    public void SelectColor(Color color)
    {
        CancelTransientToolState();
        EnsureBrushSelectionService();
        brushSelection.SelectColor(this, color);

        if (colorWheelWindow != null)
        {
            colorWheelWindow.SetColor(color, false);
        }

        UpdateBrushPreview();
        UpdateBrushCursorPreview();
        Debug.Log("선택한 색상: " + ColorUtility.ToHtmlStringRGBA(color));
    }

    public void SelectImageBrush(Sprite sprite)
    {
        SelectImageBrush(sprite, string.Empty, -1);
    }

    public void SelectImageBrush(Sprite sprite, string imagePath, int imageIndex)
    {
        SelectImageBrush(sprite, imagePath, imageIndex, 0, false, false);
    }

    public void SelectImageBrush(Sprite sprite, string imagePath, int imageIndex, int rotation, bool flipX, bool flipY)
    {
        ClearSelectedTileRegion();
        CancelTransientToolState();
        EnsureBrushSelectionService();

        if (!brushSelection.SelectImageBrush(this, sprite, imagePath, imageIndex, rotation, flipX, flipY))
        {
            return;
        }

        if (MapEditorTilesetLibraryService.TryGetByAtlasPath(imagePath, out MapEditorTilesetDefinition tileset))
        {
            activeLayer = tileset.defaultCollision ? MapEditorLayerType.WallCollision : tileset.defaultLayer;
            useWallTileBrush = tileset.defaultCollision || activeLayer == MapEditorLayerType.WallCollision;
        }

        if (useWallTileBrush)
        {
            if (EditorToolController.Instance != null)
            {
                EditorToolController.Instance.SetWallTool();
            }

            RefreshToolToolbarSelection();
            UpdateBrushCursorPreview();
        }
        else
        {
            SetBrushTool();
        }

        UpdateBrushPreview();

        if (colorWheelWindow != null && !string.IsNullOrEmpty(imagePath))
        {
            colorWheelWindow.SelectPngTile(imagePath, imageIndex);
        }

        Debug.Log("선택한 이미지 브러시: " + sprite.name);
    }

    public void SelectImageTileRegion(
        Sprite previewSprite,
        string imagePath,
        int gridSize,
        int startX,
        int startYFromTop,
        int width,
        int height)
    {
        SelectImageBrush(previewSprite, imagePath, -1);
        selectedRegionGridSize = NormalizePngPaletteGridSize(gridSize);
        selectedRegionStartX = Mathf.Clamp(startX, 0, selectedRegionGridSize - 1);
        selectedRegionStartYFromTop = Mathf.Clamp(startYFromTop, 0, selectedRegionGridSize - 1);
        selectedRegionWidth = Mathf.Clamp(width, 1, selectedRegionGridSize - selectedRegionStartX);
        selectedRegionHeight = Mathf.Clamp(height, 1, selectedRegionGridSize - selectedRegionStartYFromTop);
        UpdateBrushPreview();
        UpdateBrushCursorPreview();
    }

    private bool HasSelectedTileRegion()
    {
        return selectedImageBrush != null
            && !string.IsNullOrEmpty(selectedImagePath)
            && selectedRegionGridSize > 0
            && (selectedRegionWidth > 1 || selectedRegionHeight > 1);
    }

    private void ClearSelectedTileRegion()
    {
        selectedRegionGridSize = 0;
        selectedRegionStartX = 0;
        selectedRegionStartYFromTop = 0;
        selectedRegionWidth = 1;
        selectedRegionHeight = 1;
    }

    private void PaintSelectedTileRegion(GridCell centerCell)
    {
        int outputWidth = GetSelectedRegionOutputWidth();
        int outputHeight = GetSelectedRegionOutputHeight();
        int startMapX = centerCell.X - outputWidth / 2;
        int startMapY = centerCell.Y - outputHeight / 2;
        MapEditorPaintSelection selection = GetPaintSelection();
        selection.brushSize = 1;

        for (int y = 0; y < outputHeight; y++)
        {
            for (int x = 0; x < outputWidth; x++)
            {
                if (!cells.TryGetValue(new Vector2Int(startMapX + x, startMapY + y), out GridCell targetCell))
                {
                    continue;
                }

                GetSelectedRegionSourceCoordinate(x, y, out int localSourceX, out int localSourceYFromTop);
                int sourceX = selectedRegionStartX + localSourceX;
                int sourceYFromTop = selectedRegionStartYFromTop + localSourceYFromTop;
                int sourceY = selectedRegionGridSize - 1 - sourceYFromTop;
                int baseIndex = sourceY * selectedRegionGridSize + sourceX;
                int imageIndex = MapEditorPngTilesetService.EncodePaletteTileIndex(selectedRegionGridSize, baseIndex);
                Sprite sprite = GetPngTileSprite(
                    selectedImagePath,
                    imageIndex,
                    selectedImageRotation,
                    selectedImageFlipX,
                    selectedImageFlipY);

                if (sprite == null)
                {
                    continue;
                }

                selection.selectedImageBrush = sprite;
                selection.selectedImageIndex = imageIndex;
                mapEditing.PaintCell(targetCell, selection);
            }
        }
    }

    private int GetSelectedRegionOutputWidth()
    {
        return MapEditorBrushGeometry.GetRotatedSize(selectedRegionWidth, selectedRegionHeight, selectedImageRotation).x;
    }

    private int GetSelectedRegionOutputHeight()
    {
        return MapEditorBrushGeometry.GetRotatedSize(selectedRegionWidth, selectedRegionHeight, selectedImageRotation).y;
    }

    private void GetSelectedRegionSourceCoordinate(
        int outputX,
        int outputY,
        out int sourceX,
        out int sourceY)
    {
        Vector2Int source = MapEditorBrushGeometry.MapOutputToSource(
            outputX,
            outputY,
            selectedRegionWidth,
            selectedRegionHeight,
            selectedImageRotation,
            selectedImageFlipX,
            selectedImageFlipY);
        sourceX = source.x;
        sourceY = source.y;
    }

    public void UseCurrentTool(GridCell cell)
    {
        UseCurrentTool(cell, -1, -1);
    }

    public void UseCurrentTool(GridCell cell, int subPixelX, int subPixelY)
    {
        if (EditorToolController.Instance == null)
        {
            return;
        }

        if (EditorToolController.Instance.CurrentTool == EditorToolType.Selection
            || EditorToolController.Instance.CurrentTool == EditorToolType.PreviewRegion)
        {
            return;
        }

        if ((EditorToolController.Instance.CurrentTool == EditorToolType.Brush || EditorToolController.Instance.CurrentTool == EditorToolType.Wall) && IsAreaFillModifierPressed())
        {
            ResetPaintStroke();
            MapEditorPaintSelection selection = EditorToolController.Instance.CurrentTool == EditorToolType.Wall
                ? GetWallPaintSelection()
                : GetPaintSelection();
            mapEditing.HandleAreaFill(cell, selection);
            return;
        }

        EditorToolType tool = EditorToolController.Instance.CurrentTool;

        if (tool == EditorToolType.Brush || tool == EditorToolType.Wall || tool == EditorToolType.Eraser)
        {
            PaintInterpolatedStroke(cell, subPixelX, subPixelY, tool);
            return;
        }

        UseCurrentToolAt(cell, subPixelX, subPixelY, tool);
    }

    private void PaintInterpolatedStroke(GridCell cell, int subPixelX, int subPixelY, EditorToolType tool)
    {
        if (cell == null)
        {
            return;
        }

        int resolution = UsesSubPixelStroke(tool, subPixelX, subPixelY) ? MaxExportCellPixels : 1;
        int localX = resolution == 1 ? 0 : Mathf.Clamp(subPixelX, 0, resolution - 1);
        int localY = resolution == 1 ? 0 : Mathf.Clamp(subPixelY, 0, resolution - 1);
        Vector2Int current = new Vector2Int(cell.X * resolution + localX, cell.Y * resolution + localY);

        if (!hasPaintStrokeSample || lastPaintStrokeTool != tool || lastPaintStrokeResolution != resolution)
        {
            PaintStrokePoint(current, resolution, tool);
        }
        else
        {
            PaintStrokeLine(lastPaintStrokePoint, current, resolution, tool);
        }

        hasPaintStrokeSample = true;
        lastPaintStrokePoint = current;
        lastPaintStrokeResolution = resolution;
        lastPaintStrokeTool = tool;
    }

    private bool UsesSubPixelStroke(EditorToolType tool, int subPixelX, int subPixelY)
    {
        return tool == EditorToolType.Brush
            && !paintWholeTile
            && !useWallTileBrush
            && subPixelX >= 0
            && subPixelY >= 0;
    }

    private void PaintStrokeLine(Vector2Int start, Vector2Int end, int resolution, EditorToolType tool)
    {
        MapEditorBrushGeometry.RasterizeLine(start, end, point => PaintStrokePoint(point, resolution, tool));
    }

    private void PaintStrokePoint(Vector2Int point, int resolution, EditorToolType tool)
    {
        int mapX = Mathf.FloorToInt(point.x / (float)resolution);
        int mapY = Mathf.FloorToInt(point.y / (float)resolution);

        if (!cells.TryGetValue(new Vector2Int(mapX, mapY), out GridCell targetCell))
        {
            return;
        }

        int localX = resolution == 1 ? -1 : point.x % resolution;
        int localY = resolution == 1 ? -1 : point.y % resolution;
        UseCurrentToolAt(targetCell, localX, localY, tool);
    }

    private void UseCurrentToolAt(GridCell cell, int subPixelX, int subPixelY, EditorToolType tool)
    {

        switch (tool)
        {
            case EditorToolType.Brush:
                if (paintWholeTile)
                {
                    if (HasSelectedTileRegion())
                    {
                        PaintSelectedTileRegion(cell);
                    }
                    else
                    {
                        mapEditing.PaintCell(cell, GetPaintSelection());
                    }
                    break;
                }

                if (!useWallTileBrush && selectedImageBrush == null && useSelectedColor && subPixelX >= 0 && subPixelY >= 0)
                {
                    mapEditing.PaintSubPixelArea(
                        cell,
                        subPixelX,
                        subPixelY,
                        MaxExportCellPixels,
                        GetSubPixelBrushSide(),
                        selectedColor);
                    break;
                }

                if (!useWallTileBrush && selectedImageBrush != null && subPixelX >= 0 && subPixelY >= 0)
                {
                    mapEditing.PaintSpriteAtSubPixel(cell, subPixelX, subPixelY, MaxExportCellPixels, selectedImageBrush);
                    break;
                }

                brushTool.Use(cell);
                break;
            case EditorToolType.Wall:
                mapEditing.PaintCell(cell, GetWallPaintSelection());
                break;

            case EditorToolType.Eraser:
                eraserTool.Use(cell);
                break;

            case EditorToolType.Spawn:
                SetPixelChromaSpawnAtCell(cell);
                break;
        }
    }

    private void ResetPaintStroke()
    {
        hasPaintStrokeSample = false;
        lastPaintStrokeResolution = 0;
    }

    public void ClearSelectedImageBrush()
    {
        ClearSelectedTileRegion();
        CancelTransientToolState();
        EnsureBrushSelectionService();
        brushSelection.ClearImageBrush(this);
        UpdateBrushPreview();
        UpdateBrushCursorPreview();
    }

    public void PaintCell(GridCell cell)
    {
        mapEditing.PaintCell(cell, GetPaintSelection());
    }

    public void EraseCell(GridCell cell)
    {
        mapEditing.EraseCell(cell, brushSize);
    }

    public void PickColorUnderMouse()
    {
        eyedropperService.PickUnderMouse(this);
    }

    public void ClearMap()
    {
        ClearSelection();
        CurrentMapData.Clear();
        mapEditing.ClearHistory();
        RefreshAllCells();
        RefreshMinimap();
    }

    public void ClearActiveLayer()
    {
        ClearSelection();
        mapEditing.ClearLayer(activeLayer);
        RefreshAllCells();
        Debug.Log("Cleared layer: " + GetLayerDisplayName(activeLayer));
    }

    public void ChangeBrushSize(int delta)
    {
        EnsureBrushSelectionService();
        brushSelection.ChangeBrushSize(this, delta);
        UpdateBrushPreview();
        UpdateBrushCursorPreview();
    }

    public void RotateSelectedImageBrush()
    {
        EnsureBrushSelectionService();

        if (!brushSelection.RotateSelectedImageBrush(this))
        {
            return;
        }

        UpdateBrushPreview();
        UpdateBrushCursorPreview();
    }

    public void FlipSelectedImageBrushHorizontal()
    {
        EnsureBrushSelectionService();

        if (!brushSelection.FlipSelectedImageBrushHorizontal(this))
        {
            return;
        }

        UpdateBrushPreview();
        UpdateBrushCursorPreview();
    }

    public void FlipSelectedImageBrushVertical()
    {
        EnsureBrushSelectionService();

        if (!brushSelection.FlipSelectedImageBrushVertical(this))
        {
            return;
        }

        UpdateBrushPreview();
        UpdateBrushCursorPreview();
    }

    public void CreateNewMap()
    {
        CreateNewMap(mapWidth, mapHeight);
    }

    public void CreateNewMap(int width, int height)
    {
        previewRegion = null;
        CurrentMapData = mapSizeService.CreateNewMap(this, width, height, ClearSelection, mapEditing.ClearHistory, RegenerateGrid);
    }

    public void ResizeMap(int width, int height)
    {
        ResizeMap(width, height, true);
    }

    public void ResizeMap(int width, int height, bool refreshToolbar)
    {
        if (!mapSizeService.TryResizeMap(
            this,
            CurrentMapData,
            width,
            height,
            MaxMapSize,
            ClearSelection,
            mapEditing.ClearHistory,
            RegenerateGrid,
            RefreshMinimap,
            out MapData resizedMapData))
        {
            return;
        }

        CurrentMapData = resizedMapData;
        ClampPreviewRegionToMap();

        if (refreshToolbar && createToolToolbar)
        {
            CreateToolToolbar();
        }

        RefreshSpawnMarker();
    }

    public void Undo()
    {
        mapEditing.Undo();
    }

    public void Redo()
    {
        mapEditing.Redo();
    }

    public void BeginEditTransaction()
    {
        ResetPaintStroke();
        mapEditing.BeginTransaction();
    }

    public void CommitEditTransaction()
    {
        ResetPaintStroke();
        mapEditing.CommitTransaction();
    }

    public void SaveMap()
    {
        EnsureSpawnPointList();
        mapSaveService.SetImportedTilesets(EnsureTilesetLibrary().GetDefinitionsForSave());
        mapSaveService.SetLayerSettings(GetLayerSettingsForSave());
        mapSaveService.SetPreviewRegion(previewRegion);
        mapSaveService.Save(CurrentMapData, pngFiles.CurrentPath, pixelChromaSpawnX, pixelChromaSpawnY, GetSpawnPointsForSave());
    }

    public void SaveMap(string fileName)
    {
        EnsureSpawnPointList();
        mapSaveService.SetImportedTilesets(EnsureTilesetLibrary().GetDefinitionsForSave());
        mapSaveService.SetLayerSettings(GetLayerSettingsForSave());
        mapSaveService.SetPreviewRegion(previewRegion);
        mapSaveService.Save(CurrentMapData, pngFiles.CurrentPath, pixelChromaSpawnX, pixelChromaSpawnY, GetSpawnPointsForSave(), fileName);
    }

    public void LoadMap()
    {
        if (mapSaveService.Load(out MapSaveData saveData, out string path))
        {
            ApplyLoadedMap(saveData, path);
        }
    }

    public void ImportPixelChromaMap()
    {
        if (pixelChromaImportService.ImportWithDialog(out MapSaveData saveData, out string path))
        {
            ApplyLoadedMap(saveData, path);
        }
    }

    public void LoadMap(string fileName)
    {
        if (mapSaveService.Load(fileName, out MapSaveData saveData, out string path))
        {
            ApplyLoadedMap(saveData, path);
        }
    }

    private void ApplyLoadedMap(MapSaveData saveData, string path)
    {
        ApplyLayerSettings(saveData.layerSettings);

        if (saveData.importedTilesets != null && saveData.importedTilesets.Length > 0)
        {
            EnsureTilesetLibrary().ReplaceDefinitions(saveData.importedTilesets);
        }
        CurrentMapData = mapLoadApplyService.Apply(
            this,
            saveData,
            path,
            ClearSelection,
            mapEditing.ClearHistory,
            RegenerateGrid,
            RefreshAllCells,
            RefreshMinimap,
            LoadPngPalette,
            CreateToolToolbar,
            pngFiles
        );

        pixelChromaSpawnX = Mathf.Clamp(saveData.spawnX, 0, mapWidth - 1);
        pixelChromaSpawnY = Mathf.Clamp(saveData.spawnY, 0, mapHeight - 1);
        previewRegion = saveData.previewWidth > 0 && saveData.previewHeight > 0
            ? new RectInt(saveData.previewX, saveData.previewY, saveData.previewWidth, saveData.previewHeight)
            : (RectInt?)null;
        ClampPreviewRegionToMap();
        LoadSpawnPoints(saveData);
        RefreshSpawnMarker();
    }

    public bool IsSelectionToolActive()
    {
        return EditorToolController.Instance != null && EditorToolController.Instance.CurrentTool == EditorToolType.Selection;
    }

    public bool IsPreviewRegionToolActive()
    {
        return EditorToolController.Instance != null && EditorToolController.Instance.CurrentTool == EditorToolType.PreviewRegion;
    }

    public bool IsPointerDragToolActive()
    {
        return IsSelectionToolActive() || IsPreviewRegionToolActive();
    }

    public void CopySelection()
    {
        selectionClipboard.CopySelection();
    }

    public void CutSelection()
    {
        selectionClipboard.CutSelection();
    }

    public void PasteClipboardAtHoveredCell()
    {
        selectionClipboard.PasteClipboardAtCurrentTarget();
    }

    public void PasteLoadedPngToMap()
    {
        const int pngTileGridSize = 16;
        MapEditorClipboard pngClipboard = pngFiles.CreateCurrentPaletteClipboard();

        if (pngClipboard == null)
        {
            return;
        }

        Vector2Int topLeft = selectionClipboard.GetPasteTopLeft();
        EnsureMapContainsRect(topLeft, pngTileGridSize, pngTileGridSize);

        mapEditing.PasteClipboard(topLeft, pngClipboard);
        selectionClipboard.SetSelectionRect(new RectInt(topLeft.x, topLeft.y, pngTileGridSize, pngTileGridSize));
        Canvas.ForceUpdateCanvases();
        RefreshAllCells();
        ConfigureMapViewportVisual();
        UpdateBrushCursorPreview();
        RefreshMinimap();
        Debug.Log("불러온 PNG를 맵에 붙여넣었습니다: " + pngFiles.CurrentPath + " / 위치 " + topLeft + " / 크기 " + pngTileGridSize + "x" + pngTileGridSize);
    }

    public void SetPixelChromaSpawnAtHoveredCell()
    {
        SetSpawnTool();
    }

    private void SetPixelChromaSpawnAtCell(GridCell cell)
    {
        if (cell == null)
        {
            return;
        }

        EnsureSpawnPointList();
        int existingIndex = FindSpawnPointIndex(cell.X, cell.Y);

        if (existingIndex >= 0)
        {
            pixelChromaSpawnPoints.RemoveAt(existingIndex);
            Debug.Log("PixelChroma 시작 위치 삭제: " + cell.X + ", " + cell.Y);
        }
        else
        {
            pixelChromaSpawnPoints.Add(new MapEditorSpawnPointData(GetNextSpawnPointId(), cell.X, cell.Y, "Any"));
            Debug.Log("PixelChroma 시작 위치 추가: " + cell.X + ", " + cell.Y);
        }

        SyncPrimarySpawnPoint();
        RefreshSpawnMarker();
    }

    private void EnsureSpawnPointList()
    {
        if (pixelChromaSpawnPoints == null)
        {
            pixelChromaSpawnPoints = new List<MapEditorSpawnPointData>();
        }

        if (pixelChromaSpawnPoints.Count == 0)
        {
            pixelChromaSpawnPoints.Add(new MapEditorSpawnPointData("SpawnPoint_1", Mathf.Clamp(pixelChromaSpawnX, 0, mapWidth - 1), Mathf.Clamp(pixelChromaSpawnY, 0, mapHeight - 1), "Any"));
        }

        SyncPrimarySpawnPoint();
    }

    private void LoadSpawnPoints(MapSaveData saveData)
    {
        pixelChromaSpawnPoints.Clear();

        if (saveData.spawnPoints != null)
        {
            for (int i = 0; i < saveData.spawnPoints.Length; i++)
            {
                MapEditorSpawnPointData spawnPoint = saveData.spawnPoints[i];

                if (spawnPoint == null)
                {
                    continue;
                }

                pixelChromaSpawnPoints.Add(new MapEditorSpawnPointData(
                    string.IsNullOrEmpty(spawnPoint.id) ? "SpawnPoint_" + (pixelChromaSpawnPoints.Count + 1) : spawnPoint.id,
                    Mathf.Clamp(spawnPoint.x, 0, mapWidth - 1),
                    Mathf.Clamp(spawnPoint.y, 0, mapHeight - 1),
                    spawnPoint.role
                ));
            }
        }

        EnsureSpawnPointList();
    }

    private MapEditorSpawnPointData[] GetSpawnPointsForSave()
    {
        EnsureSpawnPointList();
        MapEditorSpawnPointData[] spawnPoints = new MapEditorSpawnPointData[pixelChromaSpawnPoints.Count];

        for (int i = 0; i < pixelChromaSpawnPoints.Count; i++)
        {
            MapEditorSpawnPointData spawnPoint = pixelChromaSpawnPoints[i];
            spawnPoints[i] = new MapEditorSpawnPointData(
                string.IsNullOrEmpty(spawnPoint.id) ? "SpawnPoint_" + (i + 1) : spawnPoint.id,
                Mathf.Clamp(spawnPoint.x, 0, mapWidth - 1),
                Mathf.Clamp(spawnPoint.y, 0, mapHeight - 1),
                spawnPoint.role
            );
        }

        return spawnPoints;
    }

    private void SyncPrimarySpawnPoint()
    {
        if (pixelChromaSpawnPoints == null || pixelChromaSpawnPoints.Count == 0)
        {
            pixelChromaSpawnX = Mathf.Clamp(pixelChromaSpawnX, 0, mapWidth - 1);
            pixelChromaSpawnY = Mathf.Clamp(pixelChromaSpawnY, 0, mapHeight - 1);
            return;
        }

        MapEditorSpawnPointData primary = pixelChromaSpawnPoints[0];
        primary.x = Mathf.Clamp(primary.x, 0, mapWidth - 1);
        primary.y = Mathf.Clamp(primary.y, 0, mapHeight - 1);
        pixelChromaSpawnX = primary.x;
        pixelChromaSpawnY = primary.y;
    }

    private int FindSpawnPointIndex(int x, int y)
    {
        for (int i = 0; i < pixelChromaSpawnPoints.Count; i++)
        {
            MapEditorSpawnPointData spawnPoint = pixelChromaSpawnPoints[i];

            if (spawnPoint != null && spawnPoint.x == x && spawnPoint.y == y)
            {
                return i;
            }
        }

        return -1;
    }

    private string GetNextSpawnPointId()
    {
        return "SpawnPoint_" + (pixelChromaSpawnPoints.Count + 1);
    }

    private void EnsureMapContainsRect(Vector2Int topLeft, int width, int height)
    {
        mapSizeService.EnsureMapContainsRect(this, topLeft, width, height, (nextWidth, nextHeight) => ResizeMap(nextWidth, nextHeight, true));
    }

    public void ClearSelection()
    {
        selectionClipboard.ClearSelection();
    }

    public void BeginSelectionDrag(GridCell cell)
    {
        selectionClipboard.BeginSelectionDrag(cell);
    }

    public void BeginPointerDrag(GridCell cell)
    {
        if (IsPreviewRegionToolActive())
        {
            BeginPreviewRegionDrag(cell);
            return;
        }

        BeginSelectionDrag(cell);
    }

    public void UpdateSelectionDrag(GridCell cell)
    {
        selectionClipboard.UpdateSelectionDrag(cell);
    }

    public void UpdatePointerDrag(GridCell cell)
    {
        if (IsPreviewRegionToolActive())
        {
            UpdatePreviewRegionDrag(cell);
            return;
        }

        UpdateSelectionDrag(cell);
    }

    public void EndSelectionDrag(GridCell cell)
    {
        selectionClipboard.EndSelectionDrag(cell);
    }

    public void EndPointerDrag(GridCell cell)
    {
        if (previewDragStart.HasValue)
        {
            EndPreviewRegionDrag(cell);
            return;
        }

        EndSelectionDrag(cell);
    }

    private void BeginPreviewRegionDrag(GridCell cell)
    {
        if (cell == null)
        {
            return;
        }

        previewDragStart = new Vector2Int(cell.X, cell.Y);
        previewRegion = new RectInt(cell.X, cell.Y, 1, 1);
        UpdateBrushCursorPreview();
    }

    private void UpdatePreviewRegionDrag(GridCell cell)
    {
        if (!previewDragStart.HasValue || cell == null || CurrentMapData == null)
        {
            return;
        }

        Vector2Int start = previewDragStart.Value;
        int minX = Mathf.Clamp(Mathf.Min(start.x, cell.X), 0, CurrentMapData.width - 1);
        int minY = Mathf.Clamp(Mathf.Min(start.y, cell.Y), 0, CurrentMapData.height - 1);
        int maxX = Mathf.Clamp(Mathf.Max(start.x, cell.X), 0, CurrentMapData.width - 1);
        int maxY = Mathf.Clamp(Mathf.Max(start.y, cell.Y), 0, CurrentMapData.height - 1);
        previewRegion = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        UpdateBrushCursorPreview();
    }

    private void EndPreviewRegionDrag(GridCell cell)
    {
        if (!previewDragStart.HasValue)
        {
            return;
        }

        UpdatePreviewRegionDrag(cell);
        previewDragStart = null;
        UpdateBrushCursorPreview();

        if (previewRegion.HasValue)
        {
            RectInt region = previewRegion.Value;
            Debug.Log("맵 프리뷰 영역을 지정했습니다: " + region.width + "x" + region.height);
        }
    }

    private void ClampPreviewRegionToMap()
    {
        if (!previewRegion.HasValue || CurrentMapData == null)
        {
            return;
        }

        RectInt region = previewRegion.Value;
        int x = Mathf.Clamp(region.x, 0, CurrentMapData.width - 1);
        int y = Mathf.Clamp(region.y, 0, CurrentMapData.height - 1);
        int width = Mathf.Clamp(region.width, 1, CurrentMapData.width - x);
        int height = Mathf.Clamp(region.height, 1, CurrentMapData.height - y);
        previewRegion = new RectInt(x, y, width, height);
    }

    public void RegisterCell(GridCell cell)
    {
        if (cell == null)
        {
            return;
        }

        cells[new Vector2Int(cell.X, cell.Y)] = cell;
        cell.SetSpawnMarkerVisible(IsSpawnPointAt(cell.X, cell.Y));
    }

    public void ClearRegisteredCells()
    {
        cells.Clear();
    }

    private GridCell GetRegisteredCell(int x, int y)
    {
        cells.TryGetValue(new Vector2Int(x, y), out GridCell cell);
        return cell;
    }

    public void SetHoveredCell(GridCell cell)
    {
        SetHoveredCell(cell, 0, 0);
    }

    public void SetHoveredCell(GridCell cell, int subPixelX, int subPixelY)
    {
        hoveredCell = cell;
        hoveredSubPixelX = subPixelX;
        hoveredSubPixelY = subPixelY;
        UpdateBrushCursorPreview();
    }

    public void ClearHoveredCell(GridCell cell)
    {
        if (hoveredCell == cell)
        {
            hoveredCell = null;
            UpdateBrushCursorPreview();
        }
    }

    public void ZoomMap(float direction)
    {
        EnsureViewportService();
        viewportService.ZoomMap(direction);
    }

    public void PanMap(Vector2 delta)
    {
        EnsureViewportService();
        viewportService.PanMap(delta);
    }

    public void RefreshCell(GridCell cell)
    {
        mapEditing.RefreshCell(cell);
    }

    public void RefreshAllCells()
    {
        mapEditing.RefreshAllCells();
        RefreshSpawnMarker();
    }

    public Color GetPreviewColor(int x, int y)
    {
        return mapEditing.GetPreviewColor(x, y);
    }

    public void CenterMapOnNormalizedPosition(float normalizedX, float normalizedY)
    {
        EnsureViewportService();
        viewportService.CenterMapOnNormalizedPosition(normalizedX, normalizedY);
    }

    public void ClampMapToViewport()
    {
        EnsureViewportService();
        viewportService.ClampMapToViewport();
    }

    public bool TryGetMapViewNormalizedRect(out Rect normalizedRect)
    {
        EnsureViewportService();
        return viewportService.TryGetMapViewNormalizedRect(out normalizedRect);
    }

    private bool IsAreaFillModifierPressed()
    {
        return MapEditorInputService.IsAreaFillModifierPressed();
    }

    private MapEditorPaintSelection GetWallPaintSelection()
    {
        MapEditorPaintSelection selection = GetPaintSelection();
        selection.useWallTileBrush = true;
        selection.useSelectedColor = true;
        return selection;
    }

    private RectInt? GetSelectionPreviewRect()
    {
        if (IsPreviewRegionToolActive() && previewRegion.HasValue)
        {
            return previewRegion;
        }

        return selectionClipboard == null ? null : selectionClipboard.SelectionPreviewRect;
    }

    private void CreateToolToolbar()
    {
        toolbarState.EnsureToolbar(this, toolToolbarOffset, pngFiles.GetRecentPaths());
        RefreshToolToolbarSelection();
        UpdateBrushPreview();
    }

    public void RefreshLocalizedUi()
    {
        CreateToolToolbar();

        if (colorWheelWindow != null)
        {
            colorWheelWindow.RefreshLocalizedText();
        }
    }

    private void ConfigureMapViewportVisual()
    {
        if (gridGenerator == null)
        {
            gridGenerator = GetComponent<GridGenerator>();
        }

        MapEditorSceneSetupService.ConfigureMapViewportVisual(gridGenerator);
    }

    private void CreateMinimap()
    {
        Vector2 dockedOffset = toolToolbarOffset + new Vector2(-186f, -140f);
        Vector2 dockedSize = new Vector2(
            Mathf.Clamp(minimapSize.x, 96f, 132f),
            Mathf.Clamp(minimapSize.y, 96f, 132f)
        );

        minimap = MapEditorSceneUiBuilder.EnsureMinimap(this, dockedOffset, dockedSize);
    }

    private void UpdateBrushCursorPreview()
    {
        RectInt? areaFillPreviewRect = null;

        if (IsAreaFillModifierPressed() && mapEditing.TryGetAreaFillRect(hoveredCell, out RectInt rect))
        {
            areaFillPreviewRect = rect;
        }

        brushCursorPreview.Update(
            showBrushCursorPreview,
            gridGenerator,
            hoveredCell,
            CurrentMapData,
            brushSize,
            selectedColor,
            selectedImageBrush,
            selectedImageRotation,
            selectedImageFlipX,
            selectedImageFlipY,
            paintWholeTile,
            HasSelectedTileRegion() ? GetSelectedRegionOutputWidth() : 1,
            HasSelectedTileRegion() ? GetSelectedRegionOutputHeight() : 1,
            MaxExportCellPixels,
            GetSubPixelBrushSide(),
            hoveredSubPixelX,
            hoveredSubPixelY,
            EditorToolController.Instance != null
                && EditorToolController.Instance.CurrentTool == EditorToolType.Brush
                && !paintWholeTile
                && !useWallTileBrush
                && selectedImageBrush == null
                && useSelectedColor,
            brushCursorAlpha,
            areaFillPreviewRect,
            GetSelectionPreviewRect()
        );
    }

    public void LoadPngPalette()
    {
        pngFiles.SetPaletteGridSize(GetPngPaletteGridSize());
        colorWheelWindow = pngFiles.LoadPaletteWithDialog(this, colorWheelWindow, colorPaletteOffset, maxRecentPngFiles);
        RefreshRecentPngList();
    }

    public void OpenTilesetLibrary()
    {
        ImportPixelChromaDefaultTilesets();
#if UNITY_EDITOR
        MapEditorTilesetImporterWindow.Open(this);
#else
        string sourcePath = MapEditorFileDialog.OpenFile("16x16 타일셋 PNG 가져오기", "png");
        if (string.IsNullOrEmpty(sourcePath))
        {
            return;
        }

        MapEditorFileDialog.RememberDirectory(sourcePath);
        ImportTileset(
            sourcePath,
            System.IO.Path.GetFileNameWithoutExtension(sourcePath),
            MaxExportCellPixels,
            MaxExportCellPixels,
            0,
            0,
            MapEditorLayerType.Ground,
            false);
#endif
    }

    public void OpenTileCreator()
    {
        MapEditorTileCreatorWindow.Open(this);
    }

    public void ImportPixelChromaDefaultTilesets()
    {
        int imported = MapEditorDefaultTilesetService.ImportPixelChromaTilesets(EnsureTilesetLibrary());

        if (imported > 0)
        {
            Debug.Log("타일셋 버튼에서 새 기본 타일셋을 선택할 수 있습니다.");
        }

    }

    public bool ImportTileset(
        string sourcePath,
        string displayName,
        int tileWidth,
        int tileHeight,
        int margin,
        int spacing,
        MapEditorLayerType defaultLayer,
        bool collision)
    {
        if (!EnsureTilesetLibrary().Import(
            sourcePath,
            displayName,
            tileWidth,
            tileHeight,
            margin,
            spacing,
            defaultLayer,
            collision,
            out MapEditorTilesetDefinition definition,
            out string error))
        {
            Debug.LogError("타일셋 가져오기 실패: " + error);
            return false;
        }

        UseImportedTileset(definition.id);
        Debug.Log("타일셋을 가져왔습니다: " + definition.displayName + " (" + definition.columns + "x" + definition.rows + " 타일)");
        return true;
    }

    public void UseImportedTileset(string id)
    {
        MapEditorTilesetDefinition definition = EnsureTilesetLibrary().FindById(id);
        if (definition == null || !definition.IsUsable || !System.IO.File.Exists(definition.atlasPath))
        {
            Debug.LogWarning("가져온 타일셋을 사용할 수 없습니다: " + id);
            return;
        }

        SetPngPaletteGridSize(definition.atlasGridSize);
        LoadPngPalette(definition.atlasPath);
        SetActiveLayer(definition.defaultCollision ? MapEditorLayerType.WallCollision : definition.defaultLayer);
    }

    public void RemoveImportedTileset(string id)
    {
        if (EnsureTilesetLibrary().Remove(id))
        {
            Debug.Log("타일셋 보관함에서 삭제했습니다: " + id);
        }
    }

    public IReadOnlyList<MapEditorTilesetDefinition> GetImportedTilesets()
    {
        return EnsureTilesetLibrary().Definitions;
    }

    public void LoadPngPalette(string path)
    {
        pngFiles.SetPaletteGridSize(GetPngPaletteGridSize());
        colorWheelWindow = pngFiles.LoadPalette(this, colorWheelWindow, colorPaletteOffset, maxRecentPngFiles, path);
        RefreshRecentPngList();
    }

    public void SetPngPaletteGridSize(int gridSize)
    {
        pngPaletteGridSize = NormalizePngPaletteGridSize(gridSize);
        pngFiles.SetPaletteGridSize(pngPaletteGridSize);
        colorWheelWindow?.SetPngPaletteGridSize(pngPaletteGridSize);
    }

    public int GetPngPaletteGridSize()
    {
        pngPaletteGridSize = NormalizePngPaletteGridSize(pngPaletteGridSize);
        return pngPaletteGridSize;
    }

    public static int NormalizePngPaletteGridSize(int gridSize)
    {
        int selected = PngPaletteGridSizeOptions[0];
        int bestDistance = Mathf.Abs(gridSize - selected);

        for (int i = 1; i < PngPaletteGridSizeOptions.Length; i++)
        {
            int option = PngPaletteGridSizeOptions[i];
            int distance = Mathf.Abs(gridSize - option);

            if (distance < bestDistance)
            {
                selected = option;
                bestDistance = distance;
            }
        }

        return selected;
    }

    public void LoadRecentPngPalette(string path)
    {
        LoadPngPalette(path);
    }

    public void ExportMapPng()
    {
        pngFiles.ExportMapPngWithDialog(CurrentMapData, GetExportCellPixels(), exportEmptyCellsTransparent);
    }

    public void ExportMapPng(string path)
    {
        pngFiles.ExportMapPng(CurrentMapData, path, GetExportCellPixels(), exportEmptyCellsTransparent);
    }

    public void ValidatePixelChromaMap()
    {
        EnsureSpawnPointList();
        PixelChromaMapValidationReport report = MapEditorPixelChromaValidationService.Validate(
            CurrentMapData,
            pixelChromaSpawnX,
            pixelChromaSpawnY,
            GetSpawnPointsForSave()
        );

        MapEditorPixelChromaValidationService.Log(report);
        toolbarState.UpdateValidationStatus(report);
        MapEditorModalPanel.ShowValidation(this, report);

        string summary =
            "PixelChroma 맵 검사: " + (report.isValid ? "통과" : "수정 필요") +
            " | 그린 타일=" + report.paintedTileCount +
            " | 벽=" + report.wallTileCount +
            " | 색상=" + report.colorTileCount +
            " | 이미지=" + report.imageTileCount +
            " | 타일셋=" + report.tilesetCount +
            " | 시작 위치=" + report.spawnPointCount +
            " | 구역=" + report.zoneCount +
            " | 오류=" + report.errors.Count +
            " | 경고=" + report.warnings.Count;

        if (report.isValid)
        {
            Debug.Log(summary);
        }
        else
        {
            Debug.LogWarning(summary);
        }
    }

    public void ExportForPixelChroma()
    {
        EnsureSpawnPointList();
        pixelChromaExportService.ExportWithDialog(CurrentMapData, pixelChromaMapId, GetExportCellPixels(), pixelChromaSpawnX, pixelChromaSpawnY, GetSpawnPointsForSave());
    }

    public void ExportForPixelChroma(string path)
    {
        EnsureSpawnPointList();
        pixelChromaExportService.Export(CurrentMapData, path, pixelChromaMapId, GetExportCellPixels(), pixelChromaSpawnX, pixelChromaSpawnY, GetSpawnPointsForSave());
    }

    public void ExportWorkshopPackage()
    {
        ConfigureWorkshopPreview();
        EnsureSpawnPointList();
        workshopExportService.ExportWithDialog(
            CurrentMapData,
            pixelChromaMapId,
            workshopTitle,
            workshopAuthor,
            workshopDescription,
            requiredPixelChromaVersion,
            steamWorkshopVisibility,
            steamWorkshopTags,
            pixelChromaSpawnX,
            pixelChromaSpawnY,
            GetSpawnPointsForSave(),
            GetExportCellPixels(),
            exportEmptyCellsTransparent
        );
    }

    public void ShowPackageSaveGuide()
    {
        MapEditorModalPanel.ShowPackageGuide(this);
    }

    public void OpenSteamWorkshopPage()
    {
        string url = steamAppId == 0
            ? "https://steamcommunity.com/workshop/"
            : "https://steamcommunity.com/app/" + steamAppId + "/workshop/";
        Application.OpenURL(url);
    }

    public void ExportWorkshopPackage(string folderPath)
    {
        ConfigureWorkshopPreview();
        EnsureSpawnPointList();
        workshopExportService.Export(
            CurrentMapData,
            folderPath,
            pixelChromaMapId,
            workshopTitle,
            workshopAuthor,
            workshopDescription,
            requiredPixelChromaVersion,
            steamWorkshopVisibility,
            steamWorkshopTags,
            pixelChromaSpawnX,
            pixelChromaSpawnY,
            GetSpawnPointsForSave(),
            GetExportCellPixels(),
            exportEmptyCellsTransparent
        );
    }

    // ── 런타임 창작마당 업로드용 seam ──────────────────────────────
    // 왜 여기 있나: 맵 데이터/메타데이터/export 서비스가 전부 이 클래스의 private 이라,
    // 외부 업로더가 접근할 수 있는 유일한 통로를 여기서 공개한다.

    // 현재 맵을 창작마당 기준으로 검증한 리포트를 돌려준다(업로드 전 확인용).
    public PixelChromaMapValidationReport ValidateForWorkshop()
    {
        EnsureSpawnPointList();
        return MapEditorPixelChromaValidationService.Validate(
            CurrentMapData,
            pixelChromaSpawnX,
            pixelChromaSpawnY,
            GetSpawnPointsForSave());
    }

    // 현재 맵을 persistentDataPath 아래의 쓰기 가능한 폴더로 export 한다.
    // 성공 시 true, folderPath 에 그 폴더 경로가 담긴다.
    public bool ExportWorkshopPackageForUpload(out string folderPath)
    {
        ConfigureWorkshopPreview();
        EnsureSpawnPointList();

        // mapId 를 폴더 이름으로 쓰므로 파일명에 못 쓰는 문자를 '_' 로 치환한다.
        // 왜: "map/01" 같은 값이 들어오면 경로가 깨지기 때문.
        string safeId = string.IsNullOrWhiteSpace(pixelChromaMapId) ? "map" : pixelChromaMapId;
        foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
        {
            safeId = safeId.Replace(invalid, '_');
        }

        // 왜 persistentDataPath: 빌드에서 Assets/ 는 없고 StreamingAssets 는 읽기 전용.
        // 유저 PC에서 항상 쓰기 가능한 곳은 여기뿐이라 Steam 이 이 폴더를 업로드할 수 있다.
        folderPath = System.IO.Path.Combine(Application.persistentDataPath, "WorkshopUpload", safeId);

        // 기존에 이미 검증/파일생성/SHA256 까지 하는 서비스를 그대로 재사용한다.
        return workshopExportService.Export(
            CurrentMapData,
            folderPath,
            pixelChromaMapId,
            workshopTitle,
            workshopAuthor,
            workshopDescription,
            requiredPixelChromaVersion,
            steamWorkshopVisibility,
            steamWorkshopTags,
            pixelChromaSpawnX,
            pixelChromaSpawnY,
            GetSpawnPointsForSave(),
            GetExportCellPixels(),
            exportEmptyCellsTransparent);
    }

    public void SetExportCellPixels(int pixels)
    {
        paintWholeTile = false;
        exportCellPixels = NormalizeExportCellPixels(pixels);

        RefreshDotSizeControls();
    }

    public void SetWholeTilePaintMode()
    {
        paintWholeTile = true;
        RefreshDotSizeControls();
    }

    public bool IsWholeTilePaintMode()
    {
        return paintWholeTile;
    }

    private void RefreshDotSizeControls()
    {

        if (colorWheelWindow != null)
        {
            colorWheelWindow.RefreshExportCellSizeSelector();
        }

        if (createToolToolbar)
        {
            CreateToolToolbar();
        }
    }

    public int GetExportCellPixels()
    {
        exportCellPixels = NormalizeExportCellPixels(exportCellPixels);
        return exportCellPixels;
    }

    public int GetSubPixelBrushSide()
    {
        switch (GetExportCellPixels())
        {
            case 4:
                return 2;
            case 8:
                return 4;
            case 16:
                return 8;
            default:
                return 1;
        }
    }

    public static int NormalizeExportCellPixels(int pixels)
    {
        int selected = ExportCellPixelOptions[0];
        int bestDistance = Mathf.Abs(pixels - selected);

        for (int i = 1; i < ExportCellPixelOptions.Length; i++)
        {
            int option = ExportCellPixelOptions[i];
            int distance = Mathf.Abs(pixels - option);

            if (distance < bestDistance)
            {
                selected = option;
                bestDistance = distance;
            }
        }

        return selected;
    }

    private void RefreshRecentPngList()
    {
        toolbarState.RefreshRecentPngList(this, pngFiles.GetRecentPaths());
    }

    private void RefreshToolToolbarSelection()
    {
        toolbarState.RefreshToolSelection();
    }

    private Sprite GetPngTileSprite(string imagePath, int imageIndex)
    {
        return pngFiles.GetTileSprite(imagePath, imageIndex);
    }

    private Sprite GetPngTileSprite(string imagePath, int imageIndex, int rotation, bool flipX, bool flipY)
    {
        return pngFiles.GetTileSprite(imagePath, imageIndex, rotation, flipX, flipY);
    }

    private void RefreshSelectedImageBrush()
    {
        EnsureBrushSelectionService();
        brushSelection.RefreshSelectedImageBrush(this);
    }

    public void UpdateBrushPreview()
    {
        toolbarState.UpdateBrushPreview(selectedImageBrush, selectedColor, selectedImageRotation, brushSize, useWallTileBrush);
    }

    private void RegenerateGrid()
    {
        if (gridGenerator == null)
        {
            gridGenerator = GetComponent<GridGenerator>();
        }

        if (gridGenerator != null)
        {
            gridGenerator.GenerateGrid();
            RefreshMinimap();
            RefreshSpawnMarker();
            return;
        }

        RefreshAllCells();
    }

    private void RefreshSpawnMarker()
    {
        foreach (KeyValuePair<Vector2Int, GridCell> pair in cells)
        {
            GridCell cell = pair.Value;

            if (cell != null)
            {
                cell.SetSpawnMarkerVisible(IsSpawnPointAt(cell.X, cell.Y));
            }
        }
    }

    private bool IsSpawnPointAt(int x, int y)
    {
        EnsureSpawnPointList();
        return FindSpawnPointIndex(x, y) >= 0;
    }

    private void RefreshMinimap()
    {
        if (minimap != null)
        {
            minimap.Refresh();
        }
    }

    public void SyncMinimapView()
    {
        if (minimap != null)
        {
            minimap.UpdateViewRect();
        }
    }



}
