using UnityEngine;
using UnityEngine.UI;

public static class MapEditorMinimapBuilder
{
    private const string MinimapObjectName = "MapEditor_Minimap";
    private const string LegacyMinimapObjectName = "MapMinimap";
    private const string MinimapImageObjectName = "Minimap_Image";
    private const string LegacyMinimapImageObjectName = "Image";

    public static MapEditorMinimap Ensure(MapEditorManager manager, Vector2 offset, Vector2 size)
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            return null;
        }

        Transform root = FindMinimapRoot(canvas.transform);

        if (root == null)
        {
            root = CreateRoot(canvas.transform, offset, size);
        }
        else
        {
            ConfigureRoot(root, offset, size);
        }

        RemoveDuplicateRoots(canvas.transform, root);

        MapEditorMinimap minimap = root.GetComponentInChildren<MapEditorMinimap>();

        if (minimap == null || minimap.GetComponent<RawImage>() == null)
        {
            minimap = CreateImage(root);
        }

        minimap.Initialize(manager);
        return minimap;
    }

    private static Transform FindMinimapRoot(Transform canvas)
    {
        Transform root = canvas.Find(MinimapObjectName);

        if (root != null)
        {
            return root;
        }

        root = canvas.Find(LegacyMinimapObjectName);

        if (root != null)
        {
            root.name = MinimapObjectName;
        }

        return root;
    }

    private static Transform CreateRoot(Transform parent, Vector2 offset, Vector2 size)
    {
        GameObject root = new GameObject(MinimapObjectName, typeof(RectTransform), typeof(Image));
        root.transform.SetParent(parent, false);
        ConfigureRoot(root.transform, offset, size);

        Image background = root.GetComponent<Image>();
        background.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);
        background.raycastTarget = false;
        return root.transform;
    }

    private static void ConfigureRoot(Transform root, Vector2 offset, Vector2 size)
    {
        RectTransform rect = root.GetComponent<RectTransform>();

        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        RectTransform parentRect = root.parent as RectTransform;
        Vector2 clampedSize = size;
        Vector2 clampedOffset = offset;

        if (parentRect != null)
        {
            float maxWidth = Mathf.Max(64f, parentRect.rect.width - 16f);
            float maxHeight = Mathf.Max(64f, parentRect.rect.height - 16f);
            clampedSize = new Vector2(
                Mathf.Min(size.x, maxWidth),
                Mathf.Min(size.y, maxHeight)
            );

            clampedOffset.x = Mathf.Clamp(offset.x, -parentRect.rect.width + clampedSize.x + 8f, -8f);
            clampedOffset.y = Mathf.Clamp(offset.y, -parentRect.rect.height + clampedSize.y + 8f, -8f);
        }

        rect.anchoredPosition = clampedOffset;
        rect.sizeDelta = clampedSize;
    }

    private static MapEditorMinimap CreateImage(Transform parent)
    {
        Transform existingImage = MapEditorObjectUtility.FindAndRenameChild(parent, MinimapImageObjectName, LegacyMinimapImageObjectName);

        if (existingImage != null && existingImage.GetComponent<Image>() != null)
        {
            MapEditorObjectUtility.DestroyObject(existingImage.gameObject);
            existingImage = null;
        }

        GameObject imageObject = existingImage != null
            ? existingImage.gameObject
            : new GameObject(MinimapImageObjectName, typeof(RectTransform), typeof(RawImage), typeof(MapEditorMinimap), typeof(Outline));

        imageObject.transform.SetParent(parent, false);

        if (imageObject.GetComponent<RawImage>() == null)
        {
            imageObject.AddComponent<RawImage>();
        }

        MapEditorMinimap imageMinimap = imageObject.GetComponent<MapEditorMinimap>();

        if (imageMinimap == null)
        {
            imageMinimap = imageObject.AddComponent<MapEditorMinimap>();
        }

        Outline imageOutline = imageObject.GetComponent<Outline>();

        if (imageOutline == null)
        {
            imageOutline = imageObject.AddComponent<Outline>();
        }

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = new Vector2(8f, 8f);
        imageRect.offsetMax = new Vector2(-8f, -8f);

        imageOutline.effectColor = Color.black;
        imageOutline.effectDistance = new Vector2(2f, -2f);
        return imageMinimap;
    }

    private static void RemoveDuplicateRoots(Transform canvas, Transform keep)
    {
        for (int i = canvas.childCount - 1; i >= 0; i--)
        {
            Transform child = canvas.GetChild(i);

            if (child == null || child == keep)
            {
                continue;
            }

            if (child.name == MinimapObjectName || child.name == LegacyMinimapObjectName)
            {
                child.name = "Destroyed_" + child.name;
                MapEditorObjectUtility.DestroyObject(child.gameObject);
            }
        }
    }

}
