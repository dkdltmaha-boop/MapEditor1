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
    [SerializeField] private int activeCanvasIndex;
    [SerializeField] private MapEditorLayerType brushLayerRole = MapEditorLayerType.Ground;
    [SerializeField] private bool brushRoleMenuOpen;
    public bool showGroundLayer = true;
    public bool showObjectLayer = true;
    public bool showWallVisualLayer = true;
    public bool showWallCollisionLayer = true;
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
    public int maxRecentPngFiles = 10;

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
    public bool showPlayerScaleGuide;
    [SerializeField] private int playerScaleGuideX;
    [SerializeField] private int playerScaleGuideY;
    [SerializeField] private bool playerScaleGuidePositionInitialized;

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
    private readonly List<Vector2Int> linePreviewCells = new List<Vector2Int>();
    private readonly MapEditorPlayerScaleGuide playerScaleGuide = new MapEditorPlayerScaleGuide();
    private readonly MapEditorMapSaveService mapSaveService = new MapEditorMapSaveService(MaxMapSize);
    private readonly MapEditorPixelChromaImportService pixelChromaImportService = new MapEditorPixelChromaImportService();
    private readonly MapEditorPixelChromaExportService pixelChromaExportService = new MapEditorPixelChromaExportService();
    private MapEditorWorkshopExportService workshopExportService;
    private readonly MapEditorToolbarStateService toolbarState = new MapEditorToolbarStateService();
    private readonly MapEditorMapSizeService mapSizeService = new MapEditorMapSizeService();
    private readonly MapEditorMapLoadApplyService mapLoadApplyService = new MapEditorMapLoadApplyService();
    private readonly MapEditorEyedropperService eyedropperService = new MapEditorEyedropperService();
    private MapEditorBrushSelectionService brushSelection;
    private PixelChromaRuntimeWorkshopUploader subscribedWorkshopUploader;

    private GridGenerator gridGenerator;
    private MapEditorMapEditingService mapEditing;
    private MapEditorSelectionClipboardService selectionClipboard;
    private MapEditorViewportService viewportService;
    private MapEditorInputService inputService;
    private ColorWheelPickerWindow colorWheelWindow;
    private MapEditorMinimap minimap;
    private EditorToolController subscribedToolController;
    private GridCell hoveredCell;
    private GridCell coordinateProxy;
    private int hoveredSubPixelX;
    private int hoveredSubPixelY;
    private int selectedRegionGridSize;
    private int selectedRegionStartX;
    private int selectedRegionStartYFromTop;
    private int selectedRegionWidth = 1;
    private int selectedRegionHeight = 1;
    private string selectedAnimationPreviewPath = string.Empty;
    private int selectedAnimationPreviewIndex = int.MinValue;
    private int selectedAnimationPreviewRotation;
    private bool selectedAnimationPreviewFlipX;
    private bool selectedAnimationPreviewFlipY;
    private string selectedAnimationPreviewId = string.Empty;
    private int selectedAnimationPreviewFrameCount;
    private Sprite[] selectedAnimationPreviewFrames;
    private float selectedAnimationPreviewFps = 8f;
    private bool selectedAnimationPreviewLoop = true;
    private bool hasPaintStrokeSample;
    private Vector2Int lastPaintStrokePoint;
    private int lastPaintStrokeResolution;
    private EditorToolType lastPaintStrokeTool;
    private Vector2Int? previewDragStart;
    private Vector2Int? lineDragStart;
    private Vector2Int? lineDragEnd;
    private Vector2Int? rectangleFillDragStart;
    private Vector2Int? rectangleFillDragEnd;
    private RectInt? previewRegion;
    private MapEditorLayerType lastPaintLayer = MapEditorLayerType.Ground;

    public GridGenerator GridGenerator => gridGenerator;
    public MapEditorLayerType ActiveLayer => activeLayer;
    public int ActiveCanvasIndex => activeCanvasIndex;
    public MapEditorLayerType BrushLayerRole => brushLayerRole;
    public bool BrushRoleMenuOpen => brushRoleMenuOpen;
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
        NormalizeCanvasSelection();
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
        NormalizeCanvasSelection();
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
        UnsubscribeWorkshopUploader();

        if (coordinateProxy != null)
        {
            MapEditorObjectUtility.DestroyObject(coordinateProxy.gameObject);
            coordinateProxy = null;
        }
    }

    private void EnsureMapEditingService()
    {
        if (mapEditing != null)
        {
            return;
        }

        mapEditing = new MapEditorMapEditingService(
            () => CurrentMapData,
            () => activeLayer,
            IsLayerVisible,
            cells,
            GetPngTileSprite,
            RefreshMinimap,
            MarkVisualCellDirty,
            MarkAllVisualsDirty);
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
            () => activeLayer,
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
        bool isPaintTool = tool == EditorToolType.Brush
            || tool == EditorToolType.Line
            || tool == EditorToolType.RectangleFill
            || tool == EditorToolType.Wall;
        useWallTileBrush = isPaintTool
            && (tool == EditorToolType.Wall
                || brushLayerRole == MapEditorLayerType.WallCollision
                || activeLayer == MapEditorLayerType.WallCollision);

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
            useWallTileBrush = useWallTileBrush || activeLayer == MapEditorLayerType.WallCollision,
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
        colorWheelWindow?.RefreshLayout(colorPaletteOffset);
        MapEditorToolbarBuilder.RefreshLayout(toolToolbarOffset);
        MapEditorMapSizePanelBuilder.RefreshLayout(canvas.transform, toolToolbarOffset);
        MapEditorLayerPanelBuilder.RefreshLayout(toolToolbarOffset);
        ConfigureMapViewportVisual();
    }

    public void SetBrushTool()
    {
        CancelTransientToolState();
        ApplyBrushRoleToActiveLayer();
        useWallTileBrush = activeLayer == MapEditorLayerType.WallCollision;

        if (EditorToolController.Instance != null)
        {
            EditorToolController.Instance.SetBrushTool();
        }

        RefreshToolToolbarSelection();
        UpdateBrushPreview();
        UpdateBrushCursorPreview();
    }

    public void SetLineTool()
    {
        CancelTransientToolState();
        ApplyBrushRoleToActiveLayer();
        useWallTileBrush = activeLayer == MapEditorLayerType.WallCollision;

        if (EditorToolController.Instance != null)
        {
            EditorToolController.Instance.SetLineTool();
        }

        RefreshToolToolbarSelection();
        UpdateBrushPreview();
        UpdateBrushCursorPreview();
    }

    public void SetRectangleFillTool()
    {
        CancelTransientToolState();
        ApplyBrushRoleToActiveLayer();
        useWallTileBrush = activeLayer == MapEditorLayerType.WallCollision;

        if (EditorToolController.Instance != null)
        {
            EditorToolController.Instance.SetRectangleFillTool();
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
        brushLayerRole = MapEditorLayerType.WallCollision;

        if (EditorToolController.Instance != null)
        {
            EditorToolController.Instance.SetBrushTool();
        }

        CreateToolToolbar();
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

    public void SetBrushEraserTool()
    {
        SetEraserTool();
    }

    public void SetSelectionTool()
    {
        CancelTransientToolState();
        useWallTileBrush = false;
        RestoreLastPaintLayer();

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
        if (layerType == MapEditorLayerType.Zone)
        {
            layerType = MapEditorLayerType.Ground;
        }

        int canvasIndex = MapEditorLayerUtility.GetCanvasIndex(layerType);

        if (canvasIndex >= 0)
        {
            activeCanvasIndex = canvasIndex;
            brushLayerRole = MapEditorLayerUtility.GetBaseLayer(layerType);
        }

        if (!IsLayerEnabled(layerType))
        {
            return;
        }

        if (activeLayer != layerType)
        {
            selectionClipboard?.ClearSelection();
        }

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
            lastPaintLayer = layerType;

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

    private void RestoreLastPaintLayer()
    {
        if (activeLayer != MapEditorLayerType.WallCollision
            && activeLayer != MapEditorLayerType.Spawn)
        {
            lastPaintLayer = activeLayer;
            return;
        }

        if (!IsLayerEnabled(lastPaintLayer)
            || lastPaintLayer == MapEditorLayerType.WallCollision
            || lastPaintLayer == MapEditorLayerType.Spawn)
        {
            lastPaintLayer = MapEditorLayerType.Ground;
        }

        activeLayer = lastPaintLayer;
        toolbarState.RefreshLayerSelection();
    }

    public void ToggleBrushRoleMenu()
    {
        brushRoleMenuOpen = !brushRoleMenuOpen;
        CreateToolToolbar();
    }

    public void SetBrushLayerRole(MapEditorLayerType role)
    {
        role = MapEditorLayerUtility.GetBaseLayer(role);
        if (role != MapEditorLayerType.Ground
            && role != MapEditorLayerType.WallCollision)
        {
            role = MapEditorLayerType.Ground;
        }

        brushLayerRole = role;
        brushRoleMenuOpen = false;

        if (role == MapEditorLayerType.WallCollision)
        {
            SetWallTileTool();
            return;
        }

        activeLayer = MapEditorLayerUtility.GetCanvasLayer(activeCanvasIndex, role);
        lastPaintLayer = activeLayer;
        SetBrushTool();
        CreateToolToolbar();
    }

    private void ApplyBrushRoleToActiveLayer()
    {
        activeLayer = MapEditorLayerUtility.GetCanvasLayer(activeCanvasIndex, brushLayerRole);
        lastPaintLayer = activeLayer;
    }

    public bool IsCanvasEnabled(int canvasIndex)
    {
        if (canvasIndex <= 0) return true;
        if (canvasIndex >= MapEditorLayerUtility.CanvasLayerCount) return false;

        return IsLayerEnabled(MapEditorLayerUtility.GetCanvasLayer(canvasIndex, MapEditorLayerType.Ground))
            || IsLayerEnabled(MapEditorLayerUtility.GetCanvasLayer(canvasIndex, MapEditorLayerType.Object))
            || IsLayerEnabled(MapEditorLayerUtility.GetCanvasLayer(canvasIndex, MapEditorLayerType.WallVisual));
    }

    public bool IsCanvasVisible(int canvasIndex)
    {
        return IsLayerVisible(MapEditorLayerUtility.GetCanvasLayer(canvasIndex, MapEditorLayerType.Ground))
            || IsLayerVisible(MapEditorLayerUtility.GetCanvasLayer(canvasIndex, MapEditorLayerType.Object))
            || IsLayerVisible(MapEditorLayerUtility.GetCanvasLayer(canvasIndex, MapEditorLayerType.WallVisual));
    }

    public string GetCanvasDisplayName(int canvasIndex)
    {
        EnsureLayerSettings();
        MapEditorLayerSetting setting = FindLayerSetting(
            MapEditorLayerUtility.GetCanvasLayer(canvasIndex, MapEditorLayerType.Ground));
        string fallback = "레이어 " + (canvasIndex + 1);
        if (setting == null || string.IsNullOrWhiteSpace(setting.displayName)) return fallback;

        string legacyDefault = GetDefaultLayerName((MapEditorLayerType)setting.layer);
        return setting.displayName == legacyDefault ? fallback : setting.displayName;
    }

    public void SetCanvasDisplayName(int canvasIndex, string displayName)
    {
        EnsureLayerSettings();
        string name = string.IsNullOrWhiteSpace(displayName) ? "레이어 " + (canvasIndex + 1) : displayName.Trim();
        if (name.Length > 18) name = name.Substring(0, 18);

        foreach (MapEditorLayerType role in new[] { MapEditorLayerType.Ground, MapEditorLayerType.Object, MapEditorLayerType.WallVisual })
        {
            MapEditorLayerSetting setting = FindLayerSetting(MapEditorLayerUtility.GetCanvasLayer(canvasIndex, role));
            if (setting != null) setting.displayName = name;
        }
    }

    public void SetActiveCanvas(int canvasIndex)
    {
        canvasIndex = Mathf.Clamp(canvasIndex, 0, MapEditorLayerUtility.CanvasLayerCount - 1);
        if (!IsCanvasEnabled(canvasIndex)) return;

        if (activeCanvasIndex != canvasIndex) selectionClipboard?.ClearSelection();
        activeCanvasIndex = canvasIndex;

        if (brushLayerRole == MapEditorLayerType.WallCollision)
        {
            activeLayer = MapEditorLayerType.WallCollision;
        }
        else
        {
            activeLayer = MapEditorLayerUtility.GetCanvasLayer(activeCanvasIndex, brushLayerRole);
            lastPaintLayer = activeLayer;
        }

        toolbarState.RefreshLayerSelection();
        CreateToolToolbar();
    }

    public void ToggleCanvasVisible(int canvasIndex)
    {
        bool visible = !IsCanvasVisible(canvasIndex);
        foreach (MapEditorLayerType role in new[] { MapEditorLayerType.Ground, MapEditorLayerType.Object, MapEditorLayerType.WallVisual })
        {
            MapEditorLayerSetting setting = FindLayerSetting(MapEditorLayerUtility.GetCanvasLayer(canvasIndex, role));
            if (setting != null && setting.enabled) setting.visible = visible;
        }

        SyncLegacyLayerVisibility();
        RefreshAllCells();
        CreateToolToolbar();
    }

    public void MoveCanvasLayer(int fromCanvasIndex, int toCanvasIndex)
    {
        fromCanvasIndex = Mathf.Clamp(fromCanvasIndex, 0, MapEditorLayerUtility.CanvasLayerCount - 1);
        toCanvasIndex = Mathf.Clamp(toCanvasIndex, 0, MapEditorLayerUtility.CanvasLayerCount - 1);

        if (fromCanvasIndex == toCanvasIndex
            || !IsCanvasEnabled(fromCanvasIndex)
            || !IsCanvasEnabled(toCanvasIndex)
            || CurrentMapData == null)
        {
            return;
        }

        int previousActiveCanvas = activeCanvasIndex;
        int direction = fromCanvasIndex < toCanvasIndex ? 1 : -1;
        for (int canvasIndex = fromCanvasIndex; canvasIndex != toCanvasIndex; canvasIndex += direction)
        {
            SwapCanvasLayerContents(canvasIndex, canvasIndex + direction);
        }

        if (previousActiveCanvas == fromCanvasIndex)
        {
            activeCanvasIndex = toCanvasIndex;
        }
        else if (fromCanvasIndex < toCanvasIndex
            && previousActiveCanvas > fromCanvasIndex
            && previousActiveCanvas <= toCanvasIndex)
        {
            activeCanvasIndex = previousActiveCanvas - 1;
        }
        else if (fromCanvasIndex > toCanvasIndex
            && previousActiveCanvas >= toCanvasIndex
            && previousActiveCanvas < fromCanvasIndex)
        {
            activeCanvasIndex = previousActiveCanvas + 1;
        }

        ApplyBrushRoleToActiveLayer();
        selectionClipboard?.ClearSelection();
        RefreshAllCells();
        CreateToolToolbar();
    }

    private void SwapCanvasLayerContents(int fromCanvasIndex, int toCanvasIndex)
    {
        foreach (MapEditorLayerType role in new[] { MapEditorLayerType.Ground, MapEditorLayerType.Object, MapEditorLayerType.WallVisual })
        {
            MapEditorLayerType fromLayer = MapEditorLayerUtility.GetCanvasLayer(fromCanvasIndex, role);
            MapEditorLayerType toLayer = MapEditorLayerUtility.GetCanvasLayer(toCanvasIndex, role);
            CurrentMapData.SwapLayers(fromLayer, toLayer);

            MapEditorLayerSetting fromSetting = FindLayerSetting(fromLayer);
            MapEditorLayerSetting toSetting = FindLayerSetting(toLayer);
            if (fromSetting == null || toSetting == null) continue;

            string name = fromSetting.displayName;
            bool visible = fromSetting.visible;
            bool enabled = fromSetting.enabled;
            fromSetting.displayName = toSetting.displayName;
            fromSetting.visible = toSetting.visible;
            fromSetting.enabled = toSetting.enabled;
            toSetting.displayName = name;
            toSetting.visible = visible;
            toSetting.enabled = enabled;
        }
    }

    public void AddCanvasLayer()
    {
        EnsureLayerSettings();
        for (int canvasIndex = 1; canvasIndex < MapEditorLayerUtility.CanvasLayerCount; canvasIndex++)
        {
            if (IsCanvasEnabled(canvasIndex)) continue;

            foreach (MapEditorLayerType role in new[] { MapEditorLayerType.Ground, MapEditorLayerType.Object, MapEditorLayerType.WallVisual })
            {
                MapEditorLayerType layer = MapEditorLayerUtility.GetCanvasLayer(canvasIndex, role);
                CurrentMapData?.ClearLayer(layer);
                MapEditorLayerSetting setting = FindLayerSetting(layer);
                if (setting == null) continue;
                setting.enabled = true;
                setting.visible = true;
                setting.displayName = "레이어 " + (canvasIndex + 1);
            }

            SetActiveCanvas(canvasIndex);
            Debug.Log("레이어 추가: 레이어 " + (canvasIndex + 1));
            return;
        }

        Debug.LogWarning("더 이상 레이어를 추가할 수 없습니다.");
    }

    public void DeleteActiveCanvasLayer()
    {
        if (activeCanvasIndex == 0)
        {
            Debug.LogWarning("기본 레이어는 삭제할 수 없습니다.");
            return;
        }

        int removedCanvas = activeCanvasIndex;
        foreach (MapEditorLayerType role in new[] { MapEditorLayerType.Ground, MapEditorLayerType.Object, MapEditorLayerType.WallVisual })
        {
            MapEditorLayerType layer = MapEditorLayerUtility.GetCanvasLayer(removedCanvas, role);
            CurrentMapData?.ClearLayer(layer);
            MapEditorLayerSetting setting = FindLayerSetting(layer);
            if (setting == null) continue;
            setting.enabled = false;
            setting.visible = false;
            setting.displayName = GetDefaultLayerName(layer);
        }

        activeCanvasIndex = 0;
        if (brushLayerRole == MapEditorLayerType.WallCollision)
        {
            activeLayer = MapEditorLayerType.WallCollision;
        }
        else
        {
            ApplyBrushRoleToActiveLayer();
        }
        RefreshAllCells();
        CreateToolToolbar();
        Debug.Log("레이어 삭제: 레이어 " + (removedCanvas + 1));
    }

    public bool IsLayerVisible(MapEditorLayerType layerType)
    {
        if (layerType == MapEditorLayerType.Zone)
        {
            return false;
        }

        if (layerSettings == null || layerSettings.Count != (int)MapEditorLayerUtility.LastLayer + 1)
        {
            EnsureLayerSettings();
        }
        MapEditorLayerSetting setting = FindLayerSetting(layerType);

        if (setting != null)
        {
            return setting.enabled && setting.visible;
        }

        return true;
    }

    public bool IsLayerEnabled(MapEditorLayerType layerType)
    {
        if (layerSettings == null || layerSettings.Count != (int)MapEditorLayerUtility.LastLayer + 1)
        {
            EnsureLayerSettings();
        }
        MapEditorLayerSetting setting = FindLayerSetting(layerType);
        return setting == null ? !MapEditorLayerUtility.IsOptional(layerType) : setting.enabled;
    }

    public void AddUserLayer(MapEditorLayerType baseLayer)
    {
        baseLayer = MapEditorLayerUtility.GetBaseLayer(baseLayer);

        if (baseLayer != MapEditorLayerType.Ground
            && baseLayer != MapEditorLayerType.Object
            && baseLayer != MapEditorLayerType.WallVisual)
        {
            return;
        }

        EnsureLayerSettings();
        MapEditorLayerType optionalLayer = baseLayer;
        MapEditorLayerSetting setting = null;
        MapEditorLayerType[] optionalLayers = MapEditorLayerUtility.GetOptionalLayers(baseLayer);

        for (int i = 0; i < optionalLayers.Length; i++)
        {
            MapEditorLayerSetting candidate = FindLayerSetting(optionalLayers[i]);

            if (candidate != null && !candidate.enabled)
            {
                optionalLayer = optionalLayers[i];
                setting = candidate;
                break;
            }
        }

        if (setting == null)
        {
            Debug.LogWarning("이 레이어 그룹의 사용자 레이어 슬롯이 모두 사용 중입니다.");
            return;
        }

        CurrentMapData?.ClearLayer(optionalLayer);
        setting.enabled = true;
        setting.visible = true;
        setting.displayName = GetDefaultLayerName(optionalLayer);
        SetActiveLayer(optionalLayer);
        CreateToolToolbar();
        Debug.Log("사용자 레이어 추가: " + setting.displayName);
    }

    public void DeleteActiveUserLayer()
    {
        if (!MapEditorLayerUtility.IsOptional(activeLayer))
        {
            Debug.LogWarning("기본 레이어는 삭제할 수 없습니다. 사용자 레이어를 선택해 주세요.");
            return;
        }

        MapEditorLayerType removedLayer = activeLayer;
        MapEditorLayerSetting setting = FindLayerSetting(removedLayer);
        mapEditing.ClearLayer(removedLayer);

        if (setting != null)
        {
            setting.enabled = false;
            setting.visible = false;
            setting.displayName = GetDefaultLayerName(removedLayer);
        }

        activeLayer = MapEditorLayerUtility.GetBaseLayer(removedLayer);
        lastPaintLayer = activeLayer;
        RefreshAllCells();
        CreateToolToolbar();
        Debug.Log("사용자 레이어 삭제: " + removedLayer);
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
                bool enabled = !MapEditorLayerUtility.IsOptional(layerType) || setting.enabled;
                layerSettings.Add(new MapEditorLayerSetting(layerType, name, setting.visible, enabled));
            }
        }

        EnsureLayerSettings();
        NormalizeCanvasSelection();
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
                bool enabled = !MapEditorLayerUtility.IsOptional(layerType);
                layerSettings.Add(new MapEditorLayerSetting(layerType, GetDefaultLayerName(layerType), GetLegacyLayerVisibility(layerType), enabled));
            }
        }

        layerSettings.Sort((left, right) => left.layer.CompareTo(right.layer));
    }

    private void NormalizeCanvasSelection()
    {
        int canvasIndex = MapEditorLayerUtility.GetCanvasIndex(activeLayer);
        if (canvasIndex >= 0)
        {
            activeCanvasIndex = canvasIndex;
            brushLayerRole = MapEditorLayerUtility.GetBaseLayer(activeLayer);
        }
        else if (activeLayer == MapEditorLayerType.WallCollision)
        {
            brushLayerRole = MapEditorLayerType.WallCollision;
        }

        activeCanvasIndex = Mathf.Clamp(activeCanvasIndex, 0, MapEditorLayerUtility.CanvasLayerCount - 1);
        if (!IsCanvasEnabled(activeCanvasIndex)) activeCanvasIndex = 0;
    }

    private MapEditorLayerSetting FindLayerSetting(MapEditorLayerType layerType)
    {
        if (layerSettings == null)
        {
            return null;
        }

        int layerValue = (int)layerType;

        if (layerValue >= 0
            && layerValue < layerSettings.Count
            && layerSettings[layerValue] != null
            && layerSettings[layerValue].layer == layerValue)
        {
            return layerSettings[layerValue];
        }

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
                MapEditorLayerType baseLayer = MapEditorLayerUtility.GetBaseLayer(layerType);

                if (MapEditorLayerUtility.IsOptional(layerType))
                {
                    MapEditorLayerType[] optionalLayers = MapEditorLayerUtility.GetOptionalLayers(baseLayer);

                    for (int i = 0; i < optionalLayers.Length; i++)
                    {
                        if (optionalLayers[i] == layerType)
                        {
                            string prefix = baseLayer == MapEditorLayerType.WallVisual ? "Wall" : baseLayer.ToString();
                            return prefix + " " + (i + 2);
                        }
                    }
                }

                return layerType.ToString();
        }
    }

    private void CancelTransientToolState()
    {
        ResetPaintStroke();
        mapEditing?.ClearPendingPaintGesture();
        selectionClipboard?.CancelActiveDrag();
        previewDragStart = null;
        lineDragStart = null;
        lineDragEnd = null;
        rectangleFillDragStart = null;
        rectangleFillDragEnd = null;
        linePreviewCells.Clear();
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
        InvalidateSelectedAnimationPreview();
        EnsureBrushSelectionService();

        if (MapEditorTilesetLibraryService.TryGetAnimation(imagePath, imageIndex, out MapEditorTilesetDefinition animationTileset, out MapEditorTilesetAnimationDefinition animation))
        {
            imageIndex = MapEditorPngTilesetService.EncodePaletteTileIndex(animationTileset.atlasGridSize, animation.GetFrameTileId(0));
            Sprite firstFrame = GetPngTileSprite(imagePath, imageIndex, rotation, flipX, flipY);
            if (firstFrame != null)
            {
                sprite = firstFrame;
            }

            SetWholeTilePaintMode();
        }

        if (!brushSelection.SelectImageBrush(this, sprite, imagePath, imageIndex, rotation, flipX, flipY))
        {
            return;
        }

        if (MapEditorTilesetLibraryService.TryGetByAtlasPath(imagePath, out MapEditorTilesetDefinition tileset))
        {
            brushLayerRole = tileset.defaultCollision
                ? MapEditorLayerType.WallCollision
                : MapEditorLayerUtility.GetBaseLayer(tileset.defaultLayer);
            activeLayer = brushLayerRole == MapEditorLayerType.WallCollision
                ? MapEditorLayerType.WallCollision
                : MapEditorLayerUtility.GetCanvasLayer(activeCanvasIndex, brushLayerRole);
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
                GridCell targetCell = GetCellForEditing(startMapX + x, startMapY + y);
                if (targetCell == null)
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
        if (showPlayerScaleGuide || EditorToolController.Instance == null)
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

        if (tool == EditorToolType.Brush
            || tool == EditorToolType.Wall
            || tool == EditorToolType.Eraser
            || tool == EditorToolType.BrushEraser)
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

        GridCell targetCell = GetCellForEditing(mapX, mapY);
        if (targetCell == null)
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
                if (activeLayer == MapEditorLayerType.Spawn)
                {
                    RemovePixelChromaSpawnAtCell(cell);
                }
                else
                {
                    mapEditing.EraseCell(cell, brushSize);
                }
                break;

            case EditorToolType.BrushEraser:
                mapEditing.EraseCell(cell, brushSize);
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
        EnsureSpawnPointList();
        MapEditorSpawnPointData[] previousSpawnPoints = GetSpawnPointsForSave();
        RectInt? previousPreviewRegion = previewRegion;

        mapEditing.BeginTransaction();
        mapEditing.ClearAllLayers();
        ApplyMapClearMetadata(System.Array.Empty<MapEditorSpawnPointData>(), null);
        mapEditing.RecordTransactionSideEffect(
            () => ApplyMapClearMetadata(previousSpawnPoints, previousPreviewRegion),
            () => ApplyMapClearMetadata(System.Array.Empty<MapEditorSpawnPointData>(), null));
        mapEditing.CommitTransaction();

        RefreshAllCells();
        UpdateBrushCursorPreview();
    }

    private void ApplyMapClearMetadata(MapEditorSpawnPointData[] spawnPoints, RectInt? restoredPreviewRegion)
    {
        EnsureSpawnPointList();
        pixelChromaSpawnPoints.Clear();
        if (spawnPoints != null)
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                MapEditorSpawnPointData spawnPoint = spawnPoints[i];
                if (spawnPoint == null)
                {
                    continue;
                }

                pixelChromaSpawnPoints.Add(new MapEditorSpawnPointData(
                    spawnPoint.id,
                    spawnPoint.x,
                    spawnPoint.y,
                    spawnPoint.role));
            }
        }

        previewRegion = restoredPreviewRegion;
        SyncPrimarySpawnPoint();
        RefreshSpawnMarker();
        UpdatePlayerScaleGuide();
        UpdateBrushCursorPreview();
    }

    public void ClearActiveLayer()
    {
        ClearSelection();

        if (activeLayer == MapEditorLayerType.Spawn)
        {
            EnsureSpawnPointList();
            pixelChromaSpawnPoints.Clear();
            RefreshSpawnMarker();
            UpdatePlayerScaleGuide();
            Debug.Log("시작 위치를 모두 지웠습니다.");
            return;
        }

        int canvasIndex = MapEditorLayerUtility.GetCanvasIndex(activeLayer);
        if (canvasIndex >= 0)
        {
            foreach (MapEditorLayerType role in new[] { MapEditorLayerType.Ground, MapEditorLayerType.Object, MapEditorLayerType.WallVisual })
            {
                mapEditing.ClearLayer(MapEditorLayerUtility.GetCanvasLayer(canvasIndex, role));
            }
        }
        else
        {
            mapEditing.ClearLayer(activeLayer);
        }
        RefreshAllCells();
        Debug.Log("현재 레이어를 지웠습니다: " + GetCanvasDisplayName(activeCanvasIndex));
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
            null,
            null,
            out MapData resizedMapData))
        {
            return;
        }

        CurrentMapData = resizedMapData;
        ClampPreviewRegionToMap();
        RegenerateGrid();

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

    public bool IsLineToolActive()
    {
        return EditorToolController.Instance != null && EditorToolController.Instance.CurrentTool == EditorToolType.Line;
    }

    public bool IsRectangleFillToolActive()
    {
        return EditorToolController.Instance != null && EditorToolController.Instance.CurrentTool == EditorToolType.RectangleFill;
    }

    public bool IsPointerDragToolActive()
    {
        return !showPlayerScaleGuide
            && (IsSelectionToolActive() || IsPreviewRegionToolActive() || IsLineToolActive() || IsRectangleFillToolActive());
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
        pixelChromaSpawnPoints.Clear();
        pixelChromaSpawnPoints.Add(new MapEditorSpawnPointData("SpawnPoint_1", cell.X, cell.Y, "Any"));
        Debug.Log("시작 위치를 새 위치로 변경했습니다: " + cell.X + ", " + cell.Y);

        SyncPrimarySpawnPoint();
        RefreshSpawnMarker();
        UpdatePlayerScaleGuide();
    }

    public bool MoveSelection(Vector2Int offset)
    {
        return selectionClipboard != null && selectionClipboard.MoveSelection(offset);
    }

    private void RemovePixelChromaSpawnAtCell(GridCell cell)
    {
        if (cell == null)
        {
            return;
        }

        EnsureSpawnPointList();
        int index = FindSpawnPointIndex(cell.X, cell.Y);

        if (index < 0)
        {
            return;
        }

        pixelChromaSpawnPoints.RemoveAt(index);
        SyncPrimarySpawnPoint();
        RefreshSpawnMarker();
        UpdatePlayerScaleGuide();
        Debug.Log("PixelChroma 시작 위치 삭제: " + cell.X + ", " + cell.Y);
    }

    private void EnsureSpawnPointList()
    {
        if (pixelChromaSpawnPoints == null)
        {
            pixelChromaSpawnPoints = new List<MapEditorSpawnPointData>();
        }

        SyncPrimarySpawnPoint();
    }

    private void LoadSpawnPoints(MapSaveData saveData)
    {
        pixelChromaSpawnPoints.Clear();

        if (saveData.spawnPoints != null)
        {
            for (int i = 0; i < saveData.spawnPoints.Length && pixelChromaSpawnPoints.Count == 0; i++)
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
        if (IsRectangleFillToolActive())
        {
            BeginRectangleFillDrag(cell);
            return;
        }

        if (IsLineToolActive())
        {
            BeginLineDrag(cell);
            return;
        }

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
        if (IsRectangleFillToolActive())
        {
            UpdateRectangleFillDrag(cell);
            return;
        }

        if (IsLineToolActive())
        {
            UpdateLineDrag(cell);
            return;
        }

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
        if (rectangleFillDragStart.HasValue)
        {
            EndRectangleFillDrag(cell);
            return;
        }

        if (lineDragStart.HasValue)
        {
            EndLineDrag(cell);
            return;
        }

        if (previewDragStart.HasValue)
        {
            EndPreviewRegionDrag(cell);
            return;
        }

        EndSelectionDrag(cell);
    }

    private void BeginRectangleFillDrag(GridCell cell)
    {
        if (cell == null)
        {
            return;
        }

        BeginEditTransaction();
        rectangleFillDragStart = new Vector2Int(cell.X, cell.Y);
        rectangleFillDragEnd = rectangleFillDragStart;
        RefreshRectangleFillPreview();
        SetHoveredCell(cell, -1, -1);
        Debug.Log("사각형 채우기 시작: " + rectangleFillDragStart.Value);
    }

    private void UpdateRectangleFillDrag(GridCell cell)
    {
        if (!rectangleFillDragStart.HasValue || cell == null)
        {
            return;
        }

        rectangleFillDragEnd = new Vector2Int(cell.X, cell.Y);
        RefreshRectangleFillPreview();
        SetHoveredCell(cell, -1, -1);
    }

    private void EndRectangleFillDrag(GridCell cell)
    {
        if (!rectangleFillDragStart.HasValue)
        {
            return;
        }

        Vector2Int start = rectangleFillDragStart.Value;
        Vector2Int end = rectangleFillDragEnd ?? start;
        rectangleFillDragStart = null;
        rectangleFillDragEnd = null;
        linePreviewCells.Clear();

        int minX = Mathf.Min(start.x, end.x);
        int maxX = Mathf.Max(start.x, end.x);
        int minY = Mathf.Min(start.y, end.y);
        int maxY = Mathf.Max(start.y, end.y);
        MapEditorPaintSelection selection = GetPaintSelection();
        selection.brushSize = 1;

        // The preview falls back to the selected color when no image brush is active.
        // Keep the committed result identical to what the user saw while dragging.
        if (selection.selectedImageBrush == null)
        {
            selection.useSelectedColor = true;
        }

        int paintedCellCount = 0;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                GridCell targetCell = GetCellForEditing(x, y);
                if (targetCell != null)
                {
                    PaintRectangleFillCell(targetCell, x - minX, y - minY, selection);
                    paintedCellCount++;
                }
            }
        }

        if (paintedCellCount == 0)
        {
            Debug.LogWarning("사각형 채우기 범위에서 적용할 수 있는 맵 타일을 찾지 못했습니다.");
        }
        else
        {
            Debug.Log("사각형 채우기 완료: " + paintedCellCount + "칸, 레이어=" + GetLayerDisplayName(activeLayer));
        }

        UpdateBrushCursorPreview();
    }

    private void PaintRectangleFillCell(
        GridCell targetCell,
        int patternX,
        int patternY,
        MapEditorPaintSelection selection)
    {
        if (!paintWholeTile || !HasSelectedTileRegion())
        {
            mapEditing.PaintCell(targetCell, selection);
            return;
        }

        int outputWidth = Mathf.Max(1, GetSelectedRegionOutputWidth());
        int outputHeight = Mathf.Max(1, GetSelectedRegionOutputHeight());
        int outputX = patternX % outputWidth;
        int outputY = patternY % outputHeight;
        GetSelectedRegionSourceCoordinate(outputX, outputY, out int localSourceX, out int localSourceYFromTop);

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
            return;
        }

        selection.selectedImageBrush = sprite;
        selection.selectedImageIndex = imageIndex;
        mapEditing.PaintCell(targetCell, selection);
    }

    private void RefreshRectangleFillPreview()
    {
        linePreviewCells.Clear();

        if (!rectangleFillDragStart.HasValue || !rectangleFillDragEnd.HasValue)
        {
            return;
        }

        Vector2Int start = rectangleFillDragStart.Value;
        Vector2Int end = rectangleFillDragEnd.Value;
        int minX = Mathf.Min(start.x, end.x);
        int maxX = Mathf.Max(start.x, end.x);
        int minY = Mathf.Min(start.y, end.y);
        int maxY = Mathf.Max(start.y, end.y);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                linePreviewCells.Add(new Vector2Int(x, y));
            }
        }

        UpdateBrushCursorPreview();
    }

    private void BeginLineDrag(GridCell cell)
    {
        if (cell == null)
        {
            return;
        }

        BeginEditTransaction();
        lineDragStart = new Vector2Int(cell.X, cell.Y);
        lineDragEnd = lineDragStart;
        RefreshLinePreview();
        SetHoveredCell(cell, -1, -1);
    }

    private void UpdateLineDrag(GridCell cell)
    {
        if (!lineDragStart.HasValue || cell == null)
        {
            return;
        }

        lineDragEnd = new Vector2Int(cell.X, cell.Y);
        RefreshLinePreview();
        SetHoveredCell(cell, -1, -1);
    }

    private void EndLineDrag(GridCell cell)
    {
        if (!lineDragStart.HasValue)
        {
            return;
        }

        Vector2Int start = lineDragStart.Value;
        Vector2Int end = lineDragEnd ?? start;
        lineDragStart = null;
        lineDragEnd = null;
        linePreviewCells.Clear();
        MapEditorBrushGeometry.RasterizeLine(start, end, point =>
        {
            GridCell targetCell = GetCellForEditing(point.x, point.y);
            if (targetCell != null)
            {
                UseCurrentToolAt(targetCell, -1, -1, EditorToolType.Brush);
            }
        });
        UpdateBrushCursorPreview();
    }

    private void RefreshLinePreview()
    {
        linePreviewCells.Clear();

        if (!lineDragStart.HasValue || !lineDragEnd.HasValue)
        {
            return;
        }

        MapEditorBrushGeometry.RasterizeLine(lineDragStart.Value, lineDragEnd.Value, linePreviewCells.Add);
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

        UpdatePreviewRegionDrag(cell != null ? cell : hoveredCell);
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

    public bool TryGetCellAtScreenPosition(Vector2 screenPosition, Camera eventCamera, out GridCell cell)
    {
        cell = null;

        if (gridGenerator == null)
        {
            gridGenerator = GetComponent<GridGenerator>();
        }

        RectTransform gridRect = gridGenerator == null ? null : gridGenerator.gridParent as RectTransform;
        if (gridRect == null
            || gridGenerator.cellSize <= 0f
            || !RectTransformUtility.ScreenPointToLocalPointInRectangle(gridRect, screenPosition, eventCamera, out Vector2 localPoint))
        {
            return false;
        }

        Rect rect = gridRect.rect;
        int x = Mathf.FloorToInt((localPoint.x - rect.xMin) / gridGenerator.cellSize);
        int y = Mathf.FloorToInt((rect.yMax - localPoint.y) / gridGenerator.cellSize);

        if (CurrentMapData == null
            || x < 0
            || y < 0
            || x >= CurrentMapData.width
            || y >= CurrentMapData.height)
        {
            return false;
        }

        cell = GetCellForEditing(x, y);
        return cell != null;
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

    private GridCell GetCellForEditing(int x, int y)
    {
        if (CurrentMapData == null || !CurrentMapData.IsInside(x, y))
        {
            return null;
        }

        if (cells.TryGetValue(new Vector2Int(x, y), out GridCell registeredCell) && registeredCell != null)
        {
            return registeredCell;
        }

        if (coordinateProxy == null)
        {
            GameObject proxyObject = new GameObject("MapEditor_CellCoordinateProxy", typeof(RectTransform), typeof(Image), typeof(GridCell));
            proxyObject.hideFlags = HideFlags.HideAndDontSave;
            Image proxyImage = proxyObject.GetComponent<Image>();
            proxyImage.color = Color.clear;
            proxyImage.raycastTarget = false;
            coordinateProxy = proxyObject.GetComponent<GridCell>();
        }

        coordinateProxy.SetCoordinates(x, y);
        return coordinateProxy;
    }

    public void HandleVirtualPointerDown(int x, int y, int subPixelX, int subPixelY)
    {
        GridCell cell = GetCellForEditing(x, y);
        if (cell == null)
        {
            return;
        }

        SetHoveredCell(cell, subPixelX, subPixelY);
        if (IsPointerDragToolActive())
        {
            BeginPointerDrag(cell);
            return;
        }

        BeginEditTransaction();
        UseCurrentTool(cell, subPixelX, subPixelY);
    }

    public void HandleVirtualPointerDrag(int x, int y, int subPixelX, int subPixelY)
    {
        GridCell cell = GetCellForEditing(x, y);
        if (cell == null)
        {
            return;
        }

        SetHoveredCell(cell, subPixelX, subPixelY);
        if (IsPointerDragToolActive())
        {
            UpdatePointerDrag(cell);
            return;
        }

        UseCurrentTool(cell, subPixelX, subPixelY);
    }

    public void HandleVirtualPointerMove(int x, int y, int subPixelX, int subPixelY)
    {
        GridCell cell = GetCellForEditing(x, y);
        if (cell != null)
        {
            SetHoveredCell(cell, subPixelX, subPixelY);
        }
    }

    public void HandleVirtualPointerUp(int x, int y, int subPixelX, int subPixelY)
    {
        GridCell cell = GetCellForEditing(x, y);
        if (cell != null)
        {
            SetHoveredCell(cell, subPixelX, subPixelY);
        }

        EndPointerDrag(cell);
        CommitEditTransaction();
    }

    public void HandleVirtualPointerUpOutside()
    {
        EndPointerDrag(hoveredCell);
        CommitEditTransaction();
    }

    public void HandleVirtualPointerExit()
    {
        hoveredCell = null;
        UpdateBrushCursorPreview();
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

    public void ZoomMap(float direction, Vector2 screenPosition)
    {
        EnsureViewportService();
        viewportService.ZoomMap(direction, screenPosition);
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
        toolbarState.EnsureToolbar(this, toolToolbarOffset, GetRecentResourcePaths());
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
        UpdatePlayerScaleGuide();
    }

    public void TogglePlayerScaleGuide()
    {
        showPlayerScaleGuide = !showPlayerScaleGuide;
        CancelTransientToolState();

        if (showPlayerScaleGuide && !playerScaleGuidePositionInitialized)
        {
            InitializePlayerScaleGuidePosition();
        }

        UpdatePlayerScaleGuide();
        RefreshToolToolbarSelection();
        UpdateBrushCursorPreview();

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            MapEditorMapSizePanelBuilder.Ensure(canvas.transform, this, toolToolbarOffset);
        }
    }

    public void SetPlayerScaleGuidePosition(int x, int y)
    {
        playerScaleGuideX = Mathf.Clamp(x, 0, Mathf.Max(0, mapWidth - 1));
        playerScaleGuideY = Mathf.Clamp(y, 0, Mathf.Max(0, mapHeight - 1));
        playerScaleGuidePositionInitialized = true;
        UpdatePlayerScaleGuide();
    }

    private void InitializePlayerScaleGuidePosition()
    {
        if (pixelChromaSpawnPoints != null && pixelChromaSpawnPoints.Count > 0)
        {
            playerScaleGuideX = pixelChromaSpawnPoints[0].x;
            playerScaleGuideY = pixelChromaSpawnPoints[0].y;
        }
        else
        {
            playerScaleGuideX = mapWidth / 2;
            playerScaleGuideY = mapHeight / 2;
        }

        playerScaleGuidePositionInitialized = true;
    }

    private void UpdatePlayerScaleGuide()
    {
        if (!playerScaleGuidePositionInitialized)
        {
            InitializePlayerScaleGuidePosition();
        }

        playerScaleGuideX = Mathf.Clamp(playerScaleGuideX, 0, Mathf.Max(0, mapWidth - 1));
        playerScaleGuideY = Mathf.Clamp(playerScaleGuideY, 0, Mathf.Max(0, mapHeight - 1));
        playerScaleGuide.Update(gridGenerator, showPlayerScaleGuide, playerScaleGuideX, playerScaleGuideY);
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

        Sprite previewImage = activeLayer == MapEditorLayerType.WallCollision
            ? null
            : selectedImageBrush;
        float previewAnimationFps = 8f;
        bool previewAnimationLoop = true;
        Sprite[] previewAnimationFrames = previewImage == null
            ? null
            : GetSelectedAnimationPreviewFrames(out previewAnimationFps, out previewAnimationLoop);

        brushCursorPreview.Update(
            showBrushCursorPreview && !showPlayerScaleGuide,
            gridGenerator,
            hoveredCell,
            CurrentMapData,
            brushSize,
            activeLayer == MapEditorLayerType.WallCollision
                ? new Color(0.18f, 0.18f, 0.18f, 1f)
                : selectedColor,
            previewImage,
            previewAnimationFrames,
            previewAnimationFps,
            previewAnimationLoop,
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
            GetSelectionPreviewRect(),
            selectionClipboard == null ? null : selectionClipboard.SelectionPreviewCells,
            linePreviewCells
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
        MapEditorTilesetLibraryWindow.Open(this);
    }

    public void OpenTileCreator()
    {
        MapEditorTileCreatorWindow.Open(this);
    }

    public void OpenAnimationTileEditor()
    {
        MapEditorAnimationTileWindow.Open(this);
    }

    public bool AddTilesetAnimation(
        string tilesetId,
        string displayName,
        int[] sourceFrameTileIds,
        float framesPerSecond,
        bool loop,
        out MapEditorTilesetAnimationDefinition animation,
        out string error)
    {
        bool success = EnsureTilesetLibrary().AddAnimation(
            tilesetId,
            displayName,
            sourceFrameTileIds,
            framesPerSecond,
            loop,
            out animation,
            out error);
        if (success)
        {
            InvalidateSelectedAnimationPreview();
        }

        return success;
    }

    public bool UpdateTilesetAnimation(
        string tilesetId,
        string animationId,
        string displayName,
        int[] sourceFrameTileIds,
        float framesPerSecond,
        bool loop,
        out string error)
    {
        bool success = EnsureTilesetLibrary().UpdateAnimation(
            tilesetId,
            animationId,
            displayName,
            sourceFrameTileIds,
            framesPerSecond,
            loop,
            out error);
        if (success)
        {
            InvalidateSelectedAnimationPreview();
            UpdateBrushCursorPreview();
        }

        return success;
    }

    public bool RemoveTilesetAnimation(string tilesetId, string animationId)
    {
        bool success = EnsureTilesetLibrary().RemoveAnimation(tilesetId, animationId);
        if (success)
        {
            InvalidateSelectedAnimationPreview();
            UpdateBrushCursorPreview();
        }

        return success;
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
        bool collision,
        bool animated = false,
        string animationName = "Animation",
        int animationStartTile = 0,
        int animationFrameCount = 2,
        float animationFps = 8f,
        bool animationLoop = true)
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

        if (animated && !EnsureTilesetLibrary().ConfigureAnimation(
                definition.id,
                animationName,
                animationStartTile,
                animationFrameCount,
                animationFps,
                animationLoop,
                out string animationError))
        {
            Debug.LogError("Animated tileset configuration failed: " + animationError);
            EnsureTilesetLibrary().Remove(definition.id);
            return false;
        }

        UseImportedTileset(definition.id);
        Debug.Log("타일셋을 가져왔습니다: " + definition.displayName + " (" + definition.columns + "x" + definition.rows + " 타일)");
        return true;
    }

    public bool ImportTilesetCollection(
        IReadOnlyList<string> sourcePaths,
        string displayName,
        int tileWidth,
        int tileHeight,
        int margin,
        int spacing,
        MapEditorLayerType defaultLayer,
        bool collision)
    {
        if (!EnsureTilesetLibrary().ImportCollection(
                sourcePaths,
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
            Debug.LogError("타일셋 묶음 가져오기 실패: " + error);
            return false;
        }

        UseImportedTileset(definition.id);
        Debug.Log("타일셋 묶음을 가져왔습니다: " + definition.displayName
            + " (PNG " + definition.SourceCount + "개, 타일 " + definition.TileCount + "개)");
        return true;
    }

    public bool RenameImportedTileset(string id, string displayName)
    {
        return EnsureTilesetLibrary().Rename(id, displayName);
    }

    public Sprite[] GetAnimationFrames(string imagePath, int imageIndex, int rotation = 0, bool flipX = false, bool flipY = false)
    {
        if (!MapEditorTilesetLibraryService.TryGetAnimation(imagePath, imageIndex, out MapEditorTilesetDefinition tileset, out MapEditorTilesetAnimationDefinition animation))
        {
            return null;
        }

        Sprite[] frames = new Sprite[Mathf.Max(1, animation.frameCount)];
        for (int i = 0; i < frames.Length; i++)
        {
            int frameIndex = MapEditorPngTilesetService.EncodePaletteTileIndex(tileset.atlasGridSize, animation.GetFrameTileId(i));
            frames[i] = GetPngTileSprite(imagePath, frameIndex, rotation, flipX, flipY);
            if (frames[i] == null)
            {
                return null;
            }
        }

        return frames;
    }

    private Sprite[] GetSelectedAnimationPreviewFrames(out float framesPerSecond, out bool loop)
    {
        framesPerSecond = 8f;
        loop = true;

        if (selectedImageBrush == null
            || !MapEditorTilesetLibraryService.TryGetAnimation(
                selectedImagePath,
                selectedImageIndex,
                out _,
                out MapEditorTilesetAnimationDefinition animation))
        {
            InvalidateSelectedAnimationPreview();
            return null;
        }

        bool cacheMatches = selectedAnimationPreviewFrames != null
            && selectedAnimationPreviewPath == selectedImagePath
            && selectedAnimationPreviewIndex == selectedImageIndex
            && selectedAnimationPreviewRotation == selectedImageRotation
            && selectedAnimationPreviewFlipX == selectedImageFlipX
            && selectedAnimationPreviewFlipY == selectedImageFlipY
            && selectedAnimationPreviewId == animation.id
            && selectedAnimationPreviewFrameCount == animation.frameCount
            && Mathf.Approximately(selectedAnimationPreviewFps, animation.framesPerSecond)
            && selectedAnimationPreviewLoop == animation.loop;

        if (!cacheMatches)
        {
            selectedAnimationPreviewFrames = GetAnimationFrames(
                selectedImagePath,
                selectedImageIndex,
                selectedImageRotation,
                selectedImageFlipX,
                selectedImageFlipY);
            selectedAnimationPreviewPath = selectedImagePath;
            selectedAnimationPreviewIndex = selectedImageIndex;
            selectedAnimationPreviewRotation = selectedImageRotation;
            selectedAnimationPreviewFlipX = selectedImageFlipX;
            selectedAnimationPreviewFlipY = selectedImageFlipY;
            selectedAnimationPreviewId = animation.id;
            selectedAnimationPreviewFrameCount = animation.frameCount;
            selectedAnimationPreviewFps = animation.framesPerSecond;
            selectedAnimationPreviewLoop = animation.loop;
        }

        framesPerSecond = animation.framesPerSecond;
        loop = animation.loop;
        return selectedAnimationPreviewFrames;
    }

    private void InvalidateSelectedAnimationPreview()
    {
        selectedAnimationPreviewPath = string.Empty;
        selectedAnimationPreviewIndex = int.MinValue;
        selectedAnimationPreviewRotation = 0;
        selectedAnimationPreviewFlipX = false;
        selectedAnimationPreviewFlipY = false;
        selectedAnimationPreviewId = string.Empty;
        selectedAnimationPreviewFrameCount = 0;
        selectedAnimationPreviewFrames = null;
        selectedAnimationPreviewFps = 8f;
        selectedAnimationPreviewLoop = true;
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
        SetBrushLayerRole(definition.defaultCollision ? MapEditorLayerType.WallCollision : definition.defaultLayer);
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

    public void ValidatePixelChromaMap()
    {
        PixelChromaMapValidationReport report = ValidateForWorkshop();
        PublishValidationResult(report);
        MapEditorModalPanel.ShowValidation(this, report);
    }

    private void PublishValidationResult(PixelChromaMapValidationReport report)
    {
        if (report == null)
        {
            return;
        }

        MapEditorPixelChromaValidationService.Log(report);
        toolbarState.UpdateValidationStatus(report);

        string summary =
            "PixelChroma 맵 검사: " + (report.isValid ? "통과" : "수정 필요") +
            " | 그린 타일=" + report.paintedTileCount +
            " | 벽=" + report.wallTileCount +
            " | 색상=" + report.colorTileCount +
            " | 이미지=" + report.imageTileCount +
            " | 타일셋=" + report.tilesetCount +
            " | 시작 위치=" + report.spawnPointCount +
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
        PixelChromaMapValidationReport report = ValidateForWorkshop();
        PublishValidationResult(report);

        if (!report.isValid)
        {
            MapEditorModalPanel.ShowValidation(this, report);
            return;
        }

        MapEditorModalPanel.ShowValidation(this, report, ExportWorkshopPackageAfterValidation);
    }

    private void ExportWorkshopPackageAfterValidation()
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
        uint appId = steamAppId > 0 ? steamAppId : 480u;
        string url = "steam://openurl/https://steamcommunity.com/app/" + appId + "/workshop/";
        Application.OpenURL(url);
    }

    public void UploadWorkshopToSteam()
    {
        PixelChromaRuntimeWorkshopUploader uploader =
            Object.FindFirstObjectByType<PixelChromaRuntimeWorkshopUploader>();

        if (uploader == null)
        {
            GameObject uploaderObject = new GameObject("MapEditor_WorkshopUploader");
            uploader = uploaderObject.AddComponent<PixelChromaRuntimeWorkshopUploader>();
        }

        if (uploader.IsBusy)
        {
            MapEditorModalPanel.Show(
                this,
                MapEditorLocalization.Choose("창작마당 업로드 중", "Workshop Upload In Progress"),
                MapEditorLocalization.Choose(
                    "이미 업로드를 진행하고 있습니다. 완료 메시지가 나올 때까지 잠시 기다려 주세요.",
                    "An upload is already in progress. Please wait for the completion message."),
                new Color(0.95f, 0.66f, 0.18f, 1f));
            return;
        }

        BindWorkshopUploader(uploader);

        uint appId = uploader.EffectiveAppId;
        bool isTestUpload = appId == 480;
        string targetDescription = isTestUpload
            ? MapEditorLocalization.Choose(
                "현재 테스트 App ID 480(Spacewar)에 미등록 상태로 업로드합니다. PixelChroma 전용 창작마당에 올리려면 맵 에디터의 Steam App ID를 실제 게임 App ID로 설정해야 합니다.",
                "This uploads an unlisted item to test App ID 480 (Spacewar). Set the map editor Steam App ID to the real game App ID to publish to the PixelChroma Workshop.")
            : MapEditorLocalization.Choose(
                "Steam App ID " + appId + " 창작마당으로 업로드합니다.",
                "Uploading to the Workshop for Steam App ID " + appId + ".");

        MapEditorModalPanel.Show(
            this,
            MapEditorLocalization.Choose("창작마당 업로드 시작", "Workshop Upload Started"),
            targetDescription + "\n\n" +
            MapEditorLocalization.Choose(
                "맵 검사, 패키지 생성, Steam 전송을 차례로 진행합니다.",
                "The editor will validate the map, create the package, and submit it to Steam."),
            new Color(0.18f, 0.48f, 0.95f, 1f));

        uploader.ValidateAndUpload();
    }

    private void BindWorkshopUploader(PixelChromaRuntimeWorkshopUploader uploader)
    {
        UnsubscribeWorkshopUploader();
        subscribedWorkshopUploader = uploader;
        subscribedWorkshopUploader.mapEditor = this;

        if (steamAppId > 0)
        {
            subscribedWorkshopUploader.testAppId = steamAppId;
            subscribedWorkshopUploader.forceUnlistedForTest = steamAppId == 480;
        }
        else if (subscribedWorkshopUploader.testAppId == 0)
        {
            subscribedWorkshopUploader.testAppId = 480;
            subscribedWorkshopUploader.forceUnlistedForTest = true;
        }

        subscribedWorkshopUploader.UploadSucceeded += OnWorkshopUploadSucceeded;
        subscribedWorkshopUploader.UploadFailed += OnWorkshopUploadFailed;
    }

    private void UnsubscribeWorkshopUploader()
    {
        if (subscribedWorkshopUploader == null)
        {
            return;
        }

        subscribedWorkshopUploader.UploadSucceeded -= OnWorkshopUploadSucceeded;
        subscribedWorkshopUploader.UploadFailed -= OnWorkshopUploadFailed;
        subscribedWorkshopUploader = null;
    }

    private void OnWorkshopUploadSucceeded(ulong publishedFileId)
    {
        string itemUrl = "steam://url/CommunityFilePage/" + publishedFileId;
        MapEditorModalPanel.ShowAction(
            this,
            MapEditorLocalization.Choose("창작마당 업로드 완료", "Workshop Upload Complete"),
            MapEditorLocalization.Choose(
                "Steam 전송이 완료되었습니다.\n게시물 ID: " + publishedFileId +
                "\n\n테스트 App ID 480을 사용한 경우 PixelChroma 창작마당이 아니라 Spacewar의 미등록 게시물로 생성됩니다.",
                "Steam upload completed.\nPublished File ID: " + publishedFileId +
                "\n\nWhen test App ID 480 is used, the item is created as an unlisted Spacewar item, not in the PixelChroma Workshop."),
            new Color(0.22f, 0.72f, 0.38f, 1f),
            MapEditorLocalization.Choose("게시물 열기", "Open Workshop Item"),
            () => Application.OpenURL(itemUrl));
    }

    private void OnWorkshopUploadFailed(string reason)
    {
        MapEditorModalPanel.ShowAction(
            this,
            MapEditorLocalization.Choose("창작마당 업로드 실패", "Workshop Upload Failed"),
            reason + "\n\n" +
            MapEditorLocalization.Choose(
                "Steam 실행 및 로그인, steam_appid.txt, 계정의 게임 라이선스와 창작마당 약관 동의를 확인하세요.",
                "Check that Steam is running and signed in, steam_appid.txt is correct, the account owns the app, and the Workshop legal agreement is accepted."),
            new Color(0.92f, 0.3f, 0.26f, 1f),
            MapEditorLocalization.Choose("Steam 창작마당 열기", "Open Steam Workshop"),
            OpenSteamWorkshopPage);
    }

    public void ExportWorkshopPackage(string folderPath)
    {
        PixelChromaMapValidationReport report = ValidateForWorkshop();
        PublishValidationResult(report);

        if (!report.isValid)
        {
            return;
        }

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
        PixelChromaMapValidationReport validation = ValidateForWorkshop();
        PublishValidationResult(validation);
        if (!validation.isValid)
        {
            folderPath = string.Empty;
            return false;
        }

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
        toolbarState.RefreshRecentPngList(this, GetRecentResourcePaths());
    }

    public IReadOnlyList<string> GetRecentResourcePaths()
    {
        List<string> result = pngFiles.GetRecentPaths();
        EnsureTilesetLibrary();
        IReadOnlyList<MapEditorTilesetDefinition> definitions = tilesetLibrary.Definitions;

        for (int i = 0; i < definitions.Count; i++)
        {
            string path = definitions[i]?.atlasPath;
            if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path) && !result.Contains(path))
            {
                result.Add(path);
            }
        }

        return result;
    }

    public string GetRecentResourceDisplayName(string path)
    {
        return pngFiles.GetRecentDisplayName(path);
    }

    public void RenameRecentResource(string path, string displayName)
    {
        pngFiles.SetRecentDisplayName(path, displayName);
        RefreshRecentPngList();
    }

    public bool IsRegisteredTilesetPath(string path)
    {
        EnsureTilesetLibrary();
        IReadOnlyList<MapEditorTilesetDefinition> definitions = tilesetLibrary.Definitions;
        for (int i = 0; i < definitions.Count; i++)
        {
            if (definitions[i] != null
                && string.Equals(definitions[i].atlasPath, path, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
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
        bool regionPreview = HasSelectedTileRegion();
        toolbarState.UpdateBrushPreview(
            selectedImageBrush,
            selectedColor,
            selectedImageRotation,
            brushSize,
            useWallTileBrush,
            regionPreview && selectedImageFlipX,
            regionPreview && selectedImageFlipY);
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

        MarkAllVisualsDirty();
    }

    private bool IsSpawnPointAt(int x, int y)
    {
        EnsureSpawnPointList();
        return FindSpawnPointIndex(x, y) >= 0;
    }

    public bool HasSpawnPointAt(int x, int y)
    {
        return IsSpawnPointAt(x, y);
    }

    public bool WriteCompositeCellPixels(
        int mapX,
        int mapY,
        int resolution,
        Color32[] target,
        int targetWidth,
        int offsetX,
        int offsetY)
    {
        EnsureMapEditingService();
        return mapEditing.WriteCompositeCellPixels(mapX, mapY, resolution, target, targetWidth, offsetX, offsetY);
    }

    private void MarkVisualCellDirty(int x, int y)
    {
        if (gridGenerator == null)
        {
            gridGenerator = GetComponent<GridGenerator>();
        }

        gridGenerator?.ChunkRenderer?.MarkCellDirty(x, y);
    }

    private void MarkAllVisualsDirty()
    {
        if (gridGenerator == null)
        {
            gridGenerator = GetComponent<GridGenerator>();
        }

        gridGenerator?.ChunkRenderer?.MarkAllDirty();
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
