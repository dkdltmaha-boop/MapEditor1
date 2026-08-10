using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ColorWheelPickerWindow : MonoBehaviour
{
    private const string WindowObjectName = "MapEditor_ColorPicker";
    private const string LegacyWindowObjectName = "ColorWheelWindow";
    private const int SquareWidth = 196;
    private const int SquareHeight = 140;
    private const int HueBarWidth = 196;
    private const int HueBarHeight = 16;
    private const float ContentVerticalOffset = 10f;
    private static readonly Vector2 PreferredWindowSize = new Vector2(246f, 620f);
    private const float WindowScreenMargin = 8f;
    private const string WallTileSelectorObjectName = "WallTileSelector";
    private const string WallTilePreviewObjectName = "WallTilePreview";
    private const string ExportCellSizeSelectorObjectName = "ExportCellSizeSelector";
    private const string ExportCellSizeButtonPrefix = "ExportCellSize";
    private const string HexColorInputObjectName = "HexColorInput";

    private MapEditorManager manager;
    private RawImage wheelImage;
    private RawImage squareImage;
    private Image previewImage;
    private Image wallTilePreviewImage;
    private Text titleText;
    private InputField hexInputField;
    private RectTransform wheelHandle;
    private RectTransform squareHandle;
    private Texture2D squareTexture;
    private ColorWheelPngPaletteView pngPaletteView;

    private float hue;
    private float saturation = 1f;
    private float value = 1f;
    private bool isBuilt;

    public static ColorWheelPickerWindow Create(MapEditorManager manager, Vector2 offset)
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            return null;
        }

        Transform existing = canvas.transform.Find(WindowObjectName);

        if (existing == null)
        {
            existing = canvas.transform.Find(LegacyWindowObjectName);

            if (existing != null)
            {
                existing.name = WindowObjectName;
            }
        }

        if (existing != null)
        {
            RectTransform existingRect = existing.GetComponent<RectTransform>();

            if (existingRect != null)
            {
                ConfigureWindowRect(existingRect, offset);
            }

            ColorWheelPickerWindow existingPicker = existing.GetComponent<ColorWheelPickerWindow>();

            if (existingPicker == null)
            {
                existingPicker = existing.gameObject.AddComponent<ColorWheelPickerWindow>();
            }

            existingPicker.Initialize(manager);
            return existingPicker;
        }

        GameObject windowObject = new GameObject(WindowObjectName, typeof(RectTransform), typeof(Image), typeof(ColorWheelPickerWindow));
        windowObject.transform.SetParent(canvas.transform, false);

        RectTransform rect = windowObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        ConfigureWindowRect(rect, offset);

        Image background = windowObject.GetComponent<Image>();
        background.color = new Color(0.18f, 0.18f, 0.18f, 0.96f);

        ColorWheelPickerWindow picker = windowObject.GetComponent<ColorWheelPickerWindow>();
        picker.Initialize(manager);
        return picker;
    }

    private static void ConfigureWindowRect(RectTransform rect, Vector2 offset)
    {
        if (rect == null)
        {
            return;
        }

        Vector2 size = PreferredWindowSize;
        Vector2 position = offset;
        RectTransform parentRect = rect.parent as RectTransform;

        if (parentRect != null)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            size.x = Mathf.Min(size.x, Mathf.Max(120f, parentRect.rect.width - WindowScreenMargin * 2f));
            position.x = Mathf.Clamp(position.x, WindowScreenMargin, parentRect.rect.width - size.x - WindowScreenMargin);
            position.y = 0f;
            size.y = 0f;
        }
        else
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
        }

        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    public void RefreshLayout(Vector2 offset)
    {
        ConfigureWindowRect(transform as RectTransform, offset);
    }

    public void Initialize(MapEditorManager manager)
    {
        this.manager = manager;
        pngPaletteView = new ColorWheelPngPaletteView(this, manager);
        RemoveMissingScripts();
        RemoveChild("AlphaControl");

        if (transform.Find("HueBar") == null)
        {
            if (transform.childCount == 0)
            {
                BuildWindow();
            }
            else
            {
                UpgradeLegacyColorPicker();
            }
        }
        else
        {
            CacheExistingReferences();
            EnsureWallTileSelector();
            EnsureExportCellSizeSelector();
            EnsureHexColorInput();
        }

        ApplyCompactVerticalLayout();
        isBuilt = true;
        SetColor(manager.selectedColor, false);
    }

    private void ApplyCompactVerticalLayout()
    {
        SetChildVerticalPosition("SaturationValueSquare", -122f + ContentVerticalOffset);
        SetChildVerticalPosition("HueBar", -210f + ContentVerticalOffset);
        SetChildVerticalPosition(HexColorInputObjectName, -228f + ContentVerticalOffset);
        SetChildVerticalPosition(WallTileSelectorObjectName, -270f + ContentVerticalOffset);
        SetChildVerticalPosition(ExportCellSizeSelectorObjectName, -312f + ContentVerticalOffset);
    }

    private void SetChildVerticalPosition(string childName, float y)
    {
        RectTransform child = transform.Find(childName) as RectTransform;
        if (child != null)
        {
            child.anchoredPosition = new Vector2(child.anchoredPosition.x, y);
        }
    }

    public void SetColor(Color color, bool notifyManager)
    {
        Color.RGBToHSV(color, out hue, out saturation, out value);

        if (saturation <= 0f)
        {
            saturation = 0f;
        }

        UpdateSquareTexture();
        UpdatePreview(notifyManager);
        UpdateHandles();
    }

    public void RefreshLocalizedText()
    {
        UpdateColorDetails();

        Transform wallTileLabel = transform.Find(WallTileSelectorObjectName + "/WallTileLabel");
        Text wallText = wallTileLabel == null ? null : wallTileLabel.GetComponent<Text>();
        if (wallText != null)
        {
            wallText.text = MapEditorLocalization.Choose("벽 타일", "Wall Tile");
            ApplySectionHeadingStyle(wallText);
        }

        Transform sizeLabel = transform.Find(ExportCellSizeSelectorObjectName + "/DotSizeLabel");
        Text sizeText = sizeLabel == null ? null : sizeLabel.GetComponent<Text>();
        if (sizeText != null)
        {
            sizeText.text = MapEditorLocalization.Choose("그리기 크기", "Paint Size");
            ApplySectionHeadingStyle(sizeText);
        }

        RefreshHexInputLabels(transform.Find(HexColorInputObjectName));
    }

    public void SetHueFromLocalPoint(Vector2 localPoint)
    {
        hue = Mathf.InverseLerp(-HueBarWidth * 0.5f, HueBarWidth * 0.5f, localPoint.x);
        UpdateSquareTexture();
        UpdatePreview(true);
        UpdateHandles();
    }

    public void SetSaturationValueFromLocalPoint(Vector2 localPoint)
    {
        float x = Mathf.InverseLerp(-SquareWidth * 0.5f, SquareWidth * 0.5f, localPoint.x);
        float y = Mathf.InverseLerp(-SquareHeight * 0.5f, SquareHeight * 0.5f, localPoint.y);

        saturation = Mathf.Clamp01(x);
        value = Mathf.Clamp01(y);
        UpdatePreview(true);
        UpdateHandles();
    }

    private void BuildWindow()
    {
        if (isBuilt)
        {
            return;
        }

        CreateTitle();
        CreatePreview();
        CreateWheel();
        CreateSquare();
        CreateHexColorInput();
        CreateWallTileSelector();
        CreateExportCellSizeSelector();
        pngPaletteView.CreateArea(transform);
    }

    private void UpgradeLegacyColorPicker()
    {
        RemoveChild("Title");
        RemoveChild("Preview");
        RemoveChild("AlphaControl");
        RemoveChild("HueWheel");
        RemoveChild("HueBar");
        RemoveChild("SaturationValueSquare");

        CreateTitle();
        CreatePreview();
        CreateSquare();
        CreateWheel();
        CreateHexColorInput();

        EnsureWallTileSelector();
        EnsureExportCellSizeSelector();

        if (transform.Find("ColorPicker_PngTilesetViewport") == null
            && transform.Find("PngPaletteViewport") == null)
        {
            pngPaletteView.CreateArea(transform);
        }
        else
        {
            pngPaletteView.CacheExistingReferences(transform);
        }
    }

    private void RemoveChild(string objectName)
    {
        Transform child = transform.Find(objectName);
        if (child != null)
        {
            MapEditorObjectUtility.DestroyObject(child.gameObject);
        }
    }

    private void CacheExistingReferences()
    {
        SetExistingText("Title", MapEditorLocalization.Choose("색상", "Color"));
        Transform preview = transform.Find("Preview");
        Transform title = transform.Find("Title");
        Transform wallTileSelector = transform.Find(WallTileSelectorObjectName);
        Transform wheel = transform.Find("HueBar");
        Transform square = transform.Find("SaturationValueSquare");
        Transform hexInput = transform.Find(HexColorInputObjectName);

        if (preview != null)
        {
            previewImage = preview.GetComponent<Image>();
        }

        titleText = title == null ? null : title.GetComponent<Text>();
        ApplySectionHeadingStyle(titleText);
        hexInputField = hexInput == null ? null : hexInput.GetComponentInChildren<InputField>(true);
        ConfigureHexInputEvents();

        if (wallTileSelector != null)
        {
            Transform wallTileLabel = wallTileSelector.Find("WallTileLabel");
            Text label = wallTileLabel == null ? null : wallTileLabel.GetComponent<Text>();
            if (label != null)
            {
                label.text = MapEditorLocalization.Choose("벽 타일", "Wall Tile");
                ApplySectionHeadingStyle(label);
            }

            Transform wallTilePreview = wallTileSelector.Find(WallTilePreviewObjectName);
            wallTilePreviewImage = wallTilePreview == null ? null : wallTilePreview.GetComponent<Image>();

            Button button = wallTileSelector.GetComponent<Button>();

            if (button == null)
            {
                button = wallTileSelector.gameObject.AddComponent<Button>();
            }

            MapEditorToolbarButton toolbarButton = wallTileSelector.GetComponent<MapEditorToolbarButton>();

            if (toolbarButton == null)
            {
                toolbarButton = wallTileSelector.gameObject.AddComponent<MapEditorToolbarButton>();
            }

            toolbarButton.manager = manager;
            toolbarButton.action = MapEditorToolbarAction.Wall;
        }

        if (wheel != null)
        {
            wheelImage = wheel.GetComponent<RawImage>();
            Transform handle = wheel.Find("HueHandle");
            wheelHandle = handle == null ? null : handle.GetComponent<RectTransform>();

            ColorWheelInput input = wheel.GetComponent<ColorWheelInput>();

            if (input == null)
            {
                input = wheel.gameObject.AddComponent<ColorWheelInput>();
            }

            input.Initialize(this, wheel.GetComponent<RectTransform>());
        }

        if (square != null)
        {
            squareImage = square.GetComponent<RawImage>();
            squareTexture = squareImage == null ? null : squareImage.texture as Texture2D;
            Transform handle = square.Find("SquareHandle");
            squareHandle = handle == null ? null : handle.GetComponent<RectTransform>();

            ColorSquareInput input = square.GetComponent<ColorSquareInput>();

            if (input == null)
            {
                input = square.gameObject.AddComponent<ColorSquareInput>();
            }

            input.Initialize(this, square.GetComponent<RectTransform>());
        }

        pngPaletteView.CacheExistingReferences(transform);
        RefreshLocalizedText();
    }

    private void SetExistingText(string objectName, string value)
    {
        Transform target = transform.Find(objectName);
        Text text = target == null ? null : target.GetComponent<Text>();

        if (text != null)
        {
            text.text = value;
            text.font = MapEditorFontProvider.Default;
        }
    }

    private void RemoveMissingScripts()
    {
#if UNITY_EDITOR
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);

        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
        }
#endif
    }

    private void CreateTitle()
    {
        GameObject titleObject = new GameObject("Title", typeof(RectTransform), typeof(Text));
        titleObject.transform.SetParent(transform, false);

        RectTransform rect = titleObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -8f);
        rect.sizeDelta = new Vector2(-68f, 24f);

        Text text = titleObject.GetComponent<Text>();
        titleText = text;
        text.text = MapEditorLocalization.Choose("색상", "Color");
        ApplySectionHeadingStyle(text);
    }

    private void CreatePreview()
    {
        GameObject previewObject = new GameObject("Preview", typeof(RectTransform), typeof(Image));
        previewObject.transform.SetParent(transform, false);

        RectTransform rect = previewObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-8f, -8f);
        rect.sizeDelta = new Vector2(44f, 24f);

        previewImage = previewObject.GetComponent<Image>();
        previewImage.raycastTarget = false;
    }

    private void CreateWheel()
    {
        GameObject wheelObject = new GameObject("HueBar", typeof(RectTransform), typeof(RawImage), typeof(ColorWheelInput), typeof(Outline));
        wheelObject.transform.SetParent(transform, false);

        RectTransform rect = wheelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -210f + ContentVerticalOffset);
        rect.sizeDelta = new Vector2(HueBarWidth, HueBarHeight);

        wheelImage = wheelObject.GetComponent<RawImage>();
        wheelImage.texture = CreateWheelTexture();
        wheelImage.raycastTarget = true;

        Outline outline = wheelObject.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.75f);
        outline.effectDistance = new Vector2(1f, -1f);

        ColorWheelInput input = wheelObject.GetComponent<ColorWheelInput>();
        input.Initialize(this, rect);

        wheelHandle = CreateHueHandle(wheelObject.transform);
    }

    private void EnsureHexColorInput()
    {
        Transform existing = transform.Find(HexColorInputObjectName);
        if (existing == null)
        {
            CreateHexColorInput();
            return;
        }

        hexInputField = existing.GetComponentInChildren<InputField>(true);
        ConfigureHexInputEvents();
        RefreshHexInputLabels(existing);
    }

    private void CreateHexColorInput()
    {
        if (transform.Find(HexColorInputObjectName) != null)
        {
            EnsureHexColorInput();
            return;
        }

        GameObject rowObject = new GameObject(HexColorInputObjectName, typeof(RectTransform), typeof(Image));
        rowObject.transform.SetParent(transform, false);
        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = new Vector2(0f, -228f + ContentVerticalOffset);
        rowRect.sizeDelta = new Vector2(-16f, 28f);
        rowObject.GetComponent<Image>().color = new Color(0.11f, 0.11f, 0.11f, 0.92f);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(rowObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = new Vector2(8f, 0f);
        labelRect.sizeDelta = new Vector2(34f, 0f);
        Text label = labelObject.GetComponent<Text>();
        label.text = "HEX";
        label.font = MapEditorFontProvider.Default;
        label.fontSize = 10;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = Color.white;

        GameObject inputObject = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(InputField));
        inputObject.transform.SetParent(rowObject.transform, false);
        RectTransform inputRect = inputObject.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0f, 0.5f);
        inputRect.anchorMax = new Vector2(0f, 0.5f);
        inputRect.pivot = new Vector2(0f, 0.5f);
        inputRect.anchoredPosition = new Vector2(42f, 0f);
        inputRect.sizeDelta = new Vector2(122f, 20f);
        Image inputBackground = inputObject.GetComponent<Image>();
        inputBackground.color = new Color(0.06f, 0.06f, 0.06f, 1f);

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(inputObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(5f, 1f);
        textRect.offsetMax = new Vector2(-5f, -1f);
        Text inputText = textObject.GetComponent<Text>();
        inputText.font = MapEditorFontProvider.Default;
        inputText.fontSize = 10;
        inputText.alignment = TextAnchor.MiddleLeft;
        inputText.color = Color.white;
        inputText.supportRichText = false;

        GameObject placeholderObject = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
        placeholderObject.transform.SetParent(inputObject.transform, false);
        RectTransform placeholderRect = placeholderObject.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(5f, 1f);
        placeholderRect.offsetMax = new Vector2(-5f, -1f);
        Text placeholder = placeholderObject.GetComponent<Text>();
        placeholder.font = MapEditorFontProvider.Default;
        placeholder.fontSize = 9;
        placeholder.fontStyle = FontStyle.Italic;
        placeholder.alignment = TextAnchor.MiddleLeft;
        placeholder.color = new Color(1f, 1f, 1f, 0.42f);

        hexInputField = inputObject.GetComponent<InputField>();
        hexInputField.targetGraphic = inputBackground;
        hexInputField.textComponent = inputText;
        hexInputField.placeholder = placeholder;
        hexInputField.lineType = InputField.LineType.SingleLine;
        hexInputField.characterLimit = 7;
        ConfigureHexInputEvents();

        GameObject applyObject = new GameObject("ApplyButton", typeof(RectTransform), typeof(Image), typeof(Button));
        applyObject.transform.SetParent(rowObject.transform, false);
        RectTransform applyRect = applyObject.GetComponent<RectTransform>();
        applyRect.anchorMin = new Vector2(1f, 0.5f);
        applyRect.anchorMax = new Vector2(1f, 0.5f);
        applyRect.pivot = new Vector2(1f, 0.5f);
        applyRect.anchoredPosition = new Vector2(-5f, 0f);
        applyRect.sizeDelta = new Vector2(48f, 20f);
        Image applyImage = applyObject.GetComponent<Image>();
        applyImage.color = new Color(0.18f, 0.48f, 0.95f, 1f);
        Button applyButton = applyObject.GetComponent<Button>();
        applyButton.targetGraphic = applyImage;
        applyButton.onClick.AddListener(() => ApplyHexColor(hexInputField == null ? string.Empty : hexInputField.text));

        GameObject applyTextObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        applyTextObject.transform.SetParent(applyObject.transform, false);
        RectTransform applyTextRect = applyTextObject.GetComponent<RectTransform>();
        applyTextRect.anchorMin = Vector2.zero;
        applyTextRect.anchorMax = Vector2.one;
        applyTextRect.offsetMin = Vector2.zero;
        applyTextRect.offsetMax = Vector2.zero;
        Text applyText = applyTextObject.GetComponent<Text>();
        applyText.font = MapEditorFontProvider.Default;
        applyText.fontSize = 9;
        applyText.alignment = TextAnchor.MiddleCenter;
        applyText.color = Color.white;

        RefreshHexInputLabels(rowObject.transform);
        UpdateColorDetails();
    }

    private void ConfigureHexInputEvents()
    {
        if (hexInputField == null)
        {
            return;
        }

        hexInputField.onEndEdit.RemoveAllListeners();
        hexInputField.onEndEdit.AddListener(ApplyHexColor);
    }

    private void ApplyHexColor(string value)
    {
        string normalized = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        if (!normalized.StartsWith("#"))
        {
            normalized = "#" + normalized;
        }

        if (normalized.Length == 7 && ColorUtility.TryParseHtmlString(normalized, out Color color))
        {
            color.a = 1f;
            SetColor(color, true);
            return;
        }

        UpdateColorDetails();
    }

    private void RefreshHexInputLabels(Transform root)
    {
        if (root == null)
        {
            return;
        }

        Text placeholder = root.Find("Input/Placeholder")?.GetComponent<Text>();
        if (placeholder != null)
        {
            placeholder.text = MapEditorLocalization.Choose("예: #46F1F1", "e.g. #46F1F1");
        }

        Text applyText = root.Find("ApplyButton/Text")?.GetComponent<Text>();
        if (applyText != null)
        {
            applyText.text = MapEditorLocalization.Choose("적용", "Apply");
        }
    }

    private void CreateSquare()
    {
        GameObject squareObject = new GameObject("SaturationValueSquare", typeof(RectTransform), typeof(RawImage), typeof(ColorSquareInput), typeof(Outline));
        squareObject.transform.SetParent(transform, false);

        RectTransform rect = squareObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -122f + ContentVerticalOffset);
        rect.sizeDelta = new Vector2(SquareWidth, SquareHeight);

        squareTexture = new Texture2D(SquareWidth, SquareHeight, TextureFormat.RGBA32, false);
        squareTexture.wrapMode = TextureWrapMode.Clamp;
        squareTexture.filterMode = FilterMode.Bilinear;
        squareImage = squareObject.GetComponent<RawImage>();
        squareImage.texture = squareTexture;

        Outline squareOutline = squareObject.GetComponent<Outline>();
        squareOutline.effectColor = new Color(0f, 0f, 0f, 0.75f);
        squareOutline.effectDistance = new Vector2(1f, -1f);

        ColorSquareInput input = squareObject.GetComponent<ColorSquareInput>();
        input.Initialize(this, rect);

        squareHandle = CreateHandle("SquareHandle", squareObject.transform, 10f);
    }

    private RectTransform CreateHueHandle(Transform parent)
    {
        GameObject handleObject = new GameObject("HueHandle", typeof(RectTransform), typeof(Image), typeof(Outline));
        handleObject.transform.SetParent(parent, false);

        RectTransform rect = handleObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(4f, HueBarHeight + 6f);

        Image image = handleObject.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = false;

        Outline outline = handleObject.GetComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1f, -1f);
        return rect;
    }

    private void CreateWallTileSelector()
    {
        GameObject selectorObject = new GameObject(WallTileSelectorObjectName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(MapEditorToolbarButton));
        selectorObject.transform.SetParent(transform, false);

        RectTransform rect = selectorObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -270f + ContentVerticalOffset);
        rect.sizeDelta = new Vector2(-16f, 34f);

        Image background = selectorObject.GetComponent<Image>();
        background.color = new Color(0.11f, 0.11f, 0.11f, 0.86f);

        Button button = selectorObject.GetComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;

        MapEditorToolbarButton toolbarButton = selectorObject.GetComponent<MapEditorToolbarButton>();
        toolbarButton.manager = manager;
        toolbarButton.action = MapEditorToolbarAction.Wall;

        GameObject labelObject = new GameObject("WallTileLabel", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(selectorObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10f, 0f);
        labelRect.offsetMax = new Vector2(-56f, 0f);

        Text label = labelObject.GetComponent<Text>();
        label.text = MapEditorLocalization.Choose("벽 타일", "Wall Tile");
        ApplySectionHeadingStyle(label);

        GameObject previewObject = new GameObject(WallTilePreviewObjectName, typeof(RectTransform), typeof(Image), typeof(Outline));
        previewObject.transform.SetParent(selectorObject.transform, false);

        RectTransform previewRect = previewObject.GetComponent<RectTransform>();
        previewRect.anchorMin = new Vector2(1f, 0.5f);
        previewRect.anchorMax = new Vector2(1f, 0.5f);
        previewRect.pivot = new Vector2(1f, 0.5f);
        previewRect.anchoredPosition = new Vector2(-10f, 0f);
        previewRect.sizeDelta = new Vector2(26f, 22f);

        wallTilePreviewImage = previewObject.GetComponent<Image>();
        wallTilePreviewImage.color = manager == null ? Color.white : manager.selectedColor;
        wallTilePreviewImage.raycastTarget = false;

        Outline outline = previewObject.GetComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);
    }

    private void EnsureWallTileSelector()
    {
        if (transform.Find(WallTileSelectorObjectName) != null)
        {
            return;
        }

        CreateWallTileSelector();
    }

    private void EnsureExportCellSizeSelector()
    {
        Transform existing = transform.Find(ExportCellSizeSelectorObjectName);

        if (existing != null)
        {
            MapEditorObjectUtility.DestroyObject(existing.gameObject);
        }

        CreateExportCellSizeSelector();
    }

    public void RefreshExportCellSizeSelector()
    {
        EnsureExportCellSizeSelector();
    }

    private void CreateExportCellSizeSelector()
    {
        GameObject selectorObject = new GameObject(ExportCellSizeSelectorObjectName, typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        selectorObject.transform.SetParent(transform, false);

        RectTransform rect = selectorObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -312f + ContentVerticalOffset);
        rect.sizeDelta = new Vector2(-16f, 24f);

        Image background = selectorObject.GetComponent<Image>();
        background.color = new Color(0.11f, 0.11f, 0.11f, 0.86f);
        background.raycastTarget = true;

        HorizontalLayoutGroup layout = selectorObject.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 6, 3, 3);
        layout.spacing = 4f;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        CreateExportCellSizeLabel(selectorObject.transform);
        CreateWholeTileButton(selectorObject.transform);

        foreach (int size in MapEditorManager.ExportCellPixelOptions)
        {
            CreateExportCellSizeButton(selectorObject.transform, size);
        }
    }

    private void CreateExportCellSizeLabel(Transform parent)
    {
        GameObject labelObject = new GameObject("DotSizeLabel", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(parent, false);
        labelObject.GetComponent<RectTransform>().sizeDelta = new Vector2(48f, 0f);

        Text label = labelObject.GetComponent<Text>();
        label.text = MapEditorLocalization.Choose("그리기 크기", "Paint Size");
        ApplySectionHeadingStyle(label);
    }

    private static void ApplySectionHeadingStyle(Text text)
    {
        if (text == null)
        {
            return;
        }

        text.font = MapEditorFontProvider.Default;
        text.fontSize = 12;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.white;
    }

    private void CreateWholeTileButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("WholeTilePaintButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(MapEditorToolbarButton));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(34f, 0f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = manager != null && manager.IsWholeTilePaintMode()
            ? new Color(0.18f, 0.48f, 0.95f, 1f)
            : new Color(0.25f, 0.25f, 0.25f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;

        MapEditorToolbarButton toolbarButton = buttonObject.GetComponent<MapEditorToolbarButton>();
        toolbarButton.manager = manager;
        toolbarButton.action = MapEditorToolbarAction.WholeTilePaint;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.text = "타일";
        text.font = MapEditorFontProvider.Default;
        text.fontSize = 9;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
    }

    private void CreateExportCellSizeButton(Transform parent, int size)
    {
        GameObject buttonObject = new GameObject(ExportCellSizeButtonPrefix + size + "Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(MapEditorToolbarButton));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(27f, 0f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = manager != null
            && !manager.IsWholeTilePaintMode()
            && manager.GetExportCellPixels() == size
            ? new Color(0.18f, 0.48f, 0.95f, 1f)
            : new Color(0.25f, 0.25f, 0.25f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;

        MapEditorToolbarButton toolbarButton = buttonObject.GetComponent<MapEditorToolbarButton>();
        toolbarButton.manager = manager;
        toolbarButton.action = MapEditorToolbarAction.ExportCellPixels;
        toolbarButton.intArgument = size;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.text = GetBrushSizeLabel(size);
        text.font = MapEditorFontProvider.Default;
        text.fontSize = 8;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
    }

    private static string GetBrushSizeLabel(int size)
    {
        switch (size)
        {
            case 4:
                return "2x2";
            case 8:
                return "4x4";
            case 16:
                return "8x8";
            default:
                return "1x1";
        }
    }

    public void SetPngPalette(Texture2D sourceTexture, string sourcePath)
    {
        pngPaletteView.SetPalette(sourceTexture, sourcePath);
    }

    public void SetPngPaletteGridSize(int gridSize)
    {
        pngPaletteView.SetGridSize(gridSize);
    }

    public void ZoomPngPalette(float direction, Vector2 viewportLocalPoint)
    {
        pngPaletteView.Zoom(direction, viewportLocalPoint);
    }

    public void PanPngPalette(Vector2 delta)
    {
        pngPaletteView.Pan(delta);
    }

    public void ResetPngPaletteView()
    {
        pngPaletteView.ResetView();
    }

    public void SelectPngTile(string imagePath, int imageIndex)
    {
        pngPaletteView.SelectTile(imagePath, imageIndex);
    }

    public void BeginPngPaletteSelection(Vector2 screenPosition, Camera eventCamera)
    {
        pngPaletteView.BeginSelection(screenPosition, eventCamera);
    }

    public void UpdatePngPaletteSelection(Vector2 screenPosition, Camera eventCamera)
    {
        pngPaletteView.UpdateSelection(screenPosition, eventCamera);
    }

    public void EndPngPaletteSelection(Vector2 screenPosition, Camera eventCamera)
    {
        pngPaletteView.EndSelection(screenPosition, eventCamera);
    }

    public void CancelPngPaletteSelection()
    {
        pngPaletteView.CancelSelection();
    }

    private RectTransform CreateHandle(string name, Transform parent, float size)
    {
        GameObject handleObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
        handleObject.transform.SetParent(parent, false);

        RectTransform rect = handleObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(size, size);

        Image image = handleObject.GetComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = false;

        Outline outline = handleObject.GetComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(2f, -2f);

        return rect;
    }

    private Texture2D CreateWheelTexture()
    {
        Texture2D texture = new Texture2D(HueBarWidth, HueBarHeight, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < HueBarHeight; y++)
        {
            for (int x = 0; x < HueBarWidth; x++)
            {
                float pixelHue = x / (float)(HueBarWidth - 1);
                texture.SetPixel(x, y, Color.HSVToRGB(pixelHue, 1f, 1f));
            }
        }

        texture.Apply();
        return texture;
    }

    private void UpdateSquareTexture()
    {
        if (squareTexture == null)
        {
            return;
        }

        for (int y = 0; y < SquareHeight; y++)
        {
            float v = y / (float)(SquareHeight - 1);

            for (int x = 0; x < SquareWidth; x++)
            {
                float s = x / (float)(SquareWidth - 1);
                squareTexture.SetPixel(x, y, Color.HSVToRGB(hue, s, v));
            }
        }

        squareTexture.Apply();
    }

    private void UpdatePreview(bool notifyManager)
    {
        Color color = Color.HSVToRGB(hue, saturation, value);

        if (previewImage != null)
        {
            previewImage.color = color;
        }

        if (wallTilePreviewImage != null)
        {
            wallTilePreviewImage.color = color;
        }

        if (notifyManager && manager != null)
        {
            manager.SelectColor(color);
        }

        UpdateColorDetails();
    }

    private void UpdateColorDetails()
    {
        Color color = Color.HSVToRGB(hue, saturation, value);
        Color32 color32 = color;

        if (titleText != null)
        {
            titleText.text = MapEditorLocalization.Choose("색상", "Color");
        }

        if (hexInputField != null)
        {
            hexInputField.SetTextWithoutNotify(string.Format(
                "#{0:X2}{1:X2}{2:X2}",
                color32.r,
                color32.g,
                color32.b));
        }

    }

    private void UpdateHandles()
    {
        if (wheelHandle != null)
        {
            wheelHandle.anchoredPosition = new Vector2(
                Mathf.Lerp(-HueBarWidth * 0.5f, HueBarWidth * 0.5f, hue),
                0f);
        }

        if (squareHandle != null)
        {
            squareHandle.anchoredPosition = new Vector2(
                Mathf.Lerp(-SquareWidth * 0.5f, SquareWidth * 0.5f, saturation),
                Mathf.Lerp(-SquareHeight * 0.5f, SquareHeight * 0.5f, value)
            );
        }
    }
}
