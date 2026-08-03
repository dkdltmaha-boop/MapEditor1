using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MapEditorPngTilesetService
{
    public const int FullImageTileIndex = 1 << 28;
    private const string RecentPngPrefsKey = "MapEditor.RecentPngFiles";
    private const int TileGridSize = 16;
    private const int FlexibleTileMarker = 1 << 30;
    private const int FlexibleBaseIndexMask = (1 << 14) - 1;
    private const int FlexibleGridShift = 14;
    private const int FlexibleResolutionShift = 16;
    private const int FlexibleSubXShift = 18;
    private const int FlexibleSubYShift = 22;
    private const int FlexibleSubSelectionFlag = 1 << 29;
    private const int EncodedSubTileOffset = 100000;
    private const int EncodedSubTileBaseMultiplier = 10000;
    private const int EncodedSubTileResolutionMultiplier = 100;

    private readonly Dictionary<string, Texture2D> loadedTextures = new Dictionary<string, Texture2D>();
    private readonly Dictionary<string, Sprite> loadedSprites = new Dictionary<string, Sprite>();
    private readonly Dictionary<int, RectInt> contentRectCache = new Dictionary<int, RectInt>();

    public Texture2D LoadTexture(string path)
    {
        if (loadedTextures.TryGetValue(path, out Texture2D cachedTexture))
        {
            return cachedTexture;
        }

        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            Debug.LogWarning("PNG 파일이 없습니다: " + path);
            return null;
        }

        byte[] bytes = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        if (!texture.LoadImage(bytes))
        {
            Debug.LogWarning("PNG 파일을 불러올 수 없습니다: " + path);
            MapEditorObjectUtility.DestroyObject(texture);
            return null;
        }

        texture.name = Path.GetFileNameWithoutExtension(path);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        loadedTextures[path] = texture;
        return texture;
    }

    public Sprite GetTileSprite(string imagePath, int imageIndex)
    {
        return GetTileSprite(imagePath, imageIndex, 0, false, false);
    }

    public Sprite GetTileSprite(string imagePath, int imageIndex, int rotation, bool flipX, bool flipY)
    {
        if (string.IsNullOrEmpty(imagePath) || imageIndex < 0)
        {
            return null;
        }

        int normalizedRotation = MapEditorRotationUtility.NormalizeQuarterTurn(rotation);
        string key = imagePath + "#" + imageIndex + "#r" + normalizedRotation + "#x" + flipX + "#y" + flipY;

        if (loadedSprites.TryGetValue(key, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        Texture2D texture = LoadTexture(imagePath);

        if (texture == null)
        {
            return null;
        }

        bool isFullImageTile = imageIndex == FullImageTileIndex;
        int paletteGridSize = TileGridSize;
        int flexibleBaseIndex = 0;
        bool flexibleSubTile = false;
        int flexibleResolution = TileGridSize;
        int flexibleSubX = 0;
        int flexibleSubY = 0;
        bool isFlexibleTile = !isFullImageTile
            && TryDecodePaletteTileIndex(
                imageIndex,
                out paletteGridSize,
                out flexibleBaseIndex,
                out flexibleSubTile,
                out flexibleResolution,
                out flexibleSubX,
                out flexibleSubY);
        int legacyBaseIndex = imageIndex;
        int legacyResolution = TileGridSize;
        int legacySubX = 0;
        int legacySubY = 0;
        bool isLegacySubTile = !isFullImageTile
            && !isFlexibleTile
            && TryDecodeSubTileIndex(imageIndex, out legacyBaseIndex, out legacyResolution, out legacySubX, out legacySubY);
        bool isSubTile = isFlexibleTile ? flexibleSubTile : isLegacySubTile;
        int sourceImageIndex = isFullImageTile ? 0 : isFlexibleTile ? flexibleBaseIndex : isLegacySubTile ? legacyBaseIndex : imageIndex;
        int subResolution = isFlexibleTile ? flexibleResolution : legacyResolution;
        int subX = isFlexibleTile ? flexibleSubX : legacySubX;
        int subY = isFlexibleTile ? flexibleSubY : legacySubY;
        int columns = isFlexibleTile ? paletteGridSize : TileGridSize;
        int x = sourceImageIndex % columns;
        int y = sourceImageIndex / columns;
        int pixelX;
        int pixelY;
        int pixelWidth;
        int pixelHeight;

        if (isFullImageTile)
        {
            pixelX = 0;
            pixelY = 0;
            pixelWidth = texture.width;
            pixelHeight = texture.height;
        }
        else if (isFlexibleTile)
        {
            RectInt contentRect = MapEditorTilesetLibraryService.IsNormalizedAtlasPath(imagePath)
                ? new RectInt(0, 0, texture.width, texture.height)
                : GetCachedContentPixelRect(texture);
            float cellWidth = contentRect.width / (float)paletteGridSize;
            float cellHeight = contentRect.height / (float)paletteGridSize;
            pixelX = contentRect.x + Mathf.FloorToInt(x * cellWidth);
            pixelY = contentRect.y + Mathf.FloorToInt(y * cellHeight);
            pixelWidth = Mathf.Max(1, contentRect.x + Mathf.FloorToInt((x + 1) * cellWidth) - pixelX);
            pixelHeight = Mathf.Max(1, contentRect.y + Mathf.FloorToInt((y + 1) * cellHeight) - pixelY);
        }
        else
        {
            float cellWidth = texture.width / (float)TileGridSize;
            float cellHeight = texture.height / (float)TileGridSize;
            pixelX = Mathf.FloorToInt(x * cellWidth);
            pixelY = Mathf.FloorToInt(y * cellHeight);
            pixelWidth = Mathf.Max(1, Mathf.FloorToInt((x + 1) * cellWidth) - pixelX);
            pixelHeight = Mathf.Max(1, Mathf.FloorToInt((y + 1) * cellHeight) - pixelY);
        }

        if (isSubTile)
        {
            int nextPixelX = Mathf.FloorToInt(pixelX + (subX + subResolution) / (float)TileGridSize * pixelWidth);
            int nextPixelY = Mathf.FloorToInt(pixelY + (subY + subResolution) / (float)TileGridSize * pixelHeight);
            pixelX = Mathf.FloorToInt(pixelX + subX / (float)TileGridSize * pixelWidth);
            pixelY = Mathf.FloorToInt(pixelY + subY / (float)TileGridSize * pixelHeight);
            pixelWidth = Mathf.Max(1, nextPixelX - pixelX);
            pixelHeight = Mathf.Max(1, nextPixelY - pixelY);
        }

        if (normalizedRotation == 0 && !flipX && !flipY)
        {
            Sprite originalSprite = Sprite.Create(
                texture,
                new Rect(pixelX, pixelY, pixelWidth, pixelHeight),
                new Vector2(0.5f, 0.5f),
                Mathf.Max(pixelWidth, pixelHeight)
            );
            originalSprite.name = texture.name + "_Tile_" + x + "_" + y;
            loadedSprites[key] = originalSprite;
            return originalSprite;
        }

        Texture2D spriteTexture = CreateTileTexture(texture, pixelX, pixelY, pixelWidth, pixelHeight, normalizedRotation, flipX, flipY);

        Sprite sprite = Sprite.Create(
            spriteTexture,
            new Rect(0, 0, spriteTexture.width, spriteTexture.height),
            new Vector2(0.5f, 0.5f),
            Mathf.Max(spriteTexture.width, spriteTexture.height)
        );
        sprite.name = texture.name + "_Tile_" + x + "_" + y + "_R" + normalizedRotation;
        loadedSprites[key] = sprite;
        return sprite;
    }

    private RectInt GetCachedContentPixelRect(Texture2D texture)
    {
        int textureId = texture.GetInstanceID();
        if (!contentRectCache.TryGetValue(textureId, out RectInt rect))
        {
            rect = GetContentPixelRect(texture);
            contentRectCache[textureId] = rect;
        }

        return rect;
    }

    public static RectInt GetContentPixelRect(Texture2D texture)
    {
        if (texture == null || texture.width <= 0 || texture.height <= 0)
        {
            return new RectInt();
        }

        Color32[] pixels = texture.GetPixels32();
        Color32 borderColor = pixels[0];
        int minX = texture.width;
        int minY = texture.height;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < texture.height; y++)
        {
            int rowOffset = y * texture.width;
            for (int x = 0; x < texture.width; x++)
            {
                if (pixels[rowOffset + x].Equals(borderColor))
                {
                    continue;
                }

                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }
        }

        bool hasUniformFrame = maxX >= minX
            && minX > 0
            && minY > 0
            && maxX < texture.width - 1
            && maxY < texture.height - 1;

        return hasUniformFrame
            ? new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1)
            : new RectInt(0, 0, texture.width, texture.height);
    }

    private Texture2D CreateTileTexture(Texture2D source, int sourceX, int sourceY, int sourceWidth, int sourceHeight, int rotation, bool flipX, bool flipY)
    {
        bool rotatedSideways = rotation == 90 || rotation == 270;
        int outputWidth = rotatedSideways ? sourceHeight : sourceWidth;
        int outputHeight = rotatedSideways ? sourceWidth : sourceHeight;
        Texture2D output = new Texture2D(outputWidth, outputHeight, TextureFormat.RGBA32, false);
        output.filterMode = FilterMode.Point;
        output.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < outputHeight; y++)
        {
            for (int x = 0; x < outputWidth; x++)
            {
                float u = (x + 0.5f) / outputWidth;
                float v = (y + 0.5f) / outputHeight;
                ApplyTransform(ref u, ref v, rotation, flipX, flipY);
                int pixelX = Mathf.Clamp(Mathf.FloorToInt(sourceX + u * sourceWidth), sourceX, sourceX + sourceWidth - 1);
                int pixelY = Mathf.Clamp(Mathf.FloorToInt(sourceY + v * sourceHeight), sourceY, sourceY + sourceHeight - 1);
                output.SetPixel(x, y, source.GetPixel(pixelX, pixelY));
            }
        }

        output.Apply();
        return output;
    }

    private void ApplyTransform(ref float u, ref float v, int rotation, bool flipX, bool flipY)
    {
        if (flipX)
        {
            u = 1f - u;
        }

        if (flipY)
        {
            v = 1f - v;
        }

        float originalU = u;
        float originalV = v;

        switch (rotation)
        {
            case 90:
                u = originalV;
                v = 1f - originalU;
                break;
            case 180:
                u = 1f - originalU;
                v = 1f - originalV;
                break;
            case 270:
                u = 1f - originalV;
                v = originalU;
                break;
        }
    }

    public static int EncodeSubTileIndex(int baseImageIndex, int resolution, int subX, int subY)
    {
        resolution = NormalizeSubTileResolution(resolution);
        subX = Mathf.Clamp(subX, 0, TileGridSize - 1);
        subY = Mathf.Clamp(subY, 0, TileGridSize - 1);

        if (TryDecodePaletteTileIndex(baseImageIndex, out int gridSize, out int decodedBaseIndex, out _, out _, out _, out _))
        {
            return FlexibleTileMarker
                | (GridSizeToCode(gridSize) << FlexibleGridShift)
                | (ResolutionToCode(resolution) << FlexibleResolutionShift)
                | (subX << FlexibleSubXShift)
                | (subY << FlexibleSubYShift)
                | FlexibleSubSelectionFlag
                | decodedBaseIndex;
        }

        return EncodedSubTileOffset
            + baseImageIndex * EncodedSubTileBaseMultiplier
            + resolution * EncodedSubTileResolutionMultiplier
            + subY * TileGridSize
            + subX;
    }

    public static bool TryDecodeSubTileIndex(int imageIndex, out int baseImageIndex, out int resolution, out int subX, out int subY)
    {
        if (TryDecodePaletteTileIndex(imageIndex, out _, out baseImageIndex, out bool hasSubSelection, out resolution, out subX, out subY))
        {
            return hasSubSelection;
        }

        baseImageIndex = imageIndex;
        resolution = TileGridSize;
        subX = 0;
        subY = 0;

        if (imageIndex < EncodedSubTileOffset)
        {
            return false;
        }

        int encoded = imageIndex - EncodedSubTileOffset;
        baseImageIndex = encoded / EncodedSubTileBaseMultiplier;
        int remainder = encoded % EncodedSubTileBaseMultiplier;
        resolution = NormalizeSubTileResolution(remainder / EncodedSubTileResolutionMultiplier);
        int subIndex = remainder % EncodedSubTileResolutionMultiplier;
        subX = subIndex % TileGridSize;
        subY = subIndex / TileGridSize;
        return true;
    }

    public static int EncodePaletteTileIndex(int gridSize, int baseImageIndex)
    {
        gridSize = MapEditorManager.NormalizePngPaletteGridSize(gridSize);
        return FlexibleTileMarker
            | (GridSizeToCode(gridSize) << FlexibleGridShift)
            | Mathf.Clamp(baseImageIndex, 0, FlexibleBaseIndexMask);
    }

    public static int GetBaseImageIndex(int imageIndex)
    {
        if (TryDecodePaletteTileIndex(imageIndex, out _, out int baseImageIndex, out _, out _, out _, out _))
        {
            return baseImageIndex;
        }

        if (TryDecodeSubTileIndex(imageIndex, out baseImageIndex, out _, out _, out _))
        {
            return baseImageIndex;
        }

        return imageIndex;
    }

    private static bool TryDecodePaletteTileIndex(int imageIndex, out int gridSize, out int baseImageIndex, out bool hasSubSelection, out int resolution, out int subX, out int subY)
    {
        gridSize = TileGridSize;
        baseImageIndex = imageIndex;
        hasSubSelection = false;
        resolution = TileGridSize;
        subX = 0;
        subY = 0;

        if ((imageIndex & FlexibleTileMarker) == 0)
        {
            return false;
        }

        gridSize = CodeToGridSize((imageIndex >> FlexibleGridShift) & 0x3);
        baseImageIndex = imageIndex & FlexibleBaseIndexMask;
        hasSubSelection = (imageIndex & FlexibleSubSelectionFlag) != 0;

        if (hasSubSelection)
        {
            resolution = CodeToResolution((imageIndex >> FlexibleResolutionShift) & 0x3);
            subX = (imageIndex >> FlexibleSubXShift) & 0xF;
            subY = (imageIndex >> FlexibleSubYShift) & 0xF;
        }

        return true;
    }

    private static int GridSizeToCode(int gridSize)
    {
        switch (MapEditorManager.NormalizePngPaletteGridSize(gridSize))
        {
            case 32: return 1;
            case 64: return 2;
            case 128: return 3;
            default: return 0;
        }
    }

    private static int CodeToGridSize(int code)
    {
        return 16 << Mathf.Clamp(code, 0, 3);
    }

    private static int ResolutionToCode(int resolution)
    {
        switch (NormalizeSubTileResolution(resolution))
        {
            case 2: return 1;
            case 4: return 2;
            case 16: return 3;
            default: return 0;
        }
    }

    private static int CodeToResolution(int code)
    {
        switch (code)
        {
            case 1: return 2;
            case 2: return 4;
            case 3: return 16;
            default: return 1;
        }
    }

    private static int NormalizeSubTileResolution(int resolution)
    {
        int[] options = { 1, 2, 4, 16 };
        int selected = options[0];
        int bestDistance = Mathf.Abs(resolution - selected);

        for (int i = 1; i < options.Length; i++)
        {
            int distance = Mathf.Abs(resolution - options[i]);
            if (distance < bestDistance)
            {
                selected = options[i];
                bestDistance = distance;
            }
        }

        return selected;
    }

    public void AddRecentPath(string path, int maxRecentFiles)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        List<string> paths = GetRecentPaths();
        paths.Remove(path);
        paths.Insert(0, path);

        while (paths.Count > Mathf.Max(1, maxRecentFiles))
        {
            paths.RemoveAt(paths.Count - 1);
        }

        PlayerPrefs.SetString(RecentPngPrefsKey, string.Join("|", paths));
        PlayerPrefs.Save();
    }

    public List<string> GetRecentPaths()
    {
        List<string> paths = new List<string>();
        string savedValue = PlayerPrefs.GetString(RecentPngPrefsKey, string.Empty);

        if (string.IsNullOrEmpty(savedValue))
        {
            return paths;
        }

        string[] splitPaths = savedValue.Split('|');

        foreach (string path in splitPaths)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path) && !paths.Contains(path))
            {
                paths.Add(path);
            }
        }

        return paths;
    }
}
