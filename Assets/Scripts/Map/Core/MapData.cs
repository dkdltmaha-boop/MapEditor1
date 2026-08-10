using UnityEngine;

[System.Serializable]
public class MapData
{
    public int width;
    public int height;

    public int[,] tileMap;
    public Color[,] colorMap;
    public string[,] imagePathMap;
    public int[,] imageIndexMap;
    public int[,] imageRotationMap;
    public bool[,] imageFlipXMap;
    public bool[,] imageFlipYMap;
    public MapTilePixelData[,] pixelMap;
    public int[,] layerMap;
    public MapLayerTileData[] layerTiles;
    [System.NonSerialized] private bool initializationComplete;

    public MapData(int width, int height)
    {
        this.width = width;
        this.height = height;

        EnsureInitialized();
    }

    public void EnsureInitialized()
    {
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);

        if (initializationComplete)
        {
            return;
        }

        bool baseMapsValid = tileMap != null
            && colorMap != null
            && imagePathMap != null
            && imageIndexMap != null
            && imageRotationMap != null
            && imageFlipXMap != null
            && imageFlipYMap != null
            && tileMap.GetLength(0) == width
            && tileMap.GetLength(1) == height
            && colorMap.GetLength(0) == width
            && colorMap.GetLength(1) == height
            && imagePathMap.GetLength(0) == width
            && imagePathMap.GetLength(1) == height
            && imageIndexMap.GetLength(0) == width
            && imageIndexMap.GetLength(1) == height
            && imageRotationMap.GetLength(0) == width
            && imageRotationMap.GetLength(1) == height
            && imageFlipXMap.GetLength(0) == width
            && imageFlipXMap.GetLength(1) == height
            && imageFlipYMap.GetLength(0) == width
            && imageFlipYMap.GetLength(1) == height;

        if (baseMapsValid && (pixelMap == null || pixelMap.GetLength(0) != width || pixelMap.GetLength(1) != height))
        {
            pixelMap = new MapTilePixelData[width, height];
        }

        if (baseMapsValid && (layerMap == null || layerMap.GetLength(0) != width || layerMap.GetLength(1) != height))
        {
            layerMap = new int[width, height];
            InitializeMissingLayerMap();
        }

        EnsureLayerTiles(baseMapsValid);

        if (tileMap != null
            && colorMap != null
            && imagePathMap != null
            && imageIndexMap != null
            && imageRotationMap != null
            && imageFlipXMap != null
            && imageFlipYMap != null
            && pixelMap != null
            && layerMap != null
            && tileMap.GetLength(0) == width
            && tileMap.GetLength(1) == height
            && colorMap.GetLength(0) == width
            && colorMap.GetLength(1) == height
            && imagePathMap.GetLength(0) == width
            && imagePathMap.GetLength(1) == height
            && imageIndexMap.GetLength(0) == width
            && imageIndexMap.GetLength(1) == height
            && imageRotationMap.GetLength(0) == width
            && imageRotationMap.GetLength(1) == height
            && imageFlipXMap.GetLength(0) == width
            && imageFlipXMap.GetLength(1) == height
            && imageFlipYMap.GetLength(0) == width
            && imageFlipYMap.GetLength(1) == height
            && pixelMap.GetLength(0) == width
            && pixelMap.GetLength(1) == height
            && layerMap.GetLength(0) == width
            && layerMap.GetLength(1) == height
            && AreLayerTilesValid())
        {
            initializationComplete = true;
            return;
        }

        tileMap = new int[width, height];
        colorMap = new Color[width, height];
        imagePathMap = new string[width, height];
        imageIndexMap = new int[width, height];
        imageRotationMap = new int[width, height];
        imageFlipXMap = new bool[width, height];
        imageFlipYMap = new bool[width, height];
        pixelMap = new MapTilePixelData[width, height];
        layerMap = new int[width, height];

        initializationComplete = true;
        Clear();
    }

    public void SetTile(int x, int y, int tileId)
    {
        SetTile(x, y, tileId, Color.white);
    }

    public void SetTile(int x, int y, int tileId, Color color)
    {
        SetTile(x, y, tileId, color, string.Empty, -1);
    }

    public void SetTile(int x, int y, int tileId, Color color, string imagePath, int imageIndex)
    {
        SetTile(x, y, tileId, color, imagePath, imageIndex, 0, false, false);
    }

    public void SetTile(int x, int y, int tileId, Color color, string imagePath, int imageIndex, int imageRotation, bool imageFlipX, bool imageFlipY)
    {
        SetTileOnLayer(x, y, GetLayerForFlatWrite(x, y, tileId), tileId, color, imagePath, imageIndex, imageRotation, imageFlipX, imageFlipY);
    }

    public void SetTileOnLayer(int x, int y, MapEditorLayerType layerType, int tileId, Color color, string imagePath, int imageIndex, int imageRotation, bool imageFlipX, bool imageFlipY)
    {
        EnsureInitialized();

        if (!IsInside(x, y)) return;

        MapLayerTileData layer = GetLayerData(layerType);
        int index = ToIndex(x, y);

        layer.tiles[index] = tileId;
        layer.colors[index] = color;
        layer.imagePaths[index] = imagePath;
        layer.imageIndices[index] = imageIndex;
        layer.imageRotations[index] = MapEditorRotationUtility.NormalizeQuarterTurn(imageRotation);
        layer.imageFlipXs[index] = imageFlipX;
        layer.imageFlipYs[index] = imageFlipY;
        layer.pixelData[index] = null;

        SyncFlatCellFromLayers(x, y);
    }

    public int GetTile(int x, int y)
    {
        EnsureInitialized();

        if (!IsInside(x, y)) return -1;

        return tileMap[x, y];
    }

    public Color GetColor(int x, int y)
    {
        EnsureInitialized();

        if (!IsInside(x, y)) return Color.white;

        return colorMap[x, y];
    }

    public string GetImagePath(int x, int y)
    {
        EnsureInitialized();

        if (!IsInside(x, y)) return string.Empty;

        return imagePathMap[x, y];
    }

    public int GetImageIndex(int x, int y)
    {
        EnsureInitialized();

        if (!IsInside(x, y)) return -1;

        return imageIndexMap[x, y];
    }

    public int GetImageRotation(int x, int y)
    {
        EnsureInitialized();

        if (!IsInside(x, y)) return 0;

        return imageRotationMap[x, y];
    }

    public bool GetImageFlipX(int x, int y)
    {
        EnsureInitialized();

        if (!IsInside(x, y)) return false;

        return imageFlipXMap[x, y];
    }

    public bool GetImageFlipY(int x, int y)
    {
        EnsureInitialized();

        if (!IsInside(x, y)) return false;

        return imageFlipYMap[x, y];
    }

    public MapTilePixelData GetPixelData(int x, int y)
    {
        EnsureInitialized();

        if (!IsInside(x, y)) return null;

        return pixelMap[x, y];
    }

    public int GetTile(int x, int y, MapEditorLayerType layerType)
    {
        EnsureInitialized();

        if (!IsInside(x, y)) return -1;

        MapLayerTileData layer = TryGetLayerData(layerType);
        return layer == null ? -1 : layer.tiles[ToIndex(x, y)];
    }

    public Color GetColor(int x, int y, MapEditorLayerType layerType)
    {
        EnsureInitialized();

        if (!IsInside(x, y)) return Color.white;

        MapLayerTileData layer = TryGetLayerData(layerType);
        return layer == null ? Color.white : layer.colors[ToIndex(x, y)];
    }

    public string GetImagePath(int x, int y, MapEditorLayerType layerType)
    {
        EnsureInitialized();

        if (!IsInside(x, y)) return string.Empty;

        MapLayerTileData layer = TryGetLayerData(layerType);
        return layer == null ? string.Empty : layer.imagePaths[ToIndex(x, y)];
    }

    public int GetImageIndex(int x, int y, MapEditorLayerType layerType)
    {
        EnsureInitialized();

        if (!IsInside(x, y)) return -1;

        MapLayerTileData layer = TryGetLayerData(layerType);
        return layer == null ? -1 : layer.imageIndices[ToIndex(x, y)];
    }

    public int GetImageRotation(int x, int y, MapEditorLayerType layerType)
    {
        EnsureInitialized();

        if (!IsInside(x, y)) return 0;

        MapLayerTileData layer = TryGetLayerData(layerType);
        return layer == null ? 0 : layer.imageRotations[ToIndex(x, y)];
    }

    public bool GetImageFlipX(int x, int y, MapEditorLayerType layerType)
    {
        EnsureInitialized();

        if (!IsInside(x, y)) return false;

        MapLayerTileData layer = TryGetLayerData(layerType);
        return layer != null && layer.imageFlipXs[ToIndex(x, y)];
    }

    public bool GetImageFlipY(int x, int y, MapEditorLayerType layerType)
    {
        EnsureInitialized();

        if (!IsInside(x, y)) return false;

        MapLayerTileData layer = TryGetLayerData(layerType);
        return layer != null && layer.imageFlipYs[ToIndex(x, y)];
    }

    public MapTilePixelData GetPixelData(int x, int y, MapEditorLayerType layerType)
    {
        EnsureInitialized();

        if (!IsInside(x, y)) return null;

        MapLayerTileData layer = TryGetLayerData(layerType);
        return layer == null ? null : layer.pixelData[ToIndex(x, y)];
    }

    public MapEditorLayerType GetLayer(int x, int y)
    {
        EnsureInitialized();

        if (!IsInside(x, y)) return MapEditorLayerType.Ground;

        return (MapEditorLayerType)Mathf.Clamp(layerMap[x, y], 0, (int)MapEditorLayerUtility.LastLayer);
    }

    public void SetLayer(int x, int y, MapEditorLayerType layerType)
    {
        EnsureInitialized();

        if (!IsInside(x, y)) return;

        layerMap[x, y] = (int)layerType;
    }

    public void SetPixelData(int x, int y, MapTilePixelData pixelData)
    {
        SetPixelDataOnLayer(x, y, GetLayer(x, y), pixelData);
    }

    public void SetPixelDataOnLayer(int x, int y, MapEditorLayerType layerType, MapTilePixelData pixelData)
    {
        EnsureInitialized();

        if (!IsInside(x, y)) return;

        MapLayerTileData layer = GetLayerData(layerType);
        int index = ToIndex(x, y);
        layer.pixelData[index] = pixelData == null ? null : pixelData.Clone();

        if (pixelData != null)
        {
            if (layer.tiles[index] != MapEditorManager.WallTileId)
            {
                layer.tiles[index] = MapEditorManager.CustomColorTileId;
            }

            layer.colors[index] = pixelData.GetAverageColor();
            layer.imagePaths[index] = string.Empty;
            layer.imageIndices[index] = -1;
            layer.imageRotations[index] = 0;
            layer.imageFlipXs[index] = false;
            layer.imageFlipYs[index] = false;
        }

        SyncFlatCellFromLayers(x, y);
    }

    public void PaintSubPixel(int x, int y, int pixelX, int pixelY, int resolution, Color color)
    {
        PaintSubPixelOnLayer(x, y, GetLayer(x, y), pixelX, pixelY, resolution, color);
    }

    public void PaintSubPixelOnLayer(int x, int y, MapEditorLayerType layerType, int pixelX, int pixelY, int resolution, Color color)
    {
        EnsureInitialized();

        if (!IsInside(x, y)) return;

        resolution = MapEditorManager.NormalizeExportCellPixels(resolution);
        MapLayerTileData layer = GetLayerData(layerType);
        int index = ToIndex(x, y);
        MapTilePixelData pixelData = layer.pixelData[index];

        if (pixelData == null || pixelData.resolution != resolution)
        {
            Color baseColor = GetTile(x, y, layerType) == -1
                ? (layerType == MapEditorLayerType.Ground ? Color.white : Color.clear)
                : GetColor(x, y, layerType);
            pixelData = pixelData == null
                ? MapTilePixelData.CreateFilled(resolution, baseColor)
                : pixelData.Resample(resolution);
        }

        pixelData.SetPixel(pixelX, pixelY, color);
        SetPixelDataOnLayer(x, y, layerType, pixelData);
    }

    public void Clear()
    {
        EnsureInitialized();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                tileMap[x, y] = -1;
                colorMap[x, y] = Color.white;
                imagePathMap[x, y] = string.Empty;
                imageIndexMap[x, y] = -1;
                imageRotationMap[x, y] = 0;
                imageFlipXMap[x, y] = false;
                imageFlipYMap[x, y] = false;
                pixelMap[x, y] = null;
                layerMap[x, y] = (int)MapEditorLayerType.Ground;
            }
        }

        if (layerTiles != null)
        {
            for (int i = 0; i < layerTiles.Length; i++)
            {
                layerTiles[i]?.Clear();
            }
        }
    }

    public void ClearLayer(MapEditorLayerType layerType)
    {
        EnsureInitialized();
        TryGetLayerData(layerType)?.Clear();
        SyncAllFlatCellsFromLayers();
    }

    public void SwapLayers(MapEditorLayerType first, MapEditorLayerType second)
    {
        EnsureInitialized();
        int firstIndex = Mathf.Clamp((int)first, 0, (int)MapEditorLayerUtility.LastLayer);
        int secondIndex = Mathf.Clamp((int)second, 0, (int)MapEditorLayerUtility.LastLayer);

        if (firstIndex == secondIndex)
        {
            return;
        }

        MapLayerTileData firstData = layerTiles[firstIndex];
        layerTiles[firstIndex] = layerTiles[secondIndex];
        layerTiles[secondIndex] = firstData;
        if (layerTiles[firstIndex] != null) layerTiles[firstIndex].layer = firstIndex;
        if (layerTiles[secondIndex] != null) layerTiles[secondIndex].layer = secondIndex;
        SyncAllFlatCellsFromLayers();
    }

    public bool IsInside(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    public MapSaveData ToSaveData()
    {
        EnsureInitialized();

        MapSaveData saveData = new MapSaveData(width, height);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                saveData.tiles[index] = tileMap[x, y];
                saveData.colors[index] = colorMap[x, y];
                saveData.imagePaths[index] = imagePathMap[x, y];
                saveData.imageIndices[index] = imageIndexMap[x, y];
                saveData.imageRotations[index] = imageRotationMap[x, y];
                saveData.imageFlipXs[index] = imageFlipXMap[x, y];
                saveData.imageFlipYs[index] = imageFlipYMap[x, y];
                saveData.pixelData[index] = pixelMap[x, y] == null ? null : pixelMap[x, y].Clone();
                saveData.layers[index] = layerMap[x, y];
            }
        }

        saveData.layerTiles = CloneLayerTilesForSave();

        return saveData;
    }

    public static MapData FromSaveData(MapSaveData saveData)
    {
        MapData mapData = new MapData(saveData.width, saveData.height);

        for (int y = 0; y < saveData.height; y++)
        {
            for (int x = 0; x < saveData.width; x++)
            {
                int index = y * saveData.width + x;

                if (index < saveData.tiles.Length)
                {
                    Color color = Color.white;

                    if (saveData.colors != null && index < saveData.colors.Length)
                    {
                        color = saveData.colors[index];
                    }

                    string imagePath = string.Empty;
                    int imageIndex = -1;

                    if (saveData.imagePaths != null && index < saveData.imagePaths.Length)
                    {
                        imagePath = saveData.imagePaths[index];
                    }

                    if (saveData.imageIndices != null && index < saveData.imageIndices.Length)
                    {
                        imageIndex = saveData.imageIndices[index];
                    }

                    int imageRotation = 0;
                    bool imageFlipX = false;
                    bool imageFlipY = false;

                    if (saveData.imageRotations != null && index < saveData.imageRotations.Length)
                    {
                        imageRotation = saveData.imageRotations[index];
                    }

                    if (saveData.imageFlipXs != null && index < saveData.imageFlipXs.Length)
                    {
                        imageFlipX = saveData.imageFlipXs[index];
                    }

                    if (saveData.imageFlipYs != null && index < saveData.imageFlipYs.Length)
                    {
                        imageFlipY = saveData.imageFlipYs[index];
                    }

                    MapEditorLayerType savedLayer = GetSavedLayer(saveData, index, saveData.tiles[index]);
                    mapData.SetTileOnLayer(x, y, savedLayer, saveData.tiles[index], color, imagePath, imageIndex, imageRotation, imageFlipX, imageFlipY);

                    if (saveData.pixelData != null && index < saveData.pixelData.Length && saveData.pixelData[index] != null)
                    {
                        mapData.SetPixelDataOnLayer(x, y, savedLayer, saveData.pixelData[index]);
                    }
                }
            }
        }

        mapData.ApplySavedLayerTiles(saveData.layerTiles);

        return mapData;
    }

    public MapData Resize(int newWidth, int newHeight)
    {
        EnsureInitialized();

        MapData resized = new MapData(Mathf.Max(1, newWidth), Mathf.Max(1, newHeight));
        int copyWidth = Mathf.Min(width, resized.width);
        int copyHeight = Mathf.Min(height, resized.height);

        for (int layerIndex = 0; layerIndex < layerTiles.Length; layerIndex++)
        {
            MapLayerTileData sourceLayer = layerTiles[layerIndex];
            if (sourceLayer == null || !sourceLayer.IsValid(width, height))
            {
                continue;
            }

            MapEditorLayerType layerType = (MapEditorLayerType)layerIndex;
            for (int y = 0; y < copyHeight; y++)
            {
                for (int x = 0; x < copyWidth; x++)
                {
                    int sourceIndex = y * width + x;
                    if (sourceLayer.tiles[sourceIndex] == -1 && sourceLayer.pixelData[sourceIndex] == null)
                    {
                        continue;
                    }

                    resized.SetTileOnLayer(
                        x,
                        y,
                        layerType,
                        sourceLayer.tiles[sourceIndex],
                        sourceLayer.colors[sourceIndex],
                        sourceLayer.imagePaths[sourceIndex],
                        sourceLayer.imageIndices[sourceIndex],
                        sourceLayer.imageRotations[sourceIndex],
                        sourceLayer.imageFlipXs[sourceIndex],
                        sourceLayer.imageFlipYs[sourceIndex]
                    );
                    resized.SetPixelDataOnLayer(x, y, layerType, sourceLayer.pixelData[sourceIndex]);
                }
            }
        }

        return resized;
    }

    public static MapEditorLayerType[] GetSerializableLayers()
    {
        return (MapEditorLayerType[])MapEditorLayerUtility.SerializableLayers.Clone();
    }

    private void InitializeMissingLayerMap()
    {
        if (layerMap == null || tileMap == null)
        {
            return;
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                layerMap[x, y] = (int)InferLayerFromTile(tileMap[x, y]);
            }
        }
    }

    private static MapEditorLayerType GetSavedLayer(MapSaveData saveData, int index, int tileId)
    {
        if (saveData.layers != null && index >= 0 && index < saveData.layers.Length)
        {
            return (MapEditorLayerType)Mathf.Clamp(saveData.layers[index], 0, (int)MapEditorLayerUtility.LastLayer);
        }

        return InferLayerFromTile(tileId);
    }

    public static MapEditorLayerType InferLayerFromTile(int tileId)
    {
        return tileId == MapEditorManager.WallTileId ? MapEditorLayerType.WallCollision : MapEditorLayerType.Ground;
    }

    private void EnsureLayerTiles(bool canMigrateFlatMaps)
    {
        if (AreLayerTilesValid())
        {
            return;
        }

        layerTiles = new MapLayerTileData[(int)MapEditorLayerUtility.LastLayer + 1];

        if (canMigrateFlatMaps)
        {
            MigrateFlatMapsToLayers();
        }
    }

    private bool AreLayerTilesValid()
    {
        if (layerTiles == null || layerTiles.Length <= (int)MapEditorLayerUtility.LastLayer)
        {
            return false;
        }

        for (int i = 0; i < layerTiles.Length; i++)
        {
            if (layerTiles[i] != null && !layerTiles[i].IsValid(width, height))
            {
                return false;
            }
        }

        return true;
    }

    private void MigrateFlatMapsToLayers()
    {
        if (tileMap == null || colorMap == null)
        {
            return;
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int tileId = tileMap[x, y];

                if (tileId == -1)
                {
                    continue;
                }

                MapEditorLayerType layerType = layerMap == null ? InferLayerFromTile(tileId) : (MapEditorLayerType)Mathf.Clamp(layerMap[x, y], 0, (int)MapEditorLayerUtility.LastLayer);
                MapLayerTileData layer = GetLayerData(layerType);
                int index = ToIndex(x, y);
                layer.tiles[index] = tileId;
                layer.colors[index] = colorMap[x, y];
                layer.imagePaths[index] = imagePathMap[x, y];
                layer.imageIndices[index] = imageIndexMap[x, y];
                layer.imageRotations[index] = imageRotationMap[x, y];
                layer.imageFlipXs[index] = imageFlipXMap[x, y];
                layer.imageFlipYs[index] = imageFlipYMap[x, y];
                layer.pixelData[index] = pixelMap == null || pixelMap[x, y] == null ? null : pixelMap[x, y].Clone();
            }
        }
    }

    private MapEditorLayerType GetLayerForFlatWrite(int x, int y, int tileId)
    {
        if (IsInside(x, y) && layerMap != null)
        {
            return GetLayer(x, y);
        }

        return InferLayerFromTile(tileId);
    }

    private MapLayerTileData GetLayerData(MapEditorLayerType layerType)
    {
        int index = Mathf.Clamp((int)layerType, 0, (int)MapEditorLayerUtility.LastLayer);

        if (layerTiles == null || layerTiles.Length <= index)
        {
            EnsureLayerTiles(false);
        }

        if (layerTiles[index] == null || !layerTiles[index].IsValid(width, height))
        {
            layerTiles[index] = new MapLayerTileData(width, height) { layer = index };
        }

        return layerTiles[index];
    }

    private MapLayerTileData TryGetLayerData(MapEditorLayerType layerType)
    {
        int index = Mathf.Clamp((int)layerType, 0, (int)MapEditorLayerUtility.LastLayer);
        return layerTiles != null
            && index < layerTiles.Length
            && layerTiles[index] != null
            && layerTiles[index].IsValid(width, height)
            ? layerTiles[index]
            : null;
    }

    private int ToIndex(int x, int y)
    {
        return y * width + x;
    }

    private void SyncFlatCellFromLayers(int x, int y)
    {
        MapEditorLayerType visibleLayer = GetTopVisibleLayer(x, y);
        MapLayerTileData layer = GetLayerData(visibleLayer);
        int index = ToIndex(x, y);

        tileMap[x, y] = layer.tiles[index];
        colorMap[x, y] = layer.colors[index];
        imagePathMap[x, y] = layer.imagePaths[index];
        imageIndexMap[x, y] = layer.imageIndices[index];
        imageRotationMap[x, y] = layer.imageRotations[index];
        imageFlipXMap[x, y] = layer.imageFlipXs[index];
        imageFlipYMap[x, y] = layer.imageFlipYs[index];
        pixelMap[x, y] = layer.pixelData[index] == null ? null : layer.pixelData[index].Clone();
        layerMap[x, y] = (int)visibleLayer;
    }

    private MapEditorLayerType GetTopVisibleLayer(int x, int y)
    {
        MapEditorLayerType[] priority = MapEditorLayerUtility.RenderPriority;

        for (int i = 0; i < priority.Length; i++)
        {
            MapEditorLayerType layerType = priority[i];

            MapLayerTileData layer = TryGetLayerData(layerType);
            if (layer != null && layer.tiles[ToIndex(x, y)] != -1)
            {
                return layerType;
            }
        }

        return MapEditorLayerType.Ground;
    }

    private MapLayerTileData[] CloneLayerTilesForSave()
    {
        EnsureInitialized();
        MapEditorLayerType[] layers = GetSerializableLayers();
        System.Collections.Generic.List<MapLayerTileData> clones = new System.Collections.Generic.List<MapLayerTileData>(layers.Length);

        for (int i = 0; i < layers.Length; i++)
        {
            MapLayerTileData layer = TryGetLayerData(layers[i]);
            if (layer == null)
            {
                continue;
            }

            MapLayerTileData clone = layer.Clone();
            clone.layer = (int)layers[i];
            clones.Add(clone);
        }

        return clones.ToArray();
    }

    private void ApplySavedLayerTiles(MapLayerTileData[] savedLayers)
    {
        if (savedLayers == null || savedLayers.Length == 0)
        {
            return;
        }

        EnsureLayerTiles(false);

        for (int i = 0; i < savedLayers.Length; i++)
        {
            MapLayerTileData savedLayer = savedLayers[i];

            if (savedLayer == null || !savedLayer.IsValid(width, height))
            {
                continue;
            }

            int layerIndex = Mathf.Clamp(savedLayer.layer, 0, (int)MapEditorLayerUtility.LastLayer);
            layerTiles[layerIndex] = savedLayer.Clone();
        }

        SyncAllFlatCellsFromLayers();
    }

    private void SyncAllFlatCellsFromLayers()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                SyncFlatCellFromLayers(x, y);
            }
        }
    }

}

[System.Serializable]
public class MapLayerTileData
{
    public int layer;
    public int width;
    public int height;
    public int[] tiles;
    public Color[] colors;
    public string[] imagePaths;
    public int[] imageIndices;
    public int[] imageRotations;
    public bool[] imageFlipXs;
    public bool[] imageFlipYs;
    public MapTilePixelData[] pixelData;

    public MapLayerTileData()
    {
    }

    public MapLayerTileData(int width, int height)
    {
        this.width = Mathf.Max(1, width);
        this.height = Mathf.Max(1, height);
        int count = this.width * this.height;
        tiles = new int[count];
        colors = new Color[count];
        imagePaths = new string[count];
        imageIndices = new int[count];
        imageRotations = new int[count];
        imageFlipXs = new bool[count];
        imageFlipYs = new bool[count];
        pixelData = new MapTilePixelData[count];
        Clear();
    }

    public bool IsValid(int expectedWidth, int expectedHeight)
    {
        int count = Mathf.Max(1, expectedWidth) * Mathf.Max(1, expectedHeight);
        return width == expectedWidth
            && height == expectedHeight
            && tiles != null
            && colors != null
            && imagePaths != null
            && imageIndices != null
            && imageRotations != null
            && imageFlipXs != null
            && imageFlipYs != null
            && pixelData != null
            && tiles.Length >= count
            && colors.Length >= count
            && imagePaths.Length >= count
            && imageIndices.Length >= count
            && imageRotations.Length >= count
            && imageFlipXs.Length >= count
            && imageFlipYs.Length >= count
            && pixelData.Length >= count;
    }

    public void Clear()
    {
        int count = width * height;

        for (int i = 0; i < count; i++)
        {
            tiles[i] = -1;
            colors[i] = Color.white;
            imagePaths[i] = string.Empty;
            imageIndices[i] = -1;
            imageRotations[i] = 0;
            imageFlipXs[i] = false;
            imageFlipYs[i] = false;
            pixelData[i] = null;
        }
    }

    public MapLayerTileData Clone()
    {
        MapLayerTileData clone = new MapLayerTileData
        {
            layer = layer,
            width = width,
            height = height,
            tiles = tiles == null ? null : (int[])tiles.Clone(),
            colors = colors == null ? null : (Color[])colors.Clone(),
            imagePaths = imagePaths == null ? null : (string[])imagePaths.Clone(),
            imageIndices = imageIndices == null ? null : (int[])imageIndices.Clone(),
            imageRotations = imageRotations == null ? null : (int[])imageRotations.Clone(),
            imageFlipXs = imageFlipXs == null ? null : (bool[])imageFlipXs.Clone(),
            imageFlipYs = imageFlipYs == null ? null : (bool[])imageFlipYs.Clone(),
            pixelData = pixelData == null ? null : new MapTilePixelData[pixelData.Length]
        };

        if (pixelData != null)
        {
            for (int i = 0; i < pixelData.Length; i++)
            {
                clone.pixelData[i] = pixelData[i] == null ? null : pixelData[i].Clone();
            }
        }

        return clone;
    }
}

[System.Serializable]
public class MapTilePixelData
{
    public int resolution;
    public Color[] colors;

    public static MapTilePixelData CreateFromSprite(Sprite sprite, int resolution)
    {
        if (sprite == null || sprite.texture == null)
        {
            return null;
        }

        resolution = MapEditorManager.NormalizeExportCellPixels(resolution);
        Rect rect = sprite.textureRect;
        MapTilePixelData data = new MapTilePixelData
        {
            resolution = resolution,
            colors = new Color[resolution * resolution]
        };

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float u = (x + 0.5f) / resolution;
                float v = 1f - ((y + 0.5f) / resolution);
                int pixelX = Mathf.Clamp(Mathf.FloorToInt(rect.xMin + u * rect.width), Mathf.FloorToInt(rect.xMin), Mathf.FloorToInt(rect.xMax) - 1);
                int pixelY = Mathf.Clamp(Mathf.FloorToInt(rect.yMin + v * rect.height), Mathf.FloorToInt(rect.yMin), Mathf.FloorToInt(rect.yMax) - 1);
                data.SetPixel(x, y, sprite.texture.GetPixel(pixelX, pixelY));
            }
        }

        return data;
    }

    public static MapTilePixelData CreateFilled(int resolution, Color color)
    {
        resolution = MapEditorManager.NormalizeExportCellPixels(resolution);
        MapTilePixelData data = new MapTilePixelData
        {
            resolution = resolution,
            colors = new Color[resolution * resolution]
        };

        for (int i = 0; i < data.colors.Length; i++)
        {
            data.colors[i] = color;
        }

        return data;
    }

    public MapTilePixelData Clone()
    {
        MapTilePixelData clone = new MapTilePixelData
        {
            resolution = MapEditorManager.NormalizeExportCellPixels(resolution),
            colors = colors == null ? null : (Color[])colors.Clone()
        };

        return clone;
    }

    public MapTilePixelData Resample(int nextResolution)
    {
        nextResolution = MapEditorManager.NormalizeExportCellPixels(nextResolution);
        int sourceResolution = Mathf.Max(1, resolution);
        MapTilePixelData resampled = new MapTilePixelData
        {
            resolution = nextResolution,
            colors = new Color[nextResolution * nextResolution]
        };

        for (int y = 0; y < nextResolution; y++)
        {
            for (int x = 0; x < nextResolution; x++)
            {
                int sourceX = Mathf.Clamp(Mathf.FloorToInt((x + 0.5f) / nextResolution * sourceResolution), 0, sourceResolution - 1);
                int sourceY = Mathf.Clamp(Mathf.FloorToInt((y + 0.5f) / nextResolution * sourceResolution), 0, sourceResolution - 1);
                resampled.colors[y * nextResolution + x] = GetPixel(sourceX, sourceY);
            }
        }

        return resampled;
    }

    public Color GetPixel(int x, int y)
    {
        if (colors == null || colors.Length == 0)
        {
            return Color.clear;
        }

        int safeResolution = Mathf.Max(1, resolution);
        x = Mathf.Clamp(x, 0, safeResolution - 1);
        y = Mathf.Clamp(y, 0, safeResolution - 1);
        int index = y * safeResolution + x;
        return index >= 0 && index < colors.Length ? colors[index] : Color.clear;
    }

    public void SetPixel(int x, int y, Color color)
    {
        int safeResolution = Mathf.Max(1, resolution);

        if (colors == null || colors.Length != safeResolution * safeResolution)
        {
            colors = new Color[safeResolution * safeResolution];
        }

        x = Mathf.Clamp(x, 0, safeResolution - 1);
        y = Mathf.Clamp(y, 0, safeResolution - 1);
        colors[y * safeResolution + x] = color;
    }

    public Color GetAverageColor()
    {
        if (colors == null || colors.Length == 0)
        {
            return Color.clear;
        }

        Color sum = Color.clear;

        for (int i = 0; i < colors.Length; i++)
        {
            sum += colors[i];
        }

        return sum / colors.Length;
    }
}

[System.Serializable]
public class MapSaveData
{
    public int formatVersion = 5;
    public int width;
    public int height;
    public string currentPngPalettePath;
    public int spawnX;
    public int spawnY;
    public int previewX;
    public int previewY;
    public int previewWidth;
    public int previewHeight;
    public MapEditorSpawnPointData[] spawnPoints;
    public int[] tiles;
    public Color[] colors;
    public string[] imagePaths;
    public int[] imageIndices;
    public int[] imageRotations;
    public bool[] imageFlipXs;
    public bool[] imageFlipYs;
    public MapTilePixelData[] pixelData;
    public int[] layers;
    public MapLayerTileData[] layerTiles;
    public MapEditorLayerSetting[] layerSettings;
    public EmbeddedPngAsset[] embeddedPngAssets;
    public MapEditorTilesetDefinition[] importedTilesets;

    public MapSaveData()
    {
    }

    public MapSaveData(int width, int height)
    {
        formatVersion = 5;
        this.width = width;
        this.height = height;
        tiles = new int[width * height];
        colors = new Color[width * height];
        imagePaths = new string[width * height];
        imageIndices = new int[width * height];
        imageRotations = new int[width * height];
        imageFlipXs = new bool[width * height];
        imageFlipYs = new bool[width * height];
        pixelData = new MapTilePixelData[width * height];
        layers = new int[width * height];
        layerTiles = new MapLayerTileData[0];
        layerSettings = new MapEditorLayerSetting[0];
        spawnPoints = new MapEditorSpawnPointData[0];
        embeddedPngAssets = new EmbeddedPngAsset[0];
        importedTilesets = new MapEditorTilesetDefinition[0];
    }
}

[System.Serializable]
public class MapEditorLayerSetting
{
    public int layer;
    public string displayName;
    public bool visible = true;
    public bool enabled = true;

    public MapEditorLayerSetting()
    {
    }

    public MapEditorLayerSetting(MapEditorLayerType layerType, string name, bool isVisible = true, bool isEnabled = true)
    {
        layer = (int)layerType;
        displayName = name;
        visible = isVisible;
        enabled = isEnabled;
    }

    public MapEditorLayerSetting Clone()
    {
        return new MapEditorLayerSetting
        {
            layer = layer,
            displayName = displayName,
            visible = visible,
            enabled = enabled
        };
    }
}

[System.Serializable]
public class MapEditorSpawnPointData
{
    public string id;
    public int x;
    public int y;
    public string role = "Any";

    public MapEditorSpawnPointData()
    {
    }

    public MapEditorSpawnPointData(string id, int x, int y, string role)
    {
        this.id = id;
        this.x = x;
        this.y = y;
        this.role = string.IsNullOrEmpty(role) ? "Any" : role;
    }
}

[System.Serializable]
public class EmbeddedPngAsset
{
    public string originalPath;
    public string fileName;
    public string base64Png;
}
