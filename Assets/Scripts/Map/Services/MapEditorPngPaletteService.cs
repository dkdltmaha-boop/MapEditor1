using System.Collections.Generic;
using UnityEngine;

public class MapEditorPngPaletteService
{
    private readonly MapEditorPngTilesetService tilesets = new MapEditorPngTilesetService();
    private int gridSize = 16;

    public string CurrentPath { get; private set; } = string.Empty;

    public void SetGridSize(int value)
    {
        gridSize = MapEditorManager.NormalizePngPaletteGridSize(value);
    }

    public Texture2D LoadPalette(string path, int maxRecentFiles)
    {
        Texture2D texture = tilesets.LoadTexture(path);

        if (texture == null)
        {
            return null;
        }

        CurrentPath = path;
        tilesets.AddRecentPath(path, maxRecentFiles);
        return texture;
    }

    public void SetCurrentPath(string path)
    {
        CurrentPath = path ?? string.Empty;
    }

    public Texture2D LoadCurrentTexture()
    {
        if (string.IsNullOrEmpty(CurrentPath))
        {
            Debug.LogWarning("불러온 PNG 팔레트가 없습니다.");
            return null;
        }

        Texture2D texture = tilesets.LoadTexture(CurrentPath);

        if (texture == null)
        {
            Debug.LogWarning("PNG가 불러와지지 않아 붙여넣을 수 없습니다: " + CurrentPath);
        }

        return texture;
    }

    public MapEditorClipboard CreateCurrentPaletteClipboard()
    {
        Texture2D texture = LoadCurrentTexture();
        if (texture == null)
        {
            return null;
        }

        int columns = gridSize;
        int rows = gridSize;
        MapEditorClipboard pngClipboard = new MapEditorClipboard(columns, rows);

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                int sourceY = rows - 1 - y;
                int imageIndex = MapEditorPngTilesetService.EncodePaletteTileIndex(gridSize, sourceY * columns + x);
                pngClipboard.Set(x, y, new MapEditorTileSnapshot(MapEditorManager.CustomImageTileId, Color.white, CurrentPath, imageIndex));
            }
        }

        return pngClipboard;
    }

    public Sprite GetTileSprite(string imagePath, int imageIndex)
    {
        return tilesets.GetTileSprite(imagePath, imageIndex);
    }

    public Sprite GetTileSprite(string imagePath, int imageIndex, int rotation, bool flipX, bool flipY)
    {
        return tilesets.GetTileSprite(imagePath, imageIndex, rotation, flipX, flipY);
    }

    public List<string> GetRecentPaths()
    {
        return tilesets.GetRecentPaths();
    }

    public string GetRecentDisplayName(string path)
    {
        return tilesets.GetRecentDisplayName(path);
    }

    public void SetRecentDisplayName(string path, string displayName)
    {
        tilesets.SetRecentDisplayName(path, displayName);
    }
}
