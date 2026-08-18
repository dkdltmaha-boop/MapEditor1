using UnityEngine;
using UnityEngine.UI;

public static class MapEditorWorkshopPreviewPanelBuilder
{
    private const string PanelName = "MapEditor_WorkshopPreviewPanel";
    private const float Width = MapEditorMapSizePanelBuilder.PanelWidth;
    private const float Height = 170f;

    public static void Ensure(MapEditorManager manager, Vector2 toolbarOffset)
    {
        Canvas canvas = MapEditorSceneUiBuilder.FindEditorCanvas();
        if (canvas == null || manager == null) return;

        Transform existing = canvas.transform.Find(PanelName);
        GameObject panelObject = existing == null
            ? new GameObject(PanelName, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup))
            : existing.gameObject;
        if (existing == null) panelObject.transform.SetParent(canvas.transform, false);

        ConfigurePanel(panelObject.transform, toolbarOffset);
        ClearChildren(panelObject.transform);
        CreateTitle(panelObject.transform);
        CreateCaptureButton(panelObject.transform, manager);
        CreatePreviewList(panelObject.transform, manager);
        panelObject.transform.SetAsLastSibling();
        MapEditorSceneUiBuilder.BringQuitButtonToFront();
    }

    public static void RefreshLayout(Vector2 toolbarOffset)
    {
        Canvas canvas = MapEditorSceneUiBuilder.FindEditorCanvas();
        Transform panel = canvas == null ? null : canvas.transform.Find(PanelName);
        if (panel != null) ConfigurePanel(panel, toolbarOffset);
    }

    private static void ConfigurePanel(Transform panel, Vector2 toolbarOffset)
    {
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        Vector2 layerPosition = MapEditorMapSizePanelBuilder.GetLayerPanelPosition(toolbarOffset);
        Vector2 position = layerPosition + new Vector2(0f, -(250f + MapEditorMapSizePanelBuilder.PanelGap));
        RectTransform parent = panel.parent as RectTransform;
        float panelHeight = Height;
        if (parent != null)
        {
            panelHeight = Mathf.Clamp(parent.rect.height + position.y - 8f, 76f, Height);
            position.x = Mathf.Clamp(position.x, -parent.rect.width + Width + 8f, -Width - 12f);
        }
        rect.sizeDelta = new Vector2(Width, panelHeight);
        rect.anchoredPosition = position;

        Image image = panel.GetComponent<Image>();
        image.color = new Color(0.13f, 0.13f, 0.13f, 0.92f);
        image.raycastTarget = false;
        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(5, 5, 5, 5);
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private static void CreateTitle(Transform parent)
    {
        Text text = CreateText(parent, "창작마당 프리뷰", 12, 18f);
        text.fontStyle = FontStyle.Bold;
    }

    private static void CreateCaptureButton(Transform parent, MapEditorManager manager)
    {
        GameObject buttonObject = new GameObject("AddPreviewButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 24f);
        Image image = buttonObject.GetComponent<Image>();
        image.color = manager.IsPreviewRegionToolActive()
            ? new Color(0.18f, 0.48f, 0.95f, 1f)
            : new Color(0.22f, 0.35f, 0.5f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(manager.SetPreviewRegionTool);
        Text label = CreateText(buttonObject.transform, "+ 프리뷰 촬영 (맵에서 드래그)", 10, 0f);
        Stretch(label.rectTransform);
        label.alignment = TextAnchor.MiddleCenter;
    }

    private static void CreatePreviewList(Transform parent, MapEditorManager manager)
    {
        GameObject viewportObject = new GameObject("PreviewList", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
        viewportObject.transform.SetParent(parent, false);
        float availableHeight = Mathf.Max(32f, ((RectTransform)parent).rect.height - 56f);
        viewportObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, availableHeight);
        viewportObject.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.06f, 0.9f);
        viewportObject.GetComponent<Mask>().showMaskGraphic = true;

        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewportObject.transform, false);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 3f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        contentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = viewportObject.GetComponent<ScrollRect>();
        scroll.viewport = viewportObject.GetComponent<RectTransform>();
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        if (manager.PreviewRegions.Count == 0)
        {
            Text empty = CreateText(contentObject.transform, "아직 촬영한 프리뷰가 없습니다.", 9, 34f);
            empty.alignment = TextAnchor.MiddleCenter;
            empty.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            return;
        }

        for (int i = 0; i < manager.PreviewRegions.Count; i++) CreatePreviewRow(contentObject.transform, manager, i);
    }

    private static void CreatePreviewRow(Transform parent, MapEditorManager manager, int index)
    {
        RectInt region = manager.PreviewRegions[index];
        GameObject row = new GameObject("Preview_" + index, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);
        row.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 48f);
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 4f;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        GameObject imageObject = new GameObject("Image", typeof(RectTransform), typeof(RawImage), typeof(MapEditorOwnedPreviewTexture));
        imageObject.transform.SetParent(row.transform, false);
        imageObject.GetComponent<RectTransform>().sizeDelta = new Vector2(78f, 44f);
        Texture2D texture = manager.CreateWorkshopPreviewTexture(region, 156, 88);
        imageObject.GetComponent<RawImage>().texture = texture;
        imageObject.GetComponent<MapEditorOwnedPreviewTexture>().texture = texture;

        Text info = CreateText(row.transform,
            (index == 0 ? "대표 " : "추가 ") + (index + 1) + "\n" + region.width + "×" + region.height,
            9, 44f);
        info.rectTransform.sizeDelta = new Vector2(54f, 44f);
        info.alignment = TextAnchor.MiddleLeft;

        GameObject deleteObject = new GameObject("Delete", typeof(RectTransform), typeof(Image), typeof(Button));
        deleteObject.transform.SetParent(row.transform, false);
        deleteObject.GetComponent<RectTransform>().sizeDelta = new Vector2(24f, 44f);
        deleteObject.GetComponent<Image>().color = new Color(0.55f, 0.2f, 0.2f, 1f);
        int capturedIndex = index;
        deleteObject.GetComponent<Button>().onClick.AddListener(() => manager.RemoveWorkshopPreview(capturedIndex));
        Text x = CreateText(deleteObject.transform, "×", 14, 0f);
        Stretch(x.rectTransform);
        x.alignment = TextAnchor.MiddleCenter;
    }

    private static Text CreateText(Transform parent, string value, int fontSize, float height)
    {
        GameObject obj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, height);
        Text text = obj.GetComponent<Text>();
        text.text = value;
        text.font = MapEditorFontProvider.Default;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            child.SetParent(null, false);
            MapEditorObjectUtility.DestroyObject(child.gameObject);
        }
    }
}

public sealed class MapEditorOwnedPreviewTexture : MonoBehaviour
{
    public Texture2D texture;
    private void OnDestroy()
    {
        if (texture != null) MapEditorObjectUtility.DestroyObject(texture);
    }
}
