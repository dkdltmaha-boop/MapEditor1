using UnityEngine;
using UnityEngine.EventSystems;

public class ColorSquareInput : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    private ColorWheelPickerWindow picker;
    private RectTransform rectTransform;

    public void Initialize(ColorWheelPickerWindow picker, RectTransform rectTransform)
    {
        this.picker = picker;
        this.rectTransform = rectTransform;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Handle(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Handle(eventData);
    }

    private void Handle(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            picker.SetSaturationValueFromLocalPoint(localPoint);
        }
    }
}
