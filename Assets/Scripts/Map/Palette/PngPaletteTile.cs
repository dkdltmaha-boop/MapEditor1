using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PngPaletteTile : MonoBehaviour, IPointerMoveHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private const int LogicalTilePixels = 16;
    private static readonly Color PreviewOutlineColor = new Color(1f, 1f, 1f, 0.9f);

    public Sprite Sprite { get; private set; }
    public string ImagePath { get; private set; }
    public int ImageIndex { get; private set; }

    private MapEditorManager manager;
    private RectTransform previewRect;
    private Image previewImage;
    private Outline previewOutline;

    public void Initialize(Sprite sprite, string imagePath, int imageIndex)
    {
        Initialize(sprite, imagePath, imageIndex, null);
    }

    public void Initialize(Sprite sprite, string imagePath, int imageIndex, MapEditorManager manager)
    {
        Sprite = sprite;
        ImagePath = imagePath;
        ImageIndex = imageIndex;
        this.manager = manager;
    }

    public bool TryPickColor(Vector2 screenPosition, Camera eventCamera, out Color color)
    {
        color = Color.white;

        if (Sprite == null || Sprite.texture == null)
        {
            return false;
        }

        RectTransform rectTransform = GetComponent<RectTransform>();

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, eventCamera, out Vector2 localPoint))
        {
            return false;
        }

        Rect rect = rectTransform.rect;
        float u = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
        float v = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);

        if (u < 0f || u > 1f || v < 0f || v > 1f)
        {
            return false;
        }

        Rect textureRect = Sprite.textureRect;
        int pixelX = Mathf.Clamp(Mathf.FloorToInt(textureRect.x + u * textureRect.width), Mathf.FloorToInt(textureRect.x), Mathf.FloorToInt(textureRect.xMax) - 1);
        int pixelY = Mathf.Clamp(Mathf.FloorToInt(textureRect.y + v * textureRect.height), Mathf.FloorToInt(textureRect.y), Mathf.FloorToInt(textureRect.yMax) - 1);
        color = Sprite.texture.GetPixel(pixelX, pixelY);
        return true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        UpdateSelectionPreview(eventData);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        UpdateSelectionPreview(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (previewRect != null)
        {
            previewRect.gameObject.SetActive(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || manager == null)
        {
            return;
        }

        if (!TryGetSubSelection(eventData, out int resolution, out int subX, out int subY))
        {
            return;
        }

        bool wholeTile = manager.IsWholeTilePaintMode();
        Sprite selectedSprite = wholeTile ? Sprite : CreateSubSprite(resolution, subX, subY);

        if (selectedSprite == null)
        {
            return;
        }

        int encodedIndex = wholeTile
            ? ImageIndex
            : MapEditorPngTilesetService.EncodeSubTileIndex(ImageIndex, resolution, subX, subY);
        manager.SelectImageBrush(selectedSprite, ImagePath, encodedIndex);
        eventData.Use();
    }

    private void UpdateSelectionPreview(PointerEventData eventData)
    {
        if (!TryGetSubSelection(eventData, out int resolution, out int subX, out int subY))
        {
            return;
        }

        EnsurePreview();

        if (previewRect == null)
        {
            return;
        }

        RectTransform tileRect = GetComponent<RectTransform>();
        Rect rect = tileRect.rect;
        float width = rect.width * resolution / LogicalTilePixels;
        float height = rect.height * resolution / LogicalTilePixels;
        float x = rect.width * subX / LogicalTilePixels;
        float y = -rect.height * (LogicalTilePixels - subY - resolution) / LogicalTilePixels;

        previewRect.gameObject.SetActive(true);
        previewRect.anchorMin = new Vector2(0f, 1f);
        previewRect.anchorMax = new Vector2(0f, 1f);
        previewRect.pivot = new Vector2(0f, 1f);
        previewRect.anchoredPosition = new Vector2(x, y);
        previewRect.sizeDelta = new Vector2(width, height);

        previewImage.color = Color.clear;
        previewOutline.effectColor = PreviewOutlineColor;
        previewOutline.effectDistance = new Vector2(1f, -1f);
        previewOutline.useGraphicAlpha = false;
        previewOutline.enabled = true;
    }

    private bool TryGetSubSelection(PointerEventData eventData, out int resolution, out int subX, out int subY)
    {
        resolution = manager == null ? LogicalTilePixels : manager.GetExportCellPixels();
        subX = 0;
        subY = 0;

        if (Sprite == null || eventData == null)
        {
            return false;
        }

        if (manager != null && manager.IsWholeTilePaintMode())
        {
            resolution = LogicalTilePixels;
            return true;
        }

        int pixelsPerTile = Mathf.Clamp(MapEditorManager.NormalizeExportCellPixels(resolution), 1, LogicalTilePixels);
        resolution = Mathf.Max(1, LogicalTilePixels / pixelsPerTile);
        RectTransform tileRect = GetComponent<RectTransform>();

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(tileRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            return false;
        }

        Rect rect = tileRect.rect;

        if (!rect.Contains(localPoint))
        {
            return false;
        }

        float u = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
        float v = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);
        int logicalX = Mathf.Clamp(Mathf.FloorToInt(u * LogicalTilePixels), 0, LogicalTilePixels - 1);
        int logicalY = Mathf.Clamp(Mathf.FloorToInt(v * LogicalTilePixels), 0, LogicalTilePixels - 1);
        subX = Mathf.Clamp((logicalX / resolution) * resolution, 0, LogicalTilePixels - resolution);
        subY = Mathf.Clamp((logicalY / resolution) * resolution, 0, LogicalTilePixels - resolution);
        return true;
    }

    private void EnsurePreview()
    {
        if (previewRect != null)
        {
            return;
        }

        GameObject previewObject = new GameObject("PngSubTilePreview", typeof(RectTransform), typeof(Image), typeof(Outline));
        previewObject.transform.SetParent(transform, false);
        previewRect = previewObject.GetComponent<RectTransform>();
        previewImage = previewObject.GetComponent<Image>();
        previewImage.raycastTarget = false;
        previewOutline = previewObject.GetComponent<Outline>();
        previewObject.SetActive(false);
    }

    private Sprite CreateSubSprite(int resolution, int subX, int subY)
    {
        if (Sprite == null || Sprite.texture == null)
        {
            return null;
        }

        Rect sourceRect = Sprite.textureRect;
        int pixelX = Mathf.FloorToInt(sourceRect.x + subX / (float)LogicalTilePixels * sourceRect.width);
        int pixelY = Mathf.FloorToInt(sourceRect.y + subY / (float)LogicalTilePixels * sourceRect.height);
        int nextPixelX = Mathf.FloorToInt(sourceRect.x + (subX + resolution) / (float)LogicalTilePixels * sourceRect.width);
        int nextPixelY = Mathf.FloorToInt(sourceRect.y + (subY + resolution) / (float)LogicalTilePixels * sourceRect.height);
        int pixelWidth = Mathf.Max(1, nextPixelX - pixelX);
        int pixelHeight = Mathf.Max(1, nextPixelY - pixelY);

        Sprite selectedSprite = Sprite.Create(
            Sprite.texture,
            new Rect(pixelX, pixelY, pixelWidth, pixelHeight),
            new Vector2(0.5f, 0.5f),
            Mathf.Max(pixelWidth, pixelHeight)
        );

        selectedSprite.name = Sprite.name + "_Sub_" + resolution + "_" + subX + "_" + subY;
        return selectedSprite;
    }
}
