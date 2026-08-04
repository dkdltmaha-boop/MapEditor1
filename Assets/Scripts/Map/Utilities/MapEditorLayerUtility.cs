public static class MapEditorLayerUtility
{
    public const MapEditorLayerType LastLayer = MapEditorLayerType.WallVisualExtra8;
    public const int CanvasLayerCount = 9;

    public static readonly MapEditorLayerType[] GroundOptionalLayers =
    {
        MapEditorLayerType.GroundExtra, MapEditorLayerType.GroundExtra2,
        MapEditorLayerType.GroundExtra3, MapEditorLayerType.GroundExtra4,
        MapEditorLayerType.GroundExtra5, MapEditorLayerType.GroundExtra6,
        MapEditorLayerType.GroundExtra7, MapEditorLayerType.GroundExtra8
    };

    public static readonly MapEditorLayerType[] ObjectOptionalLayers =
    {
        MapEditorLayerType.ObjectExtra, MapEditorLayerType.ObjectExtra2,
        MapEditorLayerType.ObjectExtra3, MapEditorLayerType.ObjectExtra4,
        MapEditorLayerType.ObjectExtra5, MapEditorLayerType.ObjectExtra6,
        MapEditorLayerType.ObjectExtra7, MapEditorLayerType.ObjectExtra8
    };

    public static readonly MapEditorLayerType[] WallOptionalLayers =
    {
        MapEditorLayerType.WallVisualExtra, MapEditorLayerType.WallVisualExtra2,
        MapEditorLayerType.WallVisualExtra3, MapEditorLayerType.WallVisualExtra4,
        MapEditorLayerType.WallVisualExtra5, MapEditorLayerType.WallVisualExtra6,
        MapEditorLayerType.WallVisualExtra7, MapEditorLayerType.WallVisualExtra8
    };

    public static readonly MapEditorLayerType[] SerializableLayers =
    {
        MapEditorLayerType.Ground,
        MapEditorLayerType.GroundExtra,
        MapEditorLayerType.Object,
        MapEditorLayerType.ObjectExtra,
        MapEditorLayerType.WallVisual,
        MapEditorLayerType.WallVisualExtra,
        MapEditorLayerType.WallCollision,
        MapEditorLayerType.Zone,
        MapEditorLayerType.GroundExtra2, MapEditorLayerType.GroundExtra3,
        MapEditorLayerType.GroundExtra4, MapEditorLayerType.GroundExtra5,
        MapEditorLayerType.GroundExtra6, MapEditorLayerType.GroundExtra7,
        MapEditorLayerType.GroundExtra8,
        MapEditorLayerType.ObjectExtra2, MapEditorLayerType.ObjectExtra3,
        MapEditorLayerType.ObjectExtra4, MapEditorLayerType.ObjectExtra5,
        MapEditorLayerType.ObjectExtra6, MapEditorLayerType.ObjectExtra7,
        MapEditorLayerType.ObjectExtra8,
        MapEditorLayerType.WallVisualExtra2, MapEditorLayerType.WallVisualExtra3,
        MapEditorLayerType.WallVisualExtra4, MapEditorLayerType.WallVisualExtra5,
        MapEditorLayerType.WallVisualExtra6, MapEditorLayerType.WallVisualExtra7,
        MapEditorLayerType.WallVisualExtra8
    };

    public static readonly MapEditorLayerType[] RenderPriority =
    {
        MapEditorLayerType.WallVisualExtra8, MapEditorLayerType.WallVisualExtra7,
        MapEditorLayerType.WallVisualExtra6, MapEditorLayerType.WallVisualExtra5,
        MapEditorLayerType.WallVisualExtra4, MapEditorLayerType.WallVisualExtra3,
        MapEditorLayerType.WallVisualExtra2, MapEditorLayerType.WallVisualExtra,
        MapEditorLayerType.WallVisual,
        MapEditorLayerType.ObjectExtra8, MapEditorLayerType.ObjectExtra7,
        MapEditorLayerType.ObjectExtra6, MapEditorLayerType.ObjectExtra5,
        MapEditorLayerType.ObjectExtra4, MapEditorLayerType.ObjectExtra3,
        MapEditorLayerType.ObjectExtra2, MapEditorLayerType.ObjectExtra,
        MapEditorLayerType.Object,
        MapEditorLayerType.GroundExtra8, MapEditorLayerType.GroundExtra7,
        MapEditorLayerType.GroundExtra6, MapEditorLayerType.GroundExtra5,
        MapEditorLayerType.GroundExtra4, MapEditorLayerType.GroundExtra3,
        MapEditorLayerType.GroundExtra2, MapEditorLayerType.GroundExtra,
        MapEditorLayerType.Ground
    };

    public static bool IsOptional(MapEditorLayerType layerType)
    {
        return Contains(GroundOptionalLayers, layerType)
            || Contains(ObjectOptionalLayers, layerType)
            || Contains(WallOptionalLayers, layerType);
    }

    public static MapEditorLayerType GetOptionalLayer(MapEditorLayerType baseLayer)
    {
        switch (GetBaseLayer(baseLayer))
        {
            case MapEditorLayerType.Ground:
                return MapEditorLayerType.GroundExtra;
            case MapEditorLayerType.Object:
                return MapEditorLayerType.ObjectExtra;
            default:
                return MapEditorLayerType.WallVisualExtra;
        }
    }

    public static MapEditorLayerType[] GetOptionalLayers(MapEditorLayerType baseLayer)
    {
        switch (GetBaseLayer(baseLayer))
        {
            case MapEditorLayerType.Ground:
                return GroundOptionalLayers;
            case MapEditorLayerType.Object:
                return ObjectOptionalLayers;
            default:
                return WallOptionalLayers;
        }
    }

    public static MapEditorLayerType GetBaseLayer(MapEditorLayerType layerType)
    {
        switch (layerType)
        {
            default:
                if (Contains(GroundOptionalLayers, layerType)) return MapEditorLayerType.Ground;
                if (Contains(ObjectOptionalLayers, layerType)) return MapEditorLayerType.Object;
                if (Contains(WallOptionalLayers, layerType)) return MapEditorLayerType.WallVisual;
                return layerType;
        }
    }

    public static int GetCanvasIndex(MapEditorLayerType layerType)
    {
        MapEditorLayerType baseLayer = GetBaseLayer(layerType);
        if (baseLayer != MapEditorLayerType.Ground
            && baseLayer != MapEditorLayerType.Object
            && baseLayer != MapEditorLayerType.WallVisual)
        {
            return -1;
        }

        if (layerType == baseLayer) return 0;
        MapEditorLayerType[] layers = GetOptionalLayers(baseLayer);

        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == layerType) return i + 1;
        }

        return -1;
    }

    public static MapEditorLayerType GetCanvasLayer(int canvasIndex, MapEditorLayerType role)
    {
        role = GetBaseLayer(role);
        if (role != MapEditorLayerType.Ground
            && role != MapEditorLayerType.Object
            && role != MapEditorLayerType.WallVisual)
        {
            return role;
        }

        canvasIndex = UnityEngine.Mathf.Clamp(canvasIndex, 0, CanvasLayerCount - 1);
        if (canvasIndex == 0) return role;

        MapEditorLayerType[] layers = GetOptionalLayers(role);
        return layers[canvasIndex - 1];
    }

    private static bool Contains(MapEditorLayerType[] layers, MapEditorLayerType layerType)
    {
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == layerType) return true;
        }

        return false;
    }
}
