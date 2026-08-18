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
        Configure(panel.transform);

        Transform textTransform = panel.transform.Find("Stats");
        Text text;
        if (textTransform == null)
        {
            GameObject textObject = new GameObject("Stats", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(panel.transform, false);
            text = textObject.GetComponent<Text>();
        }
        else text = textTransform.GetComponent<Text>();

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
        panel.GetComponent<MapEditorMapInfoPanel>().Configure(manager, text);
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
        image.color = new Color(0.09f, 0.09f, 0.09f, 0.94f);
        image.raycastTarget = false;
    }
}

public sealed class MapEditorMapInfoPanel : MonoBehaviour
{
    private MapEditorManager manager;
    private Text label;
    private float nextRefreshTime;
    private bool[] filledCells = System.Array.Empty<bool>();

    public void Configure(MapEditorManager target, Text text)
    {
        manager = target;
        label = text;
        Refresh();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshTime) return;
        nextRefreshTime = Time.unscaledTime + 0.5f;
        Refresh();
    }

    private void Refresh()
    {
        MapData map = manager == null ? null : manager.CurrentMapData;
        if (label == null || map == null) return;

        int ground = 0;
        int objects = 0;
        int wallVisual = 0;
        int collision = 0;
        int usedLayers = 0;
        int cellCount = map.width * map.height;
        if (filledCells.Length != cellCount) filledCells = new bool[cellCount];
        else System.Array.Clear(filledCells, 0, filledCells.Length);

        for (int canvasIndex = 0; canvasIndex < MapEditorLayerUtility.CanvasLayerCount; canvasIndex++)
        {
            if (!manager.IsCanvasEnabled(canvasIndex)) continue;
            ground += CountLayer(map, MapEditorLayerUtility.GetCanvasLayer(canvasIndex, MapEditorLayerType.Ground), filledCells, ref usedLayers);
            objects += CountLayer(map, MapEditorLayerUtility.GetCanvasLayer(canvasIndex, MapEditorLayerType.Object), filledCells, ref usedLayers);
            wallVisual += CountLayer(map, MapEditorLayerUtility.GetCanvasLayer(canvasIndex, MapEditorLayerType.WallVisual), filledCells, ref usedLayers);
        }
        collision = CountLayer(map, MapEditorLayerType.WallCollision, filledCells, ref usedLayers);

        int filled = 0;
        for (int i = 0; i < filledCells.Length; i++) if (filledCells[i]) filled++;
        float ratio = filledCells.Length == 0 ? 0f : filled * 100f / filledCells.Length;

        StringBuilder text = new StringBuilder(180);
        text.Append("Ground ").Append(ground)
            .Append("  |  Object ").Append(objects)
            .Append("  |  Wall Visual ").Append(wallVisual)
            .Append("  |  Collision ").Append(collision)
            .Append("  |  Spawn ").Append(manager.SpawnPointCount)
            .Append("  |  Layers ").Append(usedLayers)
            .Append("  |  Size ").Append(map.width).Append('×').Append(map.height)
            .Append("  |  Filled ").Append(ratio.ToString("0.0")).Append('%');
        label.text = text.ToString();
    }

    private static int CountLayer(MapData map, MapEditorLayerType layerType, bool[] filledCells, ref int usedLayers)
    {
        int layerIndex = (int)layerType;
        if (map.layerTiles == null || layerIndex < 0 || layerIndex >= map.layerTiles.Length) return 0;
        MapLayerTileData layer = map.layerTiles[layerIndex];
        if (layer?.tiles == null) return 0;
        int count = 0;
        for (int i = 0; i < layer.tiles.Length; i++)
        {
            if (layer.tiles[i] == -1) continue;
            count++;
            if (i < filledCells.Length) filledCells[i] = true;
        }
        if (count > 0) usedLayers++;
        return count;
    }
}
