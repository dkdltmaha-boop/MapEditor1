using System;
using System.Collections.Generic;
using UnityEngine;

public class MapEditorMapEditingService
{
    private readonly Func<MapData> getMapData;
    private readonly Func<MapEditorLayerType> getActiveLayer;
    private readonly Func<MapEditorLayerType, bool> isLayerVisible;
    private readonly Dictionary<Vector2Int, GridCell> cells;
    private readonly Func<string, int, int, bool, bool, Sprite> getPngTileSprite;
    private readonly Action refreshMinimap;
    private readonly MapEditorEditHistoryService history = new MapEditorEditHistoryService();
    private readonly MapEditorCellRenderService cellRender;
    private readonly MapEditorClipboardEditService clipboardEdit;
    private readonly MapEditorPaintOperationService paintOperations;
    private bool suppressImmediateMinimapRefresh;

    public MapEditorMapEditingService(
        Func<MapData> getMapData,
        Func<MapEditorLayerType> getActiveLayer,
        Func<MapEditorLayerType, bool> isLayerVisible,
        Dictionary<Vector2Int, GridCell> cells,
        Func<string, int, int, bool, bool, Sprite> getPngTileSprite,
        Action refreshMinimap)
    {
        this.getMapData = getMapData;
        this.getActiveLayer = getActiveLayer;
        this.isLayerVisible = isLayerVisible;
        this.cells = cells;
        this.getPngTileSprite = getPngTileSprite;
        this.refreshMinimap = refreshMinimap;
        cellRender = new MapEditorCellRenderService(getPngTileSprite, isLayerVisible);
        clipboardEdit = new MapEditorClipboardEditService(getMapData, getActiveLayer, getPngTileSprite, BeginTransaction, CommitTransaction, SetCellTileWithLayer);
        paintOperations = new MapEditorPaintOperationService(SetCellTile);
    }

    public void ClearHistory()
    {
        history.Clear();
        paintOperations.Clear();
    }

    public void ClearPendingPaintGesture()
    {
        paintOperations.Clear();
    }

    public void BeginTransaction()
    {
        history.BeginTransaction();
    }

    public void CommitTransaction()
    {
        history.CommitTransaction(refreshMinimap);
    }

    public void PaintCell(GridCell cell, MapEditorPaintSelection selection)
    {
        paintOperations.PaintCell(cell, selection);
    }

    public void PaintSubPixel(GridCell cell, int pixelX, int pixelY, int resolution, Color color)
    {
        if (cell == null)
        {
            return;
        }

        SetCellSubPixel(cell.X, cell.Y, pixelX, pixelY, resolution, color, true);
    }

    public void PaintSubPixelArea(GridCell cell, int pixelX, int pixelY, int resolution, int brushSide, Color color)
    {
        if (cell == null)
        {
            return;
        }

        resolution = Mathf.Max(1, resolution);
        brushSide = Mathf.Clamp(brushSide, 1, resolution);
        int globalStartX = cell.X * resolution + pixelX;
        int globalStartY = cell.Y * resolution + pixelY;

        for (int offsetY = 0; offsetY < brushSide; offsetY++)
        {
            for (int offsetX = 0; offsetX < brushSide; offsetX++)
            {
                int globalX = globalStartX + offsetX;
                int globalY = globalStartY + offsetY;
                int mapX = Mathf.FloorToInt(globalX / (float)resolution);
                int mapY = Mathf.FloorToInt(globalY / (float)resolution);
                int localX = Mod(globalX, resolution);
                int localY = Mod(globalY, resolution);

                SetCellSubPixel(
                    mapX,
                    mapY,
                    localX,
                    localY,
                    resolution,
                    color,
                    true);
            }
        }
    }

    public void PaintSpriteAtSubPixel(GridCell cell, int pixelX, int pixelY, int pointerResolution, Sprite sprite)
    {
        if (cell == null || sprite == null || sprite.texture == null)
        {
            return;
        }

        const int targetResolution = 16;
        pointerResolution = Mathf.Max(1, pointerResolution);
        Rect spriteRect = sprite.textureRect;
        int spriteWidth = Mathf.Max(1, Mathf.RoundToInt(spriteRect.width));
        int spriteHeight = Mathf.Max(1, Mathf.RoundToInt(spriteRect.height));
        int startX = cell.X * targetResolution + Mathf.FloorToInt(pixelX * targetResolution / (float)pointerResolution);
        int startY = cell.Y * targetResolution + Mathf.FloorToInt(pixelY * targetResolution / (float)pointerResolution);

        for (int y = 0; y < spriteHeight; y++)
        {
            for (int x = 0; x < spriteWidth; x++)
            {
                int sourceX = Mathf.Clamp(Mathf.FloorToInt(spriteRect.x + x), Mathf.FloorToInt(spriteRect.xMin), Mathf.FloorToInt(spriteRect.xMax) - 1);
                int sourceY = Mathf.Clamp(Mathf.FloorToInt(spriteRect.y + spriteHeight - 1 - y), Mathf.FloorToInt(spriteRect.yMin), Mathf.FloorToInt(spriteRect.yMax) - 1);
                Color color = sprite.texture.GetPixel(sourceX, sourceY);

                if (color.a <= 0.01f)
                {
                    continue;
                }

                int globalX = startX + x;
                int globalY = startY + y;
                int mapX = Mathf.FloorToInt(globalX / (float)targetResolution);
                int mapY = Mathf.FloorToInt(globalY / (float)targetResolution);
                int localX = Mod(globalX, targetResolution);
                int localY = Mod(globalY, targetResolution);
                SetCellSubPixel(mapX, mapY, localX, localY, targetResolution, color, true);
            }
        }
    }

    public void EraseCell(GridCell cell, int brushSize)
    {
        paintOperations.EraseCell(cell, brushSize);
    }

    public void EraseLayerAssignment(GridCell cell, int brushSize)
    {
        MapData mapData = getMapData();

        if (cell == null || mapData == null)
        {
            return;
        }

        int startX = cell.X - brushSize / 2;
        int startY = cell.Y - brushSize / 2;

        for (int y = startY; y < startY + brushSize; y++)
        {
            for (int x = startX; x < startX + brushSize; x++)
            {
                EraseLayerAssignmentAt(mapData, x, y, getActiveLayer());
            }
        }
    }

    private void EraseLayerAssignmentAt(MapData mapData, int x, int y, MapEditorLayerType sourceLayer)
    {
        if (!mapData.IsInside(x, y) || mapData.GetTile(x, y, sourceLayer) == -1)
        {
            return;
        }

        if (sourceLayer == MapEditorLayerType.WallCollision || sourceLayer == MapEditorLayerType.Zone)
        {
            ApplySnapshot(x, y, sourceLayer, CreateEmptySnapshot(sourceLayer), true);
            return;
        }

        MapEditorLayerType? destinationLayer = GetLayerEraseDestination(sourceLayer);

        if (!destinationLayer.HasValue)
        {
            Debug.LogWarning("기본 Ground 레이어의 소속은 제거할 수 없습니다. 타일을 삭제하려면 브러시 지우개를 사용하세요.");
            return;
        }

        if (mapData.GetTile(x, y, destinationLayer.Value) != -1)
        {
            Debug.LogWarning("아래 레이어에 타일이 있어 현재 타일의 레이어 소속을 안전하게 제거할 수 없습니다.");
            return;
        }

        MapEditorTileSnapshot source = CaptureSnapshot(mapData, x, y, sourceLayer);
        ApplySnapshot(x, y, destinationLayer.Value, source, true);
        ApplySnapshot(x, y, sourceLayer, CreateEmptySnapshot(sourceLayer), true);
    }

    private static MapEditorLayerType? GetLayerEraseDestination(MapEditorLayerType sourceLayer)
    {
        if (MapEditorLayerUtility.IsOptional(sourceLayer))
        {
            return MapEditorLayerUtility.GetBaseLayer(sourceLayer);
        }

        switch (sourceLayer)
        {
            case MapEditorLayerType.WallVisual:
                return MapEditorLayerType.Object;
            case MapEditorLayerType.Object:
                return MapEditorLayerType.Ground;
            default:
                return null;
        }
    }

    private static MapEditorTileSnapshot CreateEmptySnapshot(MapEditorLayerType layer)
    {
        return new MapEditorTileSnapshot(-1, Color.white, string.Empty, -1, 0, false, false, layer);
    }

    public void ClearLayer(MapEditorLayerType layerType)
    {
        MapData mapData = getMapData();

        if (mapData == null)
        {
            return;
        }

        BeginTransaction();

        for (int y = 0; y < mapData.height; y++)
        {
            for (int x = 0; x < mapData.width; x++)
            {
                if (mapData.GetTile(x, y, layerType) == -1)
                {
                    continue;
                }

                SetCellTileWithLayer(
                    x,
                    y,
                    -1,
                    Color.white,
                    null,
                    string.Empty,
                    -1,
                    0,
                    false,
                    false,
                    layerType,
                    true);
            }
        }

        CommitTransaction();
    }

    public void HandleAreaFill(GridCell cell, MapEditorPaintSelection selection)
    {
        paintOperations.HandleAreaFill(cell, selection, BeginTransaction, CommitTransaction);
    }

    public bool TryGetAreaFillRect(GridCell currentCell, out RectInt areaRect)
    {
        return paintOperations.TryGetAreaFillRect(currentCell, out areaRect);
    }

    public MapEditorClipboard CopyRect(RectInt rect)
    {
        return clipboardEdit.CopyRect(rect);
    }

    public void ClearRect(RectInt rect)
    {
        clipboardEdit.ClearRect(rect);
    }

    public void PasteClipboard(Vector2Int topLeft, MapEditorClipboard clipboard)
    {
        clipboardEdit.PasteClipboard(topLeft, clipboard);
    }

    public bool MoveConnectedCells(IReadOnlyCollection<Vector2Int> positions, MapEditorLayerType layer, Vector2Int offset)
    {
        MapData mapData = getMapData();

        if (mapData == null || positions == null || positions.Count == 0 || offset == Vector2Int.zero)
        {
            return false;
        }

        HashSet<Vector2Int> selected = new HashSet<Vector2Int>(positions);
        Dictionary<Vector2Int, MapEditorTileSnapshot> snapshots = new Dictionary<Vector2Int, MapEditorTileSnapshot>();

        foreach (Vector2Int source in selected)
        {
            Vector2Int destination = source + offset;

            if (!mapData.IsInside(destination.x, destination.y))
            {
                Debug.LogWarning("선택 영역이 맵 밖으로 나갈 수 없습니다.");
                return false;
            }

            if (!selected.Contains(destination) && mapData.GetTile(destination.x, destination.y, layer) != -1)
            {
                Debug.LogWarning("이동할 위치에 같은 레이어의 다른 타일이 있습니다.");
                return false;
            }

            snapshots[source] = CaptureSnapshot(mapData, source.x, source.y, layer);
        }

        BeginTransaction();

        foreach (Vector2Int source in selected)
        {
            ApplySnapshot(source.x, source.y, layer, new MapEditorTileSnapshot(-1, Color.white, string.Empty, -1), true);
        }

        foreach (KeyValuePair<Vector2Int, MapEditorTileSnapshot> item in snapshots)
        {
            Vector2Int destination = item.Key + offset;
            ApplySnapshot(destination.x, destination.y, layer, item.Value, true);
        }

        CommitTransaction();
        return true;
    }

    public void Undo()
    {
        suppressImmediateMinimapRefresh = true;

        try
        {
            history.Undo(SetCellTileWithoutUndo, refreshMinimap);
        }
        finally
        {
            suppressImmediateMinimapRefresh = false;
        }
    }

    public void Redo()
    {
        suppressImmediateMinimapRefresh = true;

        try
        {
            history.Redo(SetCellTileWithoutUndo, refreshMinimap);
        }
        finally
        {
            suppressImmediateMinimapRefresh = false;
        }
    }

    public void RefreshCell(GridCell cell)
    {
        cellRender.RefreshCell(cell, getMapData());
    }

    public void RefreshAllCells()
    {
        cellRender.RefreshAllCells(cells, getMapData());
        refreshMinimap();
    }

    public Color GetPreviewColor(int x, int y)
    {
        return cellRender.GetPreviewColor(getMapData(), x, y);
    }

    private void SetCellTile(int x, int y, int tileId, Color color, Sprite sprite, string imagePath, int imageIndex, int imageRotation, bool imageFlipX, bool imageFlipY, bool recordUndo)
    {
        MapEditorLayerType layer = getActiveLayer();
        SetCellTileWithLayer(x, y, tileId, color, sprite, imagePath, imageIndex, imageRotation, imageFlipX, imageFlipY, layer, recordUndo);
    }

    private void SetCellTileWithLayer(int x, int y, int tileId, Color color, Sprite sprite, string imagePath, int imageIndex, int imageRotation, bool imageFlipX, bool imageFlipY, MapEditorLayerType layer, bool recordUndo)
    {
        MapData mapData = getMapData();

        if (mapData == null || !mapData.IsInside(x, y))
        {
            return;
        }

        MapEditorLayerType afterLayer = layer;
        int beforeTileId = mapData.GetTile(x, y, afterLayer);
        Color beforeColor = mapData.GetColor(x, y, afterLayer);
        string beforeImagePath = mapData.GetImagePath(x, y, afterLayer);
        int beforeImageIndex = mapData.GetImageIndex(x, y, afterLayer);
        int beforeImageRotation = mapData.GetImageRotation(x, y, afterLayer);
        bool beforeImageFlipX = mapData.GetImageFlipX(x, y, afterLayer);
        bool beforeImageFlipY = mapData.GetImageFlipY(x, y, afterLayer);
        MapEditorLayerType beforeLayer = afterLayer;
        MapTilePixelData beforePixelData = mapData.GetPixelData(x, y, afterLayer)?.Clone();
        Sprite beforeSprite = null;

        if (cells.TryGetValue(new Vector2Int(x, y), out GridCell existingCell))
        {
            beforeSprite = existingCell.CurrentSprite;
        }

        if (beforeTileId == tileId && beforeColor == color && beforeSprite == sprite && beforeImagePath == imagePath && beforeImageIndex == imageIndex && beforeImageRotation == imageRotation && beforeImageFlipX == imageFlipX && beforeImageFlipY == imageFlipY && beforeLayer == afterLayer)
        {
            return;
        }

        mapData.SetTileOnLayer(x, y, afterLayer, tileId, color, imagePath, imageIndex, imageRotation, imageFlipX, imageFlipY);

        RefreshCellAndNeighbors(mapData, x, y);

        if (recordUndo)
        {
            TileEditAction action = new TileEditAction(x, y, beforeTileId, tileId, beforeColor, color, beforeSprite, sprite, beforeImagePath, imagePath, beforeImageIndex, imageIndex, beforeImageRotation, imageRotation, beforeImageFlipX, imageFlipX, beforeImageFlipY, imageFlipY, beforeLayer, afterLayer, beforePixelData, mapData.GetPixelData(x, y, afterLayer));

            history.Record(action);
        }

        if (!history.HasActiveTransaction && !suppressImmediateMinimapRefresh)
        {
            refreshMinimap();
        }
    }

    private void SetCellTileWithoutUndo(TileEditAction action, bool useAfter)
    {
        if (useAfter)
        {
            SetCellTileWithLayer(action.x, action.y, action.afterTileId, action.afterColor, action.afterSprite, action.afterImagePath, action.afterImageIndex, action.afterImageRotation, action.afterImageFlipX, action.afterImageFlipY, action.afterLayer, false);
            getMapData().SetPixelDataOnLayer(action.x, action.y, action.afterLayer, action.afterPixelData);
            RefreshCellAndNeighbors(getMapData(), action.x, action.y);
            return;
        }

        SetCellTileWithLayer(action.x, action.y, action.beforeTileId, action.beforeColor, action.beforeSprite, action.beforeImagePath, action.beforeImageIndex, action.beforeImageRotation, action.beforeImageFlipX, action.beforeImageFlipY, action.beforeLayer, false);
        getMapData().SetPixelDataOnLayer(action.x, action.y, action.beforeLayer, action.beforePixelData);
        RefreshCellAndNeighbors(getMapData(), action.x, action.y);
    }

    private MapEditorTileSnapshot CaptureSnapshot(MapData mapData, int x, int y, MapEditorLayerType layer)
    {
        return new MapEditorTileSnapshot(
            mapData.GetTile(x, y, layer),
            mapData.GetColor(x, y, layer),
            mapData.GetImagePath(x, y, layer),
            mapData.GetImageIndex(x, y, layer),
            mapData.GetImageRotation(x, y, layer),
            mapData.GetImageFlipX(x, y, layer),
            mapData.GetImageFlipY(x, y, layer),
            layer,
            mapData.GetPixelData(x, y, layer));
    }

    private void ApplySnapshot(int x, int y, MapEditorLayerType layer, MapEditorTileSnapshot snapshot, bool recordUndo)
    {
        MapData mapData = getMapData();

        if (mapData == null || !mapData.IsInside(x, y))
        {
            return;
        }

        MapEditorTileSnapshot before = CaptureSnapshot(mapData, x, y, layer);
        Sprite beforeSprite = GetSnapshotSprite(before);
        mapData.SetTileOnLayer(x, y, layer, snapshot.tileId, snapshot.color, snapshot.imagePath, snapshot.imageIndex, snapshot.imageRotation, snapshot.imageFlipX, snapshot.imageFlipY);
        mapData.SetPixelDataOnLayer(x, y, layer, snapshot.pixelData);
        MapEditorTileSnapshot after = CaptureSnapshot(mapData, x, y, layer);
        Sprite afterSprite = GetSnapshotSprite(after);
        RefreshCellAndNeighbors(mapData, x, y);

        if (recordUndo)
        {
            history.Record(new TileEditAction(
                x, y,
                before.tileId, after.tileId,
                before.color, after.color,
                beforeSprite, afterSprite,
                before.imagePath, after.imagePath,
                before.imageIndex, after.imageIndex,
                before.imageRotation, after.imageRotation,
                before.imageFlipX, after.imageFlipX,
                before.imageFlipY, after.imageFlipY,
                layer, layer,
                before.pixelData, after.pixelData));
        }
    }

    private Sprite GetSnapshotSprite(MapEditorTileSnapshot snapshot)
    {
        return snapshot.tileId == MapEditorManager.CustomImageTileId || snapshot.tileId == MapEditorManager.WallTileId
            ? getPngTileSprite(snapshot.imagePath, snapshot.imageIndex, snapshot.imageRotation, snapshot.imageFlipX, snapshot.imageFlipY)
            : null;
    }

    private void SetCellSubPixel(int x, int y, int pixelX, int pixelY, int resolution, Color color, bool recordUndo)
    {
        MapData mapData = getMapData();

        if (mapData == null || !mapData.IsInside(x, y))
        {
            return;
        }

        MapEditorLayerType afterLayer = getActiveLayer();
        int beforeTileId = mapData.GetTile(x, y, afterLayer);
        Color beforeColor = mapData.GetColor(x, y, afterLayer);
        string beforeImagePath = mapData.GetImagePath(x, y, afterLayer);
        int beforeImageIndex = mapData.GetImageIndex(x, y, afterLayer);
        int beforeImageRotation = mapData.GetImageRotation(x, y, afterLayer);
        bool beforeImageFlipX = mapData.GetImageFlipX(x, y, afterLayer);
        bool beforeImageFlipY = mapData.GetImageFlipY(x, y, afterLayer);
        MapEditorLayerType beforeLayer = afterLayer;
        MapTilePixelData beforePixelData = mapData.GetPixelData(x, y, afterLayer)?.Clone();
        Sprite beforeSprite = null;

        if (cells.TryGetValue(new Vector2Int(x, y), out GridCell existingCell))
        {
            beforeSprite = existingCell.CurrentSprite;
        }

        bool convertedImageTile = false;

        if (beforePixelData == null
            && beforeImageIndex >= 0
            && !string.IsNullOrEmpty(beforeImagePath)
            && (beforeTileId == MapEditorManager.CustomImageTileId || beforeTileId == MapEditorManager.WallTileId))
        {
            Sprite sourceSprite = getPngTileSprite(beforeImagePath, beforeImageIndex, beforeImageRotation, beforeImageFlipX, beforeImageFlipY);
            MapTilePixelData sourcePixels = MapTilePixelData.CreateFromSprite(sourceSprite, resolution);

            if (sourcePixels != null)
            {
                mapData.SetPixelDataOnLayer(x, y, afterLayer, sourcePixels);
                convertedImageTile = true;
            }
        }

        mapData.PaintSubPixelOnLayer(x, y, afterLayer, pixelX, pixelY, resolution, color);
        MapTilePixelData afterPixelData = mapData.GetPixelData(x, y, afterLayer);

        if (!convertedImageTile
            && beforePixelData != null
            && afterPixelData != null
            && beforePixelData.resolution == afterPixelData.resolution
            && beforePixelData.GetPixel(pixelX, pixelY) == afterPixelData.GetPixel(pixelX, pixelY))
        {
            return;
        }

        RefreshCellAndNeighbors(mapData, x, y);

        if (recordUndo)
        {
            TileEditAction action = new TileEditAction(
                x,
                y,
                beforeTileId,
                mapData.GetTile(x, y, afterLayer),
                beforeColor,
                mapData.GetColor(x, y, afterLayer),
                beforeSprite,
                null,
                beforeImagePath,
                string.Empty,
                beforeImageIndex,
                -1,
                beforeImageRotation,
                0,
                beforeImageFlipX,
                false,
                beforeImageFlipY,
                false,
                beforeLayer,
                afterLayer,
                beforePixelData,
                afterPixelData
            );

            history.Record(action);
        }

        if (!history.HasActiveTransaction && !suppressImmediateMinimapRefresh)
        {
            refreshMinimap();
        }
    }

    private static int Mod(int value, int divisor)
    {
        int result = value % divisor;
        return result < 0 ? result + divisor : result;
    }

    private void RefreshCellAndNeighbors(MapData mapData, int x, int y)
    {
        RefreshRegisteredCell(mapData, x, y);
        RefreshRegisteredCell(mapData, x, y - 1);
        RefreshRegisteredCell(mapData, x + 1, y);
        RefreshRegisteredCell(mapData, x, y + 1);
        RefreshRegisteredCell(mapData, x - 1, y);
    }

    private void RefreshRegisteredCell(MapData mapData, int x, int y)
    {
        if (cells.TryGetValue(new Vector2Int(x, y), out GridCell cell))
        {
            cellRender.RefreshCell(cell, mapData);
        }
    }

}
