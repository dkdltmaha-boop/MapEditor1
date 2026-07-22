using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GridCell : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IDragHandler, IPointerMoveHandler
{
    public int X { get; private set; }
    public int Y { get; private set; }
    public int TileId { get; private set; } = -1;
    public Color CurrentColor { get; private set; } = Color.white;
    public Sprite CurrentSprite { get; private set; }
    public string CurrentImagePath { get; private set; } = string.Empty;
    public int CurrentImageIndex { get; private set; } = -1;
    public int CurrentImageRotation { get; private set; }
    public bool CurrentImageFlipX { get; private set; }
    public bool CurrentImageFlipY { get; private set; }

    private Image image;
    private Image raycastImage;
    private Outline visualOutline;
    private Image wallTopBorder;
    private Image wallRightBorder;
    private Image wallBottomBorder;
    private Image wallLeftBorder;
    private Text spawnMarkerLabel;
    private Texture2D pixelTexture;
    private Sprite pixelSprite;

    private void Awake()
    {
        raycastImage = GetComponent<Image>();
        DisableLegacyCellOutline();
        image = EnsureVisualImage();
    }

    public void Init(int x, int y)
    {
        X = x;
        Y = y;
        Clear();
    }

    public void SetColor(Color color)
    {
        SetCustomColor(color);
    }

    public void SetCustomColor(Color color)
    {
        ClearPixelSprite();
        TileId = MapEditorManager.CustomColorTileId;
        CurrentColor = color;
        CurrentSprite = null;
        CurrentImagePath = string.Empty;
        CurrentImageIndex = -1;
        CurrentImageRotation = 0;
        CurrentImageFlipX = false;
        CurrentImageFlipY = false;
        image.sprite = null;
        image.color = color;
        SetVisualOutline(false);
        RemoveWallBorders();
        ResetImageTransform();
    }

    public void SetWallTile(Color color)
    {
        SetWallTile(color, null, string.Empty, -1, 0, false, false, true, true, true, true);
    }

    public void SetWallTile(Color color, bool showTopBorder, bool showRightBorder, bool showBottomBorder, bool showLeftBorder)
    {
        SetWallTile(color, null, string.Empty, -1, 0, false, false, showTopBorder, showRightBorder, showBottomBorder, showLeftBorder);
    }

    public void SetWallTile(Color color, Sprite sprite, string imagePath, int imageIndex, int rotation, bool flipX, bool flipY, bool showTopBorder, bool showRightBorder, bool showBottomBorder, bool showLeftBorder)
    {
        ClearPixelSprite();
        TileId = MapEditorManager.WallTileId;
        CurrentColor = color;
        CurrentSprite = sprite;
        CurrentImagePath = imagePath;
        CurrentImageIndex = imageIndex;
        CurrentImageRotation = MapEditorRotationUtility.NormalizeQuarterTurn(rotation);
        CurrentImageFlipX = flipX;
        CurrentImageFlipY = flipY;
        image.sprite = sprite;
        image.color = sprite == null ? color : Color.white;
        image.preserveAspect = false;
        SetVisualOutline(false);
        SetWallBorders(showTopBorder, showRightBorder, showBottomBorder, showLeftBorder);
        ApplyImageTransform();
    }

    public void SetWallPixelTile(MapTilePixelData pixelData, Color fallbackColor, bool showTopBorder, bool showRightBorder, bool showBottomBorder, bool showLeftBorder)
    {
        SetPixelColorTile(pixelData, fallbackColor);
        TileId = MapEditorManager.WallTileId;
        SetWallBorders(showTopBorder, showRightBorder, showBottomBorder, showLeftBorder);
    }

    public void SetCustomSprite(Sprite sprite, string imagePath, int imageIndex)
    {
        SetCustomSprite(sprite, imagePath, imageIndex, 0, false, false);
    }

    public void SetCustomSprite(Sprite sprite, string imagePath, int imageIndex, int rotation, bool flipX, bool flipY)
    {
        ClearPixelSprite();
        TileId = MapEditorManager.CustomImageTileId;
        CurrentColor = Color.white;
        CurrentSprite = sprite;
        CurrentImagePath = imagePath;
        CurrentImageIndex = imageIndex;
        CurrentImageRotation = MapEditorRotationUtility.NormalizeQuarterTurn(rotation);
        CurrentImageFlipX = flipX;
        CurrentImageFlipY = flipY;
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = false;
        SetVisualOutline(false);
        RemoveWallBorders();
        ApplyImageTransform();
    }

    public void Clear()
    {
        ClearPixelSprite();
        TileId = -1;
        CurrentColor = Color.white;
        CurrentSprite = null;
        CurrentImagePath = string.Empty;
        CurrentImageIndex = -1;
        CurrentImageRotation = 0;
        CurrentImageFlipX = false;
        CurrentImageFlipY = false;
        image.sprite = null;
        image.color = Color.white;
        SetVisualOutline(false);
        RemoveWallBorders();
        ResetImageTransform();
    }

    public void SetPixelColorTile(MapTilePixelData pixelData, Color fallbackColor)
    {
        if (pixelData == null || pixelData.colors == null || pixelData.colors.Length == 0)
        {
            SetCustomColor(fallbackColor);
            return;
        }

        int resolution = Mathf.Max(1, pixelData.resolution);

        if (pixelTexture == null || pixelTexture.width != resolution || pixelTexture.height != resolution)
        {
            ClearPixelSprite();
            pixelTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            pixelSprite = Sprite.Create(pixelTexture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), resolution);
        }

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                pixelTexture.SetPixel(x, resolution - 1 - y, pixelData.GetPixel(x, y));
            }
        }

        pixelTexture.Apply();

        TileId = MapEditorManager.CustomColorTileId;
        CurrentColor = pixelData.GetAverageColor();
        CurrentSprite = null;
        CurrentImagePath = string.Empty;
        CurrentImageIndex = -1;
        CurrentImageRotation = 0;
        CurrentImageFlipX = false;
        CurrentImageFlipY = false;
        image.sprite = pixelSprite;
        image.color = Color.white;
        image.preserveAspect = false;
        SetVisualOutline(false);
        RemoveWallBorders();
        ResetImageTransform();
    }

    public void SetSpawnMarkerVisible(bool visible)
    {
        if (visible)
        {
            EnsureSpawnMarker();
        }

        if (spawnMarkerLabel != null)
        {
            spawnMarkerLabel.gameObject.SetActive(visible);
        }
    }

    private void ApplyImageTransform()
    {
        RectTransform rect = image.GetComponent<RectTransform>();

        if (rect == null)
        {
            return;
        }

        SyncVisualSize(rect);
        rect.localEulerAngles = Vector3.zero;
        rect.localScale = Vector3.one;
    }

    private void ResetImageTransform()
    {
        RectTransform rect = image.GetComponent<RectTransform>();

        if (rect == null)
        {
            return;
        }

        SyncVisualSize(rect);
        rect.localEulerAngles = Vector3.zero;
        rect.localScale = Vector3.one;
    }

    private Image EnsureVisualImage()
    {
        Transform existing = transform.Find("TileVisual");
        Image visualImage;

        if (existing == null)
        {
            GameObject visualObject = new GameObject("TileVisual", typeof(RectTransform), typeof(Image));
            visualObject.transform.SetParent(transform, false);
            visualImage = visualObject.GetComponent<Image>();
        }
        else
        {
            visualImage = existing.GetComponent<Image>();

            if (visualImage == null)
            {
                visualImage = existing.gameObject.AddComponent<Image>();
            }
        }

        RectTransform rect = visualImage.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localEulerAngles = Vector3.zero;
        rect.localScale = Vector3.one;

        visualImage.raycastTarget = false;
        visualOutline = visualImage.GetComponent<Outline>();

        if (visualOutline == null)
        {
            visualOutline = visualImage.gameObject.AddComponent<Outline>();
        }

        visualOutline.effectColor = Color.black;
        visualOutline.effectDistance = new Vector2(2f, -2f);
        visualOutline.enabled = false;
        CacheExistingWallBorders(visualImage.transform);
        SetWallBorders(false, false, false, false);

        if (raycastImage != null)
        {
            raycastImage.raycastTarget = true;
            raycastImage.color = new Color(1f, 1f, 1f, 0f);
        }

        return visualImage;
    }

    private void DisableLegacyCellOutline()
    {
        Outline outline = GetComponent<Outline>();

        if (outline != null)
        {
            outline.enabled = false;
        }
    }

    private void EnsureWallBorders(Transform parent)
    {
        wallTopBorder = EnsureBorder(parent, "WallBorder_Top");
        wallRightBorder = EnsureBorder(parent, "WallBorder_Right");
        wallBottomBorder = EnsureBorder(parent, "WallBorder_Bottom");
        wallLeftBorder = EnsureBorder(parent, "WallBorder_Left");
        SetWallBorders(false, false, false, false);
    }

    private void CacheExistingWallBorders(Transform parent)
    {
        wallTopBorder = FindBorder(parent, "WallBorder_Top");
        wallRightBorder = FindBorder(parent, "WallBorder_Right");
        wallBottomBorder = FindBorder(parent, "WallBorder_Bottom");
        wallLeftBorder = FindBorder(parent, "WallBorder_Left");
    }

    private static Image FindBorder(Transform parent, string name)
    {
        Transform existing = parent == null ? null : parent.Find(name);
        return existing == null ? null : existing.GetComponent<Image>();
    }

    private static Image EnsureBorder(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        Image border;

        if (existing == null)
        {
            GameObject borderObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            borderObject.transform.SetParent(parent, false);
            border = borderObject.GetComponent<Image>();
        }
        else
        {
            border = existing.GetComponent<Image>();

            if (border == null)
            {
                border = existing.gameObject.AddComponent<Image>();
            }
        }

        border.color = Color.black;
        border.raycastTarget = false;
        return border;
    }

    private void SetWallBorders(bool top, bool right, bool bottom, bool left)
    {
        if ((top || right || bottom || left) && (wallTopBorder == null || wallRightBorder == null || wallBottomBorder == null || wallLeftBorder == null) && image != null)
        {
            EnsureWallBorders(image.transform);
        }

        ConfigureBorder(wallTopBorder, top, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 2f));
        ConfigureBorder(wallRightBorder, right, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(2f, 0f));
        ConfigureBorder(wallBottomBorder, bottom, Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, 2f));
        ConfigureBorder(wallLeftBorder, left, Vector2.zero, new Vector2(0f, 1f), new Vector2(2f, 0f));
    }

    private void RemoveWallBorders()
    {
        DestroyBorder(ref wallTopBorder);
        DestroyBorder(ref wallRightBorder);
        DestroyBorder(ref wallBottomBorder);
        DestroyBorder(ref wallLeftBorder);
    }

    private void ClearPixelSprite()
    {
        if (pixelSprite != null)
        {
            MapEditorObjectUtility.DestroyObject(pixelSprite);
            pixelSprite = null;
        }

        if (pixelTexture != null)
        {
            MapEditorObjectUtility.DestroyObject(pixelTexture);
            pixelTexture = null;
        }
    }

    private static void DestroyBorder(ref Image border)
    {
        if (border == null)
        {
            return;
        }

        GameObject borderObject = border.gameObject;
        border = null;
        MapEditorObjectUtility.DestroyObject(borderObject);
    }

    private static void ConfigureBorder(Image border, bool visible, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
    {
        if (border == null)
        {
            return;
        }

        border.enabled = visible;

        RectTransform rect = border.GetComponent<RectTransform>();

        if (rect == null)
        {
            return;
        }

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = Vector2.zero;
    }

    private void SetVisualOutline(bool enabled)
    {
        if (visualOutline == null && image != null)
        {
            visualOutline = image.GetComponent<Outline>();
        }

        if (visualOutline == null)
        {
            return;
        }

        visualOutline.effectColor = Color.black;
        visualOutline.effectDistance = new Vector2(2f, -2f);
        visualOutline.enabled = enabled;
    }

    private void SyncVisualSize(RectTransform visualRect)
    {
        RectTransform cellRect = transform as RectTransform;

        if (cellRect == null || visualRect == null)
        {
            return;
        }

        Vector2 size = cellRect.rect.size;

        if (size.x <= 0f || size.y <= 0f)
        {
            size = cellRect.sizeDelta;
        }

        visualRect.sizeDelta = size;
        visualRect.anchoredPosition = Vector2.zero;
    }

    private void EnsureSpawnMarker()
    {
        if (spawnMarkerLabel != null)
        {
            return;
        }

        Transform existing = transform.Find("SpawnMarker");

        if (existing == null)
        {
            GameObject markerObject = new GameObject("SpawnMarker", typeof(RectTransform), typeof(Text), typeof(Outline));
            markerObject.transform.SetParent(transform, false);
            existing = markerObject.transform;
        }

        spawnMarkerLabel = existing.GetComponent<Text>();

        if (spawnMarkerLabel == null)
        {
            spawnMarkerLabel = existing.gameObject.AddComponent<Text>();
        }

        RectTransform rect = spawnMarkerLabel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        spawnMarkerLabel.text = "SP";
        spawnMarkerLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        spawnMarkerLabel.fontSize = 10;
        spawnMarkerLabel.fontStyle = FontStyle.Bold;
        spawnMarkerLabel.alignment = TextAnchor.MiddleCenter;
        spawnMarkerLabel.color = new Color(0.1f, 0.9f, 1f, 1f);
        spawnMarkerLabel.raycastTarget = false;

        Outline outline = existing.GetComponent<Outline>();

        if (outline != null)
        {
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1f, -1f);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || MapEditorManager.Instance == null)
        {
            return;
        }

        if (MapEditorManager.Instance.IsSelectionToolActive())
        {
            MapEditorManager.Instance.BeginSelectionDrag(this);
            return;
        }

        MapEditorManager.Instance.BeginEditTransaction();
        if (TryGetPointerSubPixel(eventData, MapEditorManager.Instance.GetExportCellPixels(), out Vector2Int subPixel))
        {
            MapEditorManager.Instance.UseCurrentTool(this, subPixel.x, subPixel.y);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (MapEditorManager.Instance != null)
        {
            if (TryGetPointerSubPixel(eventData, MapEditorManager.Instance.GetExportCellPixels(), out Vector2Int subPixel))
            {
                MapEditorManager.Instance.SetHoveredCell(this, subPixel.x, subPixel.y);
            }
        }

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            return;
        }

        if (MapEditorManager.Instance != null && MapEditorManager.Instance.IsSelectionToolActive())
        {
            if (Input.GetMouseButton(0))
            {
                MapEditorManager.Instance.UpdateSelectionDrag(this);
            }

            return;
        }

        if (Input.GetMouseButton(0) && MapEditorManager.Instance != null)
        {
            if (TryGetPointerSubPixel(eventData, MapEditorManager.Instance.GetExportCellPixels(), out Vector2Int subPixel))
            {
                MapEditorManager.Instance.UseCurrentTool(this, subPixel.x, subPixel.y);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (MapEditorManager.Instance != null)
        {
            MapEditorManager.Instance.ClearHoveredCell(this);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && MapEditorManager.Instance != null)
        {
            MapEditorManager.Instance.EndSelectionDrag(this);
            MapEditorManager.Instance.CommitEditTransaction();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || MapEditorManager.Instance == null)
        {
            return;
        }

        if (MapEditorManager.Instance.IsSelectionToolActive())
        {
            GridCell targetSelectionCell = GetCellUnderPointer(eventData);
            MapEditorManager.Instance.UpdateSelectionDrag(targetSelectionCell == null ? this : targetSelectionCell);
            return;
        }

        GridCell targetCell = GetCellUnderPointer(eventData);

        if (targetCell == null)
        {
            return;
        }

        if (!targetCell.TryGetPointerSubPixel(eventData, MapEditorManager.Instance.GetExportCellPixels(), out Vector2Int subPixel))
        {
            return;
        }

        MapEditorManager.Instance.SetHoveredCell(targetCell, subPixel.x, subPixel.y);
        MapEditorManager.Instance.UseCurrentTool(targetCell, subPixel.x, subPixel.y);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (MapEditorManager.Instance == null)
        {
            return;
        }

        GridCell targetCell = GetCellUnderPointer(eventData);

        if (targetCell == null)
        {
            return;
        }

        if (targetCell.TryGetPointerSubPixel(eventData, MapEditorManager.Instance.GetExportCellPixels(), out Vector2Int subPixel))
        {
            MapEditorManager.Instance.SetHoveredCell(targetCell, subPixel.x, subPixel.y);
        }
    }

    private bool TryGetPointerSubPixel(PointerEventData eventData, int resolution, out Vector2Int subPixel)
    {
        subPixel = Vector2Int.zero;
        resolution = Mathf.Max(1, resolution);
        RectTransform rect = transform as RectTransform;

        if (rect == null || eventData == null)
        {
            return false;
        }

        Camera eventCamera = eventData.pressEventCamera != null ? eventData.pressEventCamera : eventData.enterEventCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventCamera, out Vector2 localPoint))
        {
            return false;
        }

        Rect localRect = rect.rect;

        if (!localRect.Contains(localPoint))
        {
            return false;
        }

        float normalizedX = Mathf.InverseLerp(localRect.xMin, localRect.xMax, localPoint.x);
        float normalizedY = Mathf.InverseLerp(localRect.yMax, localRect.yMin, localPoint.y);
        int pixelX = Mathf.Clamp(Mathf.FloorToInt(normalizedX * resolution), 0, resolution - 1);
        int pixelY = Mathf.Clamp(Mathf.FloorToInt(normalizedY * resolution), 0, resolution - 1);
        subPixel = new Vector2Int(pixelX, pixelY);
        return true;
    }

    private GridCell GetCellUnderPointer(PointerEventData eventData)
    {
        if (EventSystem.current == null || eventData == null)
        {
            return null;
        }

        System.Collections.Generic.List<RaycastResult> results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        for (int i = 0; i < results.Count; i++)
        {
            GridCell cell = results[i].gameObject.GetComponentInParent<GridCell>();

            if (cell != null)
            {
                return cell;
            }
        }

        return null;
    }
}
