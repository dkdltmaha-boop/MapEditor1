using UnityEngine;
using UnityEngine.EventSystems;

public sealed class MapEditorAnimationFramePaletteInput : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IEndDragHandler
{
    private MapEditorAnimationTileWindow window;
    private RectTransform rectTransform;
    private int dragSourceTileId = -1;

    public void Configure(MapEditorAnimationTileWindow owner)
    {
        window = owner;
        rectTransform = transform as RectTransform;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (window != null && TryGetTileId(eventData, out int tileId)) window.ToggleFrameTile(tileId);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragSourceTileId = TryGetTileId(eventData, out int tileId) ? tileId : -1;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (window != null && dragSourceTileId >= 0 && TryGetTileId(eventData, out int targetTileId))
        {
            window.ReorderFrameTile(dragSourceTileId, targetTileId);
        }
        dragSourceTileId = -1;
    }

    private bool TryGetTileId(PointerEventData eventData, out int tileId)
    {
        tileId = -1;
        if (rectTransform == null || window == null
            || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 local))
        {
            return false;
        }

        Rect rect = rectTransform.rect;
        if (!rect.Contains(local)) return false;
        float u = Mathf.InverseLerp(rect.xMin, rect.xMax, local.x);
        float v = Mathf.InverseLerp(rect.yMax, rect.yMin, local.y);
        int gridSize = window.FramePaletteGridSize;
        int x = Mathf.Clamp(Mathf.FloorToInt(u * gridSize), 0, gridSize - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(v * gridSize), 0, gridSize - 1);
        tileId = y * gridSize + x;
        return true;
    }
}
