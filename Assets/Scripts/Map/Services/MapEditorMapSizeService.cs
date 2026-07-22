using System;
using UnityEngine;

public sealed class MapEditorMapSizeService
{
    public MapData CreateNewMap(MapEditorManager manager, int width, int height, Action clearSelection, Action clearHistory, Action regenerateGrid)
    {
        manager.mapWidth = Mathf.Max(1, width);
        manager.mapHeight = Mathf.Max(1, height);
        clearSelection?.Invoke();

        MapData mapData = new MapData(manager.mapWidth, manager.mapHeight);
        clearHistory?.Invoke();
        regenerateGrid?.Invoke();
        return mapData;
    }

    public bool TryResizeMap(
        MapEditorManager manager,
        MapData currentMapData,
        int width,
        int height,
        int maxMapSize,
        Action clearSelection,
        Action clearHistory,
        Action regenerateGrid,
        Action refreshMinimap,
        out MapData resizedMapData)
    {
        int nextWidth = Mathf.Clamp(width, 1, maxMapSize);
        int nextHeight = Mathf.Clamp(height, 1, maxMapSize);

        if (currentMapData == null)
        {
            resizedMapData = CreateNewMap(manager, nextWidth, nextHeight, clearSelection, clearHistory, regenerateGrid);
            return true;
        }

        if (currentMapData.width == nextWidth && currentMapData.height == nextHeight)
        {
            resizedMapData = currentMapData;
            return false;
        }

        clearSelection?.Invoke();
        resizedMapData = currentMapData.Resize(nextWidth, nextHeight);
        manager.mapWidth = resizedMapData.width;
        manager.mapHeight = resizedMapData.height;
        clearHistory?.Invoke();
        regenerateGrid?.Invoke();
        refreshMinimap?.Invoke();
        return true;
    }

    public void EnsureMapContainsRect(MapEditorManager manager, Vector2Int topLeft, int width, int height, Action<int, int> resizeMap)
    {
        int requiredWidth = Mathf.Clamp(topLeft.x + Mathf.Max(1, width), 1, MapEditorManager.MaxMapSize);
        int requiredHeight = Mathf.Clamp(topLeft.y + Mathf.Max(1, height), 1, MapEditorManager.MaxMapSize);

        if (requiredWidth > manager.mapWidth || requiredHeight > manager.mapHeight)
        {
            resizeMap?.Invoke(Mathf.Max(manager.mapWidth, requiredWidth), Mathf.Max(manager.mapHeight, requiredHeight));
        }
    }
}
