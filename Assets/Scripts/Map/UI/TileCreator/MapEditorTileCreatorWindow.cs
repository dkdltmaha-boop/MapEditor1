using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MapEditorTileCreatorWindow : MonoBehaviour
{
    private const string RootName = "MapEditor_TileCreator";
    private const int TileResolution = 16;
    private const float CanvasSize = 352f;
    private const int ColorSquareWidth = 178;
    private const int ColorSquareHeight = 150;
    private const int HueBarWidth = 178;
    private const int HueBarHeight = 16;
    private const int MaxSavedTileThumbnails = 15;

    private MapEditorManager manager;
    private Texture2D tileTexture;
    private Texture2D colorSquareTexture;
    private Texture2D hueTexture;
    private readonly List<Texture2D> thumbnailTextures = new List<Texture2D>();
    private Text statusText;
    private Image selectedColorPreview;
    private InputField hexInput;
    private Transform savedTileGrid;
    private Color drawingColor;
    private float hue;
    private float saturation;
    private float value;
    private string currentTilePath = string.Empty;
    private bool tileDirty = true;

    public static MapEditorTileCreatorWindow Open(MapEditorManager manager)
    {
        if (manager == null)
        {
            return null;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            return null;
        }

        Transform existing = canvas.transform.Find(RootName);

        if (existing != null)
        {
            existing.SetAsLastSibling();
            return existing.GetComponent<MapEditorTileCreatorWindow>();
        }

        GameObject root = new GameObject(
            RootName,
            typeof(RectTransform),
            typeof(Image),
            typeof(CanvasGroup),
            typeof(MapEditorUiTransition),
            typeof(MapEditorTileCreatorWindow));
        root.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.58f);

        MapEditorTileCreatorWindow window = root.GetComponent<MapEditorTileCreatorWindow>();
        window.manager = manager;
        window.Build();
        root.transform.SetAsLastSibling();
        root.GetComponent<MapEditorUiTransition>().PlayIn(window.GetPanelRect());
        return window;
    }

    private RectTransform GetPanelRect()
    {
        Transform panel = transform.Find("Panel");
        return panel == null ? transform as RectTransform : panel as RectTransform;
    }

    private void Build()
    {
        GameObject panel = CreateUiObject("Panel", transform, typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(400f, 464f);
        panel.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.15f, 0.99f);

        CreateLabel(panel.transform, "Title", "16x16 타일 만들기", new Vector2(16f, -12f), new Vector2(300f, 28f), 18, FontStyle.Bold, TextAnchor.MiddleLeft);
        CreateButton(panel.transform, "CloseButton", "X", new Vector2(-14f, -12f), new Vector2(30f, 28f), Close, true);
        BuildColorPanel(panel.transform);
        BuildSavedTilePanel(panel.transform);

        GameObject canvasFrame = CreateUiObject("TileCanvasFrame", panel.transform, typeof(Image));
        RectTransform frameRect = canvasFrame.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0.5f, 1f);
        frameRect.anchorMax = new Vector2(0.5f, 1f);
        frameRect.pivot = new Vector2(0.5f, 1f);
        frameRect.anchoredPosition = new Vector2(0f, -52f);
        frameRect.sizeDelta = new Vector2(CanvasSize, CanvasSize);
        canvasFrame.GetComponent<Image>().color = new Color(0.06f, 0.07f, 0.08f, 1f);

        tileTexture = new Texture2D(TileResolution, TileResolution, TextureFormat.RGBA32, false)
        {
            name = "CreatedTile16x16",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        ClearTexture();

        GameObject drawing = CreateUiObject("Drawing", canvasFrame.transform, typeof(RawImage), typeof(MapEditorTileCanvasInput));
        RectTransform drawingRect = drawing.GetComponent<RectTransform>();
        drawingRect.anchorMin = Vector2.zero;
        drawingRect.anchorMax = Vector2.one;
        drawingRect.offsetMin = Vector2.zero;
        drawingRect.offsetMax = Vector2.zero;
        RawImage tileImage = drawing.GetComponent<RawImage>();
        tileImage.texture = tileTexture;
        tileImage.color = Color.white;
        drawing.GetComponent<MapEditorTileCanvasInput>().Initialize(this);
        CreateGridOverlay(canvasFrame.transform);

        statusText = CreateLabel(
            panel.transform,
            "Status",
            "좌클릭: 그리기   우클릭: 지우기",
            new Vector2(24f, -410f),
            new Vector2(352f, 18f),
            11,
            FontStyle.Normal,
            TextAnchor.MiddleLeft);

        CreateButton(panel.transform, "ClearButton", "전체 지우기", new Vector2(16f, 14f), new Vector2(88f, 28f), ClearTexture);
        CreateButton(panel.transform, "SaveTileButton", "타일 저장", new Vector2(108f, 14f), new Vector2(88f, 28f), SaveCurrentTile);
        CreateButton(panel.transform, "UseButton", "브러시로 사용", new Vector2(200f, 14f), new Vector2(112f, 28f), UseAsBrush);
        CreateButton(panel.transform, "DoneButton", "닫기", new Vector2(316f, 14f), new Vector2(68f, 28f), Close);
    }

    public void PaintAt(Vector2 normalizedPosition, bool erase)
    {
        if (tileTexture == null)
        {
            return;
        }

        int x = Mathf.Clamp(Mathf.FloorToInt(normalizedPosition.x * TileResolution), 0, TileResolution - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(normalizedPosition.y * TileResolution), 0, TileResolution - 1);
        Color color = erase ? Color.clear : drawingColor;

        if (tileTexture.GetPixel(x, y) == color)
        {
            return;
        }

        tileTexture.SetPixel(x, y, color);
        tileTexture.Apply(false, false);
        tileDirty = true;
        statusText.text = erase
            ? "지운 위치: " + x + ", " + (TileResolution - 1 - y)
            : "그린 위치: " + x + ", " + (TileResolution - 1 - y);
    }

    private void ClearTexture()
    {
        if (tileTexture == null)
        {
            return;
        }

        Color[] pixels = new Color[TileResolution * TileResolution];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        tileTexture.SetPixels(pixels);
        tileTexture.Apply(false, false);
        currentTilePath = string.Empty;
        tileDirty = true;

        if (statusText != null)
        {
            statusText.text = "캔버스를 비웠습니다.";
        }
    }

    private void UseAsBrush()
    {
        string path = SaveCurrentTileToDisk();

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        Sprite preview = Sprite.Create(
            tileTexture,
            new Rect(0f, 0f, TileResolution, TileResolution),
            new Vector2(0.5f, 0.5f),
            TileResolution);
        preview.name = Path.GetFileNameWithoutExtension(path);
        manager.SelectImageBrush(preview, path, MapEditorPngTilesetService.FullImageTileIndex);

        if (manager.selectedImageBrush != preview)
        {
            MapEditorObjectUtility.DestroyObject(preview);
        }
        else
        {
            tileTexture = null;
        }

        Close();
    }

    private void SaveCurrentTile()
    {
        string path = SaveCurrentTileToDisk();

        if (!string.IsNullOrEmpty(path))
        {
            statusText.text = "내 타일에 저장했습니다: " + Path.GetFileNameWithoutExtension(path);
            RefreshSavedTiles();
        }
    }

    private string SaveCurrentTileToDisk()
    {
        if (!HasVisiblePixel())
        {
            statusText.text = "한 픽셀 이상 그린 뒤 저장해 주세요.";
            return string.Empty;
        }

        string directory = GetCreatedTileDirectory();
        Directory.CreateDirectory(directory);
        currentTilePath = string.IsNullOrEmpty(currentTilePath)
            ? Path.Combine(directory, "tile_" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + ".png")
            : currentTilePath;

        if (tileDirty || !File.Exists(currentTilePath))
        {
            File.WriteAllBytes(currentTilePath, tileTexture.EncodeToPNG());
            tileDirty = false;
        }

        return currentTilePath;
    }

    private bool HasVisiblePixel()
    {
        Color32[] pixels = tileTexture.GetPixels32();

        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a > 0)
            {
                return true;
            }
        }

        return false;
    }

    private void BuildColorPanel(Transform parent)
    {
        GameObject panel = CreateSidePanel(parent, "ColorPanel", new Vector2(-315f, 0f));
        CreateLabel(panel.transform, "ColorTitle", "그리기 색상", new Vector2(12f, -10f), new Vector2(186f, 24f), 14, FontStyle.Bold, TextAnchor.MiddleLeft);

        drawingColor = manager == null ? Color.red : manager.selectedColor;
        Color.RGBToHSV(drawingColor, out hue, out saturation, out value);

        GameObject squareObject = CreateUiObject("ColorSquare", panel.transform, typeof(RawImage), typeof(MapEditorTileColorInput));
        RectTransform squareRect = squareObject.GetComponent<RectTransform>();
        ConfigureTopLeftRect(squareRect, new Vector2(16f, -42f), new Vector2(ColorSquareWidth, ColorSquareHeight));
        colorSquareTexture = new Texture2D(ColorSquareWidth, ColorSquareHeight, TextureFormat.RGBA32, false)
        {
            name = "TileCreatorColorSquare",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        squareObject.GetComponent<RawImage>().texture = colorSquareTexture;
        squareObject.GetComponent<MapEditorTileColorInput>().Initialize(this, false);

        GameObject hueObject = CreateUiObject("HueBar", panel.transform, typeof(RawImage), typeof(MapEditorTileColorInput));
        RectTransform hueRect = hueObject.GetComponent<RectTransform>();
        ConfigureTopLeftRect(hueRect, new Vector2(16f, -202f), new Vector2(HueBarWidth, HueBarHeight));
        hueTexture = CreateHueTexture();
        hueObject.GetComponent<RawImage>().texture = hueTexture;
        hueObject.GetComponent<MapEditorTileColorInput>().Initialize(this, true);

        GameObject previewObject = CreateUiObject("SelectedColor", panel.transform, typeof(Image));
        RectTransform previewRect = previewObject.GetComponent<RectTransform>();
        ConfigureTopLeftRect(previewRect, new Vector2(16f, -230f), new Vector2(38f, 28f));
        selectedColorPreview = previewObject.GetComponent<Image>();

        CreateLabel(panel.transform, "HexLabel", "HEX", new Vector2(62f, -230f), new Vector2(34f, 28f), 10, FontStyle.Bold, TextAnchor.MiddleLeft);
        hexInput = CreateInputField(panel.transform, "HexInput", new Vector2(96f, -230f), new Vector2(98f, 28f));
        hexInput.characterLimit = 9;
        hexInput.onEndEdit.AddListener(ApplyHexColor);

        CreateLabel(
            panel.transform,
            "ColorHelp",
            "색상 영역과 아래 막대를\n드래그해 색을 바꿀 수 있습니다.",
            new Vector2(16f, -270f),
            new Vector2(178f, 42f),
            10,
            FontStyle.Normal,
            TextAnchor.UpperLeft);

        RefreshColorUi(true);
    }

    private void BuildSavedTilePanel(Transform parent)
    {
        GameObject panel = CreateSidePanel(parent, "SavedTilePanel", new Vector2(315f, 0f));
        CreateLabel(panel.transform, "SavedTitle", "내 타일", new Vector2(12f, -10f), new Vector2(186f, 24f), 14, FontStyle.Bold, TextAnchor.MiddleLeft);
        CreateLabel(panel.transform, "SavedHelp", "클릭하면 다시 편집", new Vector2(12f, -34f), new Vector2(186f, 18f), 9, FontStyle.Normal, TextAnchor.MiddleLeft);

        GameObject gridObject = CreateUiObject("SavedTileGrid", panel.transform, typeof(GridLayoutGroup));
        RectTransform gridRect = gridObject.GetComponent<RectTransform>();
        ConfigureTopLeftRect(gridRect, new Vector2(14f, -60f), new Vector2(182f, 306f));

        GridLayoutGroup grid = gridObject.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(54f, 54f);
        grid.spacing = new Vector2(8f, 8f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.UpperLeft;
        savedTileGrid = gridObject.transform;

        CreateButton(panel.transform, "DeleteSavedTileButton", "선택 타일 삭제", new Vector2(14f, 12f), new Vector2(182f, 28f), DeleteSelectedTile);
        RefreshSavedTiles();
    }

    public void SetColorFromPicker(Vector2 normalizedPosition, bool hueBar)
    {
        if (hueBar)
        {
            hue = Mathf.Clamp01(normalizedPosition.x);
            RefreshColorUi(true);
            return;
        }

        saturation = Mathf.Clamp01(normalizedPosition.x);
        value = Mathf.Clamp01(normalizedPosition.y);
        RefreshColorUi(false);
    }

    private void ApplyHexColor(string valueText)
    {
        string normalized = string.IsNullOrWhiteSpace(valueText) ? string.Empty : valueText.Trim();

        if (!normalized.StartsWith("#", StringComparison.Ordinal))
        {
            normalized = "#" + normalized;
        }

        if (!ColorUtility.TryParseHtmlString(normalized, out Color parsed))
        {
            statusText.text = "HEX 색상 형식을 확인해 주세요.";
            RefreshColorUi(false);
            return;
        }

        parsed.a = 1f;
        drawingColor = parsed;
        Color.RGBToHSV(drawingColor, out hue, out saturation, out value);
        RefreshColorUi(true);
    }

    private void RefreshColorUi(bool refreshSquare)
    {
        drawingColor = Color.HSVToRGB(hue, saturation, value);

        if (refreshSquare && colorSquareTexture != null)
        {
            for (int y = 0; y < ColorSquareHeight; y++)
            {
                float pixelValue = y / (float)(ColorSquareHeight - 1);

                for (int x = 0; x < ColorSquareWidth; x++)
                {
                    float pixelSaturation = x / (float)(ColorSquareWidth - 1);
                    colorSquareTexture.SetPixel(x, y, Color.HSVToRGB(hue, pixelSaturation, pixelValue));
                }
            }

            colorSquareTexture.Apply(false, false);
        }

        if (selectedColorPreview != null)
        {
            selectedColorPreview.color = drawingColor;
        }

        if (hexInput != null)
        {
            hexInput.SetTextWithoutNotify("#" + ColorUtility.ToHtmlStringRGB(drawingColor));
        }
    }

    private Texture2D CreateHueTexture()
    {
        Texture2D texture = new Texture2D(HueBarWidth, HueBarHeight, TextureFormat.RGBA32, false)
        {
            name = "TileCreatorHueBar",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int y = 0; y < HueBarHeight; y++)
        {
            for (int x = 0; x < HueBarWidth; x++)
            {
                texture.SetPixel(x, y, Color.HSVToRGB(x / (float)(HueBarWidth - 1), 1f, 1f));
            }
        }

        texture.Apply(false, false);
        return texture;
    }

    private void RefreshSavedTiles()
    {
        if (savedTileGrid == null)
        {
            return;
        }

        for (int i = savedTileGrid.childCount - 1; i >= 0; i--)
        {
            MapEditorObjectUtility.DestroyObject(savedTileGrid.GetChild(i).gameObject);
        }

        for (int i = 0; i < thumbnailTextures.Count; i++)
        {
            MapEditorObjectUtility.DestroyObject(thumbnailTextures[i]);
        }

        thumbnailTextures.Clear();
        string directory = GetCreatedTileDirectory();

        if (!Directory.Exists(directory))
        {
            return;
        }

        string[] paths = Directory.GetFiles(directory, "tile_*.png");
        Array.Sort(paths, (left, right) => File.GetLastWriteTimeUtc(right).CompareTo(File.GetLastWriteTimeUtc(left)));
        int count = Mathf.Min(paths.Length, MaxSavedTileThumbnails);

        for (int i = 0; i < count; i++)
        {
            string path = paths[i];
            Texture2D thumbnail = LoadTileTexture(path);

            if (thumbnail == null)
            {
                continue;
            }

            thumbnailTextures.Add(thumbnail);
            GameObject buttonObject = CreateUiObject("SavedTile_" + i, savedTileGrid, typeof(Image), typeof(Button));
            Image background = buttonObject.GetComponent<Image>();
            bool selected = string.Equals(path, currentTilePath, StringComparison.OrdinalIgnoreCase);
            background.color = selected
                ? new Color(0.18f, 0.48f, 0.95f, 1f)
                : new Color(0.08f, 0.09f, 0.1f, 1f);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;

            GameObject imageObject = CreateUiObject("Preview", buttonObject.transform, typeof(RawImage));
            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = new Vector2(3f, 3f);
            imageRect.offsetMax = new Vector2(-3f, -3f);
            RawImage image = imageObject.GetComponent<RawImage>();
            image.texture = thumbnail;
            image.raycastTarget = false;

            string capturedPath = path;
            button.onClick.AddListener(() => LoadSavedTile(capturedPath));
        }
    }

    private void LoadSavedTile(string path)
    {
        Texture2D loaded = LoadTileTexture(path);

        if (loaded == null)
        {
            statusText.text = "저장된 타일을 불러올 수 없습니다.";
            return;
        }

        if (loaded.width != TileResolution || loaded.height != TileResolution)
        {
            MapEditorObjectUtility.DestroyObject(loaded);
            statusText.text = "16x16 타일만 다시 편집할 수 있습니다.";
            return;
        }

        tileTexture.SetPixels32(loaded.GetPixels32());
        tileTexture.Apply(false, false);
        MapEditorObjectUtility.DestroyObject(loaded);
        currentTilePath = path;
        tileDirty = false;
        RefreshSavedTiles();
        statusText.text = "저장된 타일을 불러왔습니다.";
    }

    private void DeleteSelectedTile()
    {
        if (string.IsNullOrEmpty(currentTilePath))
        {
            statusText.text = "삭제할 타일을 먼저 선택해 주세요.";
            return;
        }

        string pathToDelete = currentTilePath;

        try
        {
            if (File.Exists(pathToDelete))
            {
                File.Delete(pathToDelete);
            }

            ClearTexture();
            RefreshSavedTiles();
            statusText.text = "타일을 삭제했습니다: " + Path.GetFileNameWithoutExtension(pathToDelete);
        }
        catch (Exception exception)
        {
            statusText.text = "타일을 삭제하지 못했습니다.";
            Debug.LogWarning("저장 타일 삭제 실패: " + exception.Message);
        }
    }

    private static Texture2D LoadTileTexture(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return null;
        }

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        if (!texture.LoadImage(File.ReadAllBytes(path)))
        {
            MapEditorObjectUtility.DestroyObject(texture);
            return null;
        }

        return texture;
    }

    private static string GetCreatedTileDirectory()
    {
        return Path.Combine(Application.persistentDataPath, "MapEditor", "CreatedTiles");
    }

    private void Close()
    {
        MapEditorUiTransition transition = GetComponent<MapEditorUiTransition>();

        if (transition != null)
        {
            transition.Close();
        }
        else
        {
            MapEditorObjectUtility.DestroyObject(gameObject);
        }
    }

    private void Update()
    {
        if (Application.isPlaying && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    private void OnDestroy()
    {
        if (tileTexture != null)
        {
            MapEditorObjectUtility.DestroyObject(tileTexture);
        }

        MapEditorObjectUtility.DestroyObject(colorSquareTexture);
        MapEditorObjectUtility.DestroyObject(hueTexture);

        for (int i = 0; i < thumbnailTextures.Count; i++)
        {
            MapEditorObjectUtility.DestroyObject(thumbnailTextures[i]);
        }

        thumbnailTextures.Clear();
    }

    private static GameObject CreateSidePanel(Transform parent, string name, Vector2 position)
    {
        GameObject panel = CreateUiObject(name, parent, typeof(Image));
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(210f, 420f);
        panel.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.13f, 0.99f);
        return panel;
    }

    private static InputField CreateInputField(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject inputObject = CreateUiObject(name, parent, typeof(Image), typeof(InputField));
        RectTransform rect = inputObject.GetComponent<RectTransform>();
        ConfigureTopLeftRect(rect, position, size);

        Image background = inputObject.GetComponent<Image>();
        background.color = new Color(0.05f, 0.06f, 0.07f, 1f);

        Text inputText = CreateLabel(inputObject.transform, "Text", string.Empty, Vector2.zero, size, 10, FontStyle.Normal, TextAnchor.MiddleLeft);
        RectTransform textRect = inputText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(5f, 0f);
        textRect.offsetMax = new Vector2(-5f, 0f);

        InputField input = inputObject.GetComponent<InputField>();
        input.targetGraphic = background;
        input.textComponent = inputText;
        input.lineType = InputField.LineType.SingleLine;
        return input;
    }

    private static void ConfigureTopLeftRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void CreateGridOverlay(Transform parent)
    {
        Color lineColor = new Color(0f, 0f, 0f, 0.34f);
        float step = CanvasSize / TileResolution;

        for (int i = 0; i <= TileResolution; i++)
        {
            float position = -CanvasSize * 0.5f + i * step;
            CreateGridLine(parent, "GridV_" + i, new Vector2(position, 0f), new Vector2(1f, CanvasSize), lineColor);
            CreateGridLine(parent, "GridH_" + i, new Vector2(0f, position), new Vector2(CanvasSize, 1f), lineColor);
        }
    }

    private static void CreateGridLine(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        GameObject line = CreateUiObject(name, parent, typeof(Image));
        RectTransform rect = line.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = line.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private static Text CreateLabel(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, FontStyle style, TextAnchor alignment)
    {
        GameObject labelObject = CreateUiObject(name, parent, typeof(Text));
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text text = labelObject.GetComponent<Text>();
        text.text = value;
        text.font = MapEditorFontProvider.Default;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static void CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action, bool topRight = false)
    {
        GameObject buttonObject = CreateUiObject(name, parent, typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = topRight ? Vector2.one : Vector2.zero;
        rect.anchorMax = rect.anchorMin;
        rect.pivot = topRight ? Vector2.one : Vector2.zero;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = name == "UseButton"
            ? new Color(0.18f, 0.48f, 0.95f, 1f)
            : new Color(0.24f, 0.25f, 0.27f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        Text text = CreateLabel(buttonObject.transform, "Text", label, Vector2.zero, size, 11, FontStyle.Bold, TextAnchor.MiddleCenter);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = Vector2.zero;
    }

    private static GameObject CreateUiObject(string name, Transform parent, params Type[] components)
    {
        Type[] allComponents = new Type[components.Length + 1];
        allComponents[0] = typeof(RectTransform);
        Array.Copy(components, 0, allComponents, 1, components.Length);
        GameObject result = new GameObject(name, allComponents);
        result.transform.SetParent(parent, false);
        return result;
    }
}

public sealed class MapEditorTileCanvasInput : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    private MapEditorTileCreatorWindow owner;
    private RectTransform rect;

    public void Initialize(MapEditorTileCreatorWindow window)
    {
        owner = window;
        rect = transform as RectTransform;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Paint(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Paint(eventData);
    }

    private void Paint(PointerEventData eventData)
    {
        if (owner == null || rect == null)
        {
            return;
        }

        bool erase = eventData.button == PointerEventData.InputButton.Right;

        if (!erase && eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out Vector2 local))
        {
            return;
        }

        Rect bounds = rect.rect;
        Vector2 normalized = new Vector2(
            Mathf.InverseLerp(bounds.xMin, bounds.xMax, local.x),
            Mathf.InverseLerp(bounds.yMin, bounds.yMax, local.y));
        owner.PaintAt(normalized, erase);
    }
}

public sealed class MapEditorTileColorInput : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    private MapEditorTileCreatorWindow owner;
    private RectTransform rect;
    private bool hueBar;

    public void Initialize(MapEditorTileCreatorWindow window, bool controlsHue)
    {
        owner = window;
        hueBar = controlsHue;
        rect = transform as RectTransform;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Apply(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Apply(eventData);
    }

    private void Apply(PointerEventData eventData)
    {
        if (owner == null
            || rect == null
            || eventData.button != PointerEventData.InputButton.Left
            || !RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out Vector2 local))
        {
            return;
        }

        Rect bounds = rect.rect;
        owner.SetColorFromPicker(
            new Vector2(
                Mathf.InverseLerp(bounds.xMin, bounds.xMax, local.x),
                Mathf.InverseLerp(bounds.yMin, bounds.yMax, local.y)),
            hueBar);
    }
}
