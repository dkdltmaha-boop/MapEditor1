using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public sealed class MapEditorTilesetLibraryWindow : MonoBehaviour
{
    private const string RootName = "MapEditor_TilesetLibraryWindow";
    private const float PanelWidth = 940f;
    private const float PanelHeight = 580f;

    private readonly List<string> pendingSourcePaths = new List<string>();

    private MapEditorManager manager;
    private InputField collectionNameInput;
    private InputField tileWidthInput;
    private InputField tileHeightInput;
    private InputField marginInput;
    private InputField spacingInput;
    private Text statusText;
    private Text sourceSummaryText;
    private Text defaultLayerText;
    private RectTransform sourceListContent;
    private RectTransform libraryListContent;
    private MapEditorLayerType defaultLayer = MapEditorLayerType.Ground;

    public static MapEditorTilesetLibraryWindow Open(MapEditorManager manager)
    {
        if (manager == null)
        {
            return null;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("타일셋 보관함을 표시할 Canvas가 없습니다.");
            return null;
        }

        Transform existing = canvas.transform.Find(RootName);
        if (existing != null)
        {
            existing.SetAsLastSibling();
            return existing.GetComponent<MapEditorTilesetLibraryWindow>();
        }

        GameObject root = CreateUiObject(
            RootName,
            canvas.transform,
            typeof(Image),
            typeof(CanvasGroup),
            typeof(MapEditorUiTransition),
            typeof(MapEditorTilesetLibraryWindow));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.68f);

        MapEditorTilesetLibraryWindow window = root.GetComponent<MapEditorTilesetLibraryWindow>();
        window.manager = manager;
        RectTransform panel = window.Build();
        root.transform.SetAsLastSibling();
        root.GetComponent<MapEditorUiTransition>().PlayIn(panel);
        return window;
    }

    private RectTransform Build()
    {
        GameObject panelObject = CreateUiObject("Panel", transform, typeof(Image));
        RectTransform panel = panelObject.GetComponent<RectTransform>();
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        panelObject.GetComponent<Image>().color = new Color(0.075f, 0.095f, 0.105f, 0.99f);

        CreateLabel(panel, "Title", L("게임 내 타일셋 보관함", "Runtime Tileset Library"), 20, FontStyle.Bold,
            new Vector2(18f, -12f), new Vector2(700f, 34f), TextAnchor.MiddleLeft);
        CreateLabel(panel, "Subtitle",
            L("여러 PNG를 한 항목으로 묶어 빌드에서도 사용, 이름 변경, 삭제할 수 있습니다.",
                "Combine PNG files into one editable runtime palette item."),
            12, FontStyle.Normal, new Vector2(18f, -42f), new Vector2(700f, 24f), TextAnchor.MiddleLeft).color =
            new Color(0.72f, 0.76f, 0.79f, 1f);
        CreateButton(panel, "CloseButton", "X", new Vector2(-14f, -12f), new Vector2(32f, 30f), Close, true, false, true);

        BuildImportPanel(panel);
        BuildLibraryPanel(panel);

        statusText = CreateLabel(panel, "Status", string.Empty, 12, FontStyle.Bold,
            new Vector2(18f, -548f), new Vector2(900f, 24f), TextAnchor.MiddleLeft);
        RefreshSourceList();
        RefreshLibraryList();
        return panel;
    }

    private void BuildImportPanel(RectTransform panel)
    {
        const float left = 18f;
        const float width = 430f;

        CreateLabel(panel, "ImportTitle", L("새 타일셋 묶음", "New Tileset Collection"), 15, FontStyle.Bold,
            new Vector2(left, -78f), new Vector2(width, 26f), TextAnchor.MiddleLeft);

        collectionNameInput = CreateLabeledInput(panel, "CollectionName", L("이름", "Name"),
            new Vector2(left, -110f), new Vector2(width, 30f), L("예: 마을 타일", "Example: Village"));

        tileWidthInput = CreateCompactInput(panel, "TileWidth", L("타일 너비", "Tile W"), "16", new Vector2(left, -148f), 98f);
        tileHeightInput = CreateCompactInput(panel, "TileHeight", L("높이", "H"), "16", new Vector2(left + 108f, -148f), 86f);
        marginInput = CreateCompactInput(panel, "Margin", L("여백", "Margin"), "0", new Vector2(left + 204f, -148f), 98f);
        spacingInput = CreateCompactInput(panel, "Spacing", L("간격", "Spacing"), "0", new Vector2(left + 312f, -148f), 100f);

        Button layerButton = CreateButton(panel, "DefaultLayerButton", string.Empty,
            new Vector2(left, -186f), new Vector2(152f, 30f), CycleDefaultLayer, false, false, false);
        defaultLayerText = layerButton.GetComponentInChildren<Text>();
        RefreshDefaultLayerLabel();
        CreateButton(panel, "AddPngButton", L("PNG 여러 장 추가", "Add PNG Files"),
            new Vector2(left + 160f, -186f), new Vector2(166f, 30f), AddPngFiles, false, true, false);
        CreateButton(panel, "ClearSourceButton", L("목록 비우기", "Clear List"),
            new Vector2(left + 334f, -186f), new Vector2(96f, 30f), ClearPendingSources);

        sourceSummaryText = CreateLabel(panel, "SourceSummary", string.Empty, 12, FontStyle.Bold,
            new Vector2(left, -224f), new Vector2(width, 24f), TextAnchor.MiddleLeft);
        sourceListContent = CreateScrollView(panel, "SourceList", new Vector2(left, -252f), new Vector2(width, 196f));

        CreateLabel(panel, "ImportHint",
            L("모든 PNG에 위 타일 크기/여백/간격을 동일하게 적용하고, 투명도를 유지해 한 팔레트로 합칩니다.",
                "The same slicing settings are applied to every PNG and transparency is preserved."),
            11, FontStyle.Normal, new Vector2(left, -456f), new Vector2(width, 42f), TextAnchor.UpperLeft).color =
            new Color(0.68f, 0.72f, 0.75f, 1f);
        CreateButton(panel, "ImportCollectionButton", L("선택한 PNG를 한 항목으로 가져오기", "Import as One Collection"),
            new Vector2(left, -506f), new Vector2(width, 38f), ImportCollection, false, true, false);
    }

    private void BuildLibraryPanel(RectTransform panel)
    {
        const float left = 474f;
        const float width = 448f;

        GameObject dividerObject = CreateUiObject("Divider", panel, typeof(Image));
        RectTransform divider = dividerObject.GetComponent<RectTransform>();
        ConfigureTopLeft(divider, new Vector2(460f, -78f), new Vector2(1f, 466f));
        dividerObject.GetComponent<Image>().color = new Color(0.28f, 0.34f, 0.36f, 0.8f);

        CreateLabel(panel, "LibraryTitle", L("저장된 타일셋", "Saved Tilesets"), 15, FontStyle.Bold,
            new Vector2(left, -78f), new Vector2(260f, 26f), TextAnchor.MiddleLeft);
        CreateButton(panel, "RefreshLibraryButton", L("새로고침", "Refresh"),
            new Vector2(left + 346f, -78f), new Vector2(102f, 28f), RefreshLibraryList);
        libraryListContent = CreateScrollView(panel, "LibraryList", new Vector2(left, -112f), new Vector2(width, 432f));
    }

    private void AddPngFiles()
    {
        string[] selected = MapEditorFileDialog.OpenFiles(L("타일셋 PNG 여러 장 선택", "Select Tileset PNG Files"), "png");
        if (selected == null || selected.Length == 0)
        {
            return;
        }

        HashSet<string> existing = new HashSet<string>(pendingSourcePaths, System.StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < selected.Length; i++)
        {
            if (File.Exists(selected[i]) && existing.Add(selected[i]))
            {
                pendingSourcePaths.Add(selected[i]);
            }
        }

        if (pendingSourcePaths.Count > 0 && string.IsNullOrWhiteSpace(collectionNameInput.text))
        {
            collectionNameInput.text = Path.GetFileNameWithoutExtension(pendingSourcePaths[0]);
        }

        RefreshSourceList();
        SetStatus(L("PNG를 목록에 추가했습니다.", "PNG files added."), false);
    }

    private void ClearPendingSources()
    {
        pendingSourcePaths.Clear();
        RefreshSourceList();
    }

    private void RemovePendingSource(int index)
    {
        if (index < 0 || index >= pendingSourcePaths.Count)
        {
            return;
        }

        pendingSourcePaths.RemoveAt(index);
        RefreshSourceList();
    }

    private void ImportCollection()
    {
        if (pendingSourcePaths.Count == 0)
        {
            SetStatus(L("먼저 PNG를 한 장 이상 추가하세요.", "Add at least one PNG first."), true);
            return;
        }

        if (!TryReadPositive(tileWidthInput, out int tileWidth)
            || !TryReadPositive(tileHeightInput, out int tileHeight)
            || !TryReadNonNegative(marginInput, out int margin)
            || !TryReadNonNegative(spacingInput, out int spacing))
        {
            SetStatus(L("타일 크기는 1 이상, 여백과 간격은 0 이상이어야 합니다.",
                "Tile size must be positive; margin and spacing cannot be negative."), true);
            return;
        }

        string displayName = string.IsNullOrWhiteSpace(collectionNameInput.text)
            ? Path.GetFileNameWithoutExtension(pendingSourcePaths[0])
            : collectionNameInput.text.Trim();
        bool collision = defaultLayer == MapEditorLayerType.WallCollision;
        if (!manager.ImportTilesetCollection(
                pendingSourcePaths,
                displayName,
                tileWidth,
                tileHeight,
                margin,
                spacing,
                defaultLayer,
                collision))
        {
            SetStatus(L("가져오기에 실패했습니다. Console에서 원인을 확인하세요.",
                "Import failed. Check the Console for details."), true);
            return;
        }

        SetStatus(string.Format(L("'{0}' 항목을 만들었습니다.", "Created '{0}'."), displayName), false);
        pendingSourcePaths.Clear();
        collectionNameInput.text = string.Empty;
        RefreshSourceList();
        RefreshLibraryList();
    }

    private void CycleDefaultLayer()
    {
        switch (defaultLayer)
        {
            case MapEditorLayerType.Ground:
                defaultLayer = MapEditorLayerType.Object;
                break;
            case MapEditorLayerType.Object:
                defaultLayer = MapEditorLayerType.WallVisual;
                break;
            case MapEditorLayerType.WallVisual:
                defaultLayer = MapEditorLayerType.WallCollision;
                break;
            default:
                defaultLayer = MapEditorLayerType.Ground;
                break;
        }

        RefreshDefaultLayerLabel();
    }

    private void RefreshDefaultLayerLabel()
    {
        if (defaultLayerText == null)
        {
            return;
        }

        string label;
        switch (defaultLayer)
        {
            case MapEditorLayerType.Object:
                label = L("기본: 오브젝트", "Default: Object");
                break;
            case MapEditorLayerType.WallVisual:
                label = L("기본: 벽 모양", "Default: Wall Visual");
                break;
            case MapEditorLayerType.WallCollision:
                label = L("기본: 충돌", "Default: Collision");
                break;
            default:
                label = L("기본: 바닥", "Default: Ground");
                break;
        }

        defaultLayerText.text = label;
    }

    private void RefreshSourceList()
    {
        if (sourceListContent == null)
        {
            return;
        }

        ClearChildren(sourceListContent);
        sourceSummaryText.text = string.Format(L("선택한 PNG: {0}개", "Selected PNG files: {0}"), pendingSourcePaths.Count);
        if (pendingSourcePaths.Count == 0)
        {
            CreateEmptyLabel(sourceListContent, L("PNG 여러 장 추가 버튼으로 파일을 선택하세요.", "Choose PNG files to begin."));
            return;
        }

        const float rowHeight = 32f;
        sourceListContent.sizeDelta = new Vector2(0f, Mathf.Max(196f, pendingSourcePaths.Count * rowHeight + 8f));
        for (int i = 0; i < pendingSourcePaths.Count; i++)
        {
            int capturedIndex = i;
            GameObject row = CreateUiObject("Source_" + i, sourceListContent, typeof(Image));
            RectTransform rect = row.GetComponent<RectTransform>();
            ConfigureTopLeft(rect, new Vector2(4f, -4f - i * rowHeight), new Vector2(414f, 28f));
            row.GetComponent<Image>().color = new Color(0.15f, 0.18f, 0.19f, 1f);
            CreateLabel(row.transform, "Name", Path.GetFileName(pendingSourcePaths[i]), 11, FontStyle.Normal,
                new Vector2(8f, 0f), new Vector2(356f, 28f), TextAnchor.MiddleLeft);
            CreateButton(row.transform, "Remove", "X", new Vector2(-4f, -3f), new Vector2(28f, 22f),
                () => RemovePendingSource(capturedIndex), true, false, true);
        }
    }

    private void RefreshLibraryList()
    {
        if (libraryListContent == null || manager == null)
        {
            return;
        }

        ClearChildren(libraryListContent);
        IReadOnlyList<MapEditorTilesetDefinition> definitions = manager.GetImportedTilesets();
        if (definitions.Count == 0)
        {
            CreateEmptyLabel(libraryListContent, L("저장된 타일셋이 없습니다.", "No saved tilesets."));
            return;
        }

        const float rowHeight = 66f;
        libraryListContent.sizeDelta = new Vector2(0f, Mathf.Max(432f, definitions.Count * rowHeight + 8f));
        for (int i = 0; i < definitions.Count; i++)
        {
            MapEditorTilesetDefinition definition = definitions[i];
            if (definition == null)
            {
                continue;
            }

            string id = definition.id;
            GameObject row = CreateUiObject("Tileset_" + i, libraryListContent, typeof(Image));
            RectTransform rowRect = row.GetComponent<RectTransform>();
            ConfigureTopLeft(rowRect, new Vector2(4f, -4f - i * rowHeight), new Vector2(432f, 60f));
            row.GetComponent<Image>().color = new Color(0.14f, 0.17f, 0.18f, 1f);

            InputField nameInput = CreateInputField(row.transform, "Name", new Vector2(8f, -6f), new Vector2(278f, 26f), definition.displayName);
            nameInput.onEndEdit.AddListener(value => RenameTileset(id, value));
            CreateButton(row.transform, "Use", L("사용", "Use"), new Vector2(292f, -6f), new Vector2(62f, 26f),
                () => UseTileset(id), false, true, false);
            CreateButton(row.transform, "Delete", L("삭제", "Delete"), new Vector2(358f, -6f), new Vector2(66f, 26f),
                () => DeleteTileset(id), false, false, true);

            string info = string.Format(
                L("PNG {0}개 | 타일 {1}개 | {2}x{3}px", "{0} PNG | {1} tiles | {2}x{3}px"),
                definition.SourceCount,
                definition.TileCount,
                definition.tileWidth,
                definition.tileHeight);
            Text infoText = CreateLabel(row.transform, "Info", info, 10, FontStyle.Normal,
                new Vector2(10f, -34f), new Vector2(410f, 20f), TextAnchor.MiddleLeft);
            infoText.color = new Color(0.66f, 0.7f, 0.72f, 1f);
        }
    }

    private void RenameTileset(string id, string value)
    {
        if (!manager.RenameImportedTileset(id, value))
        {
            RefreshLibraryList();
        }
    }

    private void UseTileset(string id)
    {
        manager.UseImportedTileset(id);
        MapEditorTilesetDefinition definition = manager.GetImportedTilesets().FindById(id);
        SetStatus(definition == null
            ? L("타일셋을 선택했습니다.", "Tileset selected.")
            : string.Format(L("'{0}'을 팔레트에 열었습니다.", "Opened '{0}' in the palette."), definition.displayName), false);
    }

    private void DeleteTileset(string id)
    {
        manager.RemoveImportedTileset(id);
        RefreshLibraryList();
        SetStatus(L("타일셋 항목을 삭제했습니다.", "Tileset item deleted."), false);
    }

    private void SetStatus(string message, bool error)
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = message;
        statusText.color = error
            ? new Color(1f, 0.42f, 0.42f, 1f)
            : new Color(0.48f, 0.86f, 0.58f, 1f);
    }

    private void Update()
    {
        if (Application.isPlaying && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
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

    private static bool TryReadPositive(InputField input, out int value)
    {
        return int.TryParse(input?.text, out value) && value > 0;
    }

    private static bool TryReadNonNegative(InputField input, out int value)
    {
        return int.TryParse(input?.text, out value) && value >= 0;
    }

    private static RectTransform CreateScrollView(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject viewportObject = CreateUiObject(name, parent, typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        ConfigureTopLeft(viewport, position, size);
        viewportObject.GetComponent<Image>().color = new Color(0.045f, 0.06f, 0.065f, 1f);

        GameObject contentObject = CreateUiObject("Content", viewportObject.transform);
        RectTransform content = contentObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, size.y);

        ScrollRect scroll = viewportObject.GetComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;
        return content;
    }

    private static void CreateEmptyLabel(Transform parent, string message)
    {
        Text text = CreateLabel(parent, "Empty", message, 12, FontStyle.Normal,
            new Vector2(12f, -12f), new Vector2(390f, 48f), TextAnchor.UpperLeft);
        text.color = new Color(0.58f, 0.62f, 0.64f, 1f);
    }

    private static InputField CreateLabeledInput(
        Transform parent,
        string name,
        string label,
        Vector2 position,
        Vector2 size,
        string placeholder)
    {
        CreateLabel(parent, name + "Label", label, 12, FontStyle.Bold,
            position, new Vector2(62f, size.y), TextAnchor.MiddleLeft);
        return CreateInputField(parent, name + "Input", position + new Vector2(66f, 0f),
            new Vector2(size.x - 66f, size.y), string.Empty, placeholder);
    }

    private static InputField CreateCompactInput(
        Transform parent,
        string name,
        string label,
        string value,
        Vector2 position,
        float width)
    {
        CreateLabel(parent, name + "Label", label, 10, FontStyle.Bold,
            position, new Vector2(width, 16f), TextAnchor.MiddleLeft);
        InputField input = CreateInputField(parent, name + "Input", position + new Vector2(0f, -17f),
            new Vector2(width, 24f), value);
        input.contentType = InputField.ContentType.IntegerNumber;
        return input;
    }

    private static InputField CreateInputField(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 size,
        string value,
        string placeholderValue = "")
    {
        GameObject inputObject = CreateUiObject(name, parent, typeof(Image), typeof(InputField));
        RectTransform rect = inputObject.GetComponent<RectTransform>();
        ConfigureTopLeft(rect, position, size);
        Image background = inputObject.GetComponent<Image>();
        background.color = new Color(0.035f, 0.045f, 0.05f, 1f);

        Text inputText = CreateLabel(inputObject.transform, "Text", string.Empty, 12, FontStyle.Normal,
            Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
        StretchText(inputText.rectTransform, 7f, 7f);
        Text placeholder = CreateLabel(inputObject.transform, "Placeholder", placeholderValue, 11, FontStyle.Italic,
            Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
        StretchText(placeholder.rectTransform, 7f, 7f);
        placeholder.color = new Color(0.52f, 0.56f, 0.58f, 0.8f);

        InputField input = inputObject.GetComponent<InputField>();
        input.targetGraphic = background;
        input.textComponent = inputText;
        input.placeholder = placeholder;
        input.lineType = InputField.LineType.SingleLine;
        input.text = value;
        return input;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 position,
        Vector2 size,
        UnityEngine.Events.UnityAction action,
        bool topRight = false,
        bool accent = false,
        bool danger = false)
    {
        GameObject buttonObject = CreateUiObject(name, parent, typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        if (topRight)
        {
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
        }
        else
        {
            ConfigureTopLeft(rect, position, size);
        }

        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = buttonObject.GetComponent<Image>();
        image.color = danger
            ? new Color(0.68f, 0.17f, 0.19f, 1f)
            : accent
                ? new Color(0.15f, 0.47f, 0.92f, 1f)
                : new Color(0.22f, 0.27f, 0.29f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        Text text = CreateLabel(buttonObject.transform, "Text", label, 11, FontStyle.Bold,
            Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    private static Text CreateLabel(
        Transform parent,
        string name,
        string value,
        int fontSize,
        FontStyle style,
        Vector2 position,
        Vector2 size,
        TextAnchor alignment)
    {
        GameObject labelObject = CreateUiObject(name, parent, typeof(Text));
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        ConfigureTopLeft(rect, position, size);
        Text text = labelObject.GetComponent<Text>();
        text.text = value;
        text.font = MapEditorFontProvider.Default;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static GameObject CreateUiObject(string name, Transform parent, params System.Type[] components)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != typeof(RectTransform))
            {
                gameObject.AddComponent(components[i]);
            }
        }
        return gameObject;
    }

    private static void ConfigureTopLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void StretchText(RectTransform rect, float left, float right)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, 0f);
        rect.offsetMax = new Vector2(-right, 0f);
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            MapEditorObjectUtility.DestroyObject(parent.GetChild(i).gameObject);
        }
    }

    private static string L(string korean, string english)
    {
        return MapEditorLocalization.Current == MapEditorLanguage.Korean ? korean : english;
    }
}

internal static class MapEditorTilesetDefinitionListExtensions
{
    public static MapEditorTilesetDefinition FindById(
        this IReadOnlyList<MapEditorTilesetDefinition> definitions,
        string id)
    {
        if (definitions == null)
        {
            return null;
        }

        for (int i = 0; i < definitions.Count; i++)
        {
            if (definitions[i] != null && definitions[i].id == id)
            {
                return definitions[i];
            }
        }

        return null;
    }
}
