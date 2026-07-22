using UnityEngine;
using UnityEngine.EventSystems;

public class PngPaletteZoomInput : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler, IPointerClickHandler
{
    private ColorWheelPickerWindow picker;
    private RectTransform rectTransform;
    private Canvas canvas;

    public void Initialize(ColorWheelPickerWindow picker)
    {
        this.picker = picker;
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (picker == null)
        {
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            picker.ZoomPngPalette(eventData.scrollDelta.y > 0f ? 1f : -1f, localPoint);
        }

        eventData.Use();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (picker == null)
        {
            return;
        }

        float scaleFactor = canvas == null ? 1f : canvas.scaleFactor;
        picker.PanPngPalette(eventData.delta / scaleFactor);
        eventData.Use();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (picker == null)
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right || eventData.clickCount >= 2)
        {
            picker.ResetPngPaletteView();
            eventData.Use();
        }
    }
}
