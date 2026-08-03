using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class MapEditorSceneUiBuilder
{
    private const string BackgroundObjectName = "MapEditor_Background";
    private const string LogoObjectName = "MapEditor_Logo";
    private const string QuitButtonObjectName = "MapEditor_QuitButton";

    public static void EnsureBackground()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();

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
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

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
        EnsureQuitButton(canvas);
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
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
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

    public static MapEditorMinimap EnsureMinimap(MapEditorManager manager, Vector2 offset, Vector2 size)
    {
        return MapEditorMinimapBuilder.Ensure(manager, offset, size);
    }
}
