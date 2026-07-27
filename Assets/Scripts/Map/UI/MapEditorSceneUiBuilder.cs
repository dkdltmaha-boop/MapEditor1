using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class MapEditorSceneUiBuilder
{
    private const string BackgroundObjectName = "MapEditor_Background";
    private const string LogoObjectName = "MapEditor_Logo";

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

        Texture2D texture = MapEditorBackgroundProvider.LoadLatest();

        if (texture != null)
        {
            background.texture = texture;
        }

        background.color = Color.white;
        background.raycastTarget = false;

        RectTransform rect = background.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        RectTransform canvasRect = canvas.transform as RectTransform;
        rect.sizeDelta = canvasRect == null ? new Vector2(1920f, 1080f) : canvasRect.rect.size;

        AspectRatioFitter fitter = background.GetComponent<AspectRatioFitter>();
        if (fitter == null)
        {
            fitter = background.gameObject.AddComponent<AspectRatioFitter>();
        }

        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = texture == null || texture.height <= 0
            ? 16f / 9f
            : texture.width / (float)texture.height;

        background.transform.SetAsFirstSibling();
        EnsureLogo(canvas);
    }

    private static void EnsureLogo(Canvas canvas)
    {
        if (canvas == null)
        {
            return;
        }

        MapEditorObjectUtility.RemoveDuplicateManagedRoots(canvas.transform, null, LogoObjectName);
        Transform existing = canvas.transform.Find(LogoObjectName);
        RawImage logo;

        if (existing == null)
        {
            GameObject logoObject = new GameObject(LogoObjectName, typeof(RectTransform), typeof(RawImage));
            logoObject.transform.SetParent(canvas.transform, false);
            logo = logoObject.GetComponent<RawImage>();
        }
        else
        {
            logo = existing.GetComponent<RawImage>();
            if (logo == null)
            {
                logo = existing.gameObject.AddComponent<RawImage>();
            }
        }

        Texture2D texture = MapEditorBackgroundProvider.LoadLogo();
        logo.texture = texture;
        logo.color = Color.white;
        logo.raycastTarget = false;

        RectTransform rect = logo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -8f);

        float aspect = texture == null || texture.height <= 0
            ? 158f / 72f
            : texture.width / (float)texture.height;
        float width = 250f;
        rect.sizeDelta = new Vector2(width, width / aspect);

        logo.gameObject.SetActive(texture != null);
        logo.transform.SetSiblingIndex(Mathf.Min(1, canvas.transform.childCount - 1));
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
