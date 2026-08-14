using System.Text;
using UnityEngine;
using UnityEngine.UI;

public static class MapEditorModalPanel
{
    private const string ObjectName = "MapEditor_ModalPanel";

    public static void ShowValidation(MapEditorManager manager, PixelChromaMapValidationReport report)
    {
        ShowValidation(manager, report, null);
    }

    public static void ShowValidation(
        MapEditorManager manager,
        PixelChromaMapValidationReport report,
        System.Action continueExport)
    {
        ShowValidation(
            manager,
            report,
            MapEditorLocalization.Choose("저장 위치 선택", "Choose Save Location"),
            continueExport);
    }

    public static void ShowValidation(
        MapEditorManager manager,
        PixelChromaMapValidationReport report,
        string continueLabel,
        System.Action continueExport)
    {
        if (report == null)
        {
            return;
        }

        StringBuilder body = new StringBuilder();
        AppendWorkshopRequirements(body);
        AppendSection(body, "합격 요소", "✓ ", report.passedChecks);
        AppendSection(body, "불합격 요소", "✕ ", report.errors);
        AppendSection(body, "권장 사항", "• ", report.warnings);

        body.AppendLine();
        body.Append("타일 ").Append(report.paintedTileCount)
            .Append("  |  바닥 ").Append(report.groundTileCount)
            .Append("  |  이동 가능 ").Append(report.walkableTileCount)
            .Append("  |  오브젝트 ").Append(report.objectTileCount)
            .Append("  |  Wall ").Append(report.wallTileCount)
            .Append("  |  ").Append(MapEditorLocalization.Choose("애니메이션 ", "Animation ")).Append(report.animatedTileCount)
            .Append("  |  스폰 ").Append(report.spawnPointCount)
            .AppendLine()
            .Append("열린 경계 ").Append(report.boundaryLeakCount)
            .Append("  |  도달 불가 ").Append(report.unreachableWalkableTileCount)
            .Append("  |  고립 바닥 ").Append(report.isolatedGroundTileCount)
            .AppendLine()
            .Append("프리뷰 ")
            .Append(report.previewRegionSet
                ? report.previewWidth + "×" + report.previewHeight + " / 표시 타일 " + report.previewPaintedCellCount
                : "미지정");

        string title = MapEditorLocalization.Choose(
            report.isValid ? "맵 검사 합격" : "맵 검사 불합격",
            report.isValid ? "Map Validation Passed" : "Map Validation Failed");
        Color accent = report.isValid
            ? new Color(0.22f, 0.72f, 0.38f, 1f)
            : new Color(0.92f, 0.3f, 0.26f, 1f);

        if (report.isValid && continueExport != null)
        {
            Show(
                manager,
                title,
                body.ToString(),
                accent,
                continueLabel,
                () =>
                {
                    CloseCurrent();
                    continueExport();
                },
                string.Empty,
                null);
            return;
        }

        Show(manager, title, body.ToString(), accent);
    }

    private static void AppendWorkshopRequirements(StringBuilder builder)
    {
        builder.AppendLine(MapEditorLocalization.Choose("창작마당 합격 조건", "Workshop Pass Requirements"));
        builder.AppendLine(MapEditorLocalization.Choose(
            "• 맵 크기 32×32 이상, 이동 가능한 바닥 64칸 이상",
            "• Map size at least 32×32 with at least 64 walkable cells"));
        builder.AppendLine(MapEditorLocalization.Choose(
            "• 바닥 면적 5% 이상, 열린 가장자리·고립·도달 불가 바닥 없음",
            "• At least 5% floor coverage with no open edges, isolated cells, or unreachable floor"));
        builder.AppendLine(MapEditorLocalization.Choose(
            "• 충돌벽 존재, 시작 위치가 바닥 위에 있고 서로 겹치지 않음",
            "• Collision walls exist and start points are valid, grounded, and unique"));
        builder.AppendLine(MapEditorLocalization.Choose(
            "• 사용하는 이미지·타일셋·애니메이션 데이터를 정상적으로 읽을 수 있음",
            "• Referenced images, tilesets, and animation data are readable"));
        builder.AppendLine(MapEditorLocalization.Choose(
            "• 프리뷰 영역 4×4 이상, 맵 안에 있으며 표시 타일을 포함",
            "• Preview region is at least 4×4, inside the map, and contains visible tiles"));
        builder.AppendLine(MapEditorLocalization.Choose(
            "※ 불합격 요소가 하나라도 있으면 패키지 생성과 업로드가 중단됩니다. 권장 사항은 업로드를 막지 않습니다.",
            "Note: Any failed requirement blocks package creation and upload. Recommendations do not block upload."));
        builder.AppendLine();
        AppendWorkshopContentPolicy(builder);
    }

    private static void AppendWorkshopContentPolicy(StringBuilder builder)
    {
        builder.AppendLine(MapEditorLocalization.Choose(
            "PixelChroma Workshop 콘텐츠 정책",
            "PixelChroma Workshop Content Policy"));
        builder.AppendLine();
        builder.AppendLine(MapEditorLocalization.Choose(
            "• 본인이 제작했거나 사용할 권리가 있는 콘텐츠만 업로드할 수 있습니다.",
            "• Upload only content you created or have the right to use."));
        builder.AppendLine();
        builder.AppendLine(MapEditorLocalization.Choose(
            "• 타 게임에서 무단 추출한 에셋의 업로드를 금지합니다.",
            "• Uploading assets extracted from other games without permission is prohibited."));
        builder.AppendLine();
        builder.AppendLine(MapEditorLocalization.Choose(
            "• 타인의 저작권·상표권 등 권리를 침해하는 콘텐츠를 업로드할 수 없습니다.",
            "• Do not upload content that infringes copyrights, trademarks, or other rights."));
        builder.AppendLine();
        builder.AppendLine(MapEditorLocalization.Choose(
            "• 권리 침해 신고가 접수된 콘텐츠는 비공개 또는 삭제될 수 있습니다.",
            "• Reported infringing content may be hidden or removed."));
        builder.AppendLine();
    }

    private static void CloseCurrent()
    {
        Canvas canvas = MapEditorSceneUiBuilder.FindEditorCanvas();
        Transform existing = canvas == null ? null : canvas.transform.Find(ObjectName);

        if (existing != null)
        {
            MapEditorObjectUtility.DestroyObject(existing.gameObject);
        }
    }

    public static void ShowPackageGuide(MapEditorManager manager)
    {
        const string koreanGuide =
            "도구 선택\n" +
            "B  브러시                 L  직선\n" +
            "G  사각형 채우기\n" +
            "W  벽                     E  레이어 지우개\n" +
            "Shift+E  브러시 지우개\n" +
            "S  영역 선택              P  프리뷰 영역\n" +
            "P  프리뷰 영역 지정\n\n" +
            "그리기와 이미지\n" +
            "Shift + 클릭  같은 영역 채우기\n" +
            "[ / ]          브러시·지우개 크기 줄이기 / 키우기\n" +
            "R              선택 이미지 90도 회전\n" +
            "H / V          좌우 반전 / 상하 반전\n" +
            "I 또는 Space   마우스 아래 색상 선택\n" +
            "Ctrl + P       불러온 PNG를 맵에 붙여넣기\n\n" +
            "선택과 편집\n" +
            "S + 타일 클릭  같은 레이어의 연결된 타일 선택\n" +
            "방향키          선택한 타일을 한 칸 이동\n" +
            "Ctrl + C       선택 영역 복사\n" +
            "Ctrl + X       선택 영역 잘라내기\n" +
            "Ctrl + V       마우스 위치에 붙여넣기\n" +
            "Esc / Delete   선택 영역 해제\n" +
            "Ctrl + Z       실행 취소\n" +
            "Ctrl + Y       다시 실행\n\n" +
            "파일\n" +
            "Ctrl + S       편집용 맵 저장\n" +
            "Ctrl + L       편집용 맵 불러오기\n" +
            "F1             이 도움말 열기\n\n" +
            "화면 이동\n" +
            "마우스 휠       맵 확대 / 축소\n" +
            "휠 버튼 드래그  맵 화면 이동\n\n" +
            "창작마당 패키지\n" +
            "1. 검사 후 창작마당 업로드 버튼을 누릅니다.\n" +
            "2. 맵 검사에 합격하면 패키지를 만들고 Steam으로 전송합니다.\n" +
            "3. 완료 창의 게시물 열기로 업로드 결과를 확인합니다.\n\n" +
            "테스트 App ID 480은 Spacewar의 미등록 게시물로 업로드됩니다.\n" +
            "PixelChroma 창작마당에는 실제 게임 App ID 설정이 필요합니다.";
        const string englishGuide =
            "Tools\n" +
            "B  Brush                  W  Wall\n" +
            "E  Erase layer             S  Select area\n" +
            "P  Set preview area\n\n" +
            "Painting and images\n" +
            "Shift + Click  Fill connected area\n" +
            "[ / ]          Decrease / increase brush and eraser size\n" +
            "R              Rotate selection 90 degrees\n" +
            "H / V          Flip horizontal / vertical\n" +
            "I or Space     Pick the color under the cursor\n" +
            "Ctrl + P       Paste the loaded PNG onto the map\n\n" +
            "Selection and editing\n" +
            "S + tile click Select connected tiles on the active layer\n" +
            "Arrow keys     Move the connected selection by one tile\n" +
            "Ctrl + C / X / V  Copy / cut / paste\n" +
            "Esc / Delete     Clear selection\n" +
            "Ctrl + Z / Y     Undo / redo\n\n" +
            "Files and view\n" +
            "Ctrl + S / L     Save / load editable map\n" +
            "Mouse wheel      Zoom map\n" +
            "Middle drag      Pan map\n" +
            "F1               Open this help\n\n" +
            "Workshop package\n" +
            "1. Click Validate & Upload Workshop.\n" +
            "2. After validation passes, the package is sent to Steam.\n" +
            "3. Open the Workshop item from the completion dialog.\n\n" +
            "Test App ID 480 creates an unlisted Spacewar item.\n" +
            "The real game App ID is required for the PixelChroma Workshop.";

        Show(
            manager,
            MapEditorLocalization.Choose("맵 에디터 도움말", "Map Editor Help"),
            MapEditorLocalization.Choose(
                koreanGuide +
                "\n\n특수 레이어\n" +
                "시작 위치: 플레이어가 생성될 칸입니다. 시작 위치 도구로 다시 누르거나, 시작 위치 레이어에서 지우개를 사용하면 삭제됩니다.\n" +
                "플레이어 크기: 맵 크기 패널에서 PixelChroma 충돌 크기(1 x 1 타일) 가이드를 켤 수 있습니다.",
                englishGuide +
                "\n\nSpecial layers\n" +
                "Start Point: A player spawn cell. Click it again with the Start Point tool, or erase it on the Spawn layer.\n" +
                "Player Size: Toggle the PixelChroma 1 x 1 tile collision guide in the Map Size panel."),
            new Color(0.18f, 0.48f, 0.95f, 1f),
            MapEditorLocalization.Choose("Steam 창작마당 열기", "Open Steam Workshop"),
            manager == null ? (System.Action)null : manager.OpenSteamWorkshopPage,
            MapEditorLocalization.Choose("English", "한국어"),
            () =>
            {
                MapEditorLocalization.Toggle();
                manager?.RefreshLocalizedUi();
                ShowPackageGuide(manager);
            });
    }

    public static void Show(MapEditorManager manager, string title, string body, Color accentColor)
    {
        Show(manager, title, body, accentColor, string.Empty, null, string.Empty, null);
    }

    public static void ShowAction(
        MapEditorManager manager,
        string title,
        string body,
        Color accentColor,
        string actionLabel,
        System.Action action)
    {
        Show(manager, title, body, accentColor, actionLabel, action, string.Empty, null);
    }

    private static void Show(
        MapEditorManager manager,
        string title,
        string body,
        Color accentColor,
        string actionLabel,
        System.Action action,
        string secondaryActionLabel,
        System.Action secondaryAction)
    {
        Canvas canvas = manager == null ? null : manager.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = MapEditorSceneUiBuilder.FindEditorCanvas();
        }

        if (canvas == null)
        {
            Debug.LogWarning(title + "\n" + body);
            return;
        }

        Transform existing = canvas.transform.Find(ObjectName);
        if (existing != null)
        {
            MapEditorObjectUtility.DestroyObject(existing.gameObject);
        }

        GameObject root = new GameObject(ObjectName, typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(MapEditorUiTransition));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.58f);

        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(520f, 430f);
        panel.GetComponent<Image>().color = new Color(0.11f, 0.12f, 0.14f, 0.98f);

        CreateText(panel.transform, "Title", title, 20, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(18f, -14f), new Vector2(-72f, 46f));
        CreateAccent(panel.transform, accentColor);
        CreateScrollContent(panel.transform, body);
        MapEditorUiTransition transition = root.GetComponent<MapEditorUiTransition>();
        CreateCloseButton(transition, panel.transform);

        if (!string.IsNullOrEmpty(actionLabel) && action != null)
        {
            CreateFooterActionButton(panel.transform, actionLabel, action);
        }

        if (!string.IsNullOrEmpty(secondaryActionLabel) && secondaryAction != null)
        {
            CreateFooterActionButton(panel.transform, secondaryActionLabel, secondaryAction, 200f, 112f);
        }

        root.transform.SetAsLastSibling();
        transition.PlayIn(panelRect);
    }

    private static void CreateAccent(Transform panel, Color color)
    {
        GameObject accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
        accent.transform.SetParent(panel, false);
        RectTransform rect = accent.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, 4f);
        accent.GetComponent<Image>().color = color;
    }

    private static void CreateScrollContent(Transform panel, string body)
    {
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
        viewport.transform.SetParent(panel, false);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(18f, 58f);
        viewportRect.offsetMax = new Vector2(-18f, -66f);
        viewport.GetComponent<Image>().color = new Color(0.07f, 0.08f, 0.09f, 0.92f);
        viewport.GetComponent<Mask>().showMaskGraphic = true;

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(Text), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = new Vector2(0f, -10f);
        contentRect.sizeDelta = new Vector2(-24f, 0f);

        Text text = content.GetComponent<Text>();
        text.text = body;
        text.font = MapEditorFontProvider.Default;
        text.fontSize = 13;
        text.alignment = TextAnchor.UpperLeft;
        text.color = new Color(0.92f, 0.94f, 0.96f, 1f);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = viewport.GetComponent<ScrollRect>();
        scroll.content = contentRect;
        scroll.viewport = viewportRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = 24f;
    }

    private static void CreateCloseButton(MapEditorUiTransition transition, Transform panel)
    {
        GameObject buttonObject = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(panel, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-18f, 14f);
        rect.sizeDelta = new Vector2(92f, 32f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.24f, 0.26f, 0.3f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(transition.Close);

        CreateText(buttonObject.transform, "Text", MapEditorLocalization.Choose("닫기", "Close"), 13, FontStyle.Bold, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
    }

    private static void CreateFooterActionButton(
        Transform panel,
        string label,
        System.Action action,
        float x = 18f,
        float width = 174f)
    {
        GameObject buttonObject = new GameObject("FooterActionButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(panel, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(x, 14f);
        rect.sizeDelta = new Vector2(width, 32f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.18f, 0.48f, 0.95f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => action());

        CreateText(buttonObject.transform, "Text", label, 12, FontStyle.Bold, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
    }

    private static void CreateText(
        Transform parent,
        string objectName,
        string value,
        int fontSize,
        FontStyle fontStyle,
        TextAnchor alignment,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = objectName == "Title" ? new Vector2(0f, 1f) : Vector2.zero;
        rect.anchorMax = objectName == "Title" ? new Vector2(1f, 1f) : Vector2.one;
        rect.pivot = objectName == "Title" ? new Vector2(0f, 1f) : new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        rect.offsetMin = objectName == "Title" ? rect.offsetMin : Vector2.zero;
        rect.offsetMax = objectName == "Title" ? rect.offsetMax : Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.text = value;
        text.font = MapEditorFontProvider.Default;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    private static void AppendSection(StringBuilder builder, string title, string prefix, System.Collections.Generic.IReadOnlyList<string> messages)
    {
        builder.AppendLine(title);

        if (messages == null || messages.Count == 0)
        {
            builder.AppendLine("  없음");
        }
        else
        {
            for (int i = 0; i < messages.Count; i++)
            {
                builder.Append(prefix).AppendLine(messages[i]);
            }
        }

        builder.AppendLine();
    }
}
