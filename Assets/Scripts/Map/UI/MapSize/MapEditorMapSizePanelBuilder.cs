using UnityEngine;
using UnityEngine.UI;

public static class MapEditorMapSizePanelBuilder
{
    private const string MapSizePanelObjectName = "MapEditor_MapSizePanel";
    private const float ToolbarWidth = 176f;
    internal const float PanelWidth = 184f;
    internal const float PanelHeight = 172f;
    internal const float PanelGap = 12f;

    internal static Vector2 GetPanelPosition(Vector2 toolbarOffset)
    {
        return toolbarOffset + new Vector2(-(ToolbarWidth + 10f), 0f);
    }

    internal static Vector2 GetLayerPanelPosition(Vector2 toolbarOffset)
    {
        return GetPanelPosition(toolbarOffset) + new Vector2(0f, -(PanelHeight + PanelGap));
    }

    public static void Ensure(Transform canvas, MapEditorManager manager, Vector2 toolbarOffset)
    {
        if (canvas == null || manager == null)
        {
            return;
        }

        Transform panel = canvas.Find(MapSizePanelObjectName);

        if (panel == null)
        {
            GameObject panelObject = new GameObject(MapSizePanelObjectName, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panelObject.transform.SetParent(canvas, false);
            panel = panelObject.transform;
        }

        RemoveDuplicatePanels(canvas, panel);
        ConfigurePanel(panel, toolbarOffset);
        ClearChildren(panel);

        Text panelTitle = CreateLabel(panel, "맵 크기", 12, 16f);
        panelTitle.fontStyle = FontStyle.Bold;
        Text currentSizeText = CreateCurrentSizeLabel(panel, manager);
        CreateValueControls(panel, manager, currentSizeText, true);
        CreateValueControls(panel, manager, currentSizeText, false);
        CreatePresetRow(panel, manager, "PresetRow", "64 x 64", 64, 64, "128 x 128", 128, 128);
        CreatePresetRow(panel, manager, "LargePresetRow", "256 x 128", 256, 128, "256 x 256", 256, 256);
    }

    public static void RefreshLayout(Transform canvas, Vector2 toolbarOffset)
    {
        Transform panel = canvas == null ? null : canvas.Find(MapSizePanelObjectName);

        if (panel != null)
        {
            ConfigurePanel(panel, toolbarOffset);
        }
    }

    private static void ConfigurePanel(Transform panel, Vector2 toolbarOffset)
    {
        RectTransform rect = panel.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            Vector2 position = GetPanelPosition(toolbarOffset);
            Vector2 size = new Vector2(PanelWidth, PanelHeight);
            RectTransform parentRect = panel.parent as RectTransform;

            if (parentRect != null)
            {
                position.x = Mathf.Clamp(position.x, -parentRect.rect.width + size.x + 8f, -8f);
                position.y = Mathf.Clamp(position.y, -parentRect.rect.height + size.y + 8f, -8f);
            }

            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        Image image = panel.GetComponent<Image>();

        if (image == null)
        {
            image = panel.gameObject.AddComponent<Image>();
        }

        image.color = new Color(0.13f, 0.13f, 0.13f, 0.92f);
        image.raycastTarget = false;

        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();

        if (layout == null)
        {
            layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.spacing = 3f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private static Text CreateCurrentSizeLabel(Transform parent, MapEditorManager manager)
    {
        Text text = CreateLabel(parent, manager.mapWidth + " x " + manager.mapHeight, 10, 18f);
        text.name = "CurrentSize";
        return text;
    }

    private static void CreateValueControls(Transform parent, MapEditorManager manager, Text currentSizeText, bool widthControl)
    {
        GameObject groupObject = new GameObject(widthControl ? "WidthControl" : "HeightControl", typeof(RectTransform), typeof(VerticalLayoutGroup));
        groupObject.transform.SetParent(parent, false);
        groupObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 34f);

        VerticalLayoutGroup groupLayout = groupObject.GetComponent<VerticalLayoutGroup>();
        groupLayout.spacing = 2f;
        groupLayout.childControlWidth = true;
        groupLayout.childControlHeight = false;
        groupLayout.childForceExpandWidth = true;
        groupLayout.childForceExpandHeight = false;

        GameObject rowObject = new GameObject(widthControl ? "WidthInputRow" : "HeightInputRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rowObject.transform.SetParent(groupObject.transform, false);
        rowObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 16f);

        HorizontalLayoutGroup rowLayout = rowObject.GetComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 5f;
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = true;

        CreateSmallText(rowObject.transform, widthControl ? "너비" : "높이", 48f);
        int value = widthControl ? manager.mapWidth : manager.mapHeight;
        InputField input = CreateInput(rowObject.transform, value);
        CreateSlider(groupObject.transform, manager, currentSizeText, input, widthControl, widthControl ? "WidthSlider" : "HeightSlider");

        MapEditorMapSizeControl control = input.gameObject.AddComponent<MapEditorMapSizeControl>();
        control.Configure(manager, widthControl, input, currentSizeText);
    }

    private static InputField CreateInput(Transform parent, int value)
    {
        GameObject inputObject = new GameObject("ValueInput", typeof(RectTransform), typeof(Image), typeof(InputField));
        inputObject.transform.SetParent(parent, false);
        inputObject.GetComponent<RectTransform>().sizeDelta = new Vector2(58f, 0f);
        inputObject.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.95f);

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(inputObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(6f, 0f);
        textRect.offsetMax = new Vector2(-6f, 0f);

        Text text = textObject.GetComponent<Text>();
        text.font = MapEditorFontProvider.Default;
        text.fontSize = 11;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.white;
        text.text = value.ToString();

        InputField input = inputObject.GetComponent<InputField>();
        input.textComponent = text;
        input.contentType = InputField.ContentType.IntegerNumber;
        input.text = value.ToString();
        return input;
    }

    private static void CreateSlider(Transform parent, MapEditorManager manager, Text currentSizeText, InputField input, bool widthControl, string name)
    {
        GameObject sliderObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(MapEditorMapSizeSlider));
        sliderObject.transform.SetParent(parent, false);
        sliderObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 12f);
        sliderObject.GetComponent<Image>().color = new Color(0.07f, 0.07f, 0.07f, 0.95f);

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(sliderObject.transform, false);
        fillObject.GetComponent<Image>().color = new Color(0.18f, 0.48f, 0.95f, 1f);

        GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObject.transform.SetParent(sliderObject.transform, false);
        handleObject.GetComponent<RectTransform>().sizeDelta = new Vector2(6f, 16f);
        handleObject.GetComponent<Image>().color = Color.white;

        MapEditorMapSizeSlider slider = sliderObject.GetComponent<MapEditorMapSizeSlider>();
        slider.Configure(manager, widthControl, fillObject.GetComponent<RectTransform>(), handleObject.GetComponent<RectTransform>(), input, currentSizeText);
    }

    private static void CreatePresetRow(
        Transform parent,
        MapEditorManager manager,
        string rowName,
        string firstLabel,
        int firstWidth,
        int firstHeight,
        string secondLabel,
        int secondWidth,
        int secondHeight)
    {
        GameObject rowObject = new GameObject(rowName, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rowObject.transform.SetParent(parent, false);
        rowObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 20f);

        HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 3f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        CreatePresetButton(rowObject.transform, manager, firstLabel, firstWidth, firstHeight);
        CreatePresetButton(rowObject.transform, manager, secondLabel, secondWidth, secondHeight);
    }

    private static void CreatePresetButton(Transform parent, MapEditorManager manager, string label, int width, int height)
    {
        GameObject buttonObject = new GameObject("Preset" + label + "Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(MapEditorToolbarButton));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 20f);
        buttonObject.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f, 1f);

        MapEditorToolbarButton toolbarButton = buttonObject.GetComponent<MapEditorToolbarButton>();
        toolbarButton.manager = manager;
        toolbarButton.action = MapEditorToolbarAction.MapPresetSquare;
        toolbarButton.intArgument = width;
        toolbarButton.intArgument2 = height;

        Text text = CreateLabel(buttonObject.transform, label, 10, 0f);
        text.alignment = TextAnchor.MiddleCenter;

        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private static Text CreateSmallText(Transform parent, string text, float width)
    {
        Text label = CreateLabel(parent, text, 10, 0f);
        label.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 0f);
        return label;
    }

    private static Text CreateLabel(Transform parent, string value, int fontSize, float height)
    {
        GameObject labelObject = new GameObject(value, typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(parent, false);
        labelObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, height);

        Text label = labelObject.GetComponent<Text>();
        label.text = value;
        label.font = MapEditorFontProvider.Default;
        label.fontSize = fontSize;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = Color.white;
        return label;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            child.name = "Destroyed_" + child.name;
            MapEditorObjectUtility.DestroyObject(child.gameObject);
        }
    }

    private static void RemoveDuplicatePanels(Transform canvas, Transform keep)
    {
        for (int i = canvas.childCount - 1; i >= 0; i--)
        {
            Transform child = canvas.GetChild(i);

            if (child != null && child != keep && child.name == MapSizePanelObjectName)
            {
                child.name = "Destroyed_" + child.name;
                MapEditorObjectUtility.DestroyObject(child.gameObject);
            }
        }
    }
}
