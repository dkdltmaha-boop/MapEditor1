using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class MapEditorPngFileService
{
    private readonly MapEditorPngPaletteService palette = new MapEditorPngPaletteService();
    private readonly MapEditorMapExportService exporter;

    public MapEditorPngFileService()
    {
        exporter = new MapEditorMapExportService(GetTileSprite);
    }

    public string CurrentPath => palette.CurrentPath;

    public void SetPaletteGridSize(int gridSize)
    {
        palette.SetGridSize(gridSize);
    }

    public void SetCurrentPath(string path)
    {
        palette.SetCurrentPath(path);
    }

    public System.Collections.Generic.List<string> GetRecentPaths()
    {
        return palette.GetRecentPaths();
    }

    public MapEditorClipboard CreateCurrentPaletteClipboard()
    {
        return palette.CreateCurrentPaletteClipboard();
    }

    public Sprite GetTileSprite(string imagePath, int imageIndex)
    {
        return palette.GetTileSprite(imagePath, imageIndex);
    }

    public Sprite GetTileSprite(string imagePath, int imageIndex, int rotation, bool flipX, bool flipY)
    {
        return palette.GetTileSprite(imagePath, imageIndex, rotation, flipX, flipY);
    }

    public ColorWheelPickerWindow LoadPaletteWithDialog(
        MapEditorManager manager,
        ColorWheelPickerWindow colorWheelWindow,
        Vector2 colorPaletteOffset,
        int maxRecentFiles)
    {
#if UNITY_EDITOR
        string path = EditorUtility.OpenFilePanel("Load PNG Palette", "", "png");

        if (string.IsNullOrEmpty(path))
        {
            return colorWheelWindow;
        }

        return LoadPalette(manager, colorWheelWindow, colorPaletteOffset, maxRecentFiles, path);
#else
        Debug.LogWarning("PNG file picker is only available in the Unity Editor.");
        return colorWheelWindow;
#endif
    }

    public ColorWheelPickerWindow LoadPalette(
        MapEditorManager manager,
        ColorWheelPickerWindow colorWheelWindow,
        Vector2 colorPaletteOffset,
        int maxRecentFiles,
        string path)
    {
        Texture2D texture = palette.LoadPalette(path, maxRecentFiles);

        if (texture == null)
        {
            return colorWheelWindow;
        }

        if (colorWheelWindow == null)
        {
            colorWheelWindow = ColorWheelPickerWindow.Create(manager, colorPaletteOffset);
        }

        if (colorWheelWindow != null)
        {
            colorWheelWindow.SetPngPalette(texture, path);
        }

        Debug.Log("PNG palette loaded: " + path);
        return colorWheelWindow;
    }

    public void ExportMapPngWithDialog(MapData mapData, int cellPixels, bool emptyCellsTransparent)
    {
#if UNITY_EDITOR
        string path = EditorUtility.SaveFilePanel("Export Map PNG", "", "map.png", "png");

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        ExportMapPng(mapData, path, cellPixels, emptyCellsTransparent);
#else
        Debug.LogWarning("PNG export file picker is only available in the Unity Editor.");
#endif
    }

    public void ExportMapPng(MapData mapData, string path, int cellPixels, bool emptyCellsTransparent)
    {
        exporter.ExportPng(mapData, path, cellPixels, emptyCellsTransparent);
    }
}
