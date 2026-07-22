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

    [Header("Map")]
    public int mapWidth = 64;
    public int mapHeight = 64;

    [Header("PNG Palette")]
    public int pngPaletteGridSize = 16;

    [Header("Paint Color")]
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

    [Header("Layer")]
    public MapEditorLayerType activeLayer = MapEditorLayerType.Ground;
    public bool showGroundLayer = true;
    public bool showObjectLayer = true;
    public bool showWallVisualLayer = true;
    public bool showWallCollisionLayer = true;
    public bool showZoneLayer = true;

    [Header("Tools")]
    public BrushTool brushTool;
    public EraserTool eraserTool;

    [Header("Input")]
    public bool enableKeyboardShortcuts = true;
    public KeyCode eyedropperKey = KeyCode.Space;

    [Header("Tool Toolbar")]
    public bool createToolToolbar = true;
    public bool removeLegacyToolButtons = true;
    public Vector2 toolToolbarOffset = new Vector2(-12f, -12f);
    public int maxRecentPngFiles = 5;

    [Header("Minimap")]
    public bool createMinimap = false;
    public Vector2 minimapOffset = new Vector2(-198f, -152f);
    public Vector2 minimapSize = new Vector2(120f, 120f);

    [Header("Map Zoom")]
    public float mapZoomStep = 4f;
    public float maxMapCellSize = 96f;

    [Header("Brush Cursor Preview")]
    public bool showBrushCursorPreview = true;
    public float brushCursorAlpha = 0.45f;

    [Header("Export")]
    public int exportCellPixels = 1;
    public bool paintWholeTile;
    public bool exportEmptyCellsTransparent = true;
    public string pixelChromaMapId = "map_01";
    public string workshopTitle = "New PixelChroma Map";
    public string workshopAuthor = "Unknown";
    [TextArea(2, 4)]
    public string workshopDescription = string.Empty;
    public string requiredPixelChromaVersion = "1.0.0";
    public string steamWorkshopVisibility = "Public";
    public string steamWorkshopTags = "Map";
    public int pixelChromaSpawnX;
    public int pixelChromaSpawnY;
    public List<MapEditorSpawnPointData> pixelChromaSpawnPoints = new List<MapEditorSpawnPointData>();

    [Header("Color Window")]
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

    public GridGenerator GridGenerator => gridGenerator;
    public MapEditorLayerType ActiveLayer => activeLayer;

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

        inputService.Tick();
        UpdateBrushCursorPreview();
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

        Debug.Log("Selected layer: " + activeLayer);
    }

    public bool IsLayerVisible(MapEditorLayerType layerType)
    {
        switch (layerType)
        {
            case MapEditorLayerType.Object:
                return showObjectLayer;
            case MapEditorLayerType.WallVisual:
                return showWallVisualLayer;
            case MapEditorLayerType.WallCollision:
                return showWallCollisionLayer;
            case MapEditorLayerType.Spawn:
                return true;
            case MapEditorLayerType.Zone:
                return showZoneLayer;
            default:
                return showGroundLayer;
        }
    }

    public void ToggleLayerVisible(MapEditorLayerType layerType)
    {
        switch (layerType)
        {
            case MapEditorLayerType.Object:
                showObjectLayer = !showObjectLayer;
                break;
            case MapEditorLayerType.WallVisual:
                showWallVisualLayer = !showWallVisualLayer;
                break;
            case MapEditorLayerType.WallCollision:
                showWallCollisionLayer = !showWallCollisionLayer;
                break;
            case MapEditorLayerType.Zone:
                showZoneLayer = !showZoneLayer;
                break;
            case MapEditorLayerType.Ground:
                showGroundLayer = !showGroundLayer;
                break;
            default:
                break;
        }

        RefreshAllCells();

        if (createToolToolbar)
        {
            CreateToolToolbar();
        }
        else
        {
            RefreshToolToolbarSelection();
        }

        Debug.Log("Layer visibility: " + layerType + " = " + IsLayerVisible(layerType));
    }

    private void CancelTransientToolState()
    {
        mapEditing?.ClearPendingPaintGesture();
        selectionClipboard?.CancelActiveDrag();
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
        Debug.Log("Selected color: " + ColorUtility.ToHtmlStringRGBA(color));
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

        Debug.Log("Selected image brush: " + sprite.name);
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
        int startMapX = centerCell.X - selectedRegionWidth / 2;
        int startMapY = centerCell.Y - selectedRegionHeight / 2;
        MapEditorPaintSelection selection = GetPaintSelection();
        selection.brushSize = 1;

        mapEditing.BeginTransaction();

        for (int y = 0; y < selectedRegionHeight; y++)
        {
            int sourceYFromTop = selectedRegionStartYFromTop + y;
            int sourceY = selectedRegionGridSize - 1 - sourceYFromTop;

            for (int x = 0; x < selectedRegionWidth; x++)
            {
                if (!cells.TryGetValue(new Vector2Int(startMapX + x, startMapY + y), out GridCell targetCell))
                {
                    continue;
                }

                int sourceX = selectedRegionStartX + x;
                int baseIndex = sourceY * selectedRegionGridSize + sourceX;
                int imageIndex = MapEditorPngTilesetService.EncodePaletteTileIndex(selectedRegionGridSize, baseIndex);
                Sprite sprite = GetPngTileSprite(selectedImagePath, imageIndex);

                if (sprite == null)
                {
                    continue;
                }

                selection.selectedImageBrush = sprite;
                selection.selectedImageIndex = imageIndex;
                mapEditing.PaintCell(targetCell, selection);
            }
        }

        mapEditing.CommitTransaction();
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

        if (EditorToolController.Instance.CurrentTool == EditorToolType.Selection)
        {
            return;
        }

        if ((EditorToolController.Instance.CurrentTool == EditorToolType.Brush || EditorToolController.Instance.CurrentTool == EditorToolType.Wall) && IsAreaFillModifierPressed())
        {
            MapEditorPaintSelection selection = EditorToolController.Instance.CurrentTool == EditorToolType.Wall
                ? GetWallPaintSelection()
                : GetPaintSelection();
            mapEditing.HandleAreaFill(cell, selection);
            return;
        }

        switch (EditorToolController.Instance.CurrentTool)
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
                    mapEditing.PaintSubPixel(cell, subPixelX, subPixelY, GetExportCellPixels(), selectedColor);
                    break;
                }

                if (!useWallTileBrush && selectedImageBrush != null && subPixelX >= 0 && subPixelY >= 0)
                {
                    mapEditing.PaintSpriteAtSubPixel(cell, subPixelX, subPixelY, GetExportCellPixels(), selectedImageBrush);
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

    public void ChangeBrushSize(int delta)
    {
        EnsureBrushSelectionService();
        brushSelection.ChangeBrushSize(this, delta);
        UpdateBrushPreview();
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
        mapEditing.BeginTransaction();
    }

    public void CommitEditTransaction()
    {
        mapEditing.CommitTransaction();
    }

    public void SaveMap()
    {
        EnsureSpawnPointList();
        mapSaveService.SetImportedTilesets(EnsureTilesetLibrary().GetDefinitionsForSave());
        mapSaveService.Save(CurrentMapData, pngFiles.CurrentPath, pixelChromaSpawnX, pixelChromaSpawnY, GetSpawnPointsForSave());
    }

    public void SaveMap(string fileName)
    {
        EnsureSpawnPointList();
        mapSaveService.SetImportedTilesets(EnsureTilesetLibrary().GetDefinitionsForSave());
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
        LoadSpawnPoints(saveData);
        RefreshSpawnMarker();
    }

    public bool IsSelectionToolActive()
    {
        return EditorToolController.Instance != null && EditorToolController.Instance.CurrentTool == EditorToolType.Selection;
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
        Debug.Log("Loaded PNG pasted to map: " + pngFiles.CurrentPath + " at " + topLeft + " size " + pngTileGridSize + "x" + pngTileGridSize);
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
            Debug.Log("PixelChroma spawn point removed: " + cell.X + ", " + cell.Y);
        }
        else
        {
            pixelChromaSpawnPoints.Add(new MapEditorSpawnPointData(GetNextSpawnPointId(), cell.X, cell.Y, "Any"));
            Debug.Log("PixelChroma spawn point added: " + cell.X + ", " + cell.Y);
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

    public void UpdateSelectionDrag(GridCell cell)
    {
        selectionClipboard.UpdateSelectionDrag(cell);
    }

    public void EndSelectionDrag(GridCell cell)
    {
        selectionClipboard.EndSelectionDrag(cell);
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
        return selectionClipboard == null ? null : selectionClipboard.SelectionPreviewRect;
    }

    private void CreateToolToolbar()
    {
        toolbarState.EnsureToolbar(this, toolToolbarOffset, pngFiles.GetRecentPaths());
        RefreshToolToolbarSelection();
        UpdateBrushPreview();
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
            HasSelectedTileRegion() ? selectedRegionWidth : 1,
            HasSelectedTileRegion() ? selectedRegionHeight : 1,
            GetExportCellPixels(),
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
#if UNITY_EDITOR
        MapEditorTilesetImporterWindow.Open(this);
#else
        Debug.LogWarning("Tileset importing is only available in the Unity Editor.");
#endif
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
            Debug.LogError("Tileset import failed: " + error);
            return false;
        }

        UseImportedTileset(definition.id);
        Debug.Log("Tileset imported: " + definition.displayName + " (" + definition.columns + "x" + definition.rows + " tiles)");
        return true;
    }

    public void UseImportedTileset(string id)
    {
        MapEditorTilesetDefinition definition = EnsureTilesetLibrary().FindById(id);
        if (definition == null || !definition.IsUsable || !System.IO.File.Exists(definition.atlasPath))
        {
            Debug.LogWarning("Imported tileset is unavailable: " + id);
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
            Debug.Log("Imported tileset removed from library: " + id);
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

        string summary =
            "PixelChroma map validation: " + (report.isValid ? "Valid" : "Invalid") +
            " | painted=" + report.paintedTileCount +
            " | walls=" + report.wallTileCount +
            " | color=" + report.colorTileCount +
            " | image=" + report.imageTileCount +
            " | tilesets=" + report.tilesetCount +
            " | spawns=" + report.spawnPointCount +
            " | zones=" + report.zoneCount +
            " | errors=" + report.errors.Count +
            " | warnings=" + report.warnings.Count;

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
        EnsureWorkshopExportService();
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

    public void ExportWorkshopPackage(string folderPath)
    {
        EnsureWorkshopExportService();
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
