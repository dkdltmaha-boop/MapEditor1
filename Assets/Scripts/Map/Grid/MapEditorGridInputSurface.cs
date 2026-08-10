using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class MapEditorGridInputSurface : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IDragHandler,
    IEndDragHandler,
    IScrollHandler,
    IPointerMoveHandler,
    IPointerExitHandler
{
    private GridGenerator generator;
    private MapEditorManager manager;
    private bool pointerDown;

    public void Configure(GridGenerator owner, MapEditorManager mapEditor)
    {
        generator = owner;
        manager = mapEditor;
        enabled = true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !TryGetCoordinate(eventData, out int x, out int y, out int pixelX, out int pixelY))
        {
            return;
        }

        pointerDown = true;
        manager.HandleVirtualPointerDown(x, y, pixelX, pixelY);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!pointerDown || eventData.button != PointerEventData.InputButton.Left || !TryGetCoordinate(eventData, out int x, out int y, out int pixelX, out int pixelY))
        {
            return;
        }

        manager.HandleVirtualPointerDrag(x, y, pixelX, pixelY);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (TryGetCoordinate(eventData, out int x, out int y, out int pixelX, out int pixelY))
        {
            manager.HandleVirtualPointerMove(x, y, pixelX, pixelY);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !pointerDown)
        {
            return;
        }

        pointerDown = false;
        if (TryGetCoordinate(eventData, out int x, out int y, out int pixelX, out int pixelY))
        {
            manager.HandleVirtualPointerUp(x, y, pixelX, pixelY);
        }
        else
        {
            manager.HandleVirtualPointerUpOutside();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        OnPointerUp(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        manager?.HandleVirtualPointerExit();
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (manager == null || eventData == null || Mathf.Approximately(eventData.scrollDelta.y, 0f))
        {
            return;
        }

        manager.ZoomMap(eventData.scrollDelta.y > 0f ? 1f : -1f, eventData.position);
    }

    private bool TryGetCoordinate(PointerEventData eventData, out int x, out int y, out int pixelX, out int pixelY)
    {
        x = y = pixelX = pixelY = 0;
        RectTransform rectTransform = transform as RectTransform;

        if (generator == null || manager == null || rectTransform == null || eventData == null || generator.cellSize <= 0f)
        {
            return false;
        }

        Camera eventCamera = eventData.pressEventCamera != null ? eventData.pressEventCamera : eventData.enterEventCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventCamera, out Vector2 localPoint))
        {
            return false;
        }

        Rect rect = rectTransform.rect;
        float mapPixelX = (localPoint.x - rect.xMin) / generator.cellSize;
        float mapPixelY = (rect.yMax - localPoint.y) / generator.cellSize;
        x = Mathf.FloorToInt(mapPixelX);
        y = Mathf.FloorToInt(mapPixelY);

        if (x < 0 || y < 0 || x >= generator.width || y >= generator.height)
        {
            return false;
        }

        pixelX = Mathf.Clamp(Mathf.FloorToInt((mapPixelX - x) * MapEditorManager.MaxExportCellPixels), 0, MapEditorManager.MaxExportCellPixels - 1);
        pixelY = Mathf.Clamp(Mathf.FloorToInt((mapPixelY - y) * MapEditorManager.MaxExportCellPixels), 0, MapEditorManager.MaxExportCellPixels - 1);
        return true;
    }
}
