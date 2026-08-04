using UnityEngine;

public sealed class MapEditorPngFileService
{
    private readonly MapEditorPngPaletteService palette = new MapEditorPngPaletteService();

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
        string path = MapEditorFileDialog.OpenFile("PNG 팔레트 불러오기", "png");

        if (string.IsNullOrEmpty(path))
        {
            return colorWheelWindow;
        }

        MapEditorFileDialog.RememberDirectory(path);
        return LoadPalette(manager, colorWheelWindow, colorPaletteOffset, maxRecentFiles, path);
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

}
