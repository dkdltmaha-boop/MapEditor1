using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MapEditorPlayerScaleGuide
{
    private const string ObjectName = "MapEditor_PlayerScaleGuide";
    private const string PlayerImageName = "PlayerSprite";
    private const string PlayerTextureResource = "PixelChromaPlayer/Char_Main_Anime";
    private RectTransform root;

    public void Update(GridGenerator gridGenerator, bool visible, int cellX, int cellY)
    {
        if (gridGenerator == null || gridGenerator.gridParent == null)
        {
            return;
        }

        EnsureVisual(gridGenerator.gridParent);
        root.gameObject.SetActive(visible);

        if (!visible)
        {
            return;
        }

        float cellSize = Mathf.Max(1f, gridGenerator.cellSize);
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.sizeDelta = new Vector2(cellSize, cellSize);
        root.anchoredPosition = new Vector2(cellX * cellSize, -cellY * cellSize);
        root.SetAsLastSibling();

        MapEditorPlayerScaleGuideDragHandle dragHandle = root.GetComponent<MapEditorPlayerScaleGuideDragHandle>();
        if (dragHandle != null)
        {
            dragHandle.Configure(gridGenerator);
        }
    }

    private void EnsureVisual(Transform parent)
    {
        if (root != null && root.parent == parent)
        {
            return;
        }

        Transform existing = parent.Find(ObjectName);
        GameObject guideObject;

        if (existing == null)
        {
            guideObject = new GameObject(
                ObjectName,
                typeof(RectTransform),
                typeof(Image),
                typeof(Outline),
                typeof(MapEditorPlayerScaleGuideDragHandle));
            guideObject.transform.SetParent(parent, false);
        }
        else
        {
            guideObject = existing.gameObject;
        }

        root = guideObject.GetComponent<RectTransform>();
        Image image = guideObject.GetComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = true;

        Outline outline = guideObject.GetComponent<Outline>();
        outline.enabled = false;

        EnsurePlayerImage(guideObject.transform);

        MapEditorPlayerScaleGuideDragHandle dragHandle = guideObject.GetComponent<MapEditorPlayerScaleGuideDragHandle>();
        if (dragHandle == null)
        {
            dragHandle = guideObject.AddComponent<MapEditorPlayerScaleGuideDragHandle>();
        }

        dragHandle.Configure(parent.GetComponentInParent<GridGenerator>());
    }

    private static void EnsurePlayerImage(Transform parent)
    {
        Transform existing = parent.Find(PlayerImageName);
        RawImage playerImage;

        if (existing == null)
        {
            GameObject imageObject = new GameObject(PlayerImageName, typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(parent, false);
            playerImage = imageObject.GetComponent<RawImage>();
        }
        else
        {
            playerImage = existing.GetComponent<RawImage>();
        }

        Texture2D texture = Resources.Load<Texture2D>(PlayerTextureResource);
        if (texture != null)
        {
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
        }

        playerImage.texture = texture;
        playerImage.color = Color.white;
        playerImage.raycastTarget = false;

        if (texture != null)
        {
            playerImage.uvRect = new Rect(
                0f,
                45f / texture.height,
                16f / texture.width,
                16f / texture.height);
        }

        RectTransform imageRect = playerImage.rectTransform;
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;
        playerImage.gameObject.SetActive(texture != null);
    }
}

public sealed class MapEditorPlayerScaleGuideDragHandle : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler
{
    private GridGenerator gridGenerator;

    public void Configure(GridGenerator generator)
    {
        gridGenerator = generator;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        MoveToPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        MoveToPointer(eventData);
    }

    private void MoveToPointer(PointerEventData eventData)
    {
        if (gridGenerator == null || gridGenerator.gridParent == null || eventData == null)
        {
            return;
        }

        RectTransform gridRect = gridGenerator.gridParent as RectTransform;
        if (gridRect == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                gridRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
        {
            return;
        }

        float cellSize = Mathf.Max(1f, gridGenerator.cellSize);
        float left = -gridRect.rect.width * gridRect.pivot.x;
        float top = gridRect.rect.height * (1f - gridRect.pivot.y);
        int x = Mathf.FloorToInt((localPoint.x - left) / cellSize);
        int y = Mathf.FloorToInt((top - localPoint.y) / cellSize);

        MapEditorManager manager = gridGenerator.mapEditorManager != null
            ? gridGenerator.mapEditorManager
            : MapEditorManager.Instance;
        manager?.SetPlayerScaleGuidePosition(x, y);
    }
}
