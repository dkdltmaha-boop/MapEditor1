using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class MapEditorSceneUiBuilder
{
    private const string BackgroundObjectName = "MapEditor_Background";
    private const string GroundObjectName = "MapEditor_TitleGround";
    private const string CloudsObjectName = "MapEditor_TitleClouds";
    private const string CharactersObjectName = "MapEditor_TitleCharacters";
    private const string LogoObjectName = "MapEditor_Logo";
    private const string QuitButtonObjectName = "MapEditor_QuitButton";

    public static Canvas FindEditorCanvas()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        Canvas fallback = null;

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || !canvas.gameObject.scene.IsValid())
            {
                continue;
            }

            if (fallback == null)
            {
                fallback = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            }

            if (!canvas.isRootCanvas)
            {
                continue;
            }

            Transform root = canvas.transform;
            if (root.Find("MapEditor_MapViewport") != null
                || root.Find("MapEditor_Toolbar") != null
                || root.Find(BackgroundObjectName) != null
                || root.name == "Canvas")
            {
                return canvas;
            }
        }

        return fallback;
    }

    public static void EnsureBackground()
    {
        Canvas canvas = FindEditorCanvas();

        if (canvas == null)
        {
            return;
        }

        ConfigureCanvasScaler(canvas);
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
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        AspectRatioFitter fitter = background.GetComponent<AspectRatioFitter>();
        if (fitter != null)
        {
            MapEditorObjectUtility.DestroyObject(fitter);
        }

        background.transform.SetAsFirstSibling();
        EnsureTitleDecorations(canvas);
        EnsureLogo(canvas);
        EnsureTitleLayout(canvas, background);
        EnsureQuitButton(canvas);
    }

    private static void EnsureTitleDecorations(Canvas canvas)
    {
        EnsureDecorativeImage(
            canvas,
            CloudsObjectName,
            MapEditorBackgroundProvider.LoadClouds(),
            1);

        RawImage ground = EnsureDecorativeImage(
            canvas,
            GroundObjectName,
            MapEditorBackgroundProvider.LoadGround(),
            2);

        if (ground != null && ground.GetComponent<MapEditorBackgroundRotator>() == null)
        {
            ground.gameObject.AddComponent<MapEditorBackgroundRotator>();
        }

        EnsureDecorativeImage(
            canvas,
            CharactersObjectName,
            MapEditorBackgroundProvider.LoadCharacters(),
            3);
    }

    private static RawImage EnsureDecorativeImage(
        Canvas canvas,
        string objectName,
        Texture2D texture,
        int siblingIndex)
    {
        MapEditorObjectUtility.RemoveDuplicateManagedRoots(canvas.transform, null, objectName);
        Transform existing = canvas.transform.Find(objectName);
        RawImage image;

        if (existing == null)
        {
            GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(canvas.transform, false);
            image = imageObject.GetComponent<RawImage>();
        }
        else
        {
            image = existing.GetComponent<RawImage>();
            if (image == null)
            {
                image = existing.gameObject.AddComponent<RawImage>();
            }
        }

        image.texture = texture;
        image.color = Color.white;
        image.raycastTarget = false;
        image.gameObject.SetActive(texture != null);

        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        AspectRatioFitter fitter = image.GetComponent<AspectRatioFitter>();
        if (fitter != null)
        {
            MapEditorObjectUtility.DestroyObject(fitter);
        }

        image.transform.SetSiblingIndex(Mathf.Min(siblingIndex, canvas.transform.childCount - 1));
        return image;
    }

    private static void EnsureTitleLayout(Canvas canvas, RawImage background)
    {
        MapEditorTitleBackdropLayout layout = canvas.GetComponent<MapEditorTitleBackdropLayout>();
        if (layout == null)
        {
            layout = canvas.gameObject.AddComponent<MapEditorTitleBackdropLayout>();
        }

        layout.Configure(
            background,
            canvas.transform.Find(GroundObjectName)?.GetComponent<RawImage>(),
            canvas.transform.Find(CloudsObjectName)?.GetComponent<RawImage>(),
            canvas.transform.Find(CharactersObjectName)?.GetComponent<RawImage>(),
            canvas.transform.Find(LogoObjectName)?.GetComponent<RawImage>());
    }

    public static void ConfigureCanvasScaler(Canvas canvas)
    {
        if (canvas == null)
        {
            return;
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();

        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        scaler.referencePixelsPerUnit = 100f;
        canvas.pixelPerfect = true;
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
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -250f);
        rect.sizeDelta = new Vector2(850f, 420f);

        logo.gameObject.SetActive(texture != null);
        logo.transform.SetSiblingIndex(Mathf.Min(4, canvas.transform.childCount - 1));
    }

    public static void EnsureQuitButton(Canvas canvas)
    {
        if (canvas == null)
        {
            return;
        }

        MapEditorObjectUtility.RemoveDuplicateManagedRoots(canvas.transform, null, QuitButtonObjectName);
        Transform existing = canvas.transform.Find(QuitButtonObjectName);
        GameObject buttonObject;

        if (existing == null)
        {
            buttonObject = new GameObject(QuitButtonObjectName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(canvas.transform, false);
        }
        else
        {
            buttonObject = existing.gameObject;
        }

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-4f, -4f);
        rect.sizeDelta = new Vector2(30f, 30f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.72f, 0.16f, 0.18f, 0.98f);
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(QuitApplication);

        Transform textTransform = buttonObject.transform.Find("Text");
        Text label;

        if (textTransform == null)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(buttonObject.transform, false);
            label = textObject.GetComponent<Text>();
        }
        else
        {
            label = textTransform.GetComponent<Text>();
        }

        RectTransform textRect = label.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        label.text = "X";
        label.font = MapEditorFontProvider.Default;
        label.fontSize = 18;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;

        buttonObject.transform.SetAsLastSibling();
    }

    public static void BringQuitButtonToFront()
    {
        Canvas canvas = FindEditorCanvas();
        Transform button = canvas == null ? null : canvas.transform.Find(QuitButtonObjectName);

        if (button != null && button.GetSiblingIndex() != canvas.transform.childCount - 1)
        {
            button.SetAsLastSibling();
        }
    }

    private static void QuitApplication()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }
#else
        Application.Quit();
#endif
    }

    public static MapEditorToolbarRefs EnsureToolToolbar(MapEditorManager manager, Vector2 offset, IReadOnlyList<string> recentPngPaths)
    {
        return MapEditorToolbarBuilder.Ensure(manager, offset, recentPngPaths);
    }

    public static void RefreshRecentPngList(Transform recentPngListRoot, MapEditorManager manager, IReadOnlyList<string> paths)
    {
        MapEditorToolbarBuilder.RefreshRecentPngList(recentPngListRoot, manager, paths);
    }

    public static void RefreshAnimationTileList(Transform animationTileListRoot, MapEditorManager manager)
    {
        MapEditorToolbarBuilder.RefreshAnimationTileList(animationTileListRoot, manager);
    }

    public static void RefreshFavoriteTileList(Transform favoriteTileListRoot, MapEditorManager manager)
    {
        MapEditorToolbarBuilder.RefreshFavoriteTileList(favoriteTileListRoot, manager);
    }

    public static MapEditorMinimap EnsureMinimap(MapEditorManager manager, Vector2 offset, Vector2 size)
    {
        return MapEditorMinimapBuilder.Ensure(manager, offset, size);
    }
}
