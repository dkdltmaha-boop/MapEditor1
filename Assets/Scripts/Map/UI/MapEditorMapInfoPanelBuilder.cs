using System.Text;
using UnityEngine;
using UnityEngine.UI;

public static class MapEditorMapInfoPanelBuilder
{
    private const string PanelName = "MapEditor_MapInfoPanel";

    public static void Ensure(Transform canvas, MapEditorManager manager)
    {
        if (canvas == null || manager == null) return;
        Transform existing = canvas.Find(PanelName);
        GameObject panel = existing == null
            ? new GameObject(PanelName, typeof(RectTransform), typeof(Image), typeof(MapEditorMapInfoPanel))
            : existing.gameObject;
        if (existing == null) panel.transform.SetParent(canvas, false);

        Image panelImage = panel.GetComponent<Image>();
        if (panelImage == null) panelImage = panel.AddComponent<Image>();
        MapEditorMapInfoPanel infoPanel = panel.GetComponent<MapEditorMapInfoPanel>();
        if (infoPanel == null) infoPanel = panel.AddComponent<MapEditorMapInfoPanel>();
        Configure(panel.transform);

        Transform textTransform = panel.transform.Find("Stats");
        Text text;
        if (textTransform == null)
        {
            GameObject textObject = new GameObject("Stats", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(panel.transform, false);
            text = textObject.GetComponent<Text>();
        }
        else
        {
            text = textTransform.GetComponent<Text>();
            if (text == null) text = textTransform.gameObject.AddComponent<Text>();
        }

        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 0f);
        textRect.offsetMax = new Vector2(-10f, 0f);
        text.font = MapEditorFontProvider.Default;
        text.fontSize = 10;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        infoPanel.Configure(manager, text);
    }

    public static void RefreshLayout(Transform canvas)
    {
        Transform panel = canvas == null ? null : canvas.Find(PanelName);
        if (panel != null) Configure(panel);
    }

    private static void Configure(Transform panel)
    {
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(-90f, 8f);
        RectTransform parent = panel.parent as RectTransform;
        float width = parent == null ? 760f : Mathf.Clamp(parent.rect.width - 430f, 420f, 920f);
        rect.sizeDelta = new Vector2(width, 34f);
        Image image = panel.GetComponent<Image>();
        if (image == null) image = panel.gameObject.AddComponent<Image>();
        image.color = new Color(0.09f, 0.09f, 0.09f, 0.94f);
        image.raycastTarget = false;
    }
}
