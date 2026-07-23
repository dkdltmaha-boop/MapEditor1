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
        string path = EditorUtility.OpenFilePanel("PNG 팔레트 불러오기", "", "png");

        if (string.IsNullOrEmpty(path))
        {
            return colorWheelWindow;
        }

        return LoadPalette(manager, colorWheelWindow, colorPaletteOffset, maxRecentFiles, path);
#else
        Debug.LogWarning("PNG 파일 선택창은 Unity 에디터에서만 사용할 수 있습니다.");
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

        Debug.Log("PNG 팔레트를 불러왔습니다: " + path);
        return colorWheelWindow;
    }

    public void ExportMapPngWithDialog(MapData mapData, int cellPixels, bool emptyCellsTransparent)
    {
#if UNITY_EDITOR
        string path = EditorUtility.SaveFilePanel("맵 PNG 내보내기", "", "map.png", "png");

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        ExportMapPng(mapData, path, cellPixels, emptyCellsTransparent);
#else
        Debug.LogWarning("PNG 내보내기 파일 선택창은 Unity 에디터에서만 사용할 수 있습니다.");
#endif
    }

    public void ExportMapPng(MapData mapData, string path, int cellPixels, bool emptyCellsTransparent)
    {
        exporter.ExportPng(mapData, path, cellPixels, emptyCellsTransparent);
    }
}
