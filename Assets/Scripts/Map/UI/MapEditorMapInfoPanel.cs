using System.Text;
using UnityEngine;
using UnityEngine.UI;

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
        int usedLayers = 0;
        int cellCount = map.width * map.height;
        if (filledCells.Length != cellCount) filledCells = new bool[cellCount];
        else System.Array.Clear(filledCells, 0, filledCells.Length);

        for (int canvasIndex = 0; canvasIndex < MapEditorLayerUtility.CanvasLayerCount; canvasIndex++)
        {
            if (!manager.IsCanvasEnabled(canvasIndex)) continue;
            ground += CountLayer(map, MapEditorLayerUtility.GetCanvasLayer(canvasIndex, MapEditorLayerType.Ground), filledCells, ref usedLayers);
        }
        int collision = CountLayer(map, MapEditorLayerType.WallCollision, filledCells, ref usedLayers);
        int filled = 0;
        for (int i = 0; i < filledCells.Length; i++) if (filledCells[i]) filled++;
        float ratio = filledCells.Length == 0 ? 0f : filled * 100f / filledCells.Length;

        StringBuilder text = new StringBuilder(220);
        text.Append(MapEditorLocalization.Choose("바닥 ", "Ground ")).Append(ground)
            .Append(MapEditorLocalization.Choose("  |  충돌 ", "  |  Collision ")).Append(collision)
            .Append(MapEditorLocalization.Choose("  |  스폰 지점 ", "  |  Spawn Points ")).Append(manager.SpawnPointCount)
            .Append(MapEditorLocalization.Choose("  |  사용 레이어 ", "  |  Used Layers ")).Append(usedLayers)
            .Append(MapEditorLocalization.Choose("  |  맵 크기 ", "  |  Map Size ")).Append(map.width).Append('×').Append(map.height)
            .Append(MapEditorLocalization.Choose("  |  채워진 비율 ", "  |  Filled ")).Append(ratio.ToString("0.0")).Append('%');
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
