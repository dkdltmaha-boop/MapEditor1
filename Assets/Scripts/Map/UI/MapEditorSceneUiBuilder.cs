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

public sealed class MapEditorBackgroundRotator : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = 10f;

    private void Update()
    {
        transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
    }
}

public sealed class MapEditorTitleBackdropLayout : MonoBehaviour
{
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;

    private RawImage sky;
    private RawImage ground;
    private RawImage clouds;
    private RawImage characters;
    private RawImage logo;

    public void Configure(
        RawImage skyImage,
        RawImage groundImage,
        RawImage cloudImage,
        RawImage characterImage,
        RawImage logoImage)
    {
        sky = skyImage;
        ground = groundImage;
        clouds = cloudImage;
        characters = characterImage;
        logo = logoImage;
        ApplyLayout();
    }

    private void OnRectTransformDimensionsChange()
    {
        ApplyLayout();
    }

    private void ApplyLayout()
    {
        RectTransform canvasRect = transform as RectTransform;
        if (canvasRect == null || canvasRect.rect.width <= 0f || canvasRect.rect.height <= 0f)
        {
            return;
        }

        float scale = Mathf.Min(
            canvasRect.rect.width / ReferenceWidth,
            canvasRect.rect.height / ReferenceHeight);

        ConfigureStretch(sky);
        ConfigureStretch(characters);
        ConfigureCentered(clouds, new Vector2(ReferenceWidth, ReferenceHeight) * scale);
        ConfigureBottomCentered(ground, new Vector2(1500f, 1500f) * scale, -357f * scale);
        ConfigureTopCentered(logo, new Vector2(850f, 420f) * scale, -250f * scale);
    }

    private static void ConfigureStretch(RawImage image)
    {
        if (image == null)
        {
            return;
        }

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    private static void ConfigureCentered(RawImage image, Vector2 size)
    {
        if (image == null)
        {
            return;
        }

        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
    }

    private static void ConfigureBottomCentered(RawImage image, Vector2 size, float y)
    {
        if (image == null)
        {
            return;
        }

        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = size;
    }

    private static void ConfigureTopCentered(RawImage image, Vector2 size, float y)
    {
        if (image == null)
        {
            return;
        }

        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = size;
    }
}
