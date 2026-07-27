using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class MapEditorTilesetLibraryService
{
    private const string CatalogPrefsKey = "MapEditor.ImportedTilesets.v1";
    private const int MaxTilePixelSize = 256;
    private const int MaxAtlasPixelSize = 8192;

    private static readonly Dictionary<string, MapEditorTilesetDefinition> RegisteredByAtlasPath =
        new Dictionary<string, MapEditorTilesetDefinition>(StringComparer.OrdinalIgnoreCase);

    private readonly List<MapEditorTilesetDefinition> definitions = new List<MapEditorTilesetDefinition>();

    public MapEditorTilesetLibraryService()
    {
        LoadCatalog();
    }

    public IReadOnlyList<MapEditorTilesetDefinition> Definitions => definitions;

    public bool Import(
        string sourcePath,
        string displayName,
        int tileWidth,
        int tileHeight,
        int margin,
        int spacing,
        MapEditorLayerType defaultLayer,
        bool defaultCollision,
        out MapEditorTilesetDefinition definition,
        out string error)
    {
        definition = null;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            error = "Tileset PNG file was not found.";
            return false;
        }

        tileWidth = Mathf.Clamp(tileWidth, 1, MaxTilePixelSize);
        tileHeight = Mathf.Clamp(tileHeight, 1, MaxTilePixelSize);
        margin = Mathf.Max(0, margin);
        spacing = Mathf.Max(0, spacing);
        if (defaultLayer != MapEditorLayerType.Ground
            && defaultLayer != MapEditorLayerType.Object
            && defaultLayer != MapEditorLayerType.WallVisual
            && defaultLayer != MapEditorLayerType.WallCollision)
        {
            defaultLayer = MapEditorLayerType.Ground;
        }

        Texture2D source = LoadTexture(sourcePath);
        if (source == null)
        {
            error = "Tileset PNG could not be decoded.";
            return false;
        }

        int usableWidth = source.width - margin * 2;
        int usableHeight = source.height - margin * 2;
        int columns = (usableWidth + spacing) / (tileWidth + spacing);
        int rows = (usableHeight + spacing) / (tileHeight + spacing);

        if (columns <= 0 || rows <= 0)
        {
            MapEditorObjectUtility.DestroyObject(source);
            error = "Tile size and margin leave no tiles inside the PNG.";
            return false;
        }

        int gridSize = ResolveAtlasGridSize(Mathf.Max(columns, rows));
        if (gridSize <= 0)
        {
            MapEditorObjectUtility.DestroyObject(source);
            error = "Tileset exceeds the supported 128x128 tile grid.";
            return false;
        }

        if (gridSize * tileWidth > MaxAtlasPixelSize || gridSize * tileHeight > MaxAtlasPixelSize)
        {
            MapEditorObjectUtility.DestroyObject(source);
            error = "Generated tileset atlas would exceed 8192 pixels.";
            return false;
        }

        string id = CreateId(displayName, sourcePath);
        string atlasDirectory = Path.Combine(Application.persistentDataPath, "ImportedTilesets");
        Directory.CreateDirectory(atlasDirectory);
        string atlasPath = Path.Combine(atlasDirectory, "tileset_atlas_" + id + ".png");

        Texture2D atlas = BuildAtlas(source, tileWidth, tileHeight, margin, spacing, columns, rows, gridSize);
        File.WriteAllBytes(atlasPath, atlas.EncodeToPNG());
        MapEditorObjectUtility.DestroyObject(atlas);
        MapEditorObjectUtility.DestroyObject(source);

        definition = new MapEditorTilesetDefinition
        {
            id = id,
            displayName = string.IsNullOrWhiteSpace(displayName) ? Path.GetFileNameWithoutExtension(sourcePath) : displayName.Trim(),
            sourcePath = sourcePath,
            atlasPath = atlasPath,
            tileWidth = tileWidth,
            tileHeight = tileHeight,
            margin = margin,
            spacing = spacing,
            columns = columns,
            rows = rows,
            atlasGridSize = gridSize,
            defaultLayer = defaultCollision ? MapEditorLayerType.WallCollision : defaultLayer,
            defaultCollision = defaultCollision
        };

        definitions.Add(definition);
        Register(definition);
        SaveCatalog();
        return true;
    }

    public bool Remove(string id)
    {
        int index = definitions.FindIndex(item => item != null && item.id == id);
        if (index < 0)
        {
            return false;
        }

        MapEditorTilesetDefinition definition = definitions[index];
        definitions.RemoveAt(index);

        if (definition != null && !string.IsNullOrEmpty(definition.atlasPath))
        {
            RegisteredByAtlasPath.Remove(NormalizePath(definition.atlasPath));
        }

        SaveCatalog();
        return true;
    }

    public MapEditorTilesetDefinition FindById(string id)
    {
        return definitions.Find(item => item != null && item.id == id);
    }

    public bool ContainsSourcePath(string sourcePath)
    {
        if (string.IsNullOrEmpty(sourcePath))
        {
            return false;
        }

        string normalized = NormalizePath(sourcePath);
        return definitions.Exists(item =>
            item != null
            && !string.IsNullOrEmpty(item.sourcePath)
            && string.Equals(NormalizePath(item.sourcePath), normalized, StringComparison.OrdinalIgnoreCase));
    }

    public MapEditorTilesetDefinition[] GetDefinitionsForSave()
    {
        return definitions.ToArray();
    }

    public void ReplaceDefinitions(MapEditorTilesetDefinition[] savedDefinitions)
    {
        definitions.Clear();
        RegisteredByAtlasPath.Clear();

        if (savedDefinitions != null)
        {
            for (int i = 0; i < savedDefinitions.Length; i++)
            {
                MapEditorTilesetDefinition definition = savedDefinitions[i];
                if (definition == null || string.IsNullOrEmpty(definition.id))
                {
                    continue;
                }

                definitions.Add(definition);
                Register(definition);
            }
        }

        SaveCatalog();
    }

    public static bool TryGetByAtlasPath(string atlasPath, out MapEditorTilesetDefinition definition)
    {
        definition = null;
        return !string.IsNullOrEmpty(atlasPath)
            && RegisteredByAtlasPath.TryGetValue(NormalizePath(atlasPath), out definition);
    }

    public static bool IsNormalizedAtlasPath(string atlasPath)
    {
        return !string.IsNullOrEmpty(atlasPath)
            && Path.GetFileName(atlasPath).StartsWith("tileset_atlas_", StringComparison.OrdinalIgnoreCase);
    }

    private void LoadCatalog()
    {
        string json = PlayerPrefs.GetString(CatalogPrefsKey, string.Empty);
        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        try
        {
            MapEditorTilesetCatalogData catalog = JsonUtility.FromJson<MapEditorTilesetCatalogData>(json);
            if (catalog?.tilesets == null)
            {
                return;
            }

            for (int i = 0; i < catalog.tilesets.Length; i++)
            {
                MapEditorTilesetDefinition definition = catalog.tilesets[i];
                if (definition == null || !definition.IsUsable || !File.Exists(definition.atlasPath))
                {
                    continue;
                }

                definitions.Add(definition);
                Register(definition);
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("가져온 타일셋 목록을 읽을 수 없습니다.\n" + exception.Message);
        }
    }

    private void SaveCatalog()
    {
        MapEditorTilesetCatalogData catalog = new MapEditorTilesetCatalogData
        {
            tilesets = definitions.ToArray()
        };
        PlayerPrefs.SetString(CatalogPrefsKey, JsonUtility.ToJson(catalog));
        PlayerPrefs.Save();
    }

    private static void Register(MapEditorTilesetDefinition definition)
    {
        if (definition == null || string.IsNullOrEmpty(definition.atlasPath))
        {
            return;
        }

        RegisteredByAtlasPath[NormalizePath(definition.atlasPath)] = definition;
    }

    private static Texture2D LoadTexture(string path)
    {
        try
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (texture.LoadImage(File.ReadAllBytes(path), false))
            {
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                return texture;
            }

            MapEditorObjectUtility.DestroyObject(texture);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("타일셋 PNG를 불러올 수 없습니다: " + path + "\n" + exception.Message);
        }

        return null;
    }

    private static Texture2D BuildAtlas(Texture2D source, int tileWidth, int tileHeight, int margin, int spacing, int columns, int rows, int gridSize)
    {
        Texture2D atlas = new Texture2D(gridSize * tileWidth, gridSize * tileHeight, TextureFormat.RGBA32, false);
        atlas.filterMode = FilterMode.Point;
        atlas.wrapMode = TextureWrapMode.Clamp;
        Color32[] clearPixels = new Color32[atlas.width * atlas.height];
        atlas.SetPixels32(clearPixels);

        for (int rowFromTop = 0; rowFromTop < rows; rowFromTop++)
        {
            int sourceY = source.height - margin - tileHeight - rowFromTop * (tileHeight + spacing);
            int destinationY = (gridSize - 1 - rowFromTop) * tileHeight;

            for (int column = 0; column < columns; column++)
            {
                int sourceX = margin + column * (tileWidth + spacing);
                atlas.SetPixels(column * tileWidth, destinationY, tileWidth, tileHeight, source.GetPixels(sourceX, sourceY, tileWidth, tileHeight));
            }
        }

        atlas.Apply(false, false);
        return atlas;
    }

    private static int ResolveAtlasGridSize(int required)
    {
        int[] options = MapEditorManager.PngPaletteGridSizeOptions;
        for (int i = 0; i < options.Length; i++)
        {
            if (required <= options[i])
            {
                return options[i];
            }
        }

        return -1;
    }

    private static string CreateId(string displayName, string sourcePath)
    {
        string name = string.IsNullOrWhiteSpace(displayName) ? Path.GetFileNameWithoutExtension(sourcePath) : displayName;
        char[] invalid = Path.GetInvalidFileNameChars();
        foreach (char character in invalid)
        {
            name = name.Replace(character, '_');
        }

        name = name.Trim().Replace(' ', '_').ToLowerInvariant();
        if (string.IsNullOrEmpty(name))
        {
            name = "tileset";
        }

        return name + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
    }

    private static string NormalizePath(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch
        {
            return path ?? string.Empty;
        }
    }
}
