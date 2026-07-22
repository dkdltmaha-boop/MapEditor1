using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapEditorBrushCursorPreview
{
    private static readonly Color BrushOutlineColor = Color.black;
    private static readonly Color EraserOutlineColor = new Color(1f, 0.1f, 0.1f, 1f);
    private static readonly Color SelectionFillColor = new Color(0.2f, 0.55f, 1f, 0.14f);
    private static readonly Color SelectionOutlineColor = new Color(0.1f, 0.35f, 1f, 1f);
    private static readonly Color AreaFillColor = new Color(1f, 0.85f, 0.1f, 0.16f);
    private static readonly Color AreaFillOutlineColor = new Color(1f, 0.7f, 0f, 1f);

    private readonly List<Image> images = new List<Image>();
    private RectTransform overlay;

    public void Update(
        bool showPreview,
        GridGenerator gridGenerator,
        GridCell hoveredCell,
        MapData mapData,
        int brushSize,
        Color selectedColor,
        Sprite selectedImageBrush,
        int selectedImageRotation,
        bool selectedImageFlipX,
        bool selectedImageFlipY,
        int pixelsPerTile,
        int hoveredSubPixelX,
        int hoveredSubPixelY,
        bool showSubPixelPreview,
        float alpha,
        RectInt? areaFillPreviewRect,
        RectInt? selectionPreviewRect)
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

        if (selectedImageBrush != null)
        {
            UpdateSpriteSubPixelPreview(gridGenerator, mapData, hoveredCell, selectedImageBrush, pixelsPerTile, hoveredSubPixelX, hoveredSubPixelY, alpha);
            return;
        }

        if (showSubPixelPreview)
        {
            UpdateSubPixelPreview(gridGenerator, mapData, hoveredCell, selectedColor, pixelsPerTile, hoveredSubPixelX, hoveredSubPixelY, alpha);
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

                ConfigureCursorImage(image, gridGenerator.cellSize, mapX, mapY, selectedColor, selectedImageBrush, selectedImageRotation, selectedImageFlipX, selectedImageFlipY, alpha);
            }
        }
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

        GameObject overlayObject = new GameObject("MapEditor_BrushCursorPreview", typeof(RectTransform));
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

    private void ConfigureCursorImage(Image image, float cellSize, int mapX, int mapY, Color selectedColor, Sprite selectedImageBrush, int selectedImageRotation, bool selectedImageFlipX, bool selectedImageFlipY, float alpha)
    {
        RectTransform rect = image.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(cellSize, cellSize);
        rect.anchoredPosition = new Vector2(mapX * cellSize, -mapY * cellSize);

        if (EditorToolController.Instance != null && EditorToolController.Instance.CurrentTool == EditorToolType.Eraser)
        {
            image.sprite = null;
            image.color = new Color(1f, 0.1f, 0.1f, 0.28f);
            rect.localEulerAngles = Vector3.zero;
            rect.localScale = Vector3.one;
            ConfigureOutline(image, EraserOutlineColor, 2f);
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
            return;
        }

        image.sprite = null;
        image.color = new Color(selectedColor.r, selectedColor.g, selectedColor.b, alpha);
        rect.localEulerAngles = Vector3.zero;
        rect.localScale = Vector3.one;
        ConfigureOutline(image, BrushOutlineColor, 2f);
    }

    private void UpdateSubPixelPreview(GridGenerator gridGenerator, MapData mapData, GridCell hoveredCell, Color selectedColor, int pixelsPerTile, int subPixelX, int subPixelY, float alpha)
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

        ConfigureSubPixelPreviewImage(images[0], gridGenerator.cellSize, hoveredCell.X, hoveredCell.Y, selectedColor, pixelsPerTile, subPixelX, subPixelY, alpha);
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

    private void ConfigureSubPixelPreviewImage(Image image, float cellSize, int mapX, int mapY, Color selectedColor, int pixelsPerTile, int subPixelX, int subPixelY, float alpha)
    {
        int resolution = Mathf.Max(1, pixelsPerTile);
        float pixelSize = cellSize / resolution;
        subPixelX = Mathf.Clamp(subPixelX, 0, resolution - 1);
        subPixelY = Mathf.Clamp(subPixelY, 0, resolution - 1);

        RectTransform rect = image.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(pixelSize, pixelSize);
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
