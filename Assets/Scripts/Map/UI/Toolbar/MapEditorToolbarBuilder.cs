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
    public Image runnerSpawnButtonImage;
    public Image seekerSpawnButtonImage;
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
    private const string RecentResourceScrollObjectName = "RecentResourceScroll";
    private const string RecentResourceContentObjectName = "RecentResourceContent";
    private const float ToolbarWidth = 176f;
    private const float ToolbarMinHeight = 584f;
    private const float ToolbarHintHeight = 13f;
    private const float ToolbarLabelHeight = 15f;
    private const float ToolbarPreviewHeight = 22f;
    private const float ToolbarValidationHeight = 26f;
    private const float ToolbarRecentHeight = 180f;
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
        CacheToolbarRefs(toolbar, ref refs);
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
            MapEditorObjectUtility.DestroyObject(recentPngListRoot.GetChild(i).gameObject);
        }

        if (paths == null)
        {
            ResetRecentResourceScroll(recentPngListRoot);
            return;
        }

        foreach (string path in paths)
        {
            MapEditorToolbarButtonFactory.CreateRecentPngButton(recentPngListRoot, manager, path);
        }

        ResetRecentResourceScroll(recentPngListRoot);
    }

    public static void RefreshLayout(Vector2 offset)
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        Transform toolbar = canvas == null ? null : FindExistingToolbar(canvas.transform);

        if (toolbar != null)
        {
            ConfigureToolToolbar(toolbar, offset);
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

        RectTransform parentRect = toolbar.parent as RectTransform;
        float availableHeight = ToolbarMinHeight;
        Vector2 position = offset;

        if (parentRect != null)
        {
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            availableHeight = 0f;
            position.x = Mathf.Clamp(position.x, -parentRect.rect.width + ToolbarWidth, 0f);
            position.y = 0f;
        }
        else
        {
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
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
        CreateToolbarLabel(toolbar, L("도구", "Tools"));
        CreateBrushToolRow(toolbar, manager);
        if (manager != null && manager.BrushRoleMenuOpen)
        {
            CreateBrushRoleRow(toolbar, manager);
        }
        EnsureToolbarToolButton(toolbar, manager, L("직선", "Line"), "L", EditorToolType.Line, MapEditorToolbarAction.Line, "LineToolButton");
        EnsureToolbarToolButton(toolbar, manager, L("사각형 채우기", "Rectangle Fill"), "G", EditorToolType.RectangleFill, MapEditorToolbarAction.RectangleFill, "RectangleFillToolButton");
        EnsureToolbarToolButton(toolbar, manager, L("지우개", "Eraser"), "E", EditorToolType.Eraser, MapEditorToolbarAction.Eraser, "EraserToolButton");
        EnsureToolbarToolButton(toolbar, manager, L("선택", "Select"), "S", EditorToolType.Selection, MapEditorToolbarAction.Select, "SelectToolButton");
        EnsureToolbarToolButton(toolbar, manager, L("프리뷰 영역", "Preview Area"), L("P/드래그", "P/Drag"), EditorToolType.PreviewRegion, MapEditorToolbarAction.PreviewRegion, "PreviewRegionToolButton");
        EnsureToolbarToolButton(toolbar, manager, L("플레이어 시작", "Runner Spawn"), L("클릭", "Click"), EditorToolType.Spawn, MapEditorToolbarAction.SetSpawn, "SpawnToolButton");
        EnsureToolbarToolButton(toolbar, manager, L("술래 시작", "Seeker Spawn"), L("클릭", "Click"), EditorToolType.Spawn, MapEditorToolbarAction.SetSeekerSpawn, "SeekerSpawnToolButton");
        EnsureToolbarActionButton(toolbar, manager, L("타일 만들기", "Create Tile"), L("클릭", "Click"), MapEditorToolbarAction.OpenTileCreator, "TileCreatorButton");
        EnsureToolbarActionButton(toolbar, manager, L("애니메이션 타일", "Animated Tile"), L("클릭", "Click"), MapEditorToolbarAction.OpenAnimationTileEditor, "AnimationTileButton");
        EnsureToolbarActionButton(toolbar, manager, L("이동 경로", "Moving Path"), L("선택 후 클릭", "Select / Click"), MapEditorToolbarAction.MovingRegion, "MovingRegionButton");
        EnsureToolbarActionButton(toolbar, manager, L("맵 테스트", "Test Map"), "F5", MapEditorToolbarAction.Playtest, "PlaytestButton");
        EnsureToolbarDivider(toolbar);
        CreateToolbarLabel(toolbar, L("파일과 검사", "Files & Validation"));
        EnsureToolbarActionButton(toolbar, manager, L("게임 맵 가져오기", "Import Game Map"), L("클릭", "Click"), MapEditorToolbarAction.ImportPixelChromaMap, "ImportMapButton");
        EnsureToolbarActionButton(toolbar, manager, L("타일셋", "Tilesets"), L("클릭", "Click"), MapEditorToolbarAction.OpenTilesetLibrary, "TilesetsButton");
        EnsureToolbarActionButton(toolbar, manager, L("PNG 불러오기", "Load PNG"), L("클릭", "Click"), MapEditorToolbarAction.PngLoad, "LoadPNGButton");
        EnsureToolbarActionButton(toolbar, manager, L("게임용 내보내기", "Export Game Map"), L("클릭", "Click"), MapEditorToolbarAction.ExportPixelChroma, "GameOutButton");
        EnsureToolbarActionButton(toolbar, manager, L("검사 후 창작마당 업로드", "Validate & Upload Workshop"), L("클릭", "Click"), MapEditorToolbarAction.UploadWorkshop, "WorkshopButton");
        EnsureToolbarActionButton(toolbar, manager, L("도움말", "Help"), "F1", MapEditorToolbarAction.PackageGuide, "HelpButton");
        EnsureToolbarActionButton(toolbar, manager, L("전체 지우기", "Clear All"), L("클릭", "Click"), MapEditorToolbarAction.ClearAll, "ClearAllButton");
        EnsureValidationStatus(toolbar);
        EnsureRecentPngList(toolbar);
    }

    private static string L(string korean, string english)
    {
        return MapEditorLocalization.Choose(korean, english);
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

    private static void CacheToolbarRefs(Transform toolbar, ref MapEditorToolbarRefs refs)
    {
        MapEditorToolbarButtonFactory.CacheToolButton(toolbar, refs.toolButtonImages, "BrushButton", EditorToolType.Brush);
        MapEditorToolbarButtonFactory.CacheToolButton(toolbar, refs.toolButtonImages, "EraserButton", EditorToolType.Eraser);
        MapEditorToolbarButtonFactory.CacheToolButton(toolbar, refs.toolButtonImages, "EraseLayerButton", EditorToolType.Eraser);
        MapEditorToolbarButtonFactory.CacheToolButton(toolbar, refs.toolButtonImages, "BrushToolButton", EditorToolType.Brush);
        MapEditorToolbarButtonFactory.CacheToolButton(toolbar, refs.toolButtonImages, "LineToolButton", EditorToolType.Line);
        MapEditorToolbarButtonFactory.CacheToolButton(toolbar, refs.toolButtonImages, "RectangleFillToolButton", EditorToolType.RectangleFill);
        MapEditorToolbarButtonFactory.CacheToolButton(toolbar, refs.toolButtonImages, "EraserToolButton", EditorToolType.Eraser);
        MapEditorToolbarButtonFactory.CacheToolButton(toolbar, refs.toolButtonImages, "EraseLayerToolButton", EditorToolType.Eraser);
        MapEditorToolbarButtonFactory.CacheToolButton(toolbar, refs.toolButtonImages, "BrushEraserToolButton", EditorToolType.BrushEraser);
        MapEditorToolbarButtonFactory.CacheToolButton(toolbar, refs.toolButtonImages, "WallButton", EditorToolType.Wall);
        MapEditorToolbarButtonFactory.CacheToolButton(toolbar, refs.toolButtonImages, "WallToolButton", EditorToolType.Wall);
        MapEditorToolbarButtonFactory.CacheToolButton(toolbar, refs.toolButtonImages, "SelectButton", EditorToolType.Selection);
        MapEditorToolbarButtonFactory.CacheToolButton(toolbar, refs.toolButtonImages, "SelectToolButton", EditorToolType.Selection);
        MapEditorToolbarButtonFactory.CacheToolButton(toolbar, refs.toolButtonImages, "SpawnButton", EditorToolType.Spawn);
        MapEditorToolbarButtonFactory.CacheToolButton(toolbar, refs.toolButtonImages, "SpawnToolButton", EditorToolType.Spawn);
        MapEditorToolbarButtonFactory.CacheToolButton(toolbar, refs.toolButtonImages, "PreviewRegionToolButton", EditorToolType.PreviewRegion);
        Transform runnerSpawnButton = toolbar.Find("SpawnToolButton");
        Transform seekerSpawnButton = toolbar.Find("SeekerSpawnToolButton");
        refs.runnerSpawnButtonImage = runnerSpawnButton == null ? null : runnerSpawnButton.GetComponent<Image>();
        refs.seekerSpawnButtonImage = seekerSpawnButton == null ? null : seekerSpawnButton.GetComponent<Image>();

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

    private static void CreateBrushToolRow(Transform toolbar, MapEditorManager manager)
    {
        GameObject rowObject = new GameObject("BrushToolRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rowObject.transform.SetParent(toolbar, false);
        rowObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 18f);

        HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 2f;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        string roleLabel = GetBrushRoleLabel(manager == null ? MapEditorLayerType.Ground : manager.BrushLayerRole);
        Button brush = MapEditorToolbarButtonFactory.CreateActionButton(
            rowObject.transform, manager, L("브러시 · ", "Brush · ") + roleLabel, "B", MapEditorToolbarAction.Brush, "BrushToolButton");
        brush.GetComponent<RectTransform>().sizeDelta = new Vector2(137f, 18f);

        string arrow = manager != null && manager.BrushRoleMenuOpen ? "▲" : "▼";
        Button menu = MapEditorToolbarButtonFactory.CreateActionButton(
            rowObject.transform, manager, arrow, string.Empty, MapEditorToolbarAction.ToggleBrushRoleMenu, "BrushRoleMenuButton");
        menu.GetComponent<RectTransform>().sizeDelta = new Vector2(29f, 18f);
        Text menuText = menu.GetComponentInChildren<Text>();
        if (menuText != null) menuText.alignment = TextAnchor.MiddleCenter;
    }

    private static string GetBrushRoleLabel(MapEditorLayerType role)
    {
        switch (MapEditorLayerUtility.GetBaseLayer(role))
        {
            case MapEditorLayerType.WallCollision:
                return L("충돌", "Collision");
            default:
                return L("바닥", "Ground");
        }
    }

    private static void CreateBrushRoleRow(Transform toolbar, MapEditorManager manager)
    {
        GameObject rowObject = new GameObject("BrushRoleRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rowObject.transform.SetParent(toolbar, false);
        rowObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 18f);

        HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 2f;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        CreateBrushRoleButton(rowObject.transform, manager, L("바닥", "Ground"), MapEditorLayerType.Ground);
        CreateBrushRoleButton(rowObject.transform, manager, L("충돌", "Collision"), MapEditorLayerType.WallCollision);
    }

    private static void CreateBrushRoleButton(Transform parent, MapEditorManager manager, string label, MapEditorLayerType role)
    {
        Button button = MapEditorToolbarButtonFactory.CreateActionButton(
            parent, manager, label, string.Empty, MapEditorToolbarAction.SetBrushRole, "BrushRole_" + role);
        button.GetComponent<RectTransform>().sizeDelta = new Vector2(82f, 18f);
        MapEditorToolbarButton handler = button.GetComponent<MapEditorToolbarButton>();
        handler.intArgument = (int)role;

        Image image = button.GetComponent<Image>();
        if (manager != null && manager.BrushLayerRole == role)
        {
            image.color = new Color(0.18f, 0.48f, 0.95f, 1f);
        }

        Text text = button.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 8;
        }
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
            Transform existingContent = existing.Find(RecentResourceScrollObjectName + "/Viewport/" + RecentResourceContentObjectName);
            if (existingContent != null)
            {
                return existingContent;
            }

            MapEditorObjectUtility.DestroyObject(existing.gameObject);
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

        CreateToolbarLabel(root.transform, L("최근 리소스", "Recent Resources"));
        return CreateRecentResourceScroll(root.transform);
    }

    private static Transform CreateRecentResourceScroll(Transform parent)
    {
        GameObject scrollObject = new GameObject(
            RecentResourceScrollObjectName,
            typeof(RectTransform),
            typeof(Image),
            typeof(ScrollRect));
        scrollObject.transform.SetParent(parent, false);

        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        scrollRectTransform.sizeDelta = new Vector2(0f, ToolbarRecentHeight - ToolbarLabelHeight - 1f);

        Image scrollBackground = scrollObject.GetComponent<Image>();
        scrollBackground.color = new Color(0.05f, 0.05f, 0.05f, 0.38f);
        scrollBackground.raycastTarget = true;

        GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewportObject.transform.SetParent(scrollObject.transform, false);
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = new Vector2(-10f, 0f);
        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
        viewportImage.raycastTarget = true;

        GameObject contentObject = new GameObject(
            RecentResourceContentObjectName,
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewportObject.transform, false);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 1f;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter contentFitter = contentObject.GetComponent<ContentSizeFitter>();
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject scrollbarObject = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        scrollbarObject.transform.SetParent(scrollObject.transform, false);
        RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = Vector2.one;
        scrollbarRect.pivot = new Vector2(1f, 0.5f);
        scrollbarRect.anchoredPosition = Vector2.zero;
        scrollbarRect.sizeDelta = new Vector2(8f, 0f);
        Image scrollbarBackground = scrollbarObject.GetComponent<Image>();
        scrollbarBackground.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);

        GameObject slidingAreaObject = new GameObject("Sliding Area", typeof(RectTransform));
        slidingAreaObject.transform.SetParent(scrollbarObject.transform, false);
        RectTransform slidingAreaRect = slidingAreaObject.GetComponent<RectTransform>();
        slidingAreaRect.anchorMin = Vector2.zero;
        slidingAreaRect.anchorMax = Vector2.one;
        slidingAreaRect.offsetMin = new Vector2(1f, 1f);
        slidingAreaRect.offsetMax = new Vector2(-1f, -1f);

        GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObject.transform.SetParent(slidingAreaObject.transform, false);
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;
        Image handleImage = handleObject.GetComponent<Image>();
        handleImage.color = new Color(0.38f, 0.55f, 0.72f, 1f);

        Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
        scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = handleRect;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.content = contentRect;
        scroll.viewport = viewportRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = true;
        scroll.decelerationRate = 0.135f;
        scroll.scrollSensitivity = 24f;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        scroll.verticalNormalizedPosition = 1f;

        return contentObject.transform;
    }

    private static void ResetRecentResourceScroll(Transform content)
    {
        RectTransform contentRect = content as RectTransform;
        if (contentRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        ScrollRect scroll = content == null ? null : content.GetComponentInParent<ScrollRect>();
        if (scroll != null)
        {
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;
        }
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
    private const float PanelWidth = MapEditorMapSizePanelBuilder.PanelWidth;
    private const float PanelHeight = 250f;
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

    public static void RefreshLayout(Vector2 toolbarOffset)
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        Transform panel = canvas == null ? null : canvas.transform.Find(LayerPanelObjectName);

        if (panel != null)
        {
            ConfigurePanel(panel, toolbarOffset);
        }
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

        Vector2 position = MapEditorMapSizePanelBuilder.GetLayerPanelPosition(toolbarOffset);
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

        GameObject viewportObject = new GameObject(
            "LayerScrollViewport",
            typeof(RectTransform),
            typeof(Image),
            typeof(Mask),
            typeof(LayoutElement),
            typeof(ScrollRect));
        viewportObject.transform.SetParent(panel, false);
        viewportObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 192f);
        viewportObject.GetComponent<LayoutElement>().preferredHeight = 192f;

        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
        viewportObject.GetComponent<Mask>().showMaskGraphic = false;

        GameObject gridObject = new GameObject("LayerGrid", typeof(RectTransform), typeof(GridLayoutGroup));
        gridObject.transform.SetParent(viewportObject.transform, false);

        int enabledLayerCount = 1;
        if (manager != null)
        {
            enabledLayerCount = 0;
            for (int i = 0; i < MapEditorLayerUtility.CanvasLayerCount; i++)
            {
                if (manager.IsCanvasEnabled(i)) enabledLayerCount++;
            }
        }

        RectTransform gridRect = gridObject.GetComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0f, 1f);
        gridRect.anchorMax = new Vector2(1f, 1f);
        gridRect.pivot = new Vector2(0.5f, 1f);
        gridRect.anchoredPosition = Vector2.zero;
        gridRect.sizeDelta = new Vector2(0f, enabledLayerCount * 22f);

        ScrollRect scrollRect = viewportObject.GetComponent<ScrollRect>();
        scrollRect.viewport = viewportObject.GetComponent<RectTransform>();
        scrollRect.content = gridRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 22f;

        GridLayoutGroup grid = gridObject.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(176f, 20f);
        grid.spacing = new Vector2(0f, 2f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 1;
        grid.childAlignment = TextAnchor.UpperLeft;

        for (int canvasIndex = MapEditorLayerUtility.CanvasLayerCount - 1; canvasIndex >= 0; canvasIndex--)
        {
            if (manager == null || manager.IsCanvasEnabled(canvasIndex))
            {
                CreateCanvasLayerButton(gridObject.transform, manager, buttonImages, canvasIndex);
            }
        }
        CreateLayerManagementRow(panel, manager);
    }

    private static void CreateCanvasLayerButton(
        Transform parent,
        MapEditorManager manager,
        Dictionary<MapEditorLayerType, Image> buttonImages,
        int canvasIndex)
    {
        GameObject rowObject = new GameObject("CanvasLayerRow_" + canvasIndex, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rowObject.transform.SetParent(parent, false);

        HorizontalLayoutGroup rowLayout = rowObject.GetComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 3f;
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = true;

        GameObject orderObject = new GameObject("CanvasLayerOrder_" + canvasIndex, typeof(RectTransform), typeof(Image));
        orderObject.transform.SetParent(rowObject.transform, false);
        orderObject.GetComponent<RectTransform>().sizeDelta = new Vector2(22f, 0f);
        orderObject.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.16f, 1f);
        CreateCenteredText(orderObject.transform, (canvasIndex + 1).ToString(), ButtonFontSize);
        MapEditorCanvasLayerDragHandle dragHandle = rowObject.AddComponent<MapEditorCanvasLayerDragHandle>();
        dragHandle.manager = manager;
        dragHandle.canvasIndex = canvasIndex;

        GameObject selectObject = new GameObject("CanvasLayer_" + canvasIndex, typeof(RectTransform), typeof(Image), typeof(Button));
        selectObject.transform.SetParent(rowObject.transform, false);
        selectObject.GetComponent<RectTransform>().sizeDelta = new Vector2(30f, 0f);

        Image selectImage = selectObject.GetComponent<Image>();
        bool selected = manager != null && manager.ActiveCanvasIndex == canvasIndex;
        selectImage.color = selected ? new Color(0.18f, 0.48f, 0.95f, 1f) : new Color(0.25f, 0.25f, 0.25f, 1f);
        Button selectButton = selectObject.GetComponent<Button>();
        selectButton.targetGraphic = selectImage;
        selectButton.transition = Selectable.Transition.None;

        MapEditorToolbarButton selectHandler = selectObject.AddComponent<MapEditorToolbarButton>();
        selectHandler.manager = manager;
        selectHandler.action = MapEditorToolbarAction.SetCanvas;
        selectHandler.intArgument = canvasIndex;
        CreateCenteredText(selectObject.transform, "선택", ButtonFontSize);

        foreach (MapEditorLayerType role in new[] { MapEditorLayerType.Ground, MapEditorLayerType.Object, MapEditorLayerType.WallVisual })
        {
            buttonImages[MapEditorLayerUtility.GetCanvasLayer(canvasIndex, role)] = selectImage;
        }
        if (selected) buttonImages[MapEditorLayerType.WallCollision] = selectImage;

        CreateCanvasNameInput(rowObject.transform, manager, canvasIndex);
        CreateCanvasVisibilityButton(rowObject.transform, manager, canvasIndex);
    }

    private static void CreateCanvasNameInput(Transform parent, MapEditorManager manager, int canvasIndex)
    {
        GameObject inputObject = new GameObject("CanvasLayerName_" + canvasIndex, typeof(RectTransform), typeof(Image), typeof(InputField));
        inputObject.transform.SetParent(parent, false);
        inputObject.GetComponent<RectTransform>().sizeDelta = new Vector2(70f, 0f);

        Image background = inputObject.GetComponent<Image>();
        background.color = new Color(0.09f, 0.09f, 0.09f, 1f);
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(inputObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(5f, 1f);
        textRect.offsetMax = new Vector2(-5f, -1f);

        Text text = textObject.GetComponent<Text>();
        text.font = MapEditorFontProvider.Default;
        text.fontSize = ButtonFontSize;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.white;
        text.supportRichText = false;

        InputField input = inputObject.GetComponent<InputField>();
        input.targetGraphic = background;
        input.textComponent = text;
        input.text = manager == null ? "레이어 " + (canvasIndex + 1) : manager.GetCanvasDisplayName(canvasIndex);
        input.characterLimit = 18;
        input.lineType = InputField.LineType.SingleLine;

        MapEditorCanvasNameInput nameInput = inputObject.AddComponent<MapEditorCanvasNameInput>();
        nameInput.manager = manager;
        nameInput.canvasIndex = canvasIndex;
    }

    private static void CreateCanvasVisibilityButton(Transform parent, MapEditorManager manager, int canvasIndex)
    {
        GameObject buttonObject = new GameObject("CanvasLayerVisibility_" + canvasIndex, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(38f, 0f);

        bool visible = manager == null || manager.IsCanvasVisible(canvasIndex);
        Image image = buttonObject.GetComponent<Image>();
        image.color = visible ? new Color(0.18f, 0.48f, 0.95f, 1f) : new Color(0.18f, 0.18f, 0.18f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;

        MapEditorToolbarButton handler = buttonObject.AddComponent<MapEditorToolbarButton>();
        handler.manager = manager;
        handler.action = MapEditorToolbarAction.ToggleCanvasVisible;
        handler.intArgument = canvasIndex;
        CreateCenteredText(buttonObject.transform, visible ? "ON" : "OFF", ButtonFontSize);
    }

    private static void CreateCenteredText(Transform parent, string value, int fontSize)
    {
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.text = value;
        text.font = MapEditorFontProvider.Default;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    private static void CreateOptionalLayerButtons(
        Transform parent,
        MapEditorManager manager,
        Dictionary<MapEditorLayerType, Image> buttonImages,
        MapEditorLayerType baseLayer)
    {
        MapEditorLayerType[] optionalLayers = MapEditorLayerUtility.GetOptionalLayers(baseLayer);

        for (int i = 0; i < optionalLayers.Length; i++)
        {
            CreateOptionalLayerButton(parent, manager, buttonImages, optionalLayers[i]);
        }
    }

    private static void CreateOptionalLayerButton(
        Transform parent,
        MapEditorManager manager,
        Dictionary<MapEditorLayerType, Image> buttonImages,
        MapEditorLayerType layerType)
    {
        if (manager != null && manager.IsLayerEnabled(layerType))
        {
            CreateLayerButton(parent, manager, buttonImages, layerType);
        }
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

    private static void CreateLayerButton(Transform parent, MapEditorManager manager, Dictionary<MapEditorLayerType, Image> buttonImages, MapEditorLayerType layerType)
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
        buttonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(38f, 0f);

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
        text.text = "선택";
        text.font = MapEditorFontProvider.Default;
        text.fontSize = ButtonFontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;

        buttonImages[layerType] = image;
        CreateLayerNameInput(rowObject.transform, manager, layerType);
        CreateLayerVisibilityButton(rowObject.transform, manager, layerType);
    }

    private static void CreateLayerNameInput(Transform parent, MapEditorManager manager, MapEditorLayerType layerType)
    {
        GameObject inputObject = new GameObject("LayerName_" + layerType, typeof(RectTransform), typeof(Image), typeof(InputField));
        inputObject.transform.SetParent(parent, false);
        inputObject.GetComponent<RectTransform>().sizeDelta = new Vector2(87f, 0f);

        Image background = inputObject.GetComponent<Image>();
        background.color = new Color(0.09f, 0.09f, 0.09f, 1f);

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(inputObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(5f, 1f);
        textRect.offsetMax = new Vector2(-5f, -1f);

        Text text = textObject.GetComponent<Text>();
        text.font = MapEditorFontProvider.Default;
        text.fontSize = ButtonFontSize;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.white;
        text.supportRichText = false;

        InputField input = inputObject.GetComponent<InputField>();
        input.targetGraphic = background;
        input.textComponent = text;
        input.text = manager == null ? layerType.ToString() : manager.GetLayerDisplayName(layerType);
        input.characterLimit = 18;
        input.lineType = InputField.LineType.SingleLine;

        MapEditorLayerNameInput nameInput = inputObject.AddComponent<MapEditorLayerNameInput>();
        nameInput.manager = manager;
        nameInput.layerType = layerType;
    }

    private static void CreateLayerVisibilityButton(Transform parent, MapEditorManager manager, MapEditorLayerType layerType)
    {
        GameObject buttonObject = new GameObject("LayerVisibility_" + layerType, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(45f, 0f);

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
        text.text = visible ? "ON" : "OFF";
        text.font = MapEditorFontProvider.Default;
        text.fontSize = ButtonFontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    private static void CreateLayerManagementRow(Transform parent, MapEditorManager manager)
    {
        GameObject rowObject = new GameObject("LayerManagement", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rowObject.transform.SetParent(parent, false);
        rowObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 20f);

        HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 3f;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        CreateLayerManagementButton(rowObject.transform, manager, "+ 레이어", MapEditorToolbarAction.AddCanvas, MapEditorLayerType.Ground, 83f);
        CreateLayerManagementButton(rowObject.transform, manager, "레이어 삭제", MapEditorToolbarAction.DeleteCanvas, MapEditorLayerType.Ground, 83f);
    }

    private static void CreateLayerManagementButton(
        Transform parent,
        MapEditorManager manager,
        string label,
        MapEditorToolbarAction action,
        MapEditorLayerType layerType,
        float width)
    {
        GameObject buttonObject = new GameObject("LayerAction_" + action + "_" + layerType, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 0f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = action == MapEditorToolbarAction.DeleteLayer || action == MapEditorToolbarAction.DeleteCanvas
            ? new Color(0.55f, 0.2f, 0.2f, 1f)
            : new Color(0.22f, 0.35f, 0.5f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        MapEditorToolbarButton toolbarButton = buttonObject.AddComponent<MapEditorToolbarButton>();
        toolbarButton.manager = manager;
        toolbarButton.action = action;
        toolbarButton.intArgument = (int)layerType;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.text = label;
        text.font = MapEditorFontProvider.Default;
        text.fontSize = 8;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
    }
}
