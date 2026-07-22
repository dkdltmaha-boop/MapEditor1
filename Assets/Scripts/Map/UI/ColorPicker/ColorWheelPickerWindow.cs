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
    private const int WheelSize = 180;
    private const int WheelThickness = 26;
    private const int SquareSize = 104;
    private static readonly Vector2 PreferredWindowSize = new Vector2(246f, 620f);
    private const float WindowScreenMargin = 8f;
    private const string WallTileSelectorObjectName = "WallTileSelector";
    private const string WallTilePreviewObjectName = "WallTilePreview";
    private const string ExportCellSizeSelectorObjectName = "ExportCellSizeSelector";
    private const string ExportCellSizeButtonPrefix = "ExportCellSize";

    private MapEditorManager manager;
    private RawImage wheelImage;
    private RawImage squareImage;
    private Image previewImage;
    private Image wallTilePreviewImage;
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

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);

        Vector2 size = PreferredWindowSize;
        Vector2 position = offset;
        RectTransform parentRect = rect.parent as RectTransform;

        if (parentRect != null)
        {
            size.x = Mathf.Min(size.x, Mathf.Max(120f, parentRect.rect.width - WindowScreenMargin * 2f));
            size.y = Mathf.Min(size.y, Mathf.Max(360f, parentRect.rect.height - WindowScreenMargin * 2f));
            position.x = Mathf.Clamp(position.x, WindowScreenMargin, parentRect.rect.width - size.x - WindowScreenMargin);
            position.y = Mathf.Clamp(position.y, -parentRect.rect.height + size.y + WindowScreenMargin, -WindowScreenMargin);
        }

        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    public void Initialize(MapEditorManager manager)
    {
        this.manager = manager;
        pngPaletteView = new ColorWheelPngPaletteView(this, manager);
        RemoveMissingScripts();

        if (transform.Find("HueWheel") == null)
        {
            BuildWindow();
        }
        else
        {
            CacheExistingReferences();
            EnsureWallTileSelector();
            EnsureExportCellSizeSelector();
        }

        isBuilt = true;
        SetColor(manager.selectedColor, false);
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

    public void SetHueFromLocalPoint(Vector2 localPoint)
    {
        float angle = Mathf.Atan2(localPoint.y, localPoint.x);
        hue = Mathf.Repeat(angle / (Mathf.PI * 2f), 1f);
        UpdateSquareTexture();
        UpdatePreview(true);
        UpdateHandles();
    }

    public void SetSaturationValueFromLocalPoint(Vector2 localPoint)
    {
        float x = Mathf.InverseLerp(-SquareSize * 0.5f, SquareSize * 0.5f, localPoint.x);
        float y = Mathf.InverseLerp(-SquareSize * 0.5f, SquareSize * 0.5f, localPoint.y);

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
        CreateWallTileSelector();
        CreateExportCellSizeSelector();
        pngPaletteView.CreateArea(transform);
    }

    private void CacheExistingReferences()
    {
        Transform preview = transform.Find("Preview");
        Transform wallTileSelector = transform.Find(WallTileSelectorObjectName);
        Transform wheel = transform.Find("HueWheel");
        Transform square = transform.Find("SaturationValueSquare");

        if (preview != null)
        {
            previewImage = preview.GetComponent<Image>();
        }

        if (wallTileSelector != null)
        {
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
        rect.sizeDelta = new Vector2(-16f, 24f);

        Text text = titleObject.GetComponent<Text>();
        text.text = "Color";
        text.font = MapEditorFontProvider.Default;
        text.fontSize = 15;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.white;
    }

    private void CreatePreview()
    {
        GameObject previewObject = new GameObject("Preview", typeof(RectTransform), typeof(Image));
        previewObject.transform.SetParent(transform, false);

        RectTransform rect = previewObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-16f, -38f);
        rect.sizeDelta = new Vector2(76f, 28f);

        previewImage = previewObject.GetComponent<Image>();
        previewImage.raycastTarget = false;
    }

    private void CreateWheel()
    {
        GameObject wheelObject = new GameObject("HueWheel", typeof(RectTransform), typeof(RawImage), typeof(ColorWheelInput));
        wheelObject.transform.SetParent(transform, false);

        RectTransform rect = wheelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -142f);
        rect.sizeDelta = new Vector2(WheelSize, WheelSize);

        wheelImage = wheelObject.GetComponent<RawImage>();
        wheelImage.texture = CreateWheelTexture();

        ColorWheelInput input = wheelObject.GetComponent<ColorWheelInput>();
        input.Initialize(this, rect);

        wheelHandle = CreateHandle("HueHandle", wheelObject.transform, 13f);
    }

    private void CreateSquare()
    {
        GameObject squareObject = new GameObject("SaturationValueSquare", typeof(RectTransform), typeof(RawImage), typeof(ColorSquareInput));
        squareObject.transform.SetParent(transform, false);

        RectTransform rect = squareObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -142f);
        rect.sizeDelta = new Vector2(SquareSize, SquareSize);

        squareTexture = new Texture2D(SquareSize, SquareSize, TextureFormat.RGBA32, false);
        squareTexture.wrapMode = TextureWrapMode.Clamp;
        squareImage = squareObject.GetComponent<RawImage>();
        squareImage.texture = squareTexture;

        ColorSquareInput input = squareObject.GetComponent<ColorSquareInput>();
        input.Initialize(this, rect);

        squareHandle = CreateHandle("SquareHandle", squareObject.transform, 10f);
    }

    private void CreateWallTileSelector()
    {
        GameObject selectorObject = new GameObject(WallTileSelectorObjectName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(MapEditorToolbarButton));
        selectorObject.transform.SetParent(transform, false);

        RectTransform rect = selectorObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -270f);
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
        label.text = "Wall Tile";
        label.font = MapEditorFontProvider.Default;
        label.fontSize = 13;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = Color.white;

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
        rect.anchoredPosition = new Vector2(0f, -312f);
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
        label.text = "Dot Size";
        label.font = MapEditorFontProvider.Default;
        label.fontSize = 11;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = Color.white;
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
        text.text = "Tile";
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
        text.text = size + "px";
        text.font = MapEditorFontProvider.Default;
        text.fontSize = 8;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
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
        Texture2D texture = new Texture2D(WheelSize, WheelSize, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        float center = (WheelSize - 1) * 0.5f;
        float outerRadius = WheelSize * 0.5f;
        float innerRadius = outerRadius - WheelThickness;

        for (int y = 0; y < WheelSize; y++)
        {
            for (int x = 0; x < WheelSize; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float radius = Mathf.Sqrt(dx * dx + dy * dy);

                if (radius < innerRadius || radius > outerRadius)
                {
                    texture.SetPixel(x, y, Color.clear);
                    continue;
                }

                float pixelHue = Mathf.Repeat(Mathf.Atan2(dy, dx) / (Mathf.PI * 2f), 1f);
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

        for (int y = 0; y < SquareSize; y++)
        {
            float v = y / (float)(SquareSize - 1);

            for (int x = 0; x < SquareSize; x++)
            {
                float s = x / (float)(SquareSize - 1);
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
    }

    private void UpdateHandles()
    {
        if (wheelHandle != null)
        {
            float radius = (WheelSize * 0.5f) - (WheelThickness * 0.5f);
            float angle = hue * Mathf.PI * 2f;
            wheelHandle.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        if (squareHandle != null)
        {
            squareHandle.anchoredPosition = new Vector2(
                Mathf.Lerp(-SquareSize * 0.5f, SquareSize * 0.5f, saturation),
                Mathf.Lerp(-SquareSize * 0.5f, SquareSize * 0.5f, value)
            );
        }
    }
}
