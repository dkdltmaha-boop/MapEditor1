using UnityEngine;
using UnityEngine.UI;

public sealed class MapEditorTitleBackdropLayout : MonoBehaviour
{
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;
    private RawImage sky;
    private RawImage ground;
    private RawImage clouds;
    private RawImage characters;
    private RawImage logo;

    public void Configure(RawImage skyImage, RawImage groundImage, RawImage cloudImage, RawImage characterImage, RawImage logoImage)
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
        if (canvasRect == null || canvasRect.rect.width <= 0f || canvasRect.rect.height <= 0f) return;
        float scale = Mathf.Min(canvasRect.rect.width / ReferenceWidth, canvasRect.rect.height / ReferenceHeight);
        ConfigureStretch(sky);
        ConfigureStretch(characters);
        ConfigureCentered(clouds, new Vector2(ReferenceWidth, ReferenceHeight) * scale);
        ConfigureBottomCentered(ground, new Vector2(1500f, 1500f) * scale, -357f * scale);
        ConfigureTopCentered(logo, new Vector2(850f, 420f) * scale, -250f * scale);
    }

    private static void ConfigureStretch(RawImage image)
    {
        if (image == null) return;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    private static void ConfigureCentered(RawImage image, Vector2 size)
    {
        if (image == null) return;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
    }

    private static void ConfigureBottomCentered(RawImage image, Vector2 size, float y)
    {
        if (image == null) return;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = size;
    }

    private static void ConfigureTopCentered(RawImage image, Vector2 size, float y)
    {
        if (image == null) return;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = size;
    }
}
