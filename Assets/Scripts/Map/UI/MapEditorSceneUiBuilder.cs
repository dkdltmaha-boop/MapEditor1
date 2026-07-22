using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class MapEditorSceneUiBuilder
{
    private const string BackgroundObjectName = "MapEditor_Background";
    private const string BackgroundResourceName = "PixelChroma_Background";

    public static void EnsureBackground()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            return;
        }

        MapEditorObjectUtility.RemoveDuplicateManagedRoots(canvas.transform, null, BackgroundObjectName);
        Transform existing = canvas.transform.Find(BackgroundObjectName);
        RawImage background;

        if (existing == null)
        {
            GameObject backgroundObject = new GameObject(BackgroundObjectName, typeof(RectTransform), typeof(RawImage));
            backgroundObject.transform.SetParent(canvas.transform, false);
            background = backgroundObject.GetComponent<RawImage>();
        }
        else
        {
            background = existing.GetComponent<RawImage>();

            if (background == null)
            {
                background = existing.gameObject.AddComponent<RawImage>();
            }
        }

        Texture2D texture = Resources.Load<Texture2D>(BackgroundResourceName);

        if (texture != null)
        {
            background.texture = texture;
        }

        background.color = Color.white;
        background.raycastTarget = false;

        RectTransform rect = background.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);

        background.transform.SetAsFirstSibling();
    }

    public static MapEditorToolbarRefs EnsureToolToolbar(MapEditorManager manager, Vector2 offset, IReadOnlyList<string> recentPngPaths)
    {
        return MapEditorToolbarBuilder.Ensure(manager, offset, recentPngPaths);
    }

    public static void RefreshRecentPngList(Transform recentPngListRoot, MapEditorManager manager, IReadOnlyList<string> paths)
    {
        MapEditorToolbarBuilder.RefreshRecentPngList(recentPngListRoot, manager, paths);
    }

    public static MapEditorMinimap EnsureMinimap(MapEditorManager manager, Vector2 offset, Vector2 size)
    {
        return MapEditorMinimapBuilder.Ensure(manager, offset, size);
    }
}
