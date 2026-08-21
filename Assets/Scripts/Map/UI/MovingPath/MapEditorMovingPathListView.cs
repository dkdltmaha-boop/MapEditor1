using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public sealed class MapEditorMovingPathListView
{
    private const string RootObjectName = "MovingPathList";
    private const float TopInset = 588f;
    private const float HeaderHeight = 24f;
    private const float SpeedControlHeight = 30f;
    private const float RowHeight = 48f;
    private const float RowSpacing = 2f;

    private static readonly Color PanelColor = new Color(0.08f, 0.1f, 0.11f, 0.82f);
    private static readonly Color RowColor = new Color(0.14f, 0.16f, 0.17f, 0.96f);
    private static readonly Color SelectedRowColor = new Color(0.12f, 0.38f, 0.78f, 0.96f);
    private static readonly Color ButtonColor = new Color(0.19f, 0.22f, 0.24f, 1f);
    private static readonly Color DeleteColor = new Color(0.55f, 0.16f, 0.17f, 1f);

    private readonly MapEditorManager manager;
    private RectTransform root;
    private RectTransform content;
    private ScrollRect scrollRect;
    private InputField selectedSpeedInput;
    private Text selectedPathLabel;

    public MapEditorMovingPathListView(MapEditorManager manager)
    {
        this.manager = manager;
    }

    public void EnsureArea(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        Transform existing = parent.Find(RootObjectName);
        if (existing == null)
        {
            Build(parent);
        }
        else
        {
            root = existing as RectTransform;
            Transform contentTransform = existing.Find("ScrollView/Viewport/Content");
            content = contentTransform as RectTransform;
            scrollRect = existing.GetComponentInChildren<ScrollRect>(true);
            ConfigureRoot();
        }

        Transform legacyVisibilityToggle = root == null ? null : root.Find("VisibilityToggle");
        if (legacyVisibilityToggle != null)
        {
            MapEditorObjectUtility.DestroyObject(legacyVisibilityToggle.gameObject);
        }
        RectTransform header = root == null ? null : root.Find("Header") as RectTransform;
        if (header != null)
        {
            header.sizeDelta = new Vector2(-12f, HeaderHeight);
        }

        EnsureSelectedSpeedControl();
        RectTransform scrollView = root == null ? null : root.Find("ScrollView") as RectTransform;
        if (scrollView != null)
        {
            Stretch(scrollView, new Vector2(4f, 4f), new Vector2(-4f, -(HeaderHeight + SpeedControlHeight)));
        }

        RefreshLocalizedText();
    }

    public void RefreshLocalizedText()
    {
        Text headerText = root == null ? null : root.Find("Header")?.GetComponent<Text>();
        if (headerText != null)
        {
            headerText.text = MapEditorLocalization.Choose("이동 경로", "Moving Paths");
        }

        Text speedLabel = root == null
            ? null
            : root.Find("SelectedPathControls/SpeedLabel")?.GetComponent<Text>();
        if (speedLabel != null)
        {
            speedLabel.text = MapEditorLocalization.Choose("속도", "Speed");
        }

        Refresh();
    }

    public void Refresh()
    {
        RefreshSelectedSpeedControl();
        if (content == null)
        {
            return;
        }

        for (int i = content.childCount - 1; i >= 0; i--)
        {
            MapEditorObjectUtility.DestroyObject(content.GetChild(i).gameObject);
        }

        int count = manager == null ? 0 : manager.MovingRegionCount;
        if (count == 0)
        {
            CreateEmptyLabel();
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                CreateRow(i);
            }
        }

        float height = count == 0 ? RowHeight : count * (RowHeight + RowSpacing) - RowSpacing;
        content.sizeDelta = new Vector2(0f, Mathf.Max(RowHeight, height));
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0f);

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void Build(Transform parent)
    {
        GameObject rootObject = new GameObject(RootObjectName, typeof(RectTransform), typeof(Image));
        rootObject.transform.SetParent(parent, false);
        root = rootObject.GetComponent<RectTransform>();
        rootObject.GetComponent<Image>().color = PanelColor;
        ConfigureRoot();

        RectTransform header = CreateText(root, "Header", MapEditorLocalization.Choose("이동 경로", "Moving Paths"),
            13, FontStyle.Bold, TextAnchor.MiddleLeft, Vector2.zero, Vector2.zero);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.anchoredPosition = new Vector2(0f, -2f);
        header.sizeDelta = new Vector2(-12f, HeaderHeight);

        GameObject scrollObject = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollObject.transform.SetParent(root, false);
        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        Stretch(scrollRectTransform, new Vector2(4f, 4f), new Vector2(-4f, -(HeaderHeight + SpeedControlHeight)));

        GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewportObject.transform.SetParent(scrollObject.transform, false);
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        Stretch(viewport, Vector2.zero, new Vector2(-10f, 0f));
        viewportObject.GetComponent<Image>().color = Color.clear;

        GameObject contentObject = new GameObject("Content", typeof(RectTransform));
        contentObject.transform.SetParent(viewportObject.transform, false);
        content = contentObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, RowHeight);

        Scrollbar scrollbar = CreateScrollbar(scrollObject.transform);
        scrollRect = scrollObject.GetComponent<ScrollRect>();
        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 18f;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scrollRect.verticalScrollbarSpacing = 2f;
    }

    private void EnsureSelectedSpeedControl()
    {
        if (root == null)
        {
            return;
        }

        Transform existing = root.Find("SelectedPathControls");
        if (existing != null)
        {
            selectedPathLabel = existing.Find("PathLabel")?.GetComponent<Text>();
            selectedSpeedInput = existing.Find("Speed")?.GetComponent<InputField>();
            if (selectedPathLabel != null && selectedSpeedInput != null)
            {
                return;
            }

            MapEditorObjectUtility.DestroyObject(existing.gameObject);
        }

        GameObject controlsObject = new GameObject("SelectedPathControls", typeof(RectTransform), typeof(Image));
        controlsObject.transform.SetParent(root, false);
        RectTransform controls = controlsObject.GetComponent<RectTransform>();
        controls.anchorMin = new Vector2(0f, 1f);
        controls.anchorMax = new Vector2(1f, 1f);
        controls.pivot = new Vector2(0.5f, 1f);
        controls.anchoredPosition = new Vector2(0f, -HeaderHeight);
        controls.sizeDelta = new Vector2(-8f, SpeedControlHeight - 2f);
        controlsObject.GetComponent<Image>().color = RowColor;

        selectedPathLabel = CreateText(controls, "PathLabel", string.Empty, 9, FontStyle.Normal,
            TextAnchor.MiddleLeft, new Vector2(6f, 2f), new Vector2(-104f, -2f)).GetComponent<Text>();

        RectTransform speedLabel = CreateText(controls, "SpeedLabel",
            MapEditorLocalization.Choose("\uC18D\uB3C4", "Speed"), 9, FontStyle.Bold,
            TextAnchor.MiddleRight, Vector2.zero, Vector2.zero);
        speedLabel.anchorMin = new Vector2(1f, 0f);
        speedLabel.anchorMax = new Vector2(1f, 1f);
        speedLabel.offsetMin = new Vector2(-100f, 2f);
        speedLabel.offsetMax = new Vector2(-64f, -2f);

        selectedSpeedInput = CreateSelectedSpeedInput(controls);
        RectTransform inputRect = selectedSpeedInput.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(1f, 0f);
        inputRect.anchorMax = new Vector2(1f, 1f);
        inputRect.offsetMin = new Vector2(-60f, 3f);
        inputRect.offsetMax = new Vector2(-5f, -3f);
    }

    private InputField CreateSelectedSpeedInput(Transform parent)
    {
        GameObject inputObject = new GameObject("Speed", typeof(RectTransform), typeof(Image), typeof(InputField));
        inputObject.transform.SetParent(parent, false);
        inputObject.GetComponent<Image>().color = new Color(0.06f, 0.07f, 0.08f, 1f);
        Text text = CreateText(inputObject.transform, "Text", "1", 10, FontStyle.Bold,
            TextAnchor.MiddleCenter, new Vector2(2f, 0f), new Vector2(-2f, 0f)).GetComponent<Text>();

        InputField input = inputObject.GetComponent<InputField>();
        input.textComponent = text;
        input.characterLimit = 5;
        input.contentType = InputField.ContentType.DecimalNumber;
        input.lineType = InputField.LineType.SingleLine;
        input.onEndEdit.AddListener(nextValue =>
        {
            int index = manager == null ? -1 : manager.SelectedMovingRegionIndex;
            float parsed;
            bool valid = float.TryParse(nextValue, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
                || float.TryParse(nextValue, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed);
            if (index >= 0 && valid)
            {
                manager.SetMovingRegionSpeed(index, parsed);
            }

            RefreshSelectedSpeedControl();
        });
        return input;
    }

    private void RefreshSelectedSpeedControl()
    {
        if (selectedSpeedInput == null || selectedPathLabel == null)
        {
            return;
        }

        int index = manager == null ? -1 : manager.SelectedMovingRegionIndex;
        MapEditorMovingRegionData region = manager == null ? null : manager.GetMovingRegionAt(index);
        bool hasSelection = region != null;
        selectedSpeedInput.interactable = hasSelection;
        selectedSpeedInput.text = hasSelection
            ? region.tilesPerSecond.ToString("0.##", CultureInfo.InvariantCulture)
            : "-";
        selectedPathLabel.text = hasSelection
            ? (string.IsNullOrWhiteSpace(region.displayName) ? "#" + (index + 1) : region.displayName)
            : MapEditorLocalization.Choose("\uACBD\uB85C \uC120\uD0DD", "Select a path");
    }

    private void ConfigureRoot()
    {
        if (root == null)
        {
            return;
        }

        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.pivot = new Vector2(0.5f, 0.5f);
        root.offsetMin = new Vector2(8f, 8f);
        root.offsetMax = new Vector2(-8f, -TopInset);
        root.gameObject.SetActive(true);
    }

    private void CreateEmptyLabel()
    {
        CreateText(content, "Empty", MapEditorLocalization.Choose("저장된 이동 경로 없음", "No saved paths"), 11,
            FontStyle.Normal, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
    }

    private void CreateRow(int index)
    {
        MapEditorMovingRegionData region = manager.GetMovingRegionAt(index);
        if (region == null)
        {
            return;
        }

        GameObject rowObject = new GameObject("MovingPath_" + index, typeof(RectTransform), typeof(Image));
        rowObject.transform.SetParent(content, false);
        RectTransform row = rowObject.GetComponent<RectTransform>();
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = new Vector2(1f, 1f);
        row.pivot = new Vector2(0.5f, 1f);
        row.anchoredPosition = new Vector2(0f, -index * (RowHeight + RowSpacing));
        row.sizeDelta = new Vector2(0f, RowHeight);
        rowObject.GetComponent<Image>().color = manager.SelectedMovingRegionIndex == index ? SelectedRowColor : RowColor;

        RectTransform number = CreateText(row, "Number", (index + 1).ToString(), 10, FontStyle.Bold,
            TextAnchor.MiddleCenter, new Vector2(2f, -2f), new Vector2(18f, -4f));
        number.anchorMin = new Vector2(0f, 0.5f);
        number.anchorMax = new Vector2(0f, 1f);
        number.pivot = new Vector2(0.5f, 0.5f);
        number.offsetMin = new Vector2(2f, 2f);
        number.offsetMax = new Vector2(20f, -2f);

        InputField input = CreateNameInput(row, region.displayName, index, manager);
        RectTransform inputRect = input.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0f, 0.5f);
        inputRect.anchorMax = new Vector2(1f, 1f);
        inputRect.offsetMin = new Vector2(22f, 2f);
        inputRect.offsetMax = new Vector2(-104f, -2f);

        CreateButton(row, "Show", MapEditorLocalization.Choose("보기", "Show"), ButtonColor,
            new Vector2(-102f, 26f), new Vector2(-66f, -2f), () => manager.FocusMovingRegion(index));
        bool guideVisible = manager.IsMovingPathGuideVisible(index);
        CreateButton(row, "Visibility", guideVisible ? "ON" : "OFF",
            guideVisible ? SelectedRowColor : ButtonColor,
            new Vector2(-64f, 26f), new Vector2(-26f, -2f), () => manager.ToggleMovingPathGuide(index));
        CreateButton(row, "Delete", "X", DeleteColor,
            new Vector2(-24f, 26f), new Vector2(-2f, -2f), () => manager.DeleteMovingRegion(index));

        RectTransform layerLabel = CreateText(row, "Layer",
            MapEditorLocalization.Choose("\uB808\uC774\uC5B4 ", "Layer ") + (region.canvasLayerIndex + 1),
            9, FontStyle.Normal, TextAnchor.MiddleLeft, Vector2.zero, Vector2.zero);
        ConfigureLowerRow(layerLabel, 6f, 62f);

        RectTransform speedLabel = CreateText(row, "SpeedLabel",
            MapEditorLocalization.Choose("\uC18D\uB3C4", "Speed"),
            9, FontStyle.Normal, TextAnchor.MiddleRight, Vector2.zero, Vector2.zero);
        ConfigureLowerRow(speedLabel, 62f, 98f);

        InputField speedInput = CreateSpeedInput(row, region.tilesPerSecond, index, manager);
        ConfigureLowerRow(speedInput.GetComponent<RectTransform>(), 102f, 146f);

        RectTransform unitLabel = CreateText(row, "SpeedUnit",
            MapEditorLocalization.Choose("\uCE78/\uCD08", "tiles/s"),
            9, FontStyle.Normal, TextAnchor.MiddleLeft, Vector2.zero, Vector2.zero);
        unitLabel.anchorMin = new Vector2(0f, 0f);
        unitLabel.anchorMax = new Vector2(1f, 0.5f);
        unitLabel.offsetMin = new Vector2(150f, 2f);
        unitLabel.offsetMax = new Vector2(-2f, -2f);
    }

    private static void ConfigureLowerRow(RectTransform rect, float left, float right)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.offsetMin = new Vector2(left, 2f);
        rect.offsetMax = new Vector2(right, -2f);
    }

    private static InputField CreateNameInput(Transform parent, string value, int index, MapEditorManager manager)
    {
        GameObject inputObject = new GameObject("Name", typeof(RectTransform), typeof(Image), typeof(InputField));
        inputObject.transform.SetParent(parent, false);
        Image background = inputObject.GetComponent<Image>();
        background.color = new Color(0.06f, 0.07f, 0.08f, 1f);

        Text text = CreateText(inputObject.transform, "Text", value, 10, FontStyle.Normal,
            TextAnchor.MiddleLeft, new Vector2(4f, 0f), new Vector2(-6f, 0f)).GetComponent<Text>();
        text.horizontalOverflow = HorizontalWrapMode.Wrap;

        InputField input = inputObject.GetComponent<InputField>();
        input.textComponent = text;
        input.text = value;
        input.characterLimit = 30;
        input.lineType = InputField.LineType.SingleLine;
        input.onEndEdit.AddListener(nextName =>
        {
            if (manager != null)
            {
                manager.RenameMovingRegion(index, nextName);
            }
        });
        return input;
    }

    private static InputField CreateSpeedInput(Transform parent, float value, int index, MapEditorManager manager)
    {
        GameObject inputObject = new GameObject("Speed", typeof(RectTransform), typeof(Image), typeof(InputField));
        inputObject.transform.SetParent(parent, false);
        inputObject.GetComponent<Image>().color = new Color(0.06f, 0.07f, 0.08f, 1f);

        string formatted = value.ToString("0.##", CultureInfo.InvariantCulture);
        Text text = CreateText(inputObject.transform, "Text", formatted, 9, FontStyle.Normal,
            TextAnchor.MiddleCenter, new Vector2(2f, 0f), new Vector2(-2f, 0f)).GetComponent<Text>();

        InputField input = inputObject.GetComponent<InputField>();
        input.textComponent = text;
        input.text = formatted;
        input.characterLimit = 5;
        input.contentType = InputField.ContentType.DecimalNumber;
        input.lineType = InputField.LineType.SingleLine;
        input.onEndEdit.AddListener(nextValue =>
        {
            float parsed;
            bool valid = float.TryParse(nextValue, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
                || float.TryParse(nextValue, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed);
            if (manager != null && valid) manager.SetMovingRegionSpeed(index, parsed);

            MapEditorMovingRegionData current = manager == null ? null : manager.GetMovingRegionAt(index);
            input.text = (current == null ? value : current.tilesPerSecond).ToString("0.##", CultureInfo.InvariantCulture);
        });
        return input;
    }

    private static void CreateButton(
        Transform parent,
        string name,
        string label,
        Color color,
        Vector2 offsetMin,
        Vector2 offsetMax,
        UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        buttonObject.GetComponent<Image>().color = color;

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(action);
        CreateText(buttonObject.transform, "Text", label, 9, FontStyle.Bold, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.zero);
    }

    private static Scrollbar CreateScrollbar(Transform parent)
    {
        GameObject scrollbarObject = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        scrollbarObject.transform.SetParent(parent, false);
        RectTransform rect = scrollbarObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.offsetMin = new Vector2(-8f, 0f);
        rect.offsetMax = Vector2.zero;
        scrollbarObject.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.06f, 0.9f);

        GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObject.transform.SetParent(scrollbarObject.transform, false);
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        Stretch(handleRect, new Vector2(1f, 1f), new Vector2(-1f, -1f));
        handleObject.GetComponent<Image>().color = new Color(0.46f, 0.53f, 0.58f, 1f);

        Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleObject.GetComponent<Image>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        return scrollbar;
    }

    private static RectTransform CreateText(
        Transform parent,
        string name,
        string value,
        int fontSize,
        FontStyle fontStyle,
        TextAnchor alignment,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        Stretch(rect, offsetMin, offsetMax);

        Text text = textObject.GetComponent<Text>();
        text.font = MapEditorFontProvider.Default;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = Color.white;
        text.text = value;
        text.raycastTarget = false;
        return rect;
    }

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
