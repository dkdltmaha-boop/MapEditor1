using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MapEditorMinimap : MonoBehaviour, IPointerClickHandler
{
    public MapEditorManager manager;
    public RawImage mapImage;
    public RectTransform viewRect;

    private Texture2D texture;

    private void Awake()
    {
        EnsureRawImage();
        EnsureViewRect();
        UpdateViewRect();
    }

    public void Initialize(MapEditorManager manager)
    {
        this.manager = manager;

        EnsureRawImage();

        Refresh();
    }

    public void Refresh()
    {
        if (manager == null || manager.CurrentMapData == null || mapImage == null)
        {
            return;
        }

        int width = manager.CurrentMapData.width;
        int height = manager.CurrentMapData.height;

        if (texture == null || texture.width != width || texture.height != height)
        {
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            mapImage.texture = texture;
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                texture.SetPixel(x, height - 1 - y, manager.GetPreviewColor(x, y));
            }
        }

        texture.Apply();
        UpdateViewRect();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (manager == null)
        {
            return;
        }

        RectTransform rectTransform = GetComponent<RectTransform>();

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            return;
        }

        Rect rect = rectTransform.rect;
        float u = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
        float v = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);
        manager.CenterMapOnNormalizedPosition(u, 1f - v);
    }

    public void UpdateViewRect()
    {
        if (manager == null || viewRect == null)
        {
            return;
        }

        if (!manager.TryGetMapViewNormalizedRect(out Rect normalizedRect))
        {
            viewRect.gameObject.SetActive(false);
            return;
        }

        viewRect.gameObject.SetActive(true);
        viewRect.anchorMin = new Vector2(normalizedRect.xMin, 1f - normalizedRect.yMax);
        viewRect.anchorMax = new Vector2(normalizedRect.xMax, 1f - normalizedRect.yMin);
        viewRect.offsetMin = Vector2.zero;
        viewRect.offsetMax = Vector2.zero;
    }

    private void EnsureViewRect()
    {
        if (viewRect != null)
        {
            return;
        }

        Transform existing = transform.Find("ViewRect");

        if (existing != null)
        {
            viewRect = existing.GetComponent<RectTransform>();
            return;
        }

        GameObject rectObject = new GameObject("ViewRect", typeof(RectTransform), typeof(Image), typeof(Outline));
        rectObject.transform.SetParent(transform, false);
        viewRect = rectObject.GetComponent<RectTransform>();

        Image image = rectObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.12f);
        image.raycastTarget = false;

        Outline outline = rectObject.GetComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(1f, -1f);
    }

    private void EnsureRawImage()
    {
        if (mapImage != null)
        {
            return;
        }

        mapImage = GetComponent<RawImage>();

        if (mapImage == null)
        {
            mapImage = GetComponentInChildren<RawImage>();
        }
    }
}
