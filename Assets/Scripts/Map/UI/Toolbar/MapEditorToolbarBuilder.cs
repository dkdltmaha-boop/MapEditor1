using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public struct MapEditorToolbarRefs
{
    public Image brushPreviewImage;
    public Text brushPreviewText;
    public Text validationStatusText;
    public Transform recentPngListRoot;
    public Dictionary<EditorToolType, Image> toolButtonImages;
}

public static class MapEditorToolbarBuilder
{
    private const string ToolbarObjectName = "MapEditor_Toolbar";
    private const string LegacyToolbarObjectName = "ToolToolbar";
    private const string LegacyToolPanelObjectName = "ToolPanel";
    private const string DeprecatedToolPanelObjectName = "Deprecated_ToolPanel_RemoveOnReload";
    private const string BrushPreviewObjectName = "Toolbar_BrushPreview";
    private const string LegacyBrushPreviewObjectName = "BrushPreview";
    private const string BrushPreviewImageObjectName = "BrushPreview_Image";
    private const string LegacyBrushPreviewImageObjectName = "PreviewImage";
    private const string BrushPreviewTextObjectName = "BrushPreview_Label";
    private const string LegacyBrushPreviewTextObjectName = "PreviewText";
    private const string ValidationStatusObjectName = "Toolbar_ValidationStatus";
    private const string RecentPngListObjectName = "Toolbar_RecentPngList";
    private const string LegacyRecentPngListObjectName = "RecentTilesets";
    private const float ToolbarWidth = 176f;
    private const float ToolbarMinHeight = 584f;
    private const float ToolbarMaxMargin = 8f;
    private const float ToolbarHintHeight = 13f;
    private const float ToolbarLabelHeight = 15f;
    private const float ToolbarPreviewHeight = 22f;
    private const float ToolbarValidationHeight = 26f;
    private const float ToolbarRecentHeight = 34f;
    private const int ToolbarHintFontSize = 9;
    private const int ToolbarLabelFontSize = 12;

    public static MapEditorToolbarRefs Ensure(MapEditorManager manager, Vector2 offset, IReadOnlyList<string> recentPngPaths)
    {
        MapEditorToolbarRefs refs = new MapEditorToolbarRefs
        {
            toolButtonImages = new Dictionary<EditorToolType, Image>()
        };

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            return refs;
        }

        Transform toolbar = FindExistingToolbar(canvas.transform);

        if (toolbar == null)
        {
            toolbar = CreateToolToolbar(canvas.transform);
        }

        MapEditorObjectUtility.RemoveDuplicateManagedRoots(
            canvas.transform,
            toolbar,
            ToolbarObjectName,
            LegacyToolbarObjectName,
            LegacyToolPanelObjectName,
            DeprecatedToolPanelObjectName
        );
        toolbar.gameObject.SetActive(true);
        toolbar.SetAsLastSibling();
        ConfigureToolToolbar(toolbar, offset);
        MapEditorMapSizePanelBuilder.Ensure(canvas.transform, manager, offset);
        EnsureToolbarContents(toolbar, manager);
        CacheToolbarRefs(toolbar, refs);
        RefreshRecentPngList(refs.recentPngListRoot, manager, recentPngPaths);
        return refs;
    }

    public static void RefreshRecentPngList(Transform recentPngListRoot, MapEditorManager manager, IReadOnlyList<string> paths)
    {
        if (recentPngListRoot == null)
        {
            return;
        }

        for (int i = recentPngListRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = recentPngListRoot.GetChild(i);

            if (child.name != "최근 PNG")
            {
                MapEditorObjectUtility.DestroyObject(child.gameObject);
            }
        }

        if (paths == null)
        {
            return;
        }

        foreach (string path in paths)
        {
            MapEditorToolbarButtonFactory.CreateRecentPngButton(recentPngListRoot, manager, path);
        }
    }

    private static Transform FindExistingToolbar(Transform canvas)
    {
        Transform toolToolbar = canvas.Find(ToolbarObjectName);

        if (toolToolbar != null)
        {
            return toolToolbar;
        }

        toolToolbar = canvas.Find(LegacyToolbarObjectName);

        if (toolToolbar != null)
        {
            toolToolbar.name = ToolbarObjectName;
            return toolToolbar;
        }

        Transform toolPanel = canvas.Find(LegacyToolPanelObjectName);

        if (toolPanel == null)
        {
            toolPanel = canvas.Find(DeprecatedToolPanelObjectName);
        }

        if (toolPanel != null)
        {
            toolPanel.name = ToolbarObjectName;
        }

        return toolPanel;
    }

    private static Transform CreateToolToolbar(Transform parent)
    {
        GameObject panelObject = new GameObject(ToolbarObjectName, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        panelObject.transform.SetParent(parent, false);

        Image background = panelObject.GetComponent<Image>();
        background.color = new Color(0.13f, 0.13f, 0.13f, 0.92f);
        background.raycastTarget = false;

        VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(4, 4, 4, 4);
        layout.spacing = 1f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return panelObject.transform;
    }

    private static void ConfigureToolToolbar(Transform toolbar, Vector2 offset)
    {
        RectTransform rect = toolbar.GetComponent<RectTransform>();

        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        RectTransform parentRect = toolbar.parent as RectTransform;
        float availableHeight = ToolbarMinHeight;
        Vector2 position = offset;

        if (parentRect != null && parentRect.rect.height > ToolbarMaxMargin * 2f)
        {
            availableHeight = Mathf.Min(ToolbarMinHeight, parentRect.rect.height - ToolbarMaxMargin * 2f);
            position.x = Mathf.Clamp(position.x, -parentRect.rect.width + ToolbarWidth + ToolbarMaxMargin, -ToolbarMaxMargin);
            position.y = Mathf.Clamp(position.y, -parentRect.rect.height + availableHeight + ToolbarMaxMargin, -ToolbarMaxMargin);
        }

        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(ToolbarWidth, availableHeight);

        VerticalLayoutGroup layout = toolbar.GetComponent<VerticalLayoutGroup>();

        if (layout != null)
        {
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.spacing = 1f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }
    }

    private static void EnsureToolbarContents(Transform toolbar, MapEditorManager manager)
    {
        ClearToolbarContents(toolbar);
        CreateToolbarLabel(toolbar, "도구");
        EnsureToolbarToolButton(toolbar, manager, "브러시", "B", EditorToolType.Brush, MapEditorToolbarAction.Brush, "BrushToolButton");
        EnsureToolbarToolButton(toolbar, manager, "벽", "W", EditorToolType.Wall, MapEditorToolbarAction.Wall, "WallToolButton");
        EnsureToolbarToolButton(toolbar, manager, "레이어 지우개", "E", EditorToolType.Eraser, MapEditorToolbarAction.Eraser, "EraseLayerToolButton");
        EnsureToolbarToolButton(toolbar, manager, "선택", "S", EditorToolType.Selection, MapEditorToolbarAction.Select, "SelectToolButton");
        EnsureToolbarActionButton(toolbar, manager, "회전", "R", MapEditorToolbarAction.Rotate, "RotateButton");
        EnsureToolbarActionButton(toolbar, manager, "좌우 반전", "H", MapEditorToolbarAction.FlipH, "FlipHButton");
        EnsureToolbarActionButton(toolbar, manager, "상하 반전", "V", MapEditorToolbarAction.FlipV, "FlipVButton");
        EnsureToolbarShortcutHint(toolbar, "영역 채우기", "Shift+클릭");
        EnsureToolbarActionButton(toolbar, manager, "복사", "Ctrl+C", MapEditorToolbarAction.Copy, "CopyButton");
        EnsureToolbarActionButton(toolbar, manager, "잘라내기", "Ctrl+X", MapEditorToolbarAction.Cut, "CutButton");
        EnsureToolbarActionButton(toolbar, manager, "붙여넣기", "Ctrl+V", MapEditorToolbarAction.Paste, "PasteButton");
        EnsureToolbarActionButton(toolbar, manager, "시작 위치", "클릭", MapEditorToolbarAction.SetSpawn, "SpawnButton");
        EnsureToolbarShortcutHint(toolbar, "선택 지우기", "Esc/Del");
        EnsureToolbarActionButton(toolbar, manager, "스포이드", "I/Space", MapEditorToolbarAction.Eyedropper, "EyedropButton");
        EnsureToolbarDivider(toolbar);
        EnsureToolbarActionButton(toolbar, manager, "실행 취소", "Ctrl+Z", MapEditorToolbarAction.Undo, "UndoButton");
        EnsureToolbarActionButton(toolbar, manager, "다시 실행", "Ctrl+Y", MapEditorToolbarAction.Redo, "RedoButton");
        EnsureToolbarActionButton(toolbar, manager, "편집 저장", "Ctrl+S", MapEditorToolbarAction.Save, "SaveEditButton");
        EnsureToolbarActionButton(toolbar, manager, "편집 불러오기", "Ctrl+L", MapEditorToolbarAction.Load, "LoadEditButton");
        EnsureToolbarActionButton(toolbar, manager, "게임 맵 가져오기", "클릭", MapEditorToolbarAction.ImportPixelChromaMap, "ImportMapButton");
        EnsureToolbarActionButton(toolbar, manager, "타일셋", "클릭", MapEditorToolbarAction.OpenTilesetLibrary, "TilesetsButton");
        EnsureToolbarActionButton(toolbar, manager, "PNG 불러오기", "클릭", MapEditorToolbarAction.PngLoad, "LoadPNGButton");
        EnsureToolbarActionButton(toolbar, manager, "PNG 붙여넣기", "Ctrl+P", MapEditorToolbarAction.PastePng, "PastePNGButton");
        EnsureToolbarActionButton(toolbar, manager, "PNG 내보내기", "클릭", MapEditorToolbarAction.ExportPng, "PNGOutButton");
        EnsureToolbarActionButton(toolbar, manager, "맵 검사", "클릭", MapEditorToolbarAction.ValidateMap, "ValidateButton");
        EnsureToolbarActionButton(toolbar, manager, "게임용 내보내기", "클릭", MapEditorToolbarAction.ExportPixelChroma, "GameOutButton");
        EnsureToolbarActionButton(toolbar, manager, "창작마당 내보내기", "클릭", MapEditorToolbarAction.ExportWorkshop, "WorkshopButton");
        EnsureToolbarActionButton(toolbar, manager, "전체 지우기", "클릭", MapEditorToolbarAction.Clear, "ClearButton");
        EnsureBrushPreview(toolbar);
        EnsureValidationStatus(toolbar);
        EnsureRecentPngList(toolbar);
    }

    private static void ClearToolbarContents(Transform toolbar)
    {
        for (int i = toolbar.childCount - 1; i >= 0; i--)
        {
            Transform child = toolbar.GetChild(i);
            child.name = "Destroyed_" + child.name;
            child.SetParent(null, false);
            MapEditorObjectUtility.DestroyObject(child.gameObject);
        }
    }

    private static void CacheToolbarRefs(Transform toolbar, MapEditorToolbarRefs refs)
    {
        MapEditorToolbarButtonFactory.CacheToolButton(toolbar, refs.toolButtonImages, "BrushButton", EditorToolType.Brush);
        MapEditorToolbarButtonFactory.CacheToolButton(toolbar, refs.toolButtonImages, "EraserButton", EditorToolType.Eraser);
        MapEditorToolbarButtonFactory.CacheToolButton(toolbar, refs.toolButtonImages, "EraseLayerButton", EditorToolType.Eraser);
        MapEditorToolbarButtonFactory.CacheToolButton(toolbar, refs.toolButtonImages, "BrushToolButton", EditorToolType.Brush);
        MapEditorToolbarButtonFactory.CacheToolButton(toolbar, refs.toolButtonImages, "EraserToolButton", EditorToolType.Eraser);
        MapEditorToolbarButtonFactory.CacheToolButton(toolbar, refs.toolButtonImages, "EraseLayerToolButton", EditorToolType.Eraser);
        MapEditorToolbarButtonFactory.CacheToolButton(toolbar, refs.toolButtonImages, "WallButton", EditorToolType.Wall);
        MapEditorToolbarButtonFactory.CacheToolButton(toolbar, refs.toolButtonImages, "WallToolButton", EditorToolType.Wall);
        MapEditorToolbarButtonFactory.CacheToolButton(toolbar, refs.toolButtonImages, "SelectButton", EditorToolType.Selection);
        MapEditorToolbarButtonFactory.CacheToolButton(toolbar, refs.toolButtonImages, "SelectToolButton", EditorToolType.Selection);
        MapEditorToolbarButtonFactory.CacheToolButton(toolbar, refs.toolButtonImages, "SpawnButton", EditorToolType.Spawn);
        MapEditorToolbarButtonFactory.CacheToolButton(toolbar, refs.toolButtonImages, "SpawnToolButton", EditorToolType.Spawn);

        Transform preview = MapEditorObjectUtility.FindAndRenameChild(toolbar, BrushPreviewObjectName, LegacyBrushPreviewObjectName);

        if (preview != null)
        {
            Transform previewImage = MapEditorObjectUtility.FindAndRenameChild(preview, BrushPreviewImageObjectName, LegacyBrushPreviewImageObjectName);
            Transform previewText = MapEditorObjectUtility.FindAndRenameChild(preview, BrushPreviewTextObjectName, LegacyBrushPreviewTextObjectName);
            refs.brushPreviewImage = previewImage == null ? null : previewImage.GetComponent<Image>();
            refs.brushPreviewText = previewText == null ? null : previewText.GetComponent<Text>();
        }

        Transform validation = toolbar.Find(ValidationStatusObjectName);
        refs.validationStatusText = validation == null ? null : validation.GetComponent<Text>();
        refs.recentPngListRoot = EnsureRecentPngList(toolbar);
    }

    private static void EnsureToolbarDivider(Transform parent)
    {
        if (parent.Find("Divider") != null)
        {
            return;
        }

        CreateToolbarDivider(parent);
    }

    private static void EnsureToolbarShortcutHint(Transform parent, string label, string shortcut)
    {
        string objectName = GetToolbarObjectName(label, "Hint");
        Transform existing = parent.Find(objectName);

        if (existing != null)
        {
            ConfigureShortcutHint(existing, label, shortcut);
            return;
        }

        GameObject hintObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        hintObject.transform.SetParent(parent, false);
        ConfigureShortcutHint(hintObject.transform, label, shortcut);
    }

    private static void ConfigureShortcutHint(Transform hintTransform, string label, string shortcut)
    {
        RectTransform rect = hintTransform.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.sizeDelta = new Vector2(0f, ToolbarHintHeight);
        }

        Text text = hintTransform.GetComponent<Text>();

        if (text == null)
        {
            text = hintTransform.gameObject.AddComponent<Text>();
        }

        text.text = label + "   " + shortcut;
        text.font = MapEditorFontProvider.Default;
        text.fontSize = ToolbarHintFontSize;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = new Color(0.82f, 0.82f, 0.82f, 1f);
    }

    private static void EnsureToolbarToolButton(Transform toolbar, MapEditorManager manager, string label, string shortcut, EditorToolType toolType, MapEditorToolbarAction action, string objectName)
    {
        Transform existing = toolbar.Find(objectName);

        if (existing != null)
        {
            MapEditorToolbarButtonFactory.ConfigureActionButton(existing, manager, label, shortcut, action);
            return;
        }

        MapEditorToolbarButtonFactory.CreateActionButton(toolbar, manager, label, shortcut, action, objectName);
    }

    private static void EnsureToolbarActionButton(Transform toolbar, MapEditorManager manager, string label, string shortcut, MapEditorToolbarAction action, string objectName)
    {
        Transform existing = toolbar.Find(objectName);

        if (existing != null)
        {
            MapEditorToolbarButtonFactory.ConfigureActionButton(existing, manager, label, shortcut, action);
            return;
        }

        MapEditorToolbarButtonFactory.CreateActionButton(toolbar, manager, label, shortcut, action, objectName);
    }

    private static string GetToolbarObjectName(string label, string suffix)
    {
        return label.Replace(" ", string.Empty).Replace("/", string.Empty) + suffix;
    }

    private static Transform EnsureRecentPngList(Transform parent)
    {
        Transform existing = MapEditorObjectUtility.FindAndRenameChild(parent, RecentPngListObjectName, LegacyRecentPngListObjectName);

        if (existing != null)
        {
            return existing;
        }

        GameObject root = new GameObject(RecentPngListObjectName, typeof(RectTransform), typeof(VerticalLayoutGroup));
        root.transform.SetParent(parent, false);

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, ToolbarRecentHeight);

        VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 1f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateToolbarLabel(root.transform, "최근 PNG");
        return root.transform;
    }

    private static void EnsureBrushPreview(Transform parent)
    {
        if (MapEditorObjectUtility.FindAndRenameChild(parent, BrushPreviewObjectName, LegacyBrushPreviewObjectName) != null)
        {
            return;
        }

        GameObject root = new GameObject(BrushPreviewObjectName, typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        root.transform.SetParent(parent, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(0f, ToolbarPreviewHeight);

        Image background = root.GetComponent<Image>();
        background.color = new Color(0.08f, 0.08f, 0.08f, 0.75f);

        HorizontalLayoutGroup layout = root.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(3, 3, 2, 2);
        layout.spacing = 3f;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        GameObject previewObject = new GameObject(BrushPreviewImageObjectName, typeof(RectTransform), typeof(Image));
        previewObject.transform.SetParent(root.transform, false);
        previewObject.GetComponent<RectTransform>().sizeDelta = new Vector2(14f, 14f);
        previewObject.GetComponent<Image>().preserveAspect = false;

        GameObject textObject = new GameObject(BrushPreviewTextObjectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(root.transform, false);
        textObject.GetComponent<RectTransform>().sizeDelta = new Vector2(122f, 14f);

        Text text = textObject.GetComponent<Text>();
        text.font = MapEditorFontProvider.Default;
        text.fontSize = 8;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.white;
    }

    private static void EnsureValidationStatus(Transform parent)
    {
        Transform existing = parent.Find(ValidationStatusObjectName);

        if (existing != null)
        {
            return;
        }

        GameObject statusObject = new GameObject(ValidationStatusObjectName, typeof(RectTransform), typeof(Text));
        statusObject.transform.SetParent(parent, false);

        RectTransform rect = statusObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, ToolbarValidationHeight);

        Text text = statusObject.GetComponent<Text>();
        text.text = "맵 검사\n검사 안 함";
        text.font = MapEditorFontProvider.Default;
        text.fontSize = 9;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = new Color(0.82f, 0.82f, 0.82f, 1f);
        text.raycastTarget = false;
    }

    private static void CreateToolbarLabel(Transform parent, string text)
    {
        GameObject labelObject = new GameObject(text, typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(parent, false);

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, ToolbarLabelHeight);

        Text label = labelObject.GetComponent<Text>();
        label.text = text;
        label.font = MapEditorFontProvider.Default;
        label.fontSize = ToolbarLabelFontSize;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = Color.white;
    }

    private static void CreateToolbarDivider(Transform parent)
    {
        GameObject dividerObject = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        dividerObject.transform.SetParent(parent, false);

        RectTransform rect = dividerObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 1f);

        Image image = dividerObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.22f);
        image.raycastTarget = false;
    }

}

public static class MapEditorLayerPanelBuilder
{
    private const string LayerPanelObjectName = "MapEditor_LayerPanel";
    private const float PanelWidth = 176f;
    private const float PanelHeight = 168f;
    private const int LabelFontSize = 12;
    private const int ButtonFontSize = 9;

    public static Dictionary<MapEditorLayerType, Image> Ensure(MapEditorManager manager, Vector2 toolbarOffset)
    {
        Dictionary<MapEditorLayerType, Image> buttonImages = new Dictionary<MapEditorLayerType, Image>();
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            return buttonImages;
        }

        Transform panel = canvas.transform.Find(LayerPanelObjectName);

        if (panel == null)
        {
            panel = CreatePanel(canvas.transform);
        }

        panel.gameObject.SetActive(true);
        panel.SetAsLastSibling();
        ConfigurePanel(panel, toolbarOffset);
        EnsureContents(panel, manager, buttonImages);
        return buttonImages;
    }

    private static Transform CreatePanel(Transform parent)
    {
        GameObject panelObject = new GameObject(LayerPanelObjectName, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        panelObject.transform.SetParent(parent, false);

        Image background = panelObject.GetComponent<Image>();
        background.color = new Color(0.13f, 0.13f, 0.13f, 0.88f);
        background.raycastTarget = false;

        VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(4, 4, 4, 4);
        layout.spacing = 3f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return panelObject.transform;
    }

    private static void ConfigurePanel(Transform panel, Vector2 toolbarOffset)
    {
        RectTransform rect = panel.GetComponent<RectTransform>();

        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        Vector2 position = toolbarOffset + new Vector2(-188f, -140f);
        RectTransform parentRect = panel.parent as RectTransform;

        if (parentRect != null && parentRect.rect.width > PanelWidth * 2f)
        {
            position.x = Mathf.Clamp(position.x, -parentRect.rect.width + PanelWidth + 8f, -PanelWidth - 12f);
            position.y = Mathf.Clamp(position.y, -parentRect.rect.height + PanelHeight + 8f, -8f);
        }

        rect.anchoredPosition = position;
    }

    private static void EnsureContents(Transform panel, MapEditorManager manager, Dictionary<MapEditorLayerType, Image> buttonImages)
    {
        for (int i = panel.childCount - 1; i >= 0; i--)
        {
            Transform child = panel.GetChild(i);
            child.name = "Destroyed_" + child.name;
            child.SetParent(null, false);
            MapEditorObjectUtility.DestroyObject(child.gameObject);
        }

        CreateLabel(panel, "레이어");

        GameObject gridObject = new GameObject("LayerGrid", typeof(RectTransform), typeof(GridLayoutGroup));
        gridObject.transform.SetParent(panel, false);

        RectTransform gridRect = gridObject.GetComponent<RectTransform>();
        gridRect.sizeDelta = new Vector2(0f, 142f);

        GridLayoutGroup grid = gridObject.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(168f, 20f);
        grid.spacing = new Vector2(0f, 4f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 1;
        grid.childAlignment = TextAnchor.UpperLeft;

        CreateLayerButton(gridObject.transform, manager, buttonImages, "바닥 그리기", MapEditorLayerType.Ground);
        CreateLayerButton(gridObject.transform, manager, buttonImages, "오브젝트 그리기", MapEditorLayerType.Object);
        CreateLayerButton(gridObject.transform, manager, buttonImages, "벽 모양 그리기", MapEditorLayerType.WallVisual);
        CreateLayerButton(gridObject.transform, manager, buttonImages, "이동 막기", MapEditorLayerType.WallCollision);
        CreateLayerButton(gridObject.transform, manager, buttonImages, "시작점 지정", MapEditorLayerType.Spawn);
        CreateLayerButton(gridObject.transform, manager, buttonImages, "구역 표시", MapEditorLayerType.Zone);
    }

    private static void CreateLabel(Transform parent, string text)
    {
        GameObject labelObject = new GameObject("LayerLabel", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(parent, false);

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 15f);

        Text label = labelObject.GetComponent<Text>();
        label.text = text;
        label.font = MapEditorFontProvider.Default;
        label.fontSize = LabelFontSize;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    private static void CreateLayerButton(Transform parent, MapEditorManager manager, Dictionary<MapEditorLayerType, Image> buttonImages, string label, MapEditorLayerType layerType)
    {
        GameObject rowObject = new GameObject("LayerRow_" + layerType, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rowObject.transform.SetParent(parent, false);

        HorizontalLayoutGroup rowLayout = rowObject.GetComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 3f;
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = true;

        GameObject buttonObject = new GameObject("Layer_" + layerType, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(rowObject.transform, false);
        buttonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(110f, 0f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;

        MapEditorToolbarButton toolbarButton = buttonObject.AddComponent<MapEditorToolbarButton>();
        toolbarButton.manager = manager;
        toolbarButton.action = MapEditorToolbarAction.SetLayer;
        toolbarButton.intArgument = (int)layerType;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(4f, 0f);
        textRect.offsetMax = new Vector2(-4f, 0f);

        Text text = textObject.GetComponent<Text>();
        text.text = label;
        text.font = MapEditorFontProvider.Default;
        text.fontSize = ButtonFontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;

        buttonImages[layerType] = image;
        CreateLayerVisibilityButton(rowObject.transform, manager, layerType);
    }

    private static void CreateLayerVisibilityButton(Transform parent, MapEditorManager manager, MapEditorLayerType layerType)
    {
        GameObject buttonObject = new GameObject("LayerVisibility_" + layerType, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(55f, 0f);

        Image image = buttonObject.GetComponent<Image>();
        bool visible = manager == null || manager.IsLayerVisible(layerType);
        image.color = visible ? new Color(0.18f, 0.48f, 0.95f, 1f) : new Color(0.18f, 0.18f, 0.18f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;

        MapEditorToolbarButton toolbarButton = buttonObject.AddComponent<MapEditorToolbarButton>();
        toolbarButton.manager = manager;
        toolbarButton.action = MapEditorToolbarAction.ToggleLayerVisible;
        toolbarButton.intArgument = (int)layerType;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(3f, 0f);
        textRect.offsetMax = new Vector2(-3f, 0f);

        Text text = textObject.GetComponent<Text>();
        text.text = visible ? "표시" : "숨김";
        text.font = MapEditorFontProvider.Default;
        text.fontSize = ButtonFontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
    }
}
