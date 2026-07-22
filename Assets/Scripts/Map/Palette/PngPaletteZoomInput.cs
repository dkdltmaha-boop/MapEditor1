using UnityEngine;
using UnityEngine.EventSystems;

public class PngPaletteZoomInput : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    private ColorWheelPickerWindow picker;
    private RectTransform rectTransform;
    private Canvas canvas;
    private bool isPanning;

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
        isPanning = IsPanButton(eventData.button);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (picker == null || !isPanning)
        {
            return;
        }

        float scaleFactor = canvas == null ? 1f : canvas.scaleFactor;
        picker.PanPngPalette(eventData.delta / scaleFactor);
        eventData.Use();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsPanButton(eventData.button))
        {
            isPanning = true;
            eventData.Use();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (IsPanButton(eventData.button))
        {
            isPanning = false;
            eventData.Use();
        }
    }

    private void OnDisable()
    {
        isPanning = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (picker == null)
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right
            || (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount >= 2))
        {
            picker.ResetPngPaletteView();
            eventData.Use();
        }
    }

    private static bool IsPanButton(PointerEventData.InputButton button)
    {
        return button == PointerEventData.InputButton.Middle
            || button == PointerEventData.InputButton.Right;
    }
}
