using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MapEditorAnimationTileWindow : MonoBehaviour
{
    private const string RootName = "MapEditor_AnimationTileWindow";
    private const float PanelWidth = 1120f;
    private const float PanelHeight = 700f;
    private const float MainWidth = 684f;
    private const float SidebarLeft = 720f;
    private const float SidebarWidth = 382f;

    private MapEditorManager manager;
    private int tilesetIndex;
    private int animationIndex = -1;
    private string selectedAnimationId = string.Empty;

    private Text tilesetValueText;
    private Text animationValueText;
    private Text tilesetInfoText;
    private Text statusText;
    private InputField nameInput;
    private InputField framesInput;
    private InputField fpsInput;
    private Toggle loopToggle;
    private Button deleteButton;
    private Button useBrushButton;
    private RectTransform animationListContent;
    private RectTransform framePaletteContent;
    private Image framePaletteAtlasImage;
    private MapEditorAnimationFramePaletteGraphic framePaletteOverlay;
    private RectTransform framePaletteBadgeRoot;
    private readonly List<Image> framePaletteSizeButtonImages = new List<Image>();
    private int framePaletteCellSize = 16;
    private Image animationPreviewImage;
    private MapEditorAnimatedTilePlayer animationPreviewPlayer;

    public int FramePaletteGridSize => framePaletteCellSize;

    public static MapEditorAnimationTileWindow Open(MapEditorManager manager)
    {
        if (manager == null)
        {
            return null;
        }

        Canvas canvas = MapEditorSceneUiBuilder.FindEditorCanvas();
        if (canvas == null)
        {
            return null;
        }

        Transform existing = canvas.transform.Find(RootName);
        if (existing != null)
        {
            ConfigureModalCanvas(existing.gameObject, canvas);
            existing.SetAsLastSibling();
            return existing.GetComponent<MapEditorAnimationTileWindow>();
        }

        GameObject root = CreateUiObject(
            RootName,
            canvas.transform,
            typeof(Image),
            typeof(Canvas),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup),
            typeof(MapEditorUiTransition),
            typeof(MapEditorAnimationTileWindow));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.62f);
        ConfigureModalCanvas(root, canvas);

        MapEditorAnimationTileWindow window = root.GetComponent<MapEditorAnimationTileWindow>();
        window.manager = manager;
        RectTransform panel = window.Build();
        root.transform.SetAsLastSibling();
        root.GetComponent<MapEditorUiTransition>().PlayIn(panel);
        return window;
    }

    private static void ConfigureModalCanvas(GameObject root, Canvas parentCanvas)
    {
        if (root == null) return;
        Canvas modalCanvas = root.GetComponent<Canvas>();
        if (modalCanvas == null) modalCanvas = root.AddComponent<Canvas>();
        if (root.GetComponent<GraphicRaycaster>() == null) root.AddComponent<GraphicRaycaster>();
        modalCanvas.overrideSorting = true;
        modalCanvas.sortingOrder = (parentCanvas == null ? 0 : parentCanvas.sortingOrder) + 100;
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
        panelObject.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.14f, 0.99f);

        CreateLabel(panel, "Title", L("애니메이션 타일", "Animated Tile"), 20, FontStyle.Bold,
            new Vector2(18f, -14f), new Vector2(500f, 32f), TextAnchor.MiddleLeft);
        CreateButton(panel, "CloseButton", "X", new Vector2(-14f, -12f), new Vector2(32f, 30f), Close, true);

        BuildAnimationSidebar(panel);

        CreateSelectorRow(
            panel,
            "Tileset",
            L("타일셋", "Tileset"),
            -62f,
            out tilesetValueText,
            SelectPreviousTileset,
            SelectNextTileset);
        CreateSelectorRow(
            panel,
            "Animation",
            L("애니메이션", "Animation"),
            -108f,
            out animationValueText,
            SelectPreviousAnimation,
            SelectNextAnimation);

        CreateButton(
            panel,
            "NewAnimationButton",
            L("새로 만들기", "New"),
            new Vector2(540f, -108f),
            new Vector2(72f, 30f),
            BeginNewAnimation,
            false,
            true);

        nameInput = CreateLabeledInput(panel, "Name", L("이름", "Name"), -164f, L("예: 물결", "Example: Water"));
        framesInput = CreateLabeledInput(
            panel,
            "Frames",
            L("프레임 번호", "Frame tiles"),
            -210f,
            "0, 1, 2, 3");
        fpsInput = CreateLabeledInput(panel, "Fps", "FPS", -256f, "8");
        fpsInput.GetComponent<RectTransform>().sizeDelta = new Vector2(120f, 30f);
        loopToggle = CreateToggle(panel, L("반복 재생", "Loop"), new Vector2(286f, -256f));

        framesInput.onValueChanged.AddListener(_ => RefreshDraftFrames());
        fpsInput.onValueChanged.AddListener(_ => RefreshDraftPreview());
        loopToggle.onValueChanged.AddListener(_ => RefreshDraftPreview());

        GameObject infoBox = CreateUiObject("InfoBox", panel, typeof(Image));
        RectTransform infoRect = infoBox.GetComponent<RectTransform>();
        ConfigureTopLeft(infoRect, new Vector2(18f, -304f), new Vector2(MainWidth, 84f));
        infoBox.GetComponent<Image>().color = new Color(0.055f, 0.065f, 0.075f, 1f);
        tilesetInfoText = CreateLabel(
            infoBox.transform,
            "TilesetInfo",
            string.Empty,
            12,
            FontStyle.Normal,
            new Vector2(12f, -8f),
            new Vector2(MainWidth - 24f, 64f),
            TextAnchor.UpperLeft);

        CreateLabel(
            panel,
            "FramePaletteTitle",
            L("전체 타일셋 · 고르는 순서대로 프레임 추가", "Full Tileset · Frames follow click order"),
            13,
            FontStyle.Bold,
            new Vector2(18f, -398f),
            new Vector2(330f, 22f),
            TextAnchor.MiddleLeft);
        BuildFramePaletteSizeSelector(panel);
        BuildFramePalette(panel);

        statusText = CreateLabel(
            panel,
            "Status",
            string.Empty,
            12,
            FontStyle.Normal,
            new Vector2(18f, -638f),
            new Vector2(MainWidth, 22f),
            TextAnchor.MiddleLeft);

        CreateButton(panel, "RefreshButton", L("목록 새로고침", "Refresh"), new Vector2(18f, 16f), new Vector2(126f, 34f), RefreshTilesets);
        CreateButton(panel, "ImportTilesetButton", L("타일셋 불러오기", "Import Tileset"), new Vector2(152f, 16f), new Vector2(136f, 34f), OpenTilesetImporter);
        CreateButton(panel, "SaveButton", L("저장", "Save"), new Vector2(442f, 16f), new Vector2(86f, 34f), SaveAnimation, false, true);
        deleteButton = CreateButton(panel, "DeleteButton", L("삭제", "Delete"), new Vector2(536f, 16f), new Vector2(72f, 34f), DeleteAnimation, false, false, true);
        CreateButton(panel, "DoneButton", L("닫기", "Close"), new Vector2(616f, 16f), new Vector2(86f, 34f), Close);

        loopToggle.isOn = true;
        fpsInput.text = "8";
        RefreshTilesets();
        return panel;
    }

    private void BuildAnimationSidebar(Transform panel)
    {
        GameObject dividerObject = CreateUiObject("SidebarDivider", panel, typeof(Image));
        RectTransform dividerRect = dividerObject.GetComponent<RectTransform>();
        ConfigureTopLeft(dividerRect, new Vector2(708f, -54f), new Vector2(1f, 616f));
        dividerObject.GetComponent<Image>().color = new Color(0.28f, 0.31f, 0.34f, 0.8f);

        CreateLabel(
            panel,
            "AnimationListTitle",
            L("애니메이션 목록", "Animation List"),
            13,
            FontStyle.Bold,
            new Vector2(SidebarLeft, -58f),
            new Vector2(SidebarWidth, 22f),
            TextAnchor.MiddleLeft);

        GameObject viewportObject = CreateUiObject(
            "AnimationListViewport",
            panel,
            typeof(Image),
            typeof(RectMask2D),
            typeof(ScrollRect));
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        ConfigureTopLeft(viewportRect, new Vector2(SidebarLeft, -84f), new Vector2(SidebarWidth, 160f));
        viewportObject.GetComponent<Image>().color = new Color(0.045f, 0.055f, 0.065f, 1f);

        GameObject contentObject = CreateUiObject("AnimationListContent", viewportObject.transform);
        animationListContent = contentObject.GetComponent<RectTransform>();
        animationListContent.anchorMin = new Vector2(0f, 1f);
        animationListContent.anchorMax = new Vector2(1f, 1f);
        animationListContent.pivot = new Vector2(0.5f, 1f);
        animationListContent.anchoredPosition = Vector2.zero;
        animationListContent.sizeDelta = new Vector2(0f, 160f);

        ScrollRect scroll = viewportObject.GetComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = animationListContent;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28f;

        CreateLabel(
            panel,
            "AnimationPreviewTitle",
            L("재생 미리보기", "Playback Preview"),
            13,
            FontStyle.Bold,
            new Vector2(SidebarLeft, -260f),
            new Vector2(SidebarWidth, 22f),
            TextAnchor.MiddleLeft);

        GameObject previewBox = CreateUiObject("AnimationPreview", panel, typeof(Image));
        RectTransform previewBoxRect = previewBox.GetComponent<RectTransform>();
        ConfigureTopLeft(previewBoxRect, new Vector2(SidebarLeft, -286f), new Vector2(SidebarWidth, 250f));
        previewBox.GetComponent<Image>().color = new Color(0.045f, 0.055f, 0.065f, 1f);

        GameObject previewObject = CreateUiObject(
            "AnimationPreviewImage",
            previewBox.transform,
            typeof(Image),
            typeof(MapEditorAnimatedTilePlayer));
        RectTransform previewRect = previewObject.GetComponent<RectTransform>();
        previewRect.anchorMin = Vector2.zero;
        previewRect.anchorMax = Vector2.one;
        previewRect.offsetMin = new Vector2(8f, 8f);
        previewRect.offsetMax = new Vector2(-8f, -8f);
        animationPreviewImage = previewObject.GetComponent<Image>();
        animationPreviewImage.color = Color.white;
        animationPreviewImage.preserveAspect = true;
        animationPreviewImage.raycastTarget = false;
        animationPreviewImage.enabled = false;
        animationPreviewPlayer = previewObject.GetComponent<MapEditorAnimatedTilePlayer>();
        animationPreviewPlayer.Stop();

        useBrushButton = CreateButton(
            panel,
            "UseAnimationBrushButton",
            L("브러시로 사용", "Use as Brush"),
            new Vector2(SidebarLeft, -550f),
            new Vector2(SidebarWidth, 30f),
            UseSelectedAnimationAsBrush,
            false,
            true);
        useBrushButton.interactable = false;
    }

    private void BuildFramePalette(Transform panel)
    {
        GameObject viewportObject = CreateUiObject(
            "FramePaletteViewport", panel, typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        ConfigureTopLeft(viewportRect, new Vector2(18f, -436f), new Vector2(MainWidth, 190f));
        viewportObject.GetComponent<Image>().color = new Color(0.045f, 0.055f, 0.065f, 1f);

        GameObject contentObject = CreateUiObject("FramePaletteContent", viewportObject.transform);
        framePaletteContent = contentObject.GetComponent<RectTransform>();
        framePaletteContent.anchorMin = new Vector2(0f, 1f);
        framePaletteContent.anchorMax = new Vector2(1f, 1f);
        framePaletteContent.pivot = new Vector2(0.5f, 1f);
        framePaletteContent.anchoredPosition = Vector2.zero;
        framePaletteContent.sizeDelta = new Vector2(0f, MainWidth);

        GameObject atlasObject = CreateUiObject("Atlas", framePaletteContent, typeof(Image));
        framePaletteAtlasImage = atlasObject.GetComponent<Image>();
        framePaletteAtlasImage.preserveAspect = true;
        framePaletteAtlasImage.raycastTarget = false;
        Stretch(atlasObject.GetComponent<RectTransform>());

        GameObject overlayObject = CreateUiObject(
            "GridOverlay",
            framePaletteContent,
            typeof(CanvasRenderer),
            typeof(MapEditorAnimationFramePaletteGraphic),
            typeof(MapEditorAnimationFramePaletteInput));
        Stretch(overlayObject.GetComponent<RectTransform>());
        framePaletteOverlay = overlayObject.GetComponent<MapEditorAnimationFramePaletteGraphic>();
        framePaletteOverlay.raycastTarget = true;
        overlayObject.GetComponent<MapEditorAnimationFramePaletteInput>().Configure(this);

        GameObject badgeObject = CreateUiObject("SelectedOrder", framePaletteContent);
        framePaletteBadgeRoot = badgeObject.GetComponent<RectTransform>();
        Stretch(framePaletteBadgeRoot);

        ScrollRect scroll = viewportObject.GetComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = framePaletteContent;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28f;
    }

    private void BuildFramePaletteSizeSelector(Transform panel)
    {
        CreateLabel(panel, "FramePaletteSizeLabel", L("분할 단위", "Grid division"), 11, FontStyle.Normal,
            new Vector2(352f, -398f), new Vector2(72f, 22f), TextAnchor.MiddleRight);

        int[] sizes = { 16, 32, 64, 128 };
        for (int i = 0; i < sizes.Length; i++)
        {
            int capturedSize = sizes[i];
            Button button = CreateButton(
                panel,
                "FramePaletteSize" + capturedSize + "Button",
                capturedSize + "×" + capturedSize,
                new Vector2(430f + i * 64f, -398f),
                new Vector2(60f, 24f),
                () => SetFramePaletteCellSize(capturedSize));
            framePaletteSizeButtonImages.Add(button.GetComponent<Image>());
        }

        RefreshFramePaletteSizeButtons();
    }

    private void SetFramePaletteCellSize(int size)
    {
        int normalized = MapEditorManager.NormalizePngPaletteGridSize(size);
        if (framePaletteCellSize == normalized) return;
        framePaletteCellSize = normalized;
        framesInput.text = string.Empty;
        RefreshFramePaletteSizeButtons();
        RefreshTilesetGridInfo(GetSelectedTileset());
        RefreshFramePalette(GetSelectedTileset());
        RefreshDraftPreview();
    }

    private void RefreshFramePaletteSizeButtons()
    {
        int[] sizes = { 16, 32, 64, 128 };
        for (int i = 0; i < framePaletteSizeButtonImages.Count && i < sizes.Length; i++)
        {
            framePaletteSizeButtonImages[i].color = sizes[i] == framePaletteCellSize
                ? new Color(0.18f, 0.48f, 0.95f, 1f)
                : new Color(0.23f, 0.25f, 0.28f, 1f);
        }
    }

    private void RefreshFramePalette(MapEditorTilesetDefinition tileset)
    {
        if (framePaletteContent == null || framePaletteAtlasImage == null || framePaletteOverlay == null) return;
        if (framePaletteBadgeRoot != null)
        {
            for (int i = framePaletteBadgeRoot.childCount - 1; i >= 0; i--)
                MapEditorObjectUtility.DestroyObject(framePaletteBadgeRoot.GetChild(i).gameObject);
        }
        if (tileset == null || manager == null)
        {
            framePaletteAtlasImage.sprite = null;
            framePaletteOverlay.Configure(framePaletteCellSize, null, null);
            return;
        }

        List<int> parsedFrames = ParseFrameIdsLoose(framesInput.text);
        HashSet<int> selected = new HashSet<int>(parsedFrames);
        HashSet<int> occupiedByOtherAnimations = GetOccupiedSourceFrameIds(tileset);
        framePaletteAtlasImage.sprite = manager.GetPngFullImageSprite(tileset.atlasPath);
        framePaletteOverlay.Configure(framePaletteCellSize, selected, occupiedByOtherAnimations);

        float cellSize = MainWidth / framePaletteCellSize;
        if (framePaletteBadgeRoot == null || cellSize < 12f) return;
        for (int order = 0; order < parsedFrames.Count; order++)
        {
            int tileId = parsedFrames[order];
            if (tileId < 0 || tileId >= framePaletteCellSize * framePaletteCellSize) continue;
            int x = tileId % framePaletteCellSize;
            int y = tileId / framePaletteCellSize;
            Text badge = CreateLabel(
                framePaletteBadgeRoot,
                "Order_" + order,
                (order + 1).ToString(),
                9,
                FontStyle.Bold,
                new Vector2(x * cellSize, -y * cellSize),
                new Vector2(cellSize, cellSize),
                TextAnchor.LowerRight);
            badge.raycastTarget = false;
        }
    }

    public void ToggleFrameTile(int tileId)
    {
        MapEditorTilesetDefinition tileset = GetSelectedTileset();
        if (tileset != null && GetOccupiedSourceFrameIds(tileset).Contains(tileId))
        {
            SetStatus(L(
                "다른 애니메이션이 이미 사용하는 타일입니다.",
                "This tile is already used by another animation."), false);
            return;
        }

        List<int> frames = ParseFrameIdsLoose(framesInput.text);
        int existing = frames.IndexOf(tileId);
        if (existing >= 0) frames.RemoveAt(existing);
        else if (frames.Count < MapEditorTilesetLibraryService.MaxAnimationFrameCount) frames.Add(tileId);

        SetFrameIds(frames);
        RefreshFramePalette(GetSelectedTileset());
        RefreshDraftPreview();
    }

    private HashSet<int> GetOccupiedSourceFrameIds(MapEditorTilesetDefinition tileset)
    {
        HashSet<int> occupied = new HashSet<int>();
        if (tileset?.animations == null) return occupied;

        for (int animationListIndex = 0; animationListIndex < tileset.animations.Length; animationListIndex++)
        {
            MapEditorTilesetAnimationDefinition animation = tileset.animations[animationListIndex];
            if (animation == null
                || (!string.IsNullOrEmpty(selectedAnimationId)
                    && string.Equals(animation.id, selectedAnimationId, StringComparison.Ordinal)))
            {
                continue;
            }

            int animationGridSize = MapEditorManager.NormalizePngPaletteGridSize(
                animation.GetFrameGridSize(tileset.atlasGridSize));

            int frameCount = Mathf.Max(1, animation.frameCount);
            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                AddOverlappingSourceGridTiles(
                    occupied,
                    framePaletteCellSize,
                    animationGridSize,
                    animation.GetFrameTileId(frameIndex));
            }
        }

        return occupied;
    }

    private static void AddOverlappingSourceGridTiles(
        HashSet<int> occupied,
        int targetGridSize,
        int sourceGridSize,
        int sourceGridTileId)
    {
        int sourceX = sourceGridTileId % sourceGridSize;
        int sourceYFromBottom = sourceGridTileId / sourceGridSize;
        int firstX = Mathf.FloorToInt(sourceX * targetGridSize / (float)sourceGridSize);
        int lastX = Mathf.CeilToInt((sourceX + 1) * targetGridSize / (float)sourceGridSize) - 1;
        int firstY = Mathf.FloorToInt(sourceYFromBottom * targetGridSize / (float)sourceGridSize);
        int lastY = Mathf.CeilToInt((sourceYFromBottom + 1) * targetGridSize / (float)sourceGridSize) - 1;
        for (int yFromBottom = firstY; yFromBottom <= lastY; yFromBottom++)
        {
            int rowFromTop = targetGridSize - 1 - yFromBottom;
            for (int x = firstX; x <= lastX; x++) occupied.Add(rowFromTop * targetGridSize + x);
        }
    }

    public void ReorderFrameTile(int sourceTileId, int targetTileId)
    {
        List<int> frames = ParseFrameIdsLoose(framesInput.text);
        int sourceIndex = frames.IndexOf(sourceTileId);
        int targetIndex = frames.IndexOf(targetTileId);
        if (sourceIndex < 0 || targetIndex < 0 || sourceIndex == targetIndex) return;

        frames.RemoveAt(sourceIndex);
        targetIndex = frames.IndexOf(targetTileId);
        frames.Insert(targetIndex, sourceTileId);
        SetFrameIds(frames);
        RefreshFramePalette(GetSelectedTileset());
        RefreshDraftPreview();
    }

    private void SetFrameIds(IReadOnlyList<int> frames)
    {
        StringBuilder value = new StringBuilder();
        for (int i = 0; i < frames.Count; i++)
        {
            if (i > 0) value.Append(", ");
            value.Append(frames[i]);
        }
        framesInput.text = value.ToString();
    }

    private static List<int> ParseFrameIdsLoose(string value)
    {
        List<int> result = new List<int>();
        string[] parts = (value ?? string.Empty).Split(
            new[] { ',', ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length && result.Count < MapEditorTilesetLibraryService.MaxAnimationFrameCount; i++)
        {
            if (int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int tileId)
                && tileId >= 0
                && !result.Contains(tileId))
            {
                result.Add(tileId);
            }
        }
        return result;
    }

    private void RefreshDraftFrames()
    {
        RefreshFramePalette(GetSelectedTileset());
        RefreshDraftPreview();
    }

    private void RefreshDraftPreview()
    {
        RefreshAnimationPreview(GetSelectedTileset(), GetSelectedAnimation(GetSelectedTileset()));
    }

    private void RefreshAnimationSidebar(
        MapEditorTilesetDefinition tileset,
        MapEditorTilesetAnimationDefinition selectedAnimation)
    {
        RefreshAnimationList(tileset);
        RefreshAnimationPreview(tileset, selectedAnimation);
    }

    private void RefreshAnimationList(MapEditorTilesetDefinition tileset)
    {
        if (animationListContent == null)
        {
            return;
        }

        for (int i = animationListContent.childCount - 1; i >= 0; i--)
        {
            MapEditorObjectUtility.DestroyObject(animationListContent.GetChild(i).gameObject);
        }

        int count = tileset?.animations?.Length ?? 0;
        if (count == 0)
        {
            Text emptyText = CreateLabel(
                animationListContent,
                "EmptyAnimationList",
                L("저장된 애니메이션 없음", "No saved animations"),
                11,
                FontStyle.Normal,
                new Vector2(8f, -8f),
                new Vector2(SidebarWidth - 16f, 40f),
                TextAnchor.UpperLeft);
            emptyText.color = new Color(0.62f, 0.65f, 0.68f, 1f);
            animationListContent.sizeDelta = new Vector2(0f, 208f);
            return;
        }

        float rowHeight = 34f;
        animationListContent.sizeDelta = new Vector2(0f, Mathf.Max(208f, count * rowHeight));
        for (int i = 0; i < count; i++)
        {
            MapEditorTilesetAnimationDefinition animation = tileset.animations[i];
            if (animation == null)
            {
                continue;
            }

            int capturedIndex = i;
            bool selected = i == animationIndex;
            GameObject rowObject = CreateUiObject(
                "AnimationListItem_" + i,
                animationListContent,
                typeof(Image),
                typeof(Button));
            RectTransform rowRect = rowObject.GetComponent<RectTransform>();
            ConfigureTopLeft(rowRect, new Vector2(4f, -4f - i * rowHeight), new Vector2(SidebarWidth - 8f, 30f));

            Image rowImage = rowObject.GetComponent<Image>();
            rowImage.color = selected
                ? new Color(0.18f, 0.48f, 0.95f, 1f)
                : new Color(0.19f, 0.21f, 0.24f, 1f);

            Button rowButton = rowObject.GetComponent<Button>();
            rowButton.targetGraphic = rowImage;
            rowButton.onClick.AddListener(() => SelectAnimationAt(capturedIndex));

            string rowLabel = string.Format(
                "{0}  {1}F / {2} FPS",
                animation.displayName,
                Mathf.Max(1, animation.frameCount),
                animation.framesPerSecond.ToString("0.#", CultureInfo.InvariantCulture));
            Text rowText = CreateLabel(
                rowObject.transform,
                "Text",
                rowLabel,
                11,
                selected ? FontStyle.Bold : FontStyle.Normal,
                Vector2.zero,
                Vector2.zero,
                TextAnchor.MiddleLeft);
            rowText.rectTransform.anchorMin = Vector2.zero;
            rowText.rectTransform.anchorMax = Vector2.one;
            rowText.rectTransform.offsetMin = new Vector2(8f, 0f);
            rowText.rectTransform.offsetMax = new Vector2(-6f, 0f);
            rowText.horizontalOverflow = HorizontalWrapMode.Overflow;
            rowText.verticalOverflow = VerticalWrapMode.Truncate;
        }
    }

    private void RefreshAnimationPreview(
        MapEditorTilesetDefinition tileset,
        MapEditorTilesetAnimationDefinition animation)
    {
        if (animationPreviewImage == null || animationPreviewPlayer == null)
        {
            return;
        }

        animationPreviewPlayer.Stop();
        animationPreviewImage.sprite = null;
        animationPreviewImage.enabled = false;

        if (tileset == null || manager == null)
        {
            return;
        }

        List<int> sourceFrameIds = ParseFrameIdsLoose(framesInput == null ? string.Empty : framesInput.text);
        if (sourceFrameIds.Count == 0)
        {
            return;
        }


        List<Sprite> draftFrames = new List<Sprite>(sourceFrameIds.Count);
        for (int i = 0; i < sourceFrameIds.Count; i++)
        {
            int sourceTileId = sourceFrameIds[i];
            if (sourceTileId < 0 || sourceTileId >= framePaletteCellSize * framePaletteCellSize) continue;
            Sprite sprite = manager.GetPaletteTileSprite(
                tileset.atlasPath,
                framePaletteCellSize,
                ToGridTileId(framePaletteCellSize, sourceTileId));
            if (sprite != null) draftFrames.Add(sprite);
        }

        if (draftFrames.Count == 0) return;

        float fps = 8f;
        if (fpsInput != null && !TryParseFps(fpsInput.text, out fps)) fps = 8f;

        animationPreviewImage.enabled = true;
        animationPreviewPlayer.Configure(
            animationPreviewImage,
            draftFrames.ToArray(),
            fps,
            loopToggle == null || loopToggle.isOn);
    }

    private void SelectAnimationAt(int index)
    {
        MapEditorTilesetDefinition tileset = GetSelectedTileset();
        if (tileset?.animations == null || index < 0 || index >= tileset.animations.Length)
        {
            return;
        }

        animationIndex = index;
        selectedAnimationId = tileset.animations[index]?.id ?? string.Empty;
        RefreshSelectionFields();
    }

    private void RefreshTilesets()
    {
        IReadOnlyList<MapEditorTilesetDefinition> definitions = GetTilesets();
        if (definitions.Count == 0)
        {
            tilesetIndex = 0;
            animationIndex = -1;
            selectedAnimationId = string.Empty;
            tilesetValueText.text = L("불러온 타일셋 없음", "No imported tilesets");
            animationValueText.text = "-";
            tilesetInfoText.text = L(
                "먼저 '타일셋 불러오기'로 PNG 타일셋을 가져오세요.",
                "Import a PNG tileset before creating an animation.");
            SetStatus(L("편집할 타일셋이 없습니다.", "There is no tileset to edit."), false);
            SetEditingEnabled(false);
            RefreshAnimationSidebar(null, null);
            RefreshFramePalette(null);
            return;
        }

        tilesetIndex = Mathf.Clamp(tilesetIndex, 0, definitions.Count - 1);
        animationIndex = -1;
        selectedAnimationId = string.Empty;
        SetEditingEnabled(true);
        RefreshSelectionFields();
        SetStatus(L("프레임 번호를 입력하고 저장하세요.", "Enter frame tile numbers and save."), true);
    }

    private void SelectPreviousTileset()
    {
        SelectTileset(-1);
    }

    private void SelectNextTileset()
    {
        SelectTileset(1);
    }

    private void SelectTileset(int direction)
    {
        IReadOnlyList<MapEditorTilesetDefinition> definitions = GetTilesets();
        if (definitions.Count == 0)
        {
            return;
        }

        tilesetIndex = (tilesetIndex + direction + definitions.Count) % definitions.Count;
        animationIndex = -1;
        selectedAnimationId = string.Empty;
        RefreshSelectionFields();
    }

    private void SelectPreviousAnimation()
    {
        SelectAnimation(-1);
    }

    private void SelectNextAnimation()
    {
        SelectAnimation(1);
    }

    private void SelectAnimation(int direction)
    {
        MapEditorTilesetDefinition tileset = GetSelectedTileset();
        int count = tileset?.animations?.Length ?? 0;
        if (count == 0)
        {
            BeginNewAnimation();
            return;
        }

        animationIndex += direction;
        if (animationIndex < -1)
        {
            animationIndex = count - 1;
        }
        else if (animationIndex >= count)
        {
            animationIndex = -1;
        }

        RefreshSelectionFields();
    }

    private void BeginNewAnimation()
    {
        animationIndex = -1;
        selectedAnimationId = string.Empty;
        RefreshSelectionFields();
        SetStatus(L(
            "팔레트에서 새 애니메이션에 사용할 타일을 순서대로 2개 이상 고르세요.",
            "Choose at least two tiles for the new animation in playback order."), true);
        nameInput.ActivateInputField();
    }

    private void RefreshSelectionFields()
    {
        MapEditorTilesetDefinition tileset = GetSelectedTileset();
        if (tileset == null)
        {
            return;
        }

        tilesetValueText.text = tileset.displayName;
        MapEditorTilesetAnimationDefinition animation = GetSelectedAnimation(tileset);
        if (animation != null)
        {
            framePaletteCellSize = MapEditorManager.NormalizePngPaletteGridSize(
                animation.GetFrameGridSize(tileset.atlasGridSize));
            RefreshFramePaletteSizeButtons();
        }
        RefreshTilesetGridInfo(tileset);

        bool editingExisting = animation != null;
        deleteButton.interactable = editingExisting;
        useBrushButton.interactable = editingExisting;

        if (!editingExisting)
        {
            animationIndex = -1;
            selectedAnimationId = string.Empty;
            animationValueText.text = L("새 애니메이션", "New animation");
            nameInput.text = L("새 애니메이션 ", "New Animation ") + ((tileset.animations?.Length ?? 0) + 1);
            framesInput.text = string.Empty;
            fpsInput.text = "8";
            loopToggle.isOn = true;
            RefreshAnimationSidebar(tileset, null);
            RefreshFramePalette(tileset);
            return;
        }

        selectedAnimationId = animation.id;
        animationValueText.text = animation.displayName + "  (" + (animationIndex + 1) + "/" + tileset.animations.Length + ")";
        nameInput.text = animation.displayName;
        framesInput.text = FormatSourceFrameIds(tileset, animation);
        fpsInput.text = animation.framesPerSecond.ToString("0.##", CultureInfo.InvariantCulture);
        loopToggle.isOn = animation.loop;
        RefreshAnimationSidebar(tileset, animation);
        RefreshFramePalette(tileset);
    }

    private void RefreshTilesetGridInfo(MapEditorTilesetDefinition tileset)
    {
        if (tilesetInfoText == null || tileset == null) return;
        int tileCount = framePaletteCellSize * framePaletteCellSize;
        tilesetInfoText.text = string.Format(
            L(
                "{0}×{0} 분할 / 프레임 셀 {1}개\n프레임 번호는 왼쪽 위부터 0으로 시작합니다. 2~32개, 1~30 FPS",
                "{0}×{0} division / {1} frame cells\nFrame numbers start at 0 from the top-left. 2-32 frames, 1-30 FPS"),
            framePaletteCellSize,
            tileCount);
    }

    private void SaveAnimation()
    {
        MapEditorTilesetDefinition tileset = GetSelectedTileset();
        if (tileset == null)
        {
            SetStatus(L("타일셋을 먼저 불러오세요.", "Import a tileset first."), false);
            return;
        }

        if (!TryParseFrameIds(framesInput.text, out int[] frameIds, out string parseError))
        {
            SetStatus(parseError, false);
            return;
        }

        if (!TryParseFps(fpsInput.text, out float fps))
        {
            SetStatus(L("FPS는 1~30 사이의 숫자여야 합니다.", "FPS must be a number from 1 to 30."), false);
            return;
        }

        bool success;
        string error;
        if (string.IsNullOrEmpty(selectedAnimationId))
        {
            success = manager.AddTilesetAnimation(
                tileset.id,
                nameInput.text,
                framePaletteCellSize,
                frameIds,
                fps,
                loopToggle.isOn,
                out MapEditorTilesetAnimationDefinition created,
                out error);
            if (success)
            {
                selectedAnimationId = created.id;
                animationIndex = FindAnimationIndex(tileset, created.id);
            }
        }
        else
        {
            success = manager.UpdateTilesetAnimation(
                tileset.id,
                selectedAnimationId,
                nameInput.text,
                framePaletteCellSize,
                frameIds,
                fps,
                loopToggle.isOn,
                out error);
        }

        if (!success)
        {
            SetStatus(LocalizeServiceError(error), false);
            return;
        }

        RefreshSelectionFields();
        SetStatus(L("애니메이션을 저장했습니다.", "Animation saved."), true);
    }

    private void DeleteAnimation()
    {
        MapEditorTilesetDefinition tileset = GetSelectedTileset();
        if (tileset == null || string.IsNullOrEmpty(selectedAnimationId))
        {
            return;
        }

        if (!manager.RemoveTilesetAnimation(tileset.id, selectedAnimationId))
        {
            SetStatus(L("애니메이션을 삭제하지 못했습니다.", "Could not delete the animation."), false);
            return;
        }

        animationIndex = -1;
        selectedAnimationId = string.Empty;
        RefreshSelectionFields();
        SetStatus(L("애니메이션을 삭제했습니다.", "Animation deleted."), true);
    }

    private void UseSelectedAnimationAsBrush()
    {
        MapEditorTilesetDefinition tileset = GetSelectedTileset();
        MapEditorTilesetAnimationDefinition animation = GetSelectedAnimation(tileset);
        if (manager == null || tileset == null || animation == null)
        {
            SetStatus(L("먼저 저장된 애니메이션을 선택하세요.", "Select a saved animation first."), false);
            return;
        }

        if (!manager.SelectAnimationTileBrush(tileset.id, animation.id))
        {
            SetStatus(L("애니메이션 프레임을 불러오지 못했습니다.", "Could not load the animation frames."), false);
            return;
        }

        CloseImmediately();
    }

    private void OpenTilesetImporter()
    {
        manager.OpenTilesetLibrary();
        RefreshTilesets();
    }

    private IReadOnlyList<MapEditorTilesetDefinition> GetTilesets()
    {
        return manager == null
            ? Array.Empty<MapEditorTilesetDefinition>()
            : manager.GetImportedTilesets();
    }

    private MapEditorTilesetDefinition GetSelectedTileset()
    {
        IReadOnlyList<MapEditorTilesetDefinition> definitions = GetTilesets();
        return definitions.Count == 0 || tilesetIndex < 0 || tilesetIndex >= definitions.Count
            ? null
            : definitions[tilesetIndex];
    }

    private MapEditorTilesetAnimationDefinition GetSelectedAnimation(MapEditorTilesetDefinition tileset)
    {
        if (tileset?.animations == null || animationIndex < 0 || animationIndex >= tileset.animations.Length)
        {
            return null;
        }

        return tileset.animations[animationIndex];
    }

    private void SetEditingEnabled(bool enabled)
    {
        nameInput.interactable = enabled;
        framesInput.interactable = enabled;
        fpsInput.interactable = enabled;
        loopToggle.interactable = enabled;
        deleteButton.interactable = false;
        useBrushButton.interactable = false;
    }

    private void SetStatus(string message, bool success)
    {
        statusText.text = message;
        statusText.color = success
            ? new Color(0.55f, 0.9f, 0.66f, 1f)
            : new Color(1f, 0.52f, 0.48f, 1f);
    }

    private static bool TryParseFrameIds(string value, out int[] frameIds, out string error)
    {
        frameIds = null;
        error = string.Empty;
        string[] parts = (value ?? string.Empty).Split(
            new[] { ',', ';', ' ', '\t', '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < MapEditorTilesetLibraryService.MinAnimationFrameCount
            || parts.Length > MapEditorTilesetLibraryService.MaxAnimationFrameCount)
        {
            error = L("프레임 번호를 2~32개 입력하세요.", "Enter 2 to 32 frame tile numbers.");
            return false;
        }

        frameIds = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out frameIds[i]))
            {
                error = L("프레임 번호는 정수로 입력하세요.", "Frame tile numbers must be integers.");
                return false;
            }
        }

        return true;
    }

    private static bool TryParseFps(string value, out float fps)
    {
        bool parsed = float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out fps)
            || float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out fps);
        return parsed
            && fps >= MapEditorTilesetLibraryService.MinAnimationFramesPerSecond
            && fps <= MapEditorTilesetLibraryService.MaxAnimationFramesPerSecond;
    }

    private static int FindAnimationIndex(MapEditorTilesetDefinition tileset, string animationId)
    {
        if (tileset?.animations == null)
        {
            return -1;
        }

        for (int i = 0; i < tileset.animations.Length; i++)
        {
            if (tileset.animations[i] != null && tileset.animations[i].id == animationId)
            {
                return i;
            }
        }

        return -1;
    }

    private static string FormatSourceFrameIds(
        MapEditorTilesetDefinition tileset,
        MapEditorTilesetAnimationDefinition animation)
    {
        int frameCount = Mathf.Max(1, animation.frameCount);
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < frameCount; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            int gridSize = MapEditorManager.NormalizePngPaletteGridSize(
                animation.GetFrameGridSize(tileset.atlasGridSize));
            builder.Append(ToSourceGridTileId(gridSize, animation.GetFrameTileId(i)));
        }

        return builder.ToString();
    }

    private static int ToSourceTileId(MapEditorTilesetDefinition tileset, int atlasTileId)
    {
        int gridSize = Mathf.Max(1, tileset.atlasGridSize);
        int atlasRowFromBottom = Mathf.Max(0, atlasTileId / gridSize);
        int column = Mathf.Max(0, atlasTileId % gridSize);
        int sourceRowFromTop = Mathf.Max(0, gridSize - 1 - atlasRowFromBottom);
        return sourceRowFromTop * Mathf.Max(1, tileset.columns) + column;
    }

    private static int ToAtlasTileId(MapEditorTilesetDefinition tileset, int sourceTileId)
    {
        int sourceColumns = Mathf.Max(1, tileset.columns);
        int sourceRowFromTop = Mathf.Max(0, sourceTileId / sourceColumns);
        int sourceColumn = Mathf.Max(0, sourceTileId % sourceColumns);
        int atlasGridSize = Mathf.Max(1, tileset.atlasGridSize);
        int atlasRowFromBottom = Mathf.Max(0, atlasGridSize - 1 - sourceRowFromTop);
        return atlasRowFromBottom * atlasGridSize + sourceColumn;
    }

    private static int ToGridTileId(int gridSize, int sourceTileId)
    {
        int sourceRowFromTop = Mathf.Max(0, sourceTileId / gridSize);
        int sourceColumn = Mathf.Max(0, sourceTileId % gridSize);
        int rowFromBottom = Mathf.Max(0, gridSize - 1 - sourceRowFromTop);
        return rowFromBottom * gridSize + sourceColumn;
    }

    private static int ToSourceGridTileId(int gridSize, int gridTileId)
    {
        int rowFromBottom = Mathf.Max(0, gridTileId / gridSize);
        int column = Mathf.Max(0, gridTileId % gridSize);
        int sourceRowFromTop = Mathf.Max(0, gridSize - 1 - rowFromBottom);
        return sourceRowFromTop * gridSize + column;
    }

    private static string LocalizeServiceError(string error)
    {
        if (MapEditorLocalization.Current != MapEditorLanguage.Korean || string.IsNullOrEmpty(error))
        {
            return error;
        }

        if (error.Contains("already used")) return "다른 애니메이션이 이미 사용하는 프레임입니다.";
        if (error.Contains("outside")) return "타일셋 범위를 벗어난 프레임 번호가 있습니다.";
        if (error.Contains("same frame")) return "같은 프레임 번호를 중복 입력할 수 없습니다.";
        if (error.Contains("frame count")) return "프레임은 2~32개여야 합니다.";
        if (error.Contains("speed")) return "FPS는 1~30 사이여야 합니다.";
        return error;
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

    private void CloseImmediately()
    {
        gameObject.SetActive(false);
        MapEditorObjectUtility.DestroyObject(gameObject);
    }

    private static void CreateSelectorRow(
        Transform parent,
        string name,
        string label,
        float y,
        out Text valueText,
        UnityEngine.Events.UnityAction previous,
        UnityEngine.Events.UnityAction next)
    {
        CreateLabel(parent, name + "Label", label, 12, FontStyle.Bold,
            new Vector2(18f, y), new Vector2(110f, 30f), TextAnchor.MiddleLeft);

        GameObject valueBox = CreateUiObject(name + "Value", parent, typeof(Image));
        RectTransform valueRect = valueBox.GetComponent<RectTransform>();
        ConfigureTopLeft(valueRect, new Vector2(130f, y), new Vector2(280f, 30f));
        valueBox.GetComponent<Image>().color = new Color(0.045f, 0.055f, 0.065f, 1f);
        valueText = CreateLabel(valueBox.transform, "Text", string.Empty, 12, FontStyle.Normal,
            new Vector2(8f, 0f), new Vector2(264f, 30f), TextAnchor.MiddleLeft);

        CreateButton(parent, name + "Previous", "<", new Vector2(418f, y), new Vector2(52f, 30f), previous, false, false);
        CreateButton(parent, name + "Next", ">", new Vector2(478f, y), new Vector2(52f, 30f), next, false, false);
    }

    private static InputField CreateLabeledInput(
        Transform parent,
        string name,
        string label,
        float y,
        string placeholderValue)
    {
        CreateLabel(parent, name + "Label", label, 12, FontStyle.Bold,
            new Vector2(18f, y), new Vector2(120f, 30f), TextAnchor.MiddleLeft);

        GameObject inputObject = CreateUiObject(name + "Input", parent, typeof(Image), typeof(InputField));
        RectTransform inputRect = inputObject.GetComponent<RectTransform>();
        ConfigureTopLeft(inputRect, new Vector2(150f, y), new Vector2(462f, 30f));
        Image background = inputObject.GetComponent<Image>();
        background.color = new Color(0.045f, 0.055f, 0.065f, 1f);

        Text inputText = CreateLabel(inputObject.transform, "Text", string.Empty, 12, FontStyle.Normal,
            Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
        StretchText(inputText.rectTransform, 8f, 8f);
        Text placeholder = CreateLabel(inputObject.transform, "Placeholder", placeholderValue, 12, FontStyle.Italic,
            Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
        StretchText(placeholder.rectTransform, 8f, 8f);
        placeholder.color = new Color(0.55f, 0.58f, 0.62f, 0.8f);

        InputField input = inputObject.GetComponent<InputField>();
        input.targetGraphic = background;
        input.textComponent = inputText;
        input.placeholder = placeholder;
        input.lineType = InputField.LineType.SingleLine;
        return input;
    }

    private static Toggle CreateToggle(Transform parent, string label, Vector2 position)
    {
        GameObject toggleObject = CreateUiObject("LoopToggle", parent, typeof(Toggle));
        RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
        ConfigureTopLeft(toggleRect, position, new Vector2(170f, 30f));

        GameObject box = CreateUiObject("Background", toggleObject.transform, typeof(Image));
        RectTransform boxRect = box.GetComponent<RectTransform>();
        ConfigureTopLeft(boxRect, Vector2.zero, new Vector2(26f, 26f));
        Image boxImage = box.GetComponent<Image>();
        boxImage.color = new Color(0.045f, 0.055f, 0.065f, 1f);

        GameObject check = CreateUiObject("Checkmark", box.transform, typeof(Image));
        RectTransform checkRect = check.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkRect.pivot = new Vector2(0.5f, 0.5f);
        checkRect.sizeDelta = new Vector2(16f, 16f);
        check.GetComponent<Image>().color = new Color(0.18f, 0.48f, 0.95f, 1f);

        CreateLabel(toggleObject.transform, "Label", label, 12, FontStyle.Bold,
            new Vector2(34f, 0f), new Vector2(136f, 26f), TextAnchor.MiddleLeft);

        Toggle toggle = toggleObject.GetComponent<Toggle>();
        toggle.targetGraphic = boxImage;
        toggle.graphic = check.GetComponent<Image>();
        return toggle;
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
        else if (position.y >= 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
        }
        else
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
        }

        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = buttonObject.GetComponent<Image>();
        image.color = danger
            ? new Color(0.65f, 0.18f, 0.2f, 1f)
            : accent
                ? new Color(0.18f, 0.48f, 0.95f, 1f)
                : new Color(0.23f, 0.25f, 0.28f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        Text text = CreateLabel(buttonObject.transform, "Text", label, 12, FontStyle.Bold,
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
        FontStyle fontStyle,
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
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
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

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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

    private static string L(string korean, string english)
    {
        return MapEditorLocalization.Choose(korean, english);
    }
}

public sealed class MapEditorAnimationFrameDragItem : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    public MapEditorAnimationTileWindow window;
    public int tileId;
    private CanvasGroup canvasGroup;

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0.65f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        GameObject targetObject = eventData.pointerCurrentRaycast.gameObject;
        MapEditorAnimationFrameDragItem target = targetObject == null
            ? null
            : targetObject.GetComponentInParent<MapEditorAnimationFrameDragItem>();
        if (window != null && target != null && target != this)
        {
            window.ReorderFrameTile(tileId, target.tileId);
        }
    }
}
