using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
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

        imported += ImportAnimatedTilesets(library, projectPath);
        Debug.Log("PixelChroma 기본 타일셋/애니메이션 등록 완료: 새 항목 " + imported + "개");
        return imported;
    }

    private static int ImportAnimatedTilesets(MapEditorTilesetLibraryService library, string projectPath)
    {
        string animatedRoot = Path.Combine(projectPath, "Assets", "09. ScriptableObjects", "Animated");
        if (!Directory.Exists(animatedRoot)) return 0;

        int imported = 0;
        string[] assets = Directory.GetFiles(animatedRoot, "*.asset", SearchOption.AllDirectories);
        for (int i = 0; i < assets.Length; i++)
        {
            string yaml = File.ReadAllText(assets[i]);
            int spritesStart = yaml.IndexOf("m_AnimatedSprites:", StringComparison.Ordinal);
            int spritesEnd = spritesStart < 0 ? -1 : yaml.IndexOf("m_MinSpeed:", spritesStart, StringComparison.Ordinal);
            if (spritesStart < 0 || spritesEnd <= spritesStart) continue;
            string spriteBlock = yaml.Substring(spritesStart, spritesEnd - spritesStart);
            MatchCollection spriteRefs = Regex.Matches(
                spriteBlock,
                @"fileID:\s*(-?\d+),\s*guid:\s*([0-9a-fA-F]{32}),\s*type:\s*3");
            if (spriteRefs.Count < MapEditorTilesetLibraryService.MinAnimationFrameCount) continue;

            string spriteGuid = spriteRefs[0].Groups[2].Value;
            bool oneSpriteSheet = true;
            for (int frame = 1; frame < spriteRefs.Count; frame++)
            {
                if (!string.Equals(spriteGuid, spriteRefs[frame].Groups[2].Value, StringComparison.OrdinalIgnoreCase))
                {
                    oneSpriteSheet = false;
                    break;
                }
            }
            if (!oneSpriteSheet) continue;

            string pngPath = FindPngByGuid(projectPath, spriteGuid);
            if (string.IsNullOrEmpty(pngPath)) continue;

            ReadSpriteGrid(pngPath + ".meta", out int tileWidth, out int tileHeight, out int margin, out int spacing);
            MapEditorTilesetDefinition definition = FindBySourcePath(library, pngPath);
            bool created = false;
            string animationName = Path.GetFileNameWithoutExtension(assets[i]);
            if (definition == null)
            {
                created = library.Import(
                    pngPath,
                    "PixelChroma - " + animationName,
                    tileWidth,
                    tileHeight,
                    margin,
                    spacing,
                    MapEditorLayerType.Ground,
                    false,
                    out definition,
                    out string importError);
                if (!created)
                {
                    Debug.LogWarning("PixelChroma 애니메이션 타일셋을 가져오지 못했습니다: " + animationName + "\n" + importError);
                    continue;
                }
            }

            if (HasAnimation(definition, animationName))
            {
                if (created) imported++;
                continue;
            }

            int[] frameIds = new int[spriteRefs.Count];
            for (int frame = 0; frame < frameIds.Length; frame++) frameIds[frame] = frame;
            float fps = ReadFloat(yaml, "m_MinSpeed", 4f);
            if (library.AddAnimation(
                definition.id,
                animationName,
                frameIds,
                fps,
                true,
                out _,
                out string animationError))
            {
                imported++;
            }
            else
            {
                Debug.LogWarning("PixelChroma 애니메이션 정의를 등록하지 못했습니다: " + animationName + "\n" + animationError);
            }
        }

        return imported;
    }

    private static string FindPngByGuid(string projectPath, string guid)
    {
        string imageRoot = Path.Combine(projectPath, "Assets", "04. Images");
        if (!Directory.Exists(imageRoot)) return string.Empty;
        string marker = "guid: " + guid;
        string[] metas = Directory.GetFiles(imageRoot, "*.png.meta", SearchOption.AllDirectories);
        for (int i = 0; i < metas.Length; i++)
        {
            if (File.ReadAllText(metas[i]).Contains(marker))
            {
                return metas[i].Substring(0, metas[i].Length - ".meta".Length);
            }
        }
        return string.Empty;
    }

    private static void ReadSpriteGrid(string metaPath, out int width, out int height, out int margin, out int spacing)
    {
        width = 16;
        height = 16;
        margin = 0;
        spacing = 0;
        if (!File.Exists(metaPath)) return;

        string meta = File.ReadAllText(metaPath);
        MatchCollection rects = Regex.Matches(
            meta,
            @"rect:\s*\r?\n\s*serializedVersion:\s*\d+\s*\r?\n\s*x:\s*(-?\d+)\s*\r?\n\s*y:\s*(-?\d+)\s*\r?\n\s*width:\s*(\d+)\s*\r?\n\s*height:\s*(\d+)");
        if (rects.Count == 0) return;

        int firstX = int.Parse(rects[0].Groups[1].Value);
        int firstY = int.Parse(rects[0].Groups[2].Value);
        width = Mathf.Max(1, int.Parse(rects[0].Groups[3].Value));
        height = Mathf.Max(1, int.Parse(rects[0].Groups[4].Value));
        margin = Mathf.Max(0, Mathf.Min(firstX, firstY));
        if (rects.Count > 1)
        {
            int secondX = int.Parse(rects[1].Groups[1].Value);
            spacing = Mathf.Max(0, secondX - firstX - width);
        }
    }

    private static MapEditorTilesetDefinition FindBySourcePath(MapEditorTilesetLibraryService library, string path)
    {
        IReadOnlyList<MapEditorTilesetDefinition> definitions = library.Definitions;
        for (int i = 0; i < definitions.Count; i++)
        {
            MapEditorTilesetDefinition definition = definitions[i];
            if (definition != null && string.Equals(definition.sourcePath, path, StringComparison.OrdinalIgnoreCase))
            {
                return definition;
            }
        }
        return null;
    }

    private static bool HasAnimation(MapEditorTilesetDefinition definition, string displayName)
    {
        if (definition?.animations == null) return false;
        for (int i = 0; i < definition.animations.Length; i++)
        {
            if (definition.animations[i] != null
                && string.Equals(definition.animations[i].displayName, displayName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static float ReadFloat(string yaml, string fieldName, float fallback)
    {
        Match match = Regex.Match(yaml, @"^\s*" + Regex.Escape(fieldName) + @":\s*([0-9]+(?:\.[0-9]+)?)\s*$", RegexOptions.Multiline);
        return match.Success && float.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float value) ? value : fallback;
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
