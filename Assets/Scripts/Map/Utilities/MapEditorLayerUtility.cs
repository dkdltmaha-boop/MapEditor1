using System.Collections.Generic;

public static class MapEditorLayerUtility
{
    public const MapEditorLayerType LastLayer = MapEditorLayerType.WallVisualExtra31;
    public const int CanvasLayerCount = 32;

    public static readonly MapEditorLayerType[] GroundOptionalLayers =
    {
        MapEditorLayerType.GroundExtra, MapEditorLayerType.GroundExtra2,
        MapEditorLayerType.GroundExtra3, MapEditorLayerType.GroundExtra4,
        MapEditorLayerType.GroundExtra5, MapEditorLayerType.GroundExtra6,
        MapEditorLayerType.GroundExtra7, MapEditorLayerType.GroundExtra8,
        MapEditorLayerType.GroundExtra9, MapEditorLayerType.GroundExtra10,
        MapEditorLayerType.GroundExtra11, MapEditorLayerType.GroundExtra12,
        MapEditorLayerType.GroundExtra13, MapEditorLayerType.GroundExtra14,
        MapEditorLayerType.GroundExtra15, MapEditorLayerType.GroundExtra16,
        MapEditorLayerType.GroundExtra17, MapEditorLayerType.GroundExtra18,
        MapEditorLayerType.GroundExtra19, MapEditorLayerType.GroundExtra20,
        MapEditorLayerType.GroundExtra21, MapEditorLayerType.GroundExtra22,
        MapEditorLayerType.GroundExtra23, MapEditorLayerType.GroundExtra24,
        MapEditorLayerType.GroundExtra25, MapEditorLayerType.GroundExtra26,
        MapEditorLayerType.GroundExtra27, MapEditorLayerType.GroundExtra28,
        MapEditorLayerType.GroundExtra29, MapEditorLayerType.GroundExtra30,
        MapEditorLayerType.GroundExtra31
    };

    public static readonly MapEditorLayerType[] ObjectOptionalLayers =
    {
        MapEditorLayerType.ObjectExtra, MapEditorLayerType.ObjectExtra2,
        MapEditorLayerType.ObjectExtra3, MapEditorLayerType.ObjectExtra4,
        MapEditorLayerType.ObjectExtra5, MapEditorLayerType.ObjectExtra6,
        MapEditorLayerType.ObjectExtra7, MapEditorLayerType.ObjectExtra8,
        MapEditorLayerType.ObjectExtra9, MapEditorLayerType.ObjectExtra10,
        MapEditorLayerType.ObjectExtra11, MapEditorLayerType.ObjectExtra12,
        MapEditorLayerType.ObjectExtra13, MapEditorLayerType.ObjectExtra14,
        MapEditorLayerType.ObjectExtra15, MapEditorLayerType.ObjectExtra16,
        MapEditorLayerType.ObjectExtra17, MapEditorLayerType.ObjectExtra18,
        MapEditorLayerType.ObjectExtra19, MapEditorLayerType.ObjectExtra20,
        MapEditorLayerType.ObjectExtra21, MapEditorLayerType.ObjectExtra22,
        MapEditorLayerType.ObjectExtra23, MapEditorLayerType.ObjectExtra24,
        MapEditorLayerType.ObjectExtra25, MapEditorLayerType.ObjectExtra26,
        MapEditorLayerType.ObjectExtra27, MapEditorLayerType.ObjectExtra28,
        MapEditorLayerType.ObjectExtra29, MapEditorLayerType.ObjectExtra30,
        MapEditorLayerType.ObjectExtra31
    };

    public static readonly MapEditorLayerType[] WallOptionalLayers =
    {
        MapEditorLayerType.WallVisualExtra, MapEditorLayerType.WallVisualExtra2,
        MapEditorLayerType.WallVisualExtra3, MapEditorLayerType.WallVisualExtra4,
        MapEditorLayerType.WallVisualExtra5, MapEditorLayerType.WallVisualExtra6,
        MapEditorLayerType.WallVisualExtra7, MapEditorLayerType.WallVisualExtra8,
        MapEditorLayerType.WallVisualExtra9, MapEditorLayerType.WallVisualExtra10,
        MapEditorLayerType.WallVisualExtra11, MapEditorLayerType.WallVisualExtra12,
        MapEditorLayerType.WallVisualExtra13, MapEditorLayerType.WallVisualExtra14,
        MapEditorLayerType.WallVisualExtra15, MapEditorLayerType.WallVisualExtra16,
        MapEditorLayerType.WallVisualExtra17, MapEditorLayerType.WallVisualExtra18,
        MapEditorLayerType.WallVisualExtra19, MapEditorLayerType.WallVisualExtra20,
        MapEditorLayerType.WallVisualExtra21, MapEditorLayerType.WallVisualExtra22,
        MapEditorLayerType.WallVisualExtra23, MapEditorLayerType.WallVisualExtra24,
        MapEditorLayerType.WallVisualExtra25, MapEditorLayerType.WallVisualExtra26,
        MapEditorLayerType.WallVisualExtra27, MapEditorLayerType.WallVisualExtra28,
        MapEditorLayerType.WallVisualExtra29, MapEditorLayerType.WallVisualExtra30,
        MapEditorLayerType.WallVisualExtra31
    };

    public static readonly MapEditorLayerType[] SerializableLayers = BuildSerializableLayers();
    public static readonly MapEditorLayerType[] RenderPriority = BuildRenderPriority();

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

    private static MapEditorLayerType[] BuildSerializableLayers()
    {
        List<MapEditorLayerType> layers = new List<MapEditorLayerType>((int)LastLayer + 1)
        {
            MapEditorLayerType.Ground
        };
        layers.AddRange(GroundOptionalLayers);
        layers.Add(MapEditorLayerType.Object);
        layers.AddRange(ObjectOptionalLayers);
        layers.Add(MapEditorLayerType.WallVisual);
        layers.AddRange(WallOptionalLayers);
        layers.Add(MapEditorLayerType.WallCollision);
        layers.Add(MapEditorLayerType.Zone);
        return layers.ToArray();
    }

    private static MapEditorLayerType[] BuildRenderPriority()
    {
        List<MapEditorLayerType> layers = new List<MapEditorLayerType>(CanvasLayerCount * 3);
        AddRoleInReverse(layers, MapEditorLayerType.WallVisual, WallOptionalLayers);
        AddRoleInReverse(layers, MapEditorLayerType.Object, ObjectOptionalLayers);
        AddRoleInReverse(layers, MapEditorLayerType.Ground, GroundOptionalLayers);
        return layers.ToArray();
    }

    private static void AddRoleInReverse(List<MapEditorLayerType> destination, MapEditorLayerType baseLayer, MapEditorLayerType[] optionalLayers)
    {
        for (int i = optionalLayers.Length - 1; i >= 0; i--)
        {
            destination.Add(optionalLayers[i]);
        }

        destination.Add(baseLayer);
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
