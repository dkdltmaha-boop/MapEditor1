using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class ColorWheelPngPaletteView
{
    private const string PngDragSelectionInputObjectName = "PngDragSelectionInput";
    private const string PngDragSelectionPreviewObjectName = "PngDragSelectionPreview";
    private const string PngPaletteViewportObjectName = "ColorPicker_PngTilesetViewport";
    private const string LegacyPngPaletteViewportObjectName = "PngPaletteViewport";
    private const string PngPaletteContentObjectName = "ColorPicker_PngTilesetGrid";
    private const string PngPaletteImageObjectName = "PngPaletteImage";
    private const string LegacyPngPaletteContentObjectName = "PngPaletteContent";
    private const string PngPaletteLabelObjectName = "PngPaletteLabel";
    private const string PngPaletteSizeSelectorObjectName = "PngPaletteSizeSelector";
    private const int LogicalPixelsPerPaletteTile = 16;
    private const int PngPaletteCellSize = 10;
    private const float PngPaletteDisplaySize = 176f;
    private const float SelectionBorderThickness = 2f;
    private static readonly Vector2 PngPaletteLabelPosition = new Vector2(0f, -334f);
    private static readonly Vector2 PngPaletteViewportPosition = new Vector2(0f, -382f);

    private static readonly Color SelectedPngTileOutlineColor = new Color(1f, 0.86f, 0.08f, 1f);
    private static readonly Color SelectionPreviewFillColor = new Color(0f, 0.55f, 1f, 0.03f);
    private static readonly Color SelectionPreviewBorderColor = new Color(1f, 0.95f, 0f, 1f);

    private readonly ColorWheelPickerWindow owner;
    private readonly MapEditorManager manager;
    private readonly Dictionary<string, Outline> tileOutlines = new Dictionary<string, Outline>();

    private RectTransform viewport;
    private RectTransform contentRoot;
    private RectTransform selectionOverlay;
    private Image selectionOverlayImage;
    private RectTransform selectionPreviewRect;
    private Image selectionPreviewImage;
    private readonly Image[] selectionPreviewBorders = new Image[4];
    private GridLayoutGroup grid;
    private RawImage paletteImage;
    private Transform rootParent;
    private Text paletteLabel;
    private Outline selectedOutline;
    private Texture2D sourceTexture;
    private RectInt sourceContentRect;
    private string sourcePath;
    private Vector2Int selectionStart;
    private RectInt currentSelection;
    private bool hasSelectionStart;
    private float cellSize = PngPaletteCellSize;
    private int paletteGridSize = 16;
    private int paletteColumns = 16;
    private int paletteRows = 16;

    public ColorWheelPngPaletteView(ColorWheelPickerWindow owner, MapEditorManager manager)
    {
        this.owner = owner;
        this.manager = manager;
        paletteGridSize = manager == null ? 16 : manager.GetPngPaletteGridSize();
    }

    public void CreateArea(Transform parent)
    {
        rootParent = parent;
        GameObject labelObject = new GameObject("PngPaletteLabel", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(parent, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = PngPaletteLabelPosition;
        labelRect.sizeDelta = new Vector2(-16f, 22f);

        paletteLabel = labelObject.GetComponent<Text>();
        ConfigurePaletteLabel();
        CreateGridSizeSelector(parent);

        GameObject viewportObject = new GameObject(PngPaletteViewportObjectName, typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(PngPaletteZoomInput));
        viewportObject.transform.SetParent(parent, false);

        viewport = viewportObject.GetComponent<RectTransform>();
        viewport.anchorMin = new Vector2(0.5f, 1f);
        viewport.anchorMax = new Vector2(0.5f, 1f);
        viewport.pivot = new Vector2(0.5f, 1f);
        viewport.anchoredPosition = PngPaletteViewportPosition;
        UpdateViewportSize();

        Image background = viewportObject.GetComponent<Image>();
        background.color = Color.clear;
        background.raycastTarget = false;

        PngPaletteZoomInput zoomInput = viewportObject.GetComponent<PngPaletteZoomInput>();
        zoomInput.Initialize(owner);

        GameObject rootObject = new GameObject(PngPaletteContentObjectName, typeof(RectTransform), typeof(GridLayoutGroup));
        rootObject.transform.SetParent(viewportObject.transform, false);

        contentRoot = rootObject.GetComponent<RectTransform>();
        contentRoot.anchorMin = new Vector2(0f, 1f);
        contentRoot.anchorMax = new Vector2(0f, 1f);
        contentRoot.pivot = new Vector2(0f, 1f);
        contentRoot.anchoredPosition = Vector2.zero;
        contentRoot.sizeDelta = new Vector2(PngPaletteDisplaySize, PngPaletteDisplaySize);

        grid = rootObject.GetComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(0, 0, 0, 0);
        grid.spacing = Vector2.zero;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = paletteColumns;
        ApplyZoom();
        EnsurePaletteImage();
        EnsureSelectionOverlay();
    }

    public void CacheExistingReferences(Transform parent)
    {
        rootParent = parent;
        Transform labelTransform = parent.Find(PngPaletteLabelObjectName);
        Transform viewportTransform = MapEditorObjectUtility.FindAndRenameChild(parent, PngPaletteViewportObjectName, LegacyPngPaletteViewportObjectName);
        Transform contentTransform = viewportTransform == null ? null : MapEditorObjectUtility.FindAndRenameChild(viewportTransform, PngPaletteContentObjectName, LegacyPngPaletteContentObjectName);

        if (labelTransform != null && labelTransform is RectTransform labelRect)
        {
            labelRect.anchoredPosition = PngPaletteLabelPosition;
            paletteLabel = labelTransform.GetComponent<Text>();
            ConfigurePaletteLabel();
        }

        CreateGridSizeSelector(parent);

        if (viewportTransform != null)
        {
            viewport = viewportTransform.GetComponent<RectTransform>();

            if (viewport != null)
            {
                viewport.anchoredPosition = PngPaletteViewportPosition;
                UpdateViewportSize();

                Image background = viewport.GetComponent<Image>();
                if (background != null)
                {
                    background.color = Color.clear;
                }
            }

            PngPaletteZoomInput zoomInput = viewportTransform.GetComponent<PngPaletteZoomInput>();

            if (zoomInput == null)
            {
                zoomInput = viewportTransform.gameObject.AddComponent<PngPaletteZoomInput>();
            }

            zoomInput.Initialize(owner);
        }

        if (contentTransform != null)
        {
            contentRoot = contentTransform.GetComponent<RectTransform>();
            grid = contentTransform.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                grid.padding = new RectOffset(0, 0, 0, 0);
                grid.spacing = Vector2.zero;
                grid.constraintCount = paletteColumns;
            }
            EnsurePaletteImage();
            EnsureSelectionOverlay();
        }
    }

    private void ConfigurePaletteLabel()
    {
        if (paletteLabel == null)
        {
            return;
        }

        paletteLabel.text = MapEditorTilesetLibraryService.TryGetByAtlasPath(sourcePath, out MapEditorTilesetDefinition tileset)
            ? "타일셋: " + tileset.displayName + " (" + tileset.tileWidth + "x" + tileset.tileHeight + "px)"
            : "팔레트 " + paletteGridSize + "x" + paletteGridSize;
        paletteLabel.font = MapEditorFontProvider.Default;
        paletteLabel.fontSize = 13;
        paletteLabel.alignment = TextAnchor.MiddleLeft;
        paletteLabel.color = Color.white;
    }

    private void CreateGridSizeSelector(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        Transform existing = parent.Find(PngPaletteSizeSelectorObjectName);
        if (existing != null)
        {
            MapEditorObjectUtility.DestroyObject(existing.gameObject);
        }

        GameObject selectorObject = new GameObject(PngPaletteSizeSelectorObjectName, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        selectorObject.transform.SetParent(parent, false);

        RectTransform rect = selectorObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -358f);
        rect.sizeDelta = new Vector2(-16f, 20f);

        HorizontalLayoutGroup layout = selectorObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        foreach (int size in MapEditorManager.PngPaletteGridSizeOptions)
        {
            CreateGridSizeButton(selectorObject.transform, size);
        }
    }

    private void CreateGridSizeButton(Transform parent, int size)
    {
        GameObject buttonObject = new GameObject("PngGrid" + size + "Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(MapEditorToolbarButton));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = size == paletteGridSize
            ? new Color(0.18f, 0.48f, 0.95f, 1f)
            : new Color(0.25f, 0.25f, 0.25f, 1f);

        MapEditorToolbarButton action = buttonObject.GetComponent<MapEditorToolbarButton>();
        action.manager = manager;
        action.action = MapEditorToolbarAction.PngPaletteGridSize;
        action.intArgument = size;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.text = size.ToString();
        text.font = MapEditorFontProvider.Default;
        text.fontSize = 9;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
    }

    public void SetGridSize(int gridSize)
    {
        int normalized = MapEditorManager.NormalizePngPaletteGridSize(gridSize);
        if (paletteGridSize == normalized)
        {
            return;
        }

        CancelSelection();
        currentSelection = new RectInt();
        selectedOutline = null;
        tileOutlines.Clear();
        manager?.ClearSelectedImageBrush();
        paletteGridSize = normalized;
        paletteColumns = paletteGridSize;
        paletteRows = paletteGridSize;
        UpdateViewportSize();
        ConfigurePaletteLabel();
        CreateGridSizeSelector(rootParent);
        ResetView();

        if (sourceTexture != null)
        {
            SetPalette(sourceTexture, sourcePath);
        }
    }

    public void SetPalette(Texture2D sourceTexture, string sourcePath)
    {
        if (contentRoot == null || sourceTexture == null)
        {
            return;
        }

        this.sourceTexture = sourceTexture;
        this.sourcePath = sourcePath;
        sourceContentRect = MapEditorTilesetLibraryService.IsNormalizedAtlasPath(sourcePath)
            ? new RectInt(0, 0, sourceTexture.width, sourceTexture.height)
            : MapEditorPngTilesetService.GetContentPixelRect(sourceTexture);
        paletteColumns = paletteGridSize;
        paletteRows = paletteGridSize;
        UpdateViewportSize();
        ConfigurePaletteLabel();
        FitPaletteToViewport();

        foreach (Transform child in contentRoot)
        {
            if (child != selectionOverlay && child != selectionPreviewRect && (paletteImage == null || child != paletteImage.transform))
            {
                MapEditorObjectUtility.DestroyObject(child.gameObject);
            }
        }

        selectedOutline = null;
        tileOutlines.Clear();

        EnsurePaletteImage();
        paletteImage.texture = sourceTexture;
        UpdatePaletteImageUvRect();
        EnsureSelectionOverlay();
    }

    public void BeginSelection(Vector2 screenPosition, Camera eventCamera)
    {
        if (!TryGetLogicalPoint(screenPosition, eventCamera, out selectionStart))
        {
            hasSelectionStart = false;
            HideSelectionOverlay();
            return;
        }

        hasSelectionStart = true;
        UpdateSelection(screenPosition, eventCamera);
    }

    public void UpdateSelection(Vector2 screenPosition, Camera eventCamera)
    {
        if (!hasSelectionStart || !TryGetLogicalPoint(screenPosition, eventCamera, out Vector2Int currentPoint))
        {
            return;
        }

        currentSelection = CreateSnappedSelection(selectionStart, currentPoint);
        ShowSelectionOverlay(currentSelection);
    }

    public void EndSelection(Vector2 screenPosition, Camera eventCamera)
    {
        if (!hasSelectionStart)
        {
            return;
        }

        UpdateSelection(screenPosition, eventCamera);
        hasSelectionStart = false;

        if (sourceTexture == null || manager == null || currentSelection.width <= 0 || currentSelection.height <= 0)
        {
            return;
        }

        Sprite sprite = CreateSelectionSprite(currentSelection);

        if (sprite != null)
        {
            int widthInTiles = currentSelection.width / LogicalPixelsPerPaletteTile;
            int heightInTiles = currentSelection.height / LogicalPixelsPerPaletteTile;
            bool isMultiTileSelection = manager.IsWholeTilePaintMode()
                && currentSelection.x % LogicalPixelsPerPaletteTile == 0
                && currentSelection.y % LogicalPixelsPerPaletteTile == 0
                && currentSelection.width % LogicalPixelsPerPaletteTile == 0
                && currentSelection.height % LogicalPixelsPerPaletteTile == 0
                && (widthInTiles > 1 || heightInTiles > 1);

            if (isMultiTileSelection)
            {
                manager.SelectImageTileRegion(
                    sprite,
                    sourcePath,
                    paletteGridSize,
                    currentSelection.x / LogicalPixelsPerPaletteTile,
                    currentSelection.y / LogicalPixelsPerPaletteTile,
                    widthInTiles,
                    heightInTiles);
            }
            else
            {
                manager.SelectImageBrush(sprite, sourcePath, GetSelectionImageIndex(currentSelection));
            }
        }
    }

    public void CancelSelection()
    {
        hasSelectionStart = false;
        HideSelectionOverlay();
    }

    public void Zoom(float direction, Vector2 viewportLocalPoint)
    {
        if (contentRoot == null)
        {
            return;
        }

        float previousCellSize = cellSize;
        float minimumCellSize = GetFitCellSize();
        float nextCellSize = Mathf.Clamp(cellSize + direction * 2f, minimumCellSize, 28f);

        if (Mathf.Approximately(previousCellSize, nextCellSize))
        {
            return;
        }

        Vector2 contentPointBeforeZoom = viewportLocalPoint - contentRoot.anchoredPosition;
        float zoomRatio = nextCellSize / previousCellSize;

        cellSize = nextCellSize;
        ApplyZoom();

        contentRoot.anchoredPosition = viewportLocalPoint - contentPointBeforeZoom * zoomRatio;
        ClampPosition();
    }

    public void Pan(Vector2 delta)
    {
        if (contentRoot == null)
        {
            return;
        }

        contentRoot.anchoredPosition += delta;
        ClampPosition();
    }

    public void ResetView()
    {
        if (contentRoot == null)
        {
            return;
        }

        FitPaletteToViewport();
    }

    private void FitPaletteToViewport()
    {
        if (contentRoot == null)
        {
            return;
        }

        cellSize = GetFitCellSize();
        ApplyZoom();
        contentRoot.anchoredPosition = Vector2.zero;
        ClampPosition();
    }

    private void UpdateViewportSize()
    {
        if (viewport == null)
        {
            return;
        }

        viewport.sizeDelta = new Vector2(PngPaletteDisplaySize, PngPaletteDisplaySize);
    }

    private float GetFitCellSize()
    {
        if (viewport == null || grid == null)
        {
            return PngPaletteCellSize;
        }

        Vector2 viewportSize = viewport.rect.size;
        if (viewportSize.x <= 0f || viewportSize.y <= 0f)
        {
            viewportSize = viewport.sizeDelta;
        }

        float availableWidth = Mathf.Max(1f, viewportSize.x - grid.padding.left - grid.padding.right);
        float availableHeight = Mathf.Max(1f, viewportSize.y - grid.padding.top - grid.padding.bottom);
        float widthCellSize = availableWidth / Mathf.Max(1, paletteColumns);
        float heightCellSize = availableHeight / Mathf.Max(1, paletteRows);
        return Mathf.Max(0.25f, Mathf.Min(widthCellSize, heightCellSize));
    }

    public void SelectTile(string imagePath, int imageIndex)
    {
        if (tileOutlines.TryGetValue(GetTileKey(imagePath, imageIndex), out Outline outline))
        {
            SelectOutline(outline);
        }
    }

    private Sprite CreatePaletteSprite(Texture2D sourceTexture, int gridX, int gridY)
    {
        float sourceCellWidth = sourceContentRect.width / (float)paletteGridSize;
        float sourceCellHeight = sourceContentRect.height / (float)paletteGridSize;
        int pixelX = sourceContentRect.x + Mathf.FloorToInt(gridX * sourceCellWidth);
        int pixelY = sourceContentRect.y + Mathf.FloorToInt(gridY * sourceCellHeight);
        int pixelWidth = Mathf.Max(1, sourceContentRect.x + Mathf.FloorToInt((gridX + 1) * sourceCellWidth) - pixelX);
        int pixelHeight = Mathf.Max(1, sourceContentRect.y + Mathf.FloorToInt((gridY + 1) * sourceCellHeight) - pixelY);

        Rect rect = new Rect(pixelX, pixelY, pixelWidth, pixelHeight);
        Sprite sprite = Sprite.Create(sourceTexture, rect, new Vector2(0.5f, 0.5f), Mathf.Max(pixelWidth, pixelHeight));
        sprite.name = sourceTexture.name + "_Tile_" + gridX + "_" + gridY;
        return sprite;
    }

    private void CreateTileButton(Sprite sprite, string imagePath, int imageIndex, int x, int y)
    {
        GameObject buttonObject = new GameObject("PngTile_" + x + "_" + y, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline), typeof(PngPaletteTile));
        buttonObject.transform.SetParent(contentRoot, false);

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.raycastTarget = true;
        image.preserveAspect = false;

        Outline outline = buttonObject.GetComponent<Outline>();
        outline.effectColor = SelectedPngTileOutlineColor;
        outline.effectDistance = new Vector2(3f, -3f);
        outline.enabled = false;
        tileOutlines[GetTileKey(imagePath, imageIndex)] = outline;

        PngPaletteTile paletteTile = buttonObject.GetComponent<PngPaletteTile>();
        paletteTile.Initialize(sprite, imagePath, imageIndex, manager);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
    }

    private int GetTileIndex(int x, int y)
    {
        return MapEditorPngTilesetService.EncodePaletteTileIndex(paletteGridSize, y * paletteColumns + x);
    }

    private string GetTileKey(string imagePath, int imageIndex)
    {
        return imagePath + "#" + imageIndex;
    }

    private void SelectOutline(Outline outline)
    {
        if (selectedOutline != null)
        {
            selectedOutline.enabled = false;
        }

        selectedOutline = outline;

        if (selectedOutline != null)
        {
            selectedOutline.effectColor = SelectedPngTileOutlineColor;
            selectedOutline.effectDistance = new Vector2(3f, -3f);
            selectedOutline.enabled = true;
        }
    }

    private void ApplyZoom()
    {
        if (grid == null || contentRoot == null)
        {
            return;
        }

        grid.cellSize = new Vector2(cellSize, cellSize);
        grid.constraintCount = paletteColumns;
        contentRoot.sizeDelta = new Vector2(
            paletteColumns * cellSize + grid.padding.left + grid.padding.right,
            paletteRows * cellSize + grid.padding.top + grid.padding.bottom
        );
        LayoutPaletteImage();
        EnsureSelectionOverlay();

        if (selectionOverlay != null && selectionOverlay.gameObject.activeSelf)
        {
            ShowSelectionOverlay(currentSelection);
        }
    }

    private void ClampPosition()
    {
        if (viewport == null || contentRoot == null)
        {
            return;
        }

        Vector2 viewportSize = viewport.rect.size;
        Vector2 contentSize = contentRoot.sizeDelta;
        Vector2 position = contentRoot.anchoredPosition;

        if (contentSize.x <= viewportSize.x)
        {
            position.x = 0f;
        }
        else
        {
            float minX = viewportSize.x - contentSize.x;
            position.x = Mathf.Clamp(position.x, minX, 0f);
        }

        if (contentSize.y <= viewportSize.y)
        {
            position.y = 0f;
        }
        else
        {
            float maxY = contentSize.y - viewportSize.y;
            position.y = Mathf.Clamp(position.y, 0f, maxY);
        }

        contentRoot.anchoredPosition = position;
    }

    private void EnsureSelectionOverlay()
    {
        if (contentRoot == null)
        {
            return;
        }

        if (selectionOverlay == null)
        {
            Transform existingInput = FindReusableSelectionChild(PngDragSelectionInputObjectName);

            if (existingInput != null)
            {
                selectionOverlay = existingInput.GetComponent<RectTransform>();
                selectionOverlayImage = existingInput.GetComponent<Image>();
            }
        }

        if (selectionOverlay == null)
        {
            GameObject overlayObject = new GameObject(PngDragSelectionInputObjectName, typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(PngPaletteSelectionInput));
            overlayObject.transform.SetParent(contentRoot, false);
            selectionOverlay = overlayObject.GetComponent<RectTransform>();
            selectionOverlayImage = overlayObject.GetComponent<Image>();

            PngPaletteSelectionInput input = overlayObject.GetComponent<PngPaletteSelectionInput>();
            input.Initialize(owner);
        }

        LayoutElement overlayLayout = selectionOverlay.GetComponent<LayoutElement>();

        if (overlayLayout == null)
        {
            overlayLayout = selectionOverlay.gameObject.AddComponent<LayoutElement>();
        }

        overlayLayout.ignoreLayout = true;
        selectionOverlay.gameObject.hideFlags = HideFlags.HideAndDontSave;

        if (selectionPreviewRect == null)
        {
            Transform existingPreview = FindReusableSelectionChild(PngDragSelectionPreviewObjectName);

            if (existingPreview != null)
            {
                selectionPreviewRect = existingPreview.GetComponent<RectTransform>();
                selectionPreviewImage = existingPreview.GetComponent<Image>();
            }
        }

        if (selectionPreviewRect == null)
        {
            GameObject previewObject = new GameObject(PngDragSelectionPreviewObjectName, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            previewObject.transform.SetParent(contentRoot, false);
            selectionPreviewRect = previewObject.GetComponent<RectTransform>();
            selectionPreviewImage = previewObject.GetComponent<Image>();
        }

        if (selectionPreviewImage == null)
        {
            selectionPreviewImage = selectionPreviewRect.gameObject.AddComponent<Image>();
        }

        LayoutElement previewLayout = selectionPreviewRect.GetComponent<LayoutElement>();

        if (previewLayout == null)
        {
            previewLayout = selectionPreviewRect.gameObject.AddComponent<LayoutElement>();
        }

        previewLayout.ignoreLayout = true;
        selectionPreviewRect.gameObject.hideFlags = HideFlags.HideAndDontSave;
        RemoveDuplicateSelectionChildren(PngDragSelectionInputObjectName, selectionOverlay);
        RemoveDuplicateSelectionChildren(PngDragSelectionPreviewObjectName, selectionPreviewRect);

        selectionOverlay.anchorMin = new Vector2(0f, 1f);
        selectionOverlay.anchorMax = new Vector2(0f, 1f);
        selectionOverlay.pivot = new Vector2(0f, 1f);
        selectionOverlay.anchoredPosition = new Vector2(grid == null ? 0f : grid.padding.left, grid == null ? 0f : -grid.padding.top);
        selectionOverlay.sizeDelta = new Vector2(paletteColumns * cellSize, paletteRows * cellSize);
        selectionOverlay.SetAsLastSibling();

        if (selectionOverlayImage != null)
        {
            selectionOverlayImage.color = Color.clear;
            selectionOverlayImage.raycastTarget = true;
        }

        selectionPreviewImage.color = Color.clear;
        selectionPreviewImage.raycastTarget = false;
        RemoveLegacyOutline(selectionPreviewRect.gameObject);
        EnsureSelectionPreviewBorders();

        selectionPreviewRect.gameObject.SetActive(false);
    }

    private void EnsurePaletteImage()
    {
        if (contentRoot == null)
        {
            return;
        }

        if (paletteImage == null)
        {
            Transform existing = contentRoot.Find(PngPaletteImageObjectName);
            if (existing != null)
            {
                paletteImage = existing.GetComponent<RawImage>();
            }
        }

        if (paletteImage == null)
        {
            GameObject imageObject = new GameObject(PngPaletteImageObjectName, typeof(RectTransform), typeof(RawImage), typeof(LayoutElement));
            imageObject.transform.SetParent(contentRoot, false);
            paletteImage = imageObject.GetComponent<RawImage>();
        }

        LayoutElement layout = paletteImage.GetComponent<LayoutElement>();
        layout.ignoreLayout = true;
        paletteImage.raycastTarget = false;
        paletteImage.color = Color.white;
        UpdatePaletteImageUvRect();
        paletteImage.transform.SetAsFirstSibling();
        LayoutPaletteImage();
    }

    private void UpdatePaletteImageUvRect()
    {
        if (paletteImage == null)
        {
            return;
        }

        if (sourceTexture == null || sourceContentRect.width <= 0 || sourceContentRect.height <= 0)
        {
            paletteImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            return;
        }

        paletteImage.uvRect = new Rect(
            sourceContentRect.x / (float)sourceTexture.width,
            sourceContentRect.y / (float)sourceTexture.height,
            sourceContentRect.width / (float)sourceTexture.width,
            sourceContentRect.height / (float)sourceTexture.height
        );
    }

    private void LayoutPaletteImage()
    {
        if (paletteImage == null)
        {
            return;
        }

        RectTransform rect = paletteImage.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(grid == null ? 0f : grid.padding.left, grid == null ? 0f : -grid.padding.top);
        rect.sizeDelta = new Vector2(paletteColumns * cellSize, paletteRows * cellSize);
    }

    private int GetSelectionImageIndex(RectInt selection)
    {
        int tileX = selection.x / LogicalPixelsPerPaletteTile;
        int tileYFromTop = selection.y / LogicalPixelsPerPaletteTile;
        int localX = selection.x % LogicalPixelsPerPaletteTile;
        int localYFromTop = selection.y % LogicalPixelsPerPaletteTile;

        bool fitsSingleTile = tileX >= 0
            && tileX < paletteColumns
            && tileYFromTop >= 0
            && tileYFromTop < paletteRows
            && localX + selection.width <= LogicalPixelsPerPaletteTile
            && localYFromTop + selection.height <= LogicalPixelsPerPaletteTile;

        if (!fitsSingleTile || selection.width != selection.height)
        {
            return -1;
        }

        int sourceY = paletteRows - 1 - tileYFromTop;
        int baseIndex = MapEditorPngTilesetService.EncodePaletteTileIndex(paletteGridSize, sourceY * paletteColumns + tileX);

        if (selection.width == LogicalPixelsPerPaletteTile && localX == 0 && localYFromTop == 0)
        {
            return baseIndex;
        }

        int localYFromBottom = LogicalPixelsPerPaletteTile - localYFromTop - selection.height;
        return MapEditorPngTilesetService.EncodeSubTileIndex(baseIndex, selection.width, localX, localYFromBottom);
    }

    private static void RemoveLegacyOutline(GameObject target)
    {
        Outline outline = target.GetComponent<Outline>();

        if (outline != null)
        {
            MapEditorObjectUtility.DestroyObject(outline);
        }
    }

    private void EnsureSelectionPreviewBorders()
    {
        if (selectionPreviewRect == null)
        {
            return;
        }

        for (int i = 0; i < selectionPreviewBorders.Length; i++)
        {
            if (selectionPreviewBorders[i] != null)
            {
                selectionPreviewBorders[i].raycastTarget = false;
                selectionPreviewBorders[i].color = SelectionPreviewBorderColor;
                continue;
            }

            GameObject borderObject = new GameObject("PngSelectionBorder", typeof(RectTransform), typeof(Image));
            borderObject.transform.SetParent(selectionPreviewRect, false);
            borderObject.hideFlags = HideFlags.HideAndDontSave;

            Image borderImage = borderObject.GetComponent<Image>();
            borderImage.color = SelectionPreviewBorderColor;
            borderImage.raycastTarget = false;
            selectionPreviewBorders[i] = borderImage;
        }
    }

    private void LayoutSelectionPreviewBorders()
    {
        EnsureSelectionPreviewBorders();

        LayoutHorizontalBorder(selectionPreviewBorders[0].rectTransform, true);
        LayoutHorizontalBorder(selectionPreviewBorders[1].rectTransform, false);
        LayoutVerticalBorder(selectionPreviewBorders[2].rectTransform, false);
        LayoutVerticalBorder(selectionPreviewBorders[3].rectTransform, true);
    }

    private static void LayoutHorizontalBorder(RectTransform border, bool top)
    {
        float y = top ? 1f : 0f;
        border.anchorMin = new Vector2(0f, y);
        border.anchorMax = new Vector2(1f, y);
        border.pivot = new Vector2(0.5f, y);
        border.anchoredPosition = Vector2.zero;
        border.sizeDelta = new Vector2(0f, SelectionBorderThickness);
    }

    private static void LayoutVerticalBorder(RectTransform border, bool right)
    {
        float x = right ? 1f : 0f;
        border.anchorMin = new Vector2(x, 0f);
        border.anchorMax = new Vector2(x, 1f);
        border.pivot = new Vector2(x, 0.5f);
        border.anchoredPosition = Vector2.zero;
        border.sizeDelta = new Vector2(SelectionBorderThickness, 0f);
    }

    private Transform FindReusableSelectionChild(string objectName)
    {
        for (int i = 0; i < contentRoot.childCount; i++)
        {
            Transform child = contentRoot.GetChild(i);

            if (child != null && child.name == objectName)
            {
                return child;
            }
        }

        return null;
    }

    private void RemoveDuplicateSelectionChildren(string objectName, RectTransform keep)
    {
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = contentRoot.GetChild(i);

            if (child == null || child == keep || child.name != objectName)
            {
                continue;
            }

            MapEditorObjectUtility.DestroyObject(child.gameObject);
        }
    }

    private void HideSelectionOverlay()
    {
        if (selectionPreviewImage != null)
        {
            selectionPreviewImage.color = Color.clear;
        }

        if (selectionPreviewRect != null)
        {
            selectionPreviewRect.gameObject.SetActive(false);
        }
    }

    private bool TryGetLogicalPoint(Vector2 screenPosition, Camera eventCamera, out Vector2Int logicalPoint)
    {
        logicalPoint = Vector2Int.zero;

        if (contentRoot == null || grid == null)
        {
            return false;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(contentRoot, screenPosition, eventCamera, out Vector2 localPoint))
        {
            return false;
        }

        float displayX = localPoint.x - grid.padding.left;
        float displayY = -localPoint.y - grid.padding.top;
        int logicalWidth = paletteColumns * LogicalPixelsPerPaletteTile;
        int logicalHeight = paletteRows * LogicalPixelsPerPaletteTile;
        float displayWidth = paletteColumns * cellSize;
        float displayHeight = paletteRows * cellSize;

        if (displayX < 0f || displayY < 0f || displayX > displayWidth || displayY > displayHeight)
        {
            return false;
        }

        int logicalX = Mathf.Clamp(Mathf.FloorToInt(displayX / displayWidth * logicalWidth), 0, logicalWidth - 1);
        int logicalY = Mathf.Clamp(Mathf.FloorToInt(displayY / displayHeight * logicalHeight), 0, logicalHeight - 1);
        logicalPoint = new Vector2Int(logicalX, logicalY);
        return true;
    }

    private RectInt CreateSnappedSelection(Vector2Int start, Vector2Int end)
    {
        int pixelsPerTile = manager == null ? 16 : manager.GetExportCellPixels();
        int step = manager != null && manager.IsWholeTilePaintMode()
            ? LogicalPixelsPerPaletteTile
            : Mathf.Max(1, 16 / Mathf.Max(1, MapEditorManager.NormalizeExportCellPixels(pixelsPerTile)));
        int startX = SnapDown(start.x, step);
        int startY = SnapDown(start.y, step);
        int endX = SnapDown(end.x, step);
        int endY = SnapDown(end.y, step);
        int minX = Mathf.Min(startX, endX);
        int minY = Mathf.Min(startY, endY);
        int maxX = Mathf.Max(startX, endX) + step;
        int maxY = Mathf.Max(startY, endY) + step;
        int logicalWidth = paletteColumns * LogicalPixelsPerPaletteTile;
        int logicalHeight = paletteRows * LogicalPixelsPerPaletteTile;
        maxX = Mathf.Clamp(maxX, 1, logicalWidth);
        maxY = Mathf.Clamp(maxY, 1, logicalHeight);
        return new RectInt(minX, minY, Mathf.Max(step, maxX - minX), Mathf.Max(step, maxY - minY));
    }

    private int SnapDown(int value, int step)
    {
        int logicalMax = Mathf.Max(paletteColumns, paletteRows) * LogicalPixelsPerPaletteTile;
        return Mathf.Clamp((value / step) * step, 0, logicalMax - 1);
    }

    private void ShowSelectionOverlay(RectInt selection)
    {
        EnsureSelectionOverlay();

        if (selectionOverlay == null || selectionPreviewRect == null || selectionPreviewImage == null)
        {
            return;
        }

        float logicalWidth = paletteColumns * LogicalPixelsPerPaletteTile;
        float logicalHeight = paletteRows * LogicalPixelsPerPaletteTile;
        float x = selection.x / logicalWidth * paletteColumns * cellSize;
        float y = selection.y / logicalHeight * paletteRows * cellSize;
        float width = selection.width / logicalWidth * paletteColumns * cellSize;
        float height = selection.height / logicalHeight * paletteRows * cellSize;
        selectionPreviewRect.gameObject.SetActive(true);
        selectionPreviewRect.anchorMin = new Vector2(0f, 1f);
        selectionPreviewRect.anchorMax = new Vector2(0f, 1f);
        selectionPreviewRect.pivot = new Vector2(0f, 1f);
        selectionPreviewRect.anchoredPosition = new Vector2((grid == null ? 0f : grid.padding.left) + x, -(grid == null ? 0f : grid.padding.top) - y);
        selectionPreviewRect.sizeDelta = new Vector2(width, height);
        selectionPreviewImage.color = SelectionPreviewFillColor;
        selectionPreviewImage.raycastTarget = false;
        LayoutSelectionPreviewBorders();
        selectionOverlay.SetAsLastSibling();
        selectionPreviewRect.SetAsLastSibling();
    }

    private Sprite CreateSelectionSprite(RectInt selection)
    {
        int outputWidth = Mathf.Max(1, selection.width);
        int outputHeight = Mathf.Max(1, selection.height);
        Texture2D texture = new Texture2D(outputWidth, outputHeight, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int y = 0; y < outputHeight; y++)
        {
            for (int x = 0; x < outputWidth; x++)
            {
                float logicalWidth = paletteColumns * LogicalPixelsPerPaletteTile;
                float logicalHeight = paletteRows * LogicalPixelsPerPaletteTile;
                float u = (selection.x + x + 0.5f) / logicalWidth;
                float vTop = (selection.y + y + 0.5f) / logicalHeight;
                int sourceX = Mathf.Clamp(sourceContentRect.x + Mathf.FloorToInt(u * sourceContentRect.width), sourceContentRect.x, sourceContentRect.xMax - 1);
                int sourceY = Mathf.Clamp(sourceContentRect.y + Mathf.FloorToInt((1f - vTop) * sourceContentRect.height), sourceContentRect.y, sourceContentRect.yMax - 1);
                texture.SetPixel(x, outputHeight - 1 - y, sourceTexture.GetPixel(sourceX, sourceY));
            }
        }

        texture.Apply();
        Rect rect = new Rect(0, 0, outputWidth, outputHeight);
        Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), Mathf.Max(outputWidth, outputHeight));
        sprite.name = sourceTexture.name + "_Selection_" + selection.x + "_" + selection.y + "_" + selection.width + "_" + selection.height;
        return sprite;
    }

}
