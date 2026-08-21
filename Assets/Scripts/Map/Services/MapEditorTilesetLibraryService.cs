using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class MapEditorTilesetLibraryService
{
    private const string CatalogPrefsKey = "MapEditor.ImportedTilesets.v1";
    private const int MaxTilePixelSize = 256;
    private const int MaxAtlasPixelSize = 8192;
    public const int MinAnimationFrameCount = 2;
    public const int MaxAnimationFrameCount = 32;
    public const float MinAnimationFramesPerSecond = 1f;
    public const float MaxAnimationFramesPerSecond = 30f;

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
            sourcePaths = new[] { sourcePath },
            atlasPath = atlasPath,
            tileWidth = tileWidth,
            tileHeight = tileHeight,
            margin = margin,
            spacing = spacing,
            columns = columns,
            rows = rows,
            tileCount = columns * rows,
            atlasGridSize = gridSize,
            defaultLayer = defaultCollision ? MapEditorLayerType.WallCollision : defaultLayer,
            defaultCollision = defaultCollision
        };

        definitions.Add(definition);
        Register(definition);
        SaveCatalog();
        return true;
    }

    public bool ImportCollection(
        IReadOnlyList<string> sourcePaths,
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

        List<string> validPaths = NormalizeSourcePaths(sourcePaths);
        if (validPaths.Count == 0)
        {
            error = "Tileset PNG files were not found.";
            return false;
        }

        if (validPaths.Count == 1)
        {
            return Import(
                validPaths[0],
                displayName,
                tileWidth,
                tileHeight,
                margin,
                spacing,
                defaultLayer,
                defaultCollision,
                out definition,
                out error);
        }

        tileWidth = Mathf.Clamp(tileWidth, 1, MaxTilePixelSize);
        tileHeight = Mathf.Clamp(tileHeight, 1, MaxTilePixelSize);
        margin = Mathf.Max(0, margin);
        spacing = Mathf.Max(0, spacing);
        defaultLayer = NormalizeDefaultLayer(defaultLayer);

        List<TilesetSource> sources = new List<TilesetSource>(validPaths.Count);
        int totalTileCount = 0;

        try
        {
            for (int i = 0; i < validPaths.Count; i++)
            {
                Texture2D texture = LoadTexture(validPaths[i]);
                if (texture == null)
                {
                    error = "Tileset PNG could not be decoded: " + Path.GetFileName(validPaths[i]);
                    return false;
                }

                int usableWidth = texture.width - margin * 2;
                int usableHeight = texture.height - margin * 2;
                int columns = (usableWidth + spacing) / (tileWidth + spacing);
                int rows = (usableHeight + spacing) / (tileHeight + spacing);
                if (columns <= 0 || rows <= 0)
                {
                    MapEditorObjectUtility.DestroyObject(texture);
                    error = "Tile size and margin leave no tiles inside: " + Path.GetFileName(validPaths[i]);
                    return false;
                }

                sources.Add(new TilesetSource(texture, columns, rows));
                totalTileCount += columns * rows;
            }

            int requiredGrid = Mathf.CeilToInt(Mathf.Sqrt(totalTileCount));
            int gridSize = ResolveAtlasGridSize(requiredGrid);
            if (gridSize <= 0)
            {
                error = "Combined tileset exceeds the supported 128x128 tile grid.";
                return false;
            }

            if (gridSize * tileWidth > MaxAtlasPixelSize || gridSize * tileHeight > MaxAtlasPixelSize)
            {
                error = "Generated combined tileset atlas would exceed 8192 pixels.";
                return false;
            }

            string id = CreateId(displayName, validPaths[0]);
            string atlasDirectory = Path.Combine(Application.persistentDataPath, "ImportedTilesets");
            Directory.CreateDirectory(atlasDirectory);
            string atlasPath = Path.Combine(atlasDirectory, "tileset_atlas_" + id + ".png");
            Texture2D atlas = BuildCollectionAtlas(
                sources,
                tileWidth,
                tileHeight,
                margin,
                spacing,
                gridSize);
            File.WriteAllBytes(atlasPath, atlas.EncodeToPNG());
            MapEditorObjectUtility.DestroyObject(atlas);

            definition = new MapEditorTilesetDefinition
            {
                id = id,
                displayName = string.IsNullOrWhiteSpace(displayName)
                    ? MapEditorLocalization.Choose("타일셋 묶음", "Tileset Collection")
                    : displayName.Trim(),
                sourcePath = validPaths[0],
                sourcePaths = validPaths.ToArray(),
                atlasPath = atlasPath,
                tileWidth = tileWidth,
                tileHeight = tileHeight,
                margin = margin,
                spacing = spacing,
                columns = gridSize,
                rows = Mathf.CeilToInt(totalTileCount / (float)gridSize),
                tileCount = totalTileCount,
                atlasGridSize = gridSize,
                defaultLayer = defaultCollision ? MapEditorLayerType.WallCollision : defaultLayer,
                defaultCollision = defaultCollision
            };

            definitions.Add(definition);
            Register(definition);
            SaveCatalog();
            return true;
        }
        finally
        {
            for (int i = 0; i < sources.Count; i++)
            {
                MapEditorObjectUtility.DestroyObject(sources[i].texture);
            }
        }
    }

    public bool Rename(string id, string displayName)
    {
        MapEditorTilesetDefinition definition = FindById(id);
        if (definition == null || string.IsNullOrWhiteSpace(displayName))
        {
            return false;
        }

        definition.displayName = displayName.Trim();
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
        return definitions.Exists(item => ContainsSourcePath(item, normalized));
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

    public bool ConfigureAnimation(
        string tilesetId,
        string displayName,
        int startTileId,
        int frameCount,
        float framesPerSecond,
        bool loop,
        out string error)
    {
        return AddAnimation(
            tilesetId,
            displayName,
            startTileId,
            frameCount,
            framesPerSecond,
            loop,
            out _,
            out error);
    }

    public bool AddAnimation(
        string tilesetId,
        string displayName,
        int startTileId,
        int frameCount,
        float framesPerSecond,
        bool loop,
        out MapEditorTilesetAnimationDefinition animation,
        out string error)
    {
        animation = null;
        if (!TryCreateContiguousFrameIds(startTileId, frameCount, out int[] sourceFrameTileIds, out error))
        {
            return false;
        }

        return AddAnimation(
            tilesetId,
            displayName,
            sourceFrameTileIds,
            framesPerSecond,
            loop,
            out animation,
            out error);
    }

    public bool AddAnimation(
        string tilesetId,
        string displayName,
        IReadOnlyList<int> sourceFrameTileIds,
        float framesPerSecond,
        bool loop,
        out MapEditorTilesetAnimationDefinition animation,
        out string error)
    {
        animation = null;
        MapEditorTilesetDefinition definition = FindById(tilesetId);
        if (!TryBuildAnimation(
                definition,
                null,
                displayName,
                sourceFrameTileIds,
                0,
                false,
                framesPerSecond,
                loop,
                out animation,
                out error))
        {
            return false;
        }

        List<MapEditorTilesetAnimationDefinition> animations = GetMutableAnimations(definition);
        animations.Add(animation);
        definition.animations = animations.ToArray();
        SaveAnimationChanges(definition);
        return true;
    }

    public bool AddGridAnimation(
        string tilesetId,
        string displayName,
        int frameGridSize,
        IReadOnlyList<int> sourceFrameTileIds,
        float framesPerSecond,
        bool loop,
        out MapEditorTilesetAnimationDefinition animation,
        out string error)
    {
        animation = null;
        MapEditorTilesetDefinition definition = FindById(tilesetId);
        if (!TryBuildAnimation(
                definition,
                null,
                displayName,
                sourceFrameTileIds,
                frameGridSize,
                true,
                framesPerSecond,
                loop,
                out animation,
                out error))
        {
            return false;
        }

        List<MapEditorTilesetAnimationDefinition> animations = GetMutableAnimations(definition);
        animations.Add(animation);
        definition.animations = animations.ToArray();
        SaveAnimationChanges(definition);
        return true;
    }

    public bool UpdateAnimation(
        string tilesetId,
        string animationId,
        string displayName,
        IReadOnlyList<int> sourceFrameTileIds,
        float framesPerSecond,
        bool loop,
        out string error)
    {
        MapEditorTilesetDefinition definition = FindById(tilesetId);
        int animationIndex = FindAnimationIndex(definition, animationId);
        if (animationIndex < 0)
        {
            error = definition == null ? "Tileset was not found." : "Animation was not found.";
            return false;
        }

        if (!TryBuildAnimation(
                definition,
                animationId,
                displayName,
                sourceFrameTileIds,
                0,
                false,
                framesPerSecond,
                loop,
                out MapEditorTilesetAnimationDefinition updated,
                out error))
        {
            return false;
        }

        definition.animations[animationIndex] = updated;
        SaveAnimationChanges(definition);
        return true;
    }

    public bool UpdateGridAnimation(
        string tilesetId,
        string animationId,
        string displayName,
        int frameGridSize,
        IReadOnlyList<int> sourceFrameTileIds,
        float framesPerSecond,
        bool loop,
        out string error)
    {
        MapEditorTilesetDefinition definition = FindById(tilesetId);
        int animationIndex = FindAnimationIndex(definition, animationId);
        if (animationIndex < 0)
        {
            error = definition == null ? "Tileset was not found." : "Animation was not found.";
            return false;
        }

        if (!TryBuildAnimation(
                definition,
                animationId,
                displayName,
                sourceFrameTileIds,
                frameGridSize,
                true,
                framesPerSecond,
                loop,
                out MapEditorTilesetAnimationDefinition updated,
                out error))
        {
            return false;
        }

        definition.animations[animationIndex] = updated;
        SaveAnimationChanges(definition);
        return true;
    }

    public bool RemoveAnimation(string tilesetId, string animationId)
    {
        MapEditorTilesetDefinition definition = FindById(tilesetId);
        int animationIndex = FindAnimationIndex(definition, animationId);
        if (animationIndex < 0)
        {
            return false;
        }

        List<MapEditorTilesetAnimationDefinition> animations = GetMutableAnimations(definition);
        animations.RemoveAt(animationIndex);
        definition.animations = animations.ToArray();
        SaveAnimationChanges(definition);
        return true;
    }

    public MapEditorTilesetAnimationDefinition FindAnimation(string tilesetId, string animationId)
    {
        MapEditorTilesetDefinition definition = FindById(tilesetId);
        int animationIndex = FindAnimationIndex(definition, animationId);
        return animationIndex >= 0 ? definition.animations[animationIndex] : null;
    }

    public static bool TryGetAnimation(
        string atlasPath,
        int imageIndex,
        out MapEditorTilesetDefinition tileset,
        out MapEditorTilesetAnimationDefinition animation)
    {
        animation = null;

        if (!TryGetByAtlasPath(atlasPath, out tileset) || tileset.animations == null)
        {
            return false;
        }

        int tileId = MapEditorPngTilesetService.GetBaseImageIndex(imageIndex);
        int imageGridSize = GetEncodedPaletteGridSize(imageIndex, tileset.atlasGridSize);
        MapEditorTilesetAnimationDefinition gridFallback = null;

        for (int i = 0; i < tileset.animations.Length; i++)
        {
            MapEditorTilesetAnimationDefinition candidate = tileset.animations[i];
            int candidateGridSize = candidate != null && candidate.frameGridSize > 0
                ? MapEditorManager.NormalizePngPaletteGridSize(candidate.frameGridSize)
                : Mathf.Max(1, tileset.atlasGridSize);
            if (candidate != null && candidateGridSize == imageGridSize && candidate.ContainsTile(tileId))
            {
                animation = candidate;
                return true;
            }

            // Older UI call sites encoded the first frame with the tileset grid. Keep them loadable,
            // then the manager normalizes the brush to the animation's own grid.
            if (candidate != null && gridFallback == null && candidate.ContainsTile(tileId))
            {
                gridFallback = candidate;
            }
        }

        animation = gridFallback;
        return animation != null;
    }

    private static int GetEncodedPaletteGridSize(int imageIndex, int fallbackGridSize)
    {
        const int flexibleTileMarker = 1 << 30;
        const int flexibleGridShift = 14;
        if ((imageIndex & flexibleTileMarker) == 0)
        {
            return Mathf.Max(1, fallbackGridSize);
        }

        int code = (imageIndex >> flexibleGridShift) & 0x3;
        return 16 << code;
    }

    private static bool TryCreateContiguousFrameIds(int startTileId, int frameCount, out int[] frameTileIds, out string error)
    {
        frameTileIds = null;
        error = string.Empty;

        if (startTileId < 0)
        {
            error = "Animation start tile must be zero or greater.";
            return false;
        }

        if (frameCount < MinAnimationFrameCount || frameCount > MaxAnimationFrameCount)
        {
            error = "Animation frame count must be between 2 and 32.";
            return false;
        }

        frameTileIds = new int[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            frameTileIds[i] = startTileId + i;
        }

        return true;
    }

    private static bool TryBuildAnimation(
        MapEditorTilesetDefinition definition,
        string existingAnimationId,
        string displayName,
        IReadOnlyList<int> sourceFrameTileIds,
        int frameGridSize,
        bool useGridFrames,
        float framesPerSecond,
        bool loop,
        out MapEditorTilesetAnimationDefinition animation,
        out string error)
    {
        animation = null;
        error = string.Empty;

        if (definition == null)
        {
            error = "Tileset was not found.";
            return false;
        }

        int frameCount = sourceFrameTileIds?.Count ?? 0;
        if (frameCount < MinAnimationFrameCount || frameCount > MaxAnimationFrameCount)
        {
            error = "Animation frame count must be between 2 and 32.";
            return false;
        }

        if (float.IsNaN(framesPerSecond)
            || float.IsInfinity(framesPerSecond)
            || framesPerSecond < MinAnimationFramesPerSecond
            || framesPerSecond > MaxAnimationFramesPerSecond)
        {
            error = "Animation speed must be between 1 and 30 FPS.";
            return false;
        }

        int normalizedFrameGridSize = useGridFrames
            ? MapEditorManager.NormalizePngPaletteGridSize(frameGridSize)
            : Mathf.Max(1, definition.atlasGridSize);
        int tileCount = useGridFrames
            ? normalizedFrameGridSize * normalizedFrameGridSize
            : definition.TileCount;
        int[] atlasFrameTileIds = new int[frameCount];
        HashSet<int> uniqueFrames = new HashSet<int>();

        for (int i = 0; i < frameCount; i++)
        {
            int sourceTileId = sourceFrameTileIds[i];
            if (sourceTileId < 0 || sourceTileId >= tileCount)
            {
                error = "Animation frame tile is outside the imported tileset: " + sourceTileId;
                return false;
            }

            int atlasTileId = useGridFrames
                ? ToGridTileId(normalizedFrameGridSize, sourceTileId)
                : ToAtlasTileId(definition, sourceTileId);
            if (!uniqueFrames.Add(atlasTileId))
            {
                error = "An animation cannot use the same frame more than once.";
                return false;
            }

            if (IsFrameUsedByAnotherAnimation(
                    definition,
                    existingAnimationId,
                    normalizedFrameGridSize,
                    atlasTileId))
            {
                error = "Tile " + sourceTileId + " is already used by another animation.";
                return false;
            }

            atlasFrameTileIds[i] = atlasTileId;
        }

        string animationId = string.IsNullOrEmpty(existingAnimationId)
            ? CreateAnimationId(definition)
            : existingAnimationId;
        int existingIndex = FindAnimationIndex(definition, animationId);
        string fallbackName = existingIndex >= 0
            ? definition.animations[existingIndex].displayName
            : "Animation " + ((definition.animations?.Length ?? 0) + 1);

        animation = new MapEditorTilesetAnimationDefinition
        {
            id = animationId,
            displayName = string.IsNullOrWhiteSpace(displayName) ? fallbackName : displayName.Trim(),
            startTileId = sourceFrameTileIds[0],
            frameCount = frameCount,
            frameTileIds = atlasFrameTileIds,
            frameGridSize = useGridFrames ? normalizedFrameGridSize : 0,
            framesPerSecond = framesPerSecond,
            loop = loop
        };
        return true;
    }

    private static bool IsFrameUsedByAnotherAnimation(
        MapEditorTilesetDefinition definition,
        string ignoredAnimationId,
        int frameGridSize,
        int atlasTileId)
    {
        if (definition.animations == null)
        {
            return false;
        }

        for (int i = 0; i < definition.animations.Length; i++)
        {
            MapEditorTilesetAnimationDefinition candidate = definition.animations[i];
            if (candidate == null || string.Equals(candidate.id, ignoredAnimationId, StringComparison.Ordinal))
            {
                continue;
            }

            int candidateGridSize = candidate.frameGridSize > 0
                ? MapEditorManager.NormalizePngPaletteGridSize(candidate.frameGridSize)
                : Mathf.Max(1, definition.atlasGridSize);
            int candidateFrameCount = Mathf.Max(1, candidate.frameCount);
            for (int frameIndex = 0; frameIndex < candidateFrameCount; frameIndex++)
            {
                if (GridCellsOverlap(
                        frameGridSize,
                        atlasTileId,
                        candidateGridSize,
                        candidate.GetFrameTileId(frameIndex)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool GridCellsOverlap(int firstGrid, int firstTileId, int secondGrid, int secondTileId)
    {
        int firstX = firstTileId % firstGrid;
        int firstY = firstTileId / firstGrid;
        int secondX = secondTileId % secondGrid;
        int secondY = secondTileId / secondGrid;
        return firstX * secondGrid < (secondX + 1) * firstGrid
            && secondX * firstGrid < (firstX + 1) * secondGrid
            && firstY * secondGrid < (secondY + 1) * firstGrid
            && secondY * firstGrid < (firstY + 1) * secondGrid;
    }

    private static List<MapEditorTilesetAnimationDefinition> GetMutableAnimations(MapEditorTilesetDefinition definition)
    {
        return definition.animations == null
            ? new List<MapEditorTilesetAnimationDefinition>()
            : new List<MapEditorTilesetAnimationDefinition>(definition.animations);
    }

    private static int FindAnimationIndex(MapEditorTilesetDefinition definition, string animationId)
    {
        if (definition?.animations == null || string.IsNullOrEmpty(animationId))
        {
            return -1;
        }

        for (int i = 0; i < definition.animations.Length; i++)
        {
            MapEditorTilesetAnimationDefinition animation = definition.animations[i];
            if (animation != null && string.Equals(animation.id, animationId, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static string CreateAnimationId(MapEditorTilesetDefinition definition)
    {
        int index = 0;
        string candidate;
        do
        {
            candidate = definition.id + "_animation_" + index;
            index++;
        }
        while (FindAnimationIndex(definition, candidate) >= 0);

        return candidate;
    }

    private void SaveAnimationChanges(MapEditorTilesetDefinition definition)
    {
        Register(definition);
        SaveCatalog();
    }

    private static int ToAtlasTileId(MapEditorTilesetDefinition definition, int sourceTileId)
    {
        int sourceColumns = Mathf.Max(1, definition.columns);
        int sourceRowFromTop = Mathf.Max(0, sourceTileId / sourceColumns);
        int sourceColumn = Mathf.Max(0, sourceTileId % sourceColumns);
        int atlasRowFromBottom = Mathf.Max(0, definition.atlasGridSize - 1 - sourceRowFromTop);
        return atlasRowFromBottom * definition.atlasGridSize + sourceColumn;
    }

    private static int ToGridTileId(int gridSize, int sourceTileId)
    {
        int sourceRowFromTop = Mathf.Max(0, sourceTileId / gridSize);
        int sourceColumn = Mathf.Max(0, sourceTileId % gridSize);
        int rowFromBottom = Mathf.Max(0, gridSize - 1 - sourceRowFromTop);
        return rowFromBottom * gridSize + sourceColumn;
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

        if ((definition.sourcePaths == null || definition.sourcePaths.Length == 0)
            && !string.IsNullOrEmpty(definition.sourcePath))
        {
            definition.sourcePaths = new[] { definition.sourcePath };
        }

        if (definition.tileCount <= 0)
        {
            definition.tileCount = Mathf.Max(0, definition.columns * definition.rows);
        }

        NormalizeAnimationDefinitions(definition);
        RegisteredByAtlasPath[NormalizePath(definition.atlasPath)] = definition;
    }

    private static void NormalizeAnimationDefinitions(MapEditorTilesetDefinition definition)
    {
        if (definition.animations == null || definition.animations.Length == 0)
        {
            definition.animations = Array.Empty<MapEditorTilesetAnimationDefinition>();
            return;
        }

        List<MapEditorTilesetAnimationDefinition> normalized = new List<MapEditorTilesetAnimationDefinition>();
        HashSet<string> usedIds = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < definition.animations.Length; i++)
        {
            MapEditorTilesetAnimationDefinition animation = definition.animations[i];
            if (animation == null)
            {
                continue;
            }

            string animationId = animation.id;
            if (string.IsNullOrWhiteSpace(animationId) || usedIds.Contains(animationId))
            {
                int suffix = i;
                do
                {
                    animationId = definition.id + "_animation_" + suffix;
                    suffix++;
                }
                while (usedIds.Contains(animationId));

                animation.id = animationId;
            }

            usedIds.Add(animation.id);

            if (string.IsNullOrWhiteSpace(animation.displayName))
            {
                animation.displayName = "Animation " + (normalized.Count + 1);
            }

            if (animation.frameTileIds == null || animation.frameTileIds.Length == 0)
            {
                int tileCount = Mathf.Max(1, definition.TileCount);
                int legacyStartTileId = Mathf.Clamp(animation.startTileId, 0, tileCount - 1);
                int legacyFrameCount = Mathf.Clamp(
                    Mathf.Max(1, animation.frameCount),
                    1,
                    tileCount - legacyStartTileId);
                animation.startTileId = legacyStartTileId;
                animation.frameTileIds = new int[legacyFrameCount];
                for (int frameIndex = 0; frameIndex < legacyFrameCount; frameIndex++)
                {
                    animation.frameTileIds[frameIndex] = ToAtlasTileId(
                        definition,
                        legacyStartTileId + frameIndex);
                }

                animation.frameCount = legacyFrameCount;
            }
            else
            {
                animation.frameCount = animation.frameTileIds.Length;
            }

            if (animation.frameCount < 1)
            {
                animation.frameCount = 1;
            }

            if (animation.framesPerSecond <= 0f
                || float.IsNaN(animation.framesPerSecond)
                || float.IsInfinity(animation.framesPerSecond))
            {
                animation.framesPerSecond = 8f;
            }

            normalized.Add(animation);
        }

        definition.animations = normalized.ToArray();
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

    private static Texture2D BuildCollectionAtlas(
        IReadOnlyList<TilesetSource> sources,
        int tileWidth,
        int tileHeight,
        int margin,
        int spacing,
        int gridSize)
    {
        Texture2D atlas = new Texture2D(gridSize * tileWidth, gridSize * tileHeight, TextureFormat.RGBA32, false);
        atlas.filterMode = FilterMode.Point;
        atlas.wrapMode = TextureWrapMode.Clamp;
        atlas.SetPixels32(new Color32[atlas.width * atlas.height]);

        int destinationTile = 0;
        for (int sourceIndex = 0; sourceIndex < sources.Count; sourceIndex++)
        {
            TilesetSource source = sources[sourceIndex];
            for (int rowFromTop = 0; rowFromTop < source.rows; rowFromTop++)
            {
                int sourceY = source.texture.height - margin - tileHeight - rowFromTop * (tileHeight + spacing);
                for (int column = 0; column < source.columns; column++)
                {
                    int destinationColumn = destinationTile % gridSize;
                    int destinationRowFromTop = destinationTile / gridSize;
                    int destinationY = (gridSize - 1 - destinationRowFromTop) * tileHeight;
                    int sourceX = margin + column * (tileWidth + spacing);
                    atlas.SetPixels(
                        destinationColumn * tileWidth,
                        destinationY,
                        tileWidth,
                        tileHeight,
                        source.texture.GetPixels(sourceX, sourceY, tileWidth, tileHeight));
                    destinationTile++;
                }
            }
        }

        atlas.Apply(false, false);
        return atlas;
    }

    private static List<string> NormalizeSourcePaths(IReadOnlyList<string> sourcePaths)
    {
        List<string> paths = new List<string>();
        HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (sourcePaths == null)
        {
            return paths;
        }

        for (int i = 0; i < sourcePaths.Count; i++)
        {
            string path = sourcePaths[i];
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                continue;
            }

            string normalized = NormalizePath(path);
            if (seen.Add(normalized))
            {
                paths.Add(normalized);
            }
        }

        return paths;
    }

    private static bool ContainsSourcePath(MapEditorTilesetDefinition definition, string normalizedPath)
    {
        if (definition == null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(definition.sourcePath)
            && string.Equals(NormalizePath(definition.sourcePath), normalizedPath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (definition.sourcePaths == null)
        {
            return false;
        }

        for (int i = 0; i < definition.sourcePaths.Length; i++)
        {
            if (string.Equals(NormalizePath(definition.sourcePaths[i]), normalizedPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static MapEditorLayerType NormalizeDefaultLayer(MapEditorLayerType layer)
    {
        return layer == MapEditorLayerType.Ground
            || layer == MapEditorLayerType.Object
            || layer == MapEditorLayerType.WallVisual
            || layer == MapEditorLayerType.WallCollision
            ? layer
            : MapEditorLayerType.Ground;
    }

    private readonly struct TilesetSource
    {
        public readonly Texture2D texture;
        public readonly int columns;
        public readonly int rows;

        public TilesetSource(Texture2D texture, int columns, int rows)
        {
            this.texture = texture;
            this.columns = columns;
            this.rows = rows;
        }
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
