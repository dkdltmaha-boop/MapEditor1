using System;
using System.IO;
using UnityEngine;

public sealed class MapEditorMapLoadApplyService
{
    public MapData Apply(
        MapEditorManager manager,
        MapSaveData saveData,
        string path,
        Action clearSelection,
        Action clearHistory,
        Action regenerateGrid,
        Action refreshAllCells,
        Action refreshMinimap,
        Action<string> loadPngPalette,
        Action createToolToolbar,
        MapEditorPngFileService pngFiles)
    {
        if (saveData == null)
        {
            return manager.CurrentMapData;
        }

        bool sizeChanged = saveData.width != manager.mapWidth || saveData.height != manager.mapHeight;
        RepairImagePathsFromSavedPalette(saveData);
        MapData loadedMapData = MapData.FromSaveData(saveData);
        manager.mapWidth = loadedMapData.width;
        manager.mapHeight = loadedMapData.height;
        manager.SetCurrentMapDataForLoad(loadedMapData);

        clearSelection?.Invoke();
        clearHistory?.Invoke();
        RestorePngPalette(saveData.currentPngPalettePath, loadPngPalette, pngFiles);

        if (sizeChanged)
        {
            regenerateGrid?.Invoke();
        }
        else
        {
            refreshAllCells?.Invoke();
        }

        refreshMinimap?.Invoke();

        if (manager.createToolToolbar)
        {
            createToolToolbar?.Invoke();
        }

        Debug.Log("Map loaded: " + path + " (" + manager.mapWidth + "x" + manager.mapHeight + ", v" + saveData.formatVersion + ")");
        return loadedMapData;
    }

    private static void RepairImagePathsFromSavedPalette(MapSaveData saveData)
    {
        if (saveData == null
            || saveData.imagePaths == null
            || saveData.imageIndices == null
            || string.IsNullOrEmpty(saveData.currentPngPalettePath)
            || !File.Exists(saveData.currentPngPalettePath))
        {
            return;
        }

        int repairedCount = 0;
        int count = Math.Min(saveData.imagePaths.Length, saveData.imageIndices.Length);

        for (int i = 0; i < count; i++)
        {
            if (saveData.imageIndices[i] < 0)
            {
                continue;
            }

            string imagePath = saveData.imagePaths[i];

            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                continue;
            }

            saveData.imagePaths[i] = saveData.currentPngPalettePath;
            repairedCount++;
        }

        if (repairedCount > 0)
        {
            Debug.LogWarning("Repaired saved PNG tile paths from embedded edit data: " + repairedCount + " tiles");
        }
    }

    private static void RestorePngPalette(string pngPalettePath, Action<string> loadPngPalette, MapEditorPngFileService pngFiles)
    {
        pngFiles.SetCurrentPath(pngPalettePath);

        if (!string.IsNullOrEmpty(pngFiles.CurrentPath))
        {
            Debug.Log("Saved PNG palette path restored for internal references: " + pngFiles.CurrentPath);
        }
    }
}
