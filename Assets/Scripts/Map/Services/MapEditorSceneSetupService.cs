using UnityEngine;
using UnityEngine.UI;

public static class MapEditorSceneSetupService
{
    private const float LeftPanelWidth = 246f;
    private const float RightToolbarWidth = 176f;
    private const float MapSizePanelWidth = 184f;
    private const float MapSizePanelToolbarGap = 10f;
    private const float MapViewportLeftGap = 12f;
    private const float MapViewportRightGap = 24f;
    private const float MapViewportTopGap = 36f;
    private const float MapViewportBottomGap = 36f;
    private const float MapViewportMinSize = 240f;

    public static void RemoveLegacyToolButtons()
    {
        RemoveObjectByName("BrushButton");
        RemoveObjectByName("EraserButton");
        RemoveObjectByName("TilePalettePanel");
        RemoveObjectByName("Deprecated_TilePalettePanel_RemoveOnReload");
    }

    public static void RemoveMinimapObjects()
    {
        RemoveObjectByName("MapEditor_Minimap");
        RemoveObjectByName("MapMinimap");
    }

    public static void ConfigureMapViewportVisual(GridGenerator gridGenerator)
    {
        if (gridGenerator == null || gridGenerator.gridParent == null || gridGenerator.gridParent.parent == null)
        {
            return;
        }

        gridGenerator.EnsureGridContentMask();

        Image viewportImage = gridGenerator.gridParent.parent.GetComponent<Image>();

        if (viewportImage == null)
        {
            return;
        }

        viewportImage.color = new Color(1f, 1f, 1f, 0.392f);
        viewportImage.raycastTarget = false;

        ConfigureMapViewportRect(gridGenerator.gridParent.parent as RectTransform);
        gridGenerator.gridParent.SetAsLastSibling();
    }

    private static void ConfigureMapViewportRect(RectTransform viewportRect)
    {
        if (viewportRect == null)
        {
            return;
        }

        RectTransform canvasRect = viewportRect.parent as RectTransform;

        if (canvasRect == null)
        {
            return;
        }

        float left = LeftPanelWidth + MapViewportLeftGap;
        float right = RightToolbarWidth + MapSizePanelToolbarGap + MapSizePanelWidth + MapViewportRightGap;
        float availableWidth = Mathf.Max(MapViewportMinSize, canvasRect.rect.width - left - right);
        float availableHeight = Mathf.Max(MapViewportMinSize, canvasRect.rect.height - MapViewportTopGap - MapViewportBottomGap);
        float size = Mathf.Min(availableWidth, availableHeight);
        float centeredLeft = left + Mathf.Max(0f, (availableWidth - size) * 0.5f);

        viewportRect.anchorMin = new Vector2(0f, 1f);
        viewportRect.anchorMax = new Vector2(0f, 1f);
        viewportRect.pivot = new Vector2(0f, 1f);
        viewportRect.anchoredPosition = new Vector2(centeredLeft, -MapViewportTopGap);
        viewportRect.sizeDelta = new Vector2(size, size);
    }

    private static void RemoveObjectByName(string objectName)
    {
        Canvas canvas = MapEditorSceneUiBuilder.FindEditorCanvas();

        if (canvas == null)
        {
            return;
        }

        RemoveObjectByName(canvas.transform, objectName);
    }

    private static void RemoveObjectByName(Transform root, string objectName)
    {
        if (root == null)
        {
            return;
        }

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);

            if (child == null)
            {
                continue;
            }

            RemoveObjectByName(child, objectName);

            if (child != null && child.name == objectName && !IsInsideToolToolbar(child))
            {
                MapEditorObjectUtility.DestroyObject(child.gameObject);
            }
        }
    }

    private static bool IsInsideToolToolbar(Transform target)
    {
        while (target != null)
        {
            string targetName = target.name;

            if (targetName == "MapEditor_Toolbar" || targetName == "ToolToolbar" || targetName == "ToolPanel" || targetName == "Deprecated_ToolPanel_RemoveOnReload")
            {
                return true;
            }

            target = target.parent;
        }

        return false;
    }
}
