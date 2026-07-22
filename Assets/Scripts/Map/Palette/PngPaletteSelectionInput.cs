using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PngPaletteSelectionInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IScrollHandler
{
    private ColorWheelPickerWindow picker;
    private Canvas canvas;
    private bool isSelecting;
    private bool isPanning;

    public void Initialize(ColorWheelPickerWindow picker)
    {
        this.picker = picker;
        canvas = GetComponentInParent<Canvas>();

        Image image = GetComponent<Image>();

        if (image != null)
        {
            image.color = Color.clear;
            image.raycastTarget = true;
        }
    }

    private void OnDisable()
    {
        CancelPointerState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (picker == null)
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            isSelecting = true;
            picker.BeginPngPaletteSelection(eventData.position, eventData.pressEventCamera);
            eventData.Use();
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            isPanning = true;
            eventData.Use();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (picker == null)
        {
            return;
        }

        if (isSelecting)
        {
            picker.UpdatePngPaletteSelection(eventData.position, eventData.pressEventCamera);
            eventData.Use();
            return;
        }

        if (isPanning)
        {
            float scaleFactor = canvas == null ? 1f : canvas.scaleFactor;
            picker.PanPngPalette(eventData.delta / scaleFactor);
            eventData.Use();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (picker == null)
        {
            return;
        }

        if (isSelecting && eventData.button == PointerEventData.InputButton.Left)
        {
            picker.EndPngPaletteSelection(eventData.position, eventData.pressEventCamera);
            isSelecting = false;
            eventData.Use();
        }

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            isPanning = false;
        }
    }

    private void CancelPointerState()
    {
        if (picker != null && isSelecting)
        {
            picker.CancelPngPaletteSelection();
        }

        isSelecting = false;
        isPanning = false;
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (picker == null)
        {
            return;
        }

        RectTransform rectTransform = transform.parent as RectTransform;

        if (rectTransform != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            picker.ZoomPngPalette(eventData.scrollDelta.y > 0f ? 1f : -1f, localPoint);
            eventData.Use();
        }
    }
}
