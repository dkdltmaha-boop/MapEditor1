using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapEditorBrushCursorPreview
{
    private static readonly Color BrushOutlineColor = Color.black;
    private static readonly Color EraserOutlineColor = new Color(1f, 0.1f, 0.1f, 1f);
    private static readonly Color LayerEraserOutlineColor = new Color(1f, 0.75f, 0.1f, 1f);
    private static readonly Color SelectionFillColor = new Color(0.2f, 0.55f, 1f, 0.14f);
    private static readonly Color SelectionOutlineColor = new Color(0.1f, 0.35f, 1f, 1f);
    private static readonly Color AreaFillColor = new Color(1f, 0.85f, 0.1f, 0.16f);
    private static readonly Color AreaFillOutlineColor = new Color(1f, 0.7f, 0f, 1f);

    private readonly List<Image> images = new List<Image>();
    private readonly string overlayObjectName;
    private RectTransform overlay;

    public MapEditorBrushCursorPreview(string overlayObjectName = "MapEditor_BrushCursorPreview")
    {
        this.overlayObjectName = string.IsNullOrWhiteSpace(overlayObjectName)
            ? "MapEditor_BrushCursorPreview"
            : overlayObjectName;
    }

    public void UpdateCellGuides(
        bool showGuides,
        GridGenerator gridGenerator,
        MapData mapData,
        IReadOnlyCollection<Vector2Int> guideCells,
        Color fillColor,
        Color outlineColor)
    {
        if (!showGuides || gridGenerator == null || gridGenerator.gridParent == null || mapData == null
            || guideCells == null || guideCells.Count == 0)
        {
            Hide();
            return;
        }

        EnsureOverlay(gridGenerator);
        SyncOverlayTransform(gridGenerator);
        if (overlay == null)
        {
            return;
        }

        overlay.SetAsLastSibling();
        UpdateCellSetPreview(gridGenerator, mapData, guideCells, fillColor, outlineColor);
    }

    public void Update(
        bool showPreview,
        GridGenerator gridGenerator,
        GridCell hoveredCell,
        MapData mapData,
        int brushSize,
        Color selectedColor,
        Sprite selectedImageBrush,
        Sprite[] selectedAnimationFrames,
        float selectedAnimationFps,
        bool selectedAnimationLoop,
        int selectedImageRotation,
        bool selectedImageFlipX,
        bool selectedImageFlipY,
        bool paintWholeTile,
        int tileRegionWidth,
        int tileRegionHeight,
        int pixelsPerTile,
        int subPixelBrushSide,
        int hoveredSubPixelX,
        int hoveredSubPixelY,
        bool showSubPixelPreview,
        float alpha,
        RectInt? areaFillPreviewRect,
        RectInt? selectionPreviewRect,
        IReadOnlyCollection<Vector2Int> selectionPreviewCells,
        IReadOnlyCollection<Vector2Int> linePreviewCells)
    {
        if (!showPreview || gridGenerator == null || gridGenerator.gridParent == null || mapData == null)
        {
            Hide();
            return;
        }

        EnsureOverlay(gridGenerator);
        SyncOverlayTransform(gridGenerator);

        if (overlay == null)
        {
            return;
        }

        overlay.SetAsLastSibling();

        if (selectionPreviewCells != null && selectionPreviewCells.Count > 0)
        {
            UpdateCellSetPreview(gridGenerator, mapData, selectionPreviewCells, SelectionFillColor, SelectionOutlineColor);
            return;
        }

        if (linePreviewCells != null && linePreviewCells.Count > 0)
        {
            UpdateLinePreview(
                gridGenerator,
                mapData,
                linePreviewCells,
                selectedColor,
                selectedImageBrush,
                selectedAnimationFrames,
                selectedAnimationFps,
                selectedAnimationLoop,
                selectedImageRotation,
                selectedImageFlipX,
                selectedImageFlipY,
                alpha);
            return;
        }

        if (selectionPreviewRect.HasValue)
        {
            UpdateRectPreview(gridGenerator, mapData, selectionPreviewRect.Value, SelectionFillColor, SelectionOutlineColor);
            return;
        }

        if (areaFillPreviewRect.HasValue)
        {
            UpdateRectPreview(gridGenerator, mapData, areaFillPreviewRect.Value, AreaFillColor, AreaFillOutlineColor);
            return;
        }

        if (hoveredCell == null)
        {
            Hide();
            return;
        }

        if (paintWholeTile && selectedImageBrush != null && (tileRegionWidth > 1 || tileRegionHeight > 1))
        {
            UpdateTileRegionPreview(
                gridGenerator,
                hoveredCell,
                selectedImageBrush,
                tileRegionWidth,
                tileRegionHeight,
                selectedImageFlipX,
                selectedImageFlipY,
                alpha);
            return;
        }

        if (selectedImageBrush != null && !paintWholeTile)
        {
            UpdateSpriteSubPixelPreview(gridGenerator, mapData, hoveredCell, selectedImageBrush, pixelsPerTile, hoveredSubPixelX, hoveredSubPixelY, alpha);
            return;
        }

        if (showSubPixelPreview)
        {
            UpdateSubPixelPreview(gridGenerator, mapData, hoveredCell, selectedColor, pixelsPerTile, subPixelBrushSide, hoveredSubPixelX, hoveredSubPixelY, alpha);
            return;
        }

        int requiredCount = brushSize * brushSize;

        while (images.Count < requiredCount)
        {
            images.Add(CreateCursorImage());
        }

        for (int i = 0; i < images.Count; i++)
        {
            images[i].gameObject.SetActive(i < requiredCount);
        }

        int startX = hoveredCell.X - brushSize / 2;
        int startY = hoveredCell.Y - brushSize / 2;
        int imageIndex = 0;

        for (int y = 0; y < brushSize; y++)
        {
            for (int x = 0; x < brushSize; x++)
            {
                int mapX = startX + x;
                int mapY = startY + y;
                Image image = images[imageIndex++];

                if (!mapData.IsInside(mapX, mapY))
                {
                    image.gameObject.SetActive(false);
                    continue;
                }

                ConfigureCursorImage(
                    image,
                    gridGenerator.cellSize,
                    mapX,
                    mapY,
                    selectedColor,
                    selectedImageBrush,
                    selectedImageRotation,
                    selectedImageFlipX,
                    selectedImageFlipY,
                    alpha,
                    selectedAnimationFrames,
                    selectedAnimationFps,
                    selectedAnimationLoop);
            }
        }
    }

    private void UpdateCellSetPreview(GridGenerator gridGenerator, MapData mapData, IReadOnlyCollection<Vector2Int> points, Color fillColor, Color outlineColor)
    {
        while (images.Count < points.Count)
        {
            images.Add(CreateCursorImage());
        }

        int imageIndex = 0;

        foreach (Vector2Int point in points)
        {
            Image image = images[imageIndex++];
            image.gameObject.SetActive(mapData.IsInside(point.x, point.y));

            if (image.gameObject.activeSelf)
            {
                ConfigureRectPreviewImage(image, gridGenerator.cellSize, point.x, point.y, fillColor, outlineColor);
            }
        }

        for (int i = imageIndex; i < images.Count; i++)
        {
            images[i].gameObject.SetActive(false);
        }
    }

    private void UpdateLinePreview(
        GridGenerator gridGenerator,
        MapData mapData,
        IReadOnlyCollection<Vector2Int> points,
        Color selectedColor,
        Sprite selectedImageBrush,
        Sprite[] selectedAnimationFrames,
        float selectedAnimationFps,
        bool selectedAnimationLoop,
        int selectedImageRotation,
        bool selectedImageFlipX,
        bool selectedImageFlipY,
        float alpha)
    {
        while (images.Count < points.Count)
        {
            images.Add(CreateCursorImage());
        }

        int imageIndex = 0;

        foreach (Vector2Int point in points)
        {
            Image image = images[imageIndex++];
            image.gameObject.SetActive(mapData.IsInside(point.x, point.y));

            if (image.gameObject.activeSelf)
            {
                ConfigureCursorImage(
                    image,
                    gridGenerator.cellSize,
                    point.x,
                    point.y,
                    selectedColor,
                    selectedImageBrush,
                    selectedImageRotation,
                    selectedImageFlipX,
                    selectedImageFlipY,
                    Mathf.Max(alpha, 0.5f),
                    selectedAnimationFrames,
                    selectedAnimationFps,
                    selectedAnimationLoop);
            }
        }

        for (int i = imageIndex; i < images.Count; i++)
        {
            images[i].gameObject.SetActive(false);
        }
    }

    private void UpdateTileRegionPreview(
        GridGenerator gridGenerator,
        GridCell hoveredCell,
        Sprite sprite,
        int width,
        int height,
        bool flipX,
        bool flipY,
        float alpha)
    {
        while (images.Count < 1)
        {
            images.Add(CreateCursorImage());
        }

        for (int i = 0; i < images.Count; i++)
        {
            images[i].gameObject.SetActive(i == 0);
        }

        int startX = hoveredCell.X - width / 2;
        int startY = hoveredCell.Y - height / 2;
        Image image = images[0];
        ConfigureCursorImage(image, gridGenerator.cellSize, startX, startY, Color.white, sprite, 0, false, false, alpha);
        RectTransform rect = image.rectTransform;
        float previewWidth = width * gridGenerator.cellSize;
        float previewHeight = height * gridGenerator.cellSize;
        rect.sizeDelta = new Vector2(previewWidth, previewHeight);
        rect.localScale = new Vector3(flipX ? -1f : 1f, flipY ? -1f : 1f, 1f);

        Vector2 position = rect.anchoredPosition;
        if (flipX) position.x += previewWidth;
        if (flipY) position.y -= previewHeight;
        rect.anchoredPosition = position;
    }

    public void Hide()
    {
        foreach (Image image in images)
        {
            if (image != null)
            {
                image.gameObject.SetActive(false);
            }
        }
    }

    private void EnsureOverlay(GridGenerator gridGenerator)
    {
        if (overlay != null || gridGenerator == null || gridGenerator.gridParent == null)
        {
            return;
        }

        Transform parent = gridGenerator.gridParent.parent;

        if (parent == null)
        {
            return;
        }

        GameObject overlayObject = new GameObject(overlayObjectName, typeof(RectTransform));
        overlayObject.transform.SetParent(parent, false);
        overlay = overlayObject.GetComponent<RectTransform>();
        overlay.SetAsLastSibling();
        SyncOverlayTransform(gridGenerator);
    }

    private void SyncOverlayTransform(GridGenerator gridGenerator)
    {
        if (overlay == null || gridGenerator == null || gridGenerator.gridParent == null)
        {
            return;
        }

        RectTransform gridRect = gridGenerator.gridParent.GetComponent<RectTransform>();

        if (gridRect == null)
        {
            return;
        }

        overlay.anchorMin = gridRect.anchorMin;
        overlay.anchorMax = gridRect.anchorMax;
        overlay.pivot = gridRect.pivot;
        overlay.anchoredPosition = gridRect.anchoredPosition;
        overlay.sizeDelta = gridRect.sizeDelta;
        overlay.localScale = gridRect.localScale;
        overlay.localRotation = gridRect.localRotation;
    }

    private Image CreateCursorImage()
    {
        GameObject imageObject = new GameObject("BrushCursorCell", typeof(RectTransform), typeof(Image), typeof(Outline));
        imageObject.transform.SetParent(overlay, false);

        Image image = imageObject.GetComponent<Image>();
        image.raycastTarget = false;

        Outline outline = imageObject.GetComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1f, -1f);

        return image;
    }

    private void UpdateRectPreview(GridGenerator gridGenerator, MapData mapData, RectInt areaRect, Color fillColor, Color outlineColor)
    {
        int requiredCount = areaRect.width * areaRect.height;

        while (images.Count < requiredCount)
        {
            images.Add(CreateCursorImage());
        }

        for (int i = 0; i < images.Count; i++)
        {
            images[i].gameObject.SetActive(i < requiredCount);
        }

        int imageIndex = 0;

        for (int y = areaRect.yMin; y < areaRect.yMax; y++)
        {
            for (int x = areaRect.xMin; x < areaRect.xMax; x++)
            {
                Image image = images[imageIndex++];

                if (!mapData.IsInside(x, y))
                {
                    image.gameObject.SetActive(false);
                    continue;
                }

                ConfigureRectPreviewImage(image, gridGenerator.cellSize, x, y, fillColor, outlineColor);
            }
        }
    }

    private void ConfigureRectPreviewImage(Image image, float cellSize, int mapX, int mapY, Color fillColor, Color outlineColor)
    {
        StopCursorAnimation(image);
        RectTransform rect = image.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(cellSize, cellSize);
        rect.anchoredPosition = new Vector2(mapX * cellSize, -mapY * cellSize);

        image.sprite = null;
        image.color = fillColor;
        image.preserveAspect = false;
        rect.localEulerAngles = Vector3.zero;
        rect.localScale = Vector3.one;
        ConfigureOutline(image, outlineColor, 1.5f);
    }

    private void ConfigureCursorImage(
        Image image,
        float cellSize,
        int mapX,
        int mapY,
        Color selectedColor,
        Sprite selectedImageBrush,
        int selectedImageRotation,
        bool selectedImageFlipX,
        bool selectedImageFlipY,
        float alpha,
        Sprite[] animationFrames = null,
        float animationFps = 8f,
        bool animationLoop = true)
    {
        RectTransform rect = image.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(cellSize, cellSize);
        rect.anchoredPosition = new Vector2(mapX * cellSize, -mapY * cellSize);

        if (EditorToolController.Instance != null && EditorToolController.Instance.CurrentTool == EditorToolType.BrushEraser)
        {
            StopCursorAnimation(image);
            image.sprite = null;
            image.color = new Color(1f, 0.1f, 0.1f, 0.28f);
            rect.localEulerAngles = Vector3.zero;
            rect.localScale = Vector3.one;
            ConfigureOutline(image, EraserOutlineColor, 2f);
            return;
        }

        if (EditorToolController.Instance != null && EditorToolController.Instance.CurrentTool == EditorToolType.Eraser)
        {
            StopCursorAnimation(image);
            image.sprite = null;
            image.color = new Color(1f, 0.75f, 0.1f, 0.12f);
            rect.localEulerAngles = Vector3.zero;
            rect.localScale = Vector3.one;
            ConfigureOutline(image, LayerEraserOutlineColor, 2f);
            return;
        }

        if (selectedImageBrush != null)
        {
            image.sprite = selectedImageBrush;
            image.color = new Color(1f, 1f, 1f, Mathf.Max(alpha, 0.65f));
            image.preserveAspect = false;
            rect.localEulerAngles = Vector3.zero;
            rect.localScale = Vector3.one;
            ConfigureOutline(image, BrushOutlineColor, 2f);
            ConfigureCursorAnimation(image, animationFrames, animationFps, animationLoop);
            return;
        }

        StopCursorAnimation(image);
        image.sprite = null;
        image.color = new Color(selectedColor.r, selectedColor.g, selectedColor.b, alpha);
        rect.localEulerAngles = Vector3.zero;
        rect.localScale = Vector3.one;
        ConfigureOutline(image, BrushOutlineColor, 2f);
    }

    private static void ConfigureCursorAnimation(Image image, Sprite[] frames, float fps, bool loop)
    {
        if (image == null)
        {
            return;
        }

        MapEditorAnimatedTilePlayer player = image.GetComponent<MapEditorAnimatedTilePlayer>();
        if (frames == null || frames.Length <= 1)
        {
            player?.Stop();
            return;
        }

        if (player == null)
        {
            player = image.gameObject.AddComponent<MapEditorAnimatedTilePlayer>();
        }

        player.Configure(image, frames, fps, loop);
    }

    private static void StopCursorAnimation(Image image)
    {
        image?.GetComponent<MapEditorAnimatedTilePlayer>()?.Stop();
    }

    private void UpdateSubPixelPreview(GridGenerator gridGenerator, MapData mapData, GridCell hoveredCell, Color selectedColor, int pixelsPerTile, int brushSide, int subPixelX, int subPixelY, float alpha)
    {
        while (images.Count < 1)
        {
            images.Add(CreateCursorImage());
        }

        for (int i = 0; i < images.Count; i++)
        {
            images[i].gameObject.SetActive(i == 0);
        }

        if (!mapData.IsInside(hoveredCell.X, hoveredCell.Y))
        {
            images[0].gameObject.SetActive(false);
            return;
        }

        ConfigureSubPixelPreviewImage(images[0], gridGenerator.cellSize, hoveredCell.X, hoveredCell.Y, selectedColor, pixelsPerTile, brushSide, subPixelX, subPixelY, alpha);
    }

    private void UpdateSpriteSubPixelPreview(GridGenerator gridGenerator, MapData mapData, GridCell hoveredCell, Sprite sprite, int pixelsPerTile, int subPixelX, int subPixelY, float alpha)
    {
        while (images.Count < 1)
        {
            images.Add(CreateCursorImage());
        }

        for (int i = 0; i < images.Count; i++)
        {
            images[i].gameObject.SetActive(i == 0);
        }

        if (!mapData.IsInside(hoveredCell.X, hoveredCell.Y))
        {
            images[0].gameObject.SetActive(false);
            return;
        }

        ConfigureSpriteSubPixelPreviewImage(images[0], gridGenerator.cellSize, hoveredCell.X, hoveredCell.Y, sprite, pixelsPerTile, subPixelX, subPixelY, alpha);
    }

    private void ConfigureSpriteSubPixelPreviewImage(Image image, float cellSize, int mapX, int mapY, Sprite sprite, int pixelsPerTile, int subPixelX, int subPixelY, float alpha)
    {
        StopCursorAnimation(image);
        int pointerResolution = Mathf.Max(1, pixelsPerTile);
        float unitSize = cellSize / 16f;
        float startX = mapX * cellSize + subPixelX * cellSize / pointerResolution;
        float startY = mapY * cellSize + subPixelY * cellSize / pointerResolution;
        float width = Mathf.Max(unitSize, sprite.rect.width * unitSize);
        float height = Mathf.Max(unitSize, sprite.rect.height * unitSize);

        RectTransform rect = image.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(startX, -startY);

        image.sprite = sprite;
        image.color = new Color(1f, 1f, 1f, Mathf.Max(alpha, 0.62f));
        image.preserveAspect = false;
        rect.localEulerAngles = Vector3.zero;
        rect.localScale = Vector3.one;
        ConfigureOutline(image, BrushOutlineColor, 1.5f);
    }

    private void ConfigureSubPixelPreviewImage(Image image, float cellSize, int mapX, int mapY, Color selectedColor, int pixelsPerTile, int brushSide, int subPixelX, int subPixelY, float alpha)
    {
        StopCursorAnimation(image);
        int resolution = Mathf.Max(1, pixelsPerTile);
        float pixelSize = cellSize / resolution;
        brushSide = Mathf.Clamp(brushSide, 1, resolution);
        subPixelX = Mathf.Clamp(subPixelX, 0, resolution - 1);
        subPixelY = Mathf.Clamp(subPixelY, 0, resolution - 1);

        RectTransform rect = image.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(pixelSize * brushSide, pixelSize * brushSide);
        rect.anchoredPosition = new Vector2(mapX * cellSize + subPixelX * pixelSize, -(mapY * cellSize + subPixelY * pixelSize));

        image.sprite = null;
        image.color = new Color(selectedColor.r, selectedColor.g, selectedColor.b, Mathf.Max(alpha, 0.55f));
        image.preserveAspect = false;
        rect.localEulerAngles = Vector3.zero;
        rect.localScale = Vector3.one;
        ConfigureOutline(image, BrushOutlineColor, 1f);
    }

    private void ConfigureOutline(Image image, Color color, float distance)
    {
        Outline outline = image == null ? null : image.GetComponent<Outline>();

        if (outline == null)
        {
            return;
        }

        outline.effectColor = color;
        outline.effectDistance = new Vector2(distance, -distance);
    }
}
