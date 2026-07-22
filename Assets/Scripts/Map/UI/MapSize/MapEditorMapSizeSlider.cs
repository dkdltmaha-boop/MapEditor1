using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MapEditorMapSizeSlider : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public MapEditorManager manager;
    public bool controlsWidth = true;
    public RectTransform fillRect;
    public RectTransform handleRect;
    public InputField inputField;
    public Text currentSizeText;

    private const int MinSize = 1;
    private const int MaxSize = MapEditorManager.MaxMapSize;

    private void OnEnable()
    {
        Refresh();
    }

    public void Configure(MapEditorManager targetManager, bool targetWidth, RectTransform fill, RectTransform handle, InputField input, Text sizeText)
    {
        manager = targetManager;
        controlsWidth = targetWidth;
        fillRect = fill;
        handleRect = handle;
        inputField = input;
        currentSizeText = sizeText;
        Refresh();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        ApplyPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        ApplyPointer(eventData);
    }

    public void Refresh()
    {
        MapEditorManager target = manager != null ? manager : MapEditorManager.Instance;

        if (target == null)
        {
            return;
        }

        int value = controlsWidth ? target.mapWidth : target.mapHeight;
        SetVisualValue(value);
    }

    private void ApplyPointer(PointerEventData eventData)
    {
        RectTransform rect = transform as RectTransform;
        MapEditorManager target = manager != null ? manager : MapEditorManager.Instance;

        if (rect == null || target == null)
        {
            return;
        }

        Camera eventCamera = eventData.pressEventCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventCamera, out Vector2 localPoint))
        {
            return;
        }

        float normalized = Mathf.Clamp01((localPoint.x - rect.rect.xMin) / Mathf.Max(1f, rect.rect.width));
        int value = Mathf.RoundToInt(Mathf.Lerp(MinSize, MaxSize, normalized));

        if (controlsWidth)
        {
            target.ResizeMap(value, target.mapHeight, false);
        }
        else
        {
            target.ResizeMap(target.mapWidth, value, false);
        }

        if (inputField != null)
        {
            inputField.text = value.ToString();
        }

        if (currentSizeText != null)
        {
            currentSizeText.text = target.mapWidth + " x " + target.mapHeight;
        }

        SetVisualValue(value);
    }

    private void SetVisualValue(int value)
    {
        float normalized = Mathf.InverseLerp(MinSize, MaxSize, Mathf.Clamp(value, MinSize, MaxSize));

        if (fillRect != null)
        {
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(normalized, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
        }

        if (handleRect != null)
        {
            handleRect.anchorMin = new Vector2(normalized, 0.5f);
            handleRect.anchorMax = new Vector2(normalized, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
        }
    }
}
