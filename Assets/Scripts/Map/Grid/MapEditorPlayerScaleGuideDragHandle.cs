using UnityEngine;
using UnityEngine.EventSystems;

public sealed class MapEditorPlayerScaleGuideDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
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
        if (gridGenerator == null || gridGenerator.gridParent == null || eventData == null) return;
        RectTransform gridRect = gridGenerator.gridParent as RectTransform;
        if (gridRect == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                gridRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint)) return;

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
