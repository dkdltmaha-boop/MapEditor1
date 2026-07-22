using UnityEngine;
using UnityEngine.UI;

public class MapEditorMapSizeControl : MonoBehaviour
{
    public MapEditorManager manager;
    public bool controlsWidth = true;
    public InputField inputField;
    public Text currentSizeText;

    private bool suppressEvents;

    private void OnEnable()
    {
        WireEvents();
        RefreshValues();
    }

    private void OnDisable()
    {
        if (inputField != null)
        {
            inputField.onEndEdit.RemoveListener(HandleInputEndEdit);
        }

    }

    public void Configure(MapEditorManager targetManager, bool targetWidth, InputField targetInput, Text sizeText)
    {
        manager = targetManager;
        controlsWidth = targetWidth;
        inputField = targetInput;
        currentSizeText = sizeText;
        WireEvents();
        RefreshValues();
    }

    private void WireEvents()
    {
        if (inputField != null)
        {
            inputField.onEndEdit.RemoveListener(HandleInputEndEdit);
            inputField.onEndEdit.AddListener(HandleInputEndEdit);
        }

    }

    private void RefreshValues()
    {
        MapEditorManager target = manager != null ? manager : MapEditorManager.Instance;

        if (target == null)
        {
            return;
        }

        int value = controlsWidth ? target.mapWidth : target.mapHeight;
        suppressEvents = true;

        if (inputField != null)
        {
            inputField.text = value.ToString();
        }

        UpdateCurrentSizeText(target);
        suppressEvents = false;
    }

    private void HandleInputEndEdit(string value)
    {
        if (suppressEvents)
        {
            return;
        }

        if (!int.TryParse(value, out int parsedValue))
        {
            RefreshValues();
            return;
        }

        ApplyValue(parsedValue, true);
    }

    private void ApplyValue(int value, bool refreshToolbar)
    {
        MapEditorManager target = manager != null ? manager : MapEditorManager.Instance;

        if (target == null)
        {
            return;
        }

        int clampedValue = Mathf.Clamp(value, 1, MapEditorManager.MaxMapSize);

        if (controlsWidth)
        {
            target.ResizeMap(clampedValue, target.mapHeight, refreshToolbar);
        }
        else
        {
            target.ResizeMap(target.mapWidth, clampedValue, refreshToolbar);
        }

        suppressEvents = true;

        if (inputField != null)
        {
            inputField.text = clampedValue.ToString();
        }

        UpdateCurrentSizeText(target);
        suppressEvents = false;
    }

    private void UpdateCurrentSizeText(MapEditorManager target)
    {
        if (currentSizeText != null && target != null)
        {
            currentSizeText.text = target.mapWidth + " x " + target.mapHeight;
        }
    }
}
