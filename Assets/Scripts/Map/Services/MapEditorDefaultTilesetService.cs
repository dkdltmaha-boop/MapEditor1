using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class MapEditorDefaultTilesetService
{
    private const string TileMapFolder = "10. Tile_Map";

    public static int ImportPixelChromaTilesets(MapEditorTilesetLibraryService library)
    {
        if (library == null)
        {
            return 0;
        }

        string projectPath = MapEditorPixelChromaProjectLocator.FindProjectPath();
        string tileRoot = string.IsNullOrEmpty(projectPath)
            ? string.Empty
            : Path.Combine(projectPath, "Assets", TileMapFolder);

        if (!Directory.Exists(tileRoot))
        {
            Debug.LogWarning("PixelChroma 기본 타일 폴더를 찾지 못했습니다. PIXELCHROMA_PROJECT_PATH를 확인하세요.");
            return 0;
        }

        List<string> candidates = new List<string>();
        AddPngFiles(Path.Combine(tileRoot, "Tilesets"), candidates);
        AddFile(Path.Combine(tileRoot, "16x16", "Interiors_free_16x16.png"), candidates);
        AddFile(Path.Combine(tileRoot, "16x16", "Room_Builder_free_16x16.png"), candidates);

        int imported = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            string sourcePath = candidates[i];

            if (library.ContainsSourcePath(sourcePath))
            {
                continue;
            }

            if (library.Import(
                sourcePath,
                "PixelChroma - " + Path.GetFileNameWithoutExtension(sourcePath),
                16,
                16,
                0,
                0,
                MapEditorLayerType.Ground,
                false,
                out _,
                out string error))
            {
                imported++;
            }
            else
            {
                Debug.LogWarning("기본 타일셋을 등록하지 못했습니다: " + sourcePath + "\n" + error);
            }
        }

        Debug.Log("PixelChroma 기본 타일셋 등록 완료: 새 항목 " + imported + "개");
        return imported;
    }

    private static void AddPngFiles(string directory, List<string> paths)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        string[] files = Directory.GetFiles(directory, "*.png", SearchOption.AllDirectories);
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);
        paths.AddRange(files);
    }

    private static void AddFile(string path, List<string> paths)
    {
        if (File.Exists(path))
        {
            paths.Add(path);
        }
    }
}
