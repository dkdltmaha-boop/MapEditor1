using UnityEngine;

public class MapEditorViewportService
{
    private readonly System.Func<GridGenerator> getGridGenerator;
    private readonly System.Func<int> getMapWidth;
    private readonly System.Func<int> getMapHeight;
    private readonly System.Func<float> getMapZoomStep;
    private readonly System.Func<float> getMaxMapCellSize;
    private readonly System.Action syncMinimapView;

    public MapEditorViewportService(
        System.Func<GridGenerator> getGridGenerator,
        System.Func<int> getMapWidth,
        System.Func<int> getMapHeight,
        System.Func<float> getMapZoomStep,
        System.Func<float> getMaxMapCellSize,
        System.Action syncMinimapView)
    {
        this.getGridGenerator = getGridGenerator;
        this.getMapWidth = getMapWidth;
        this.getMapHeight = getMapHeight;
        this.getMapZoomStep = getMapZoomStep;
        this.getMaxMapCellSize = getMaxMapCellSize;
        this.syncMinimapView = syncMinimapView;
    }

    public void ZoomMap(float direction)
    {
        ZoomMap(direction, null);
    }

    public void ZoomMap(float direction, Vector2 screenPosition)
    {
        ZoomMap(direction, (Vector2?)screenPosition);
    }

    private void ZoomMap(float direction, Vector2? screenPosition)
    {
        GridGenerator gridGenerator = getGridGenerator();

        if (gridGenerator == null || gridGenerator.gridParent == null)
        {
            return;
        }

        RectTransform gridRect = gridGenerator.gridParent.GetComponent<RectTransform>();

        if (gridRect == null)
        {
            return;
        }

        float currentZoom = Mathf.Max(0.01f, gridRect.localScale.x);
        float zoomStep = getMapZoomStep() / Mathf.Max(1f, gridGenerator.cellSize);
        float minZoom = GetMinMapZoomForViewport(gridGenerator);
        float maxZoom = Mathf.Max(minZoom, getMaxMapCellSize() / Mathf.Max(1f, gridGenerator.cellSize));
        float nextZoom = Mathf.Clamp(currentZoom + direction * zoomStep, minZoom, maxZoom);

        if (Mathf.Approximately(nextZoom, currentZoom))
        {
            return;
        }

        RectTransform viewportRect = gridRect.parent as RectTransform;
        Camera eventCamera = GetEventCamera(gridRect);
        Vector2 gridLocalFocus = Vector2.zero;
        Vector2 viewportLocalFocus = Vector2.zero;
        bool preserveFocus = viewportRect != null;

        if (preserveFocus)
        {
            Vector2 focusScreenPosition = screenPosition ?? RectTransformUtility.WorldToScreenPoint(
                eventCamera,
                viewportRect.TransformPoint(viewportRect.rect.center));
            preserveFocus = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    gridRect,
                    focusScreenPosition,
                    eventCamera,
                    out gridLocalFocus)
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    viewportRect,
                    focusScreenPosition,
                    eventCamera,
                    out viewportLocalFocus);
        }

        gridRect.localScale = new Vector3(nextZoom, nextZoom, 1f);

        if (preserveFocus)
        {
            Vector2 scaledFocusInViewport = viewportRect.InverseTransformPoint(gridRect.TransformPoint(gridLocalFocus));
            gridRect.anchoredPosition += viewportLocalFocus - scaledFocusInViewport;
        }

        ClampMapToViewport();
        syncMinimapView();
    }

    public void PanMap(Vector2 delta)
    {
        GridGenerator gridGenerator = getGridGenerator();

        if (gridGenerator == null || gridGenerator.gridParent == null)
        {
            return;
        }

        RectTransform rect = gridGenerator.gridParent.GetComponent<RectTransform>();

        if (rect == null)
        {
            return;
        }

        rect.anchoredPosition += delta;
        ClampMapToViewport();
        syncMinimapView();
    }

    public void CenterMapOnNormalizedPosition(float normalizedX, float normalizedY)
    {
        GridGenerator gridGenerator = getGridGenerator();

        if (gridGenerator == null || gridGenerator.gridParent == null)
        {
            return;
        }

        RectTransform gridRect = gridGenerator.gridParent.GetComponent<RectTransform>();
        RectTransform viewportRect = gridGenerator.gridParent.parent as RectTransform;

        if (gridRect == null || viewportRect == null)
        {
            return;
        }

        Vector2 scaledContentSize = GetScaledRectSize(gridRect);
        float targetX = normalizedX * scaledContentSize.x;
        float targetY = normalizedY * scaledContentSize.y;
        Vector2 viewportCenter = viewportRect.rect.size * 0.5f;
        gridRect.anchoredPosition = new Vector2(viewportCenter.x - targetX, targetY - viewportCenter.y);

        ClampMapToViewport();
        syncMinimapView();
    }

    public void ClampMapToViewport()
    {
        GridGenerator gridGenerator = getGridGenerator();

        if (gridGenerator == null || gridGenerator.gridParent == null)
        {
            return;
        }

        RectTransform gridRect = gridGenerator.gridParent.GetComponent<RectTransform>();
        RectTransform viewportRect = gridGenerator.gridParent.parent as RectTransform;

        if (gridRect == null || viewportRect == null)
        {
            return;
        }

        Vector2 contentSize = GetScaledRectSize(gridRect);
        Vector2 viewportSize = viewportRect.rect.size;
        Vector2 position = gridRect.anchoredPosition;

        position.x = ClampAxisToViewport(position.x, contentSize.x, viewportSize.x);
        position.y = ClampAxisToViewport(position.y, contentSize.y, viewportSize.y);
        gridRect.anchoredPosition = position;
    }

    public bool TryGetMapViewNormalizedRect(out Rect normalizedRect)
    {
        normalizedRect = new Rect(0f, 0f, 1f, 1f);
        GridGenerator gridGenerator = getGridGenerator();

        if (gridGenerator == null || gridGenerator.gridParent == null)
        {
            return false;
        }

        RectTransform gridRect = gridGenerator.gridParent.GetComponent<RectTransform>();
        RectTransform viewportRect = gridGenerator.gridParent.parent as RectTransform;
        Vector2 contentSize = GetScaledRectSize(gridRect);

        if (gridRect == null || viewportRect == null || contentSize.x <= 0f || contentSize.y <= 0f)
        {
            return false;
        }

        Vector2 position = gridRect.anchoredPosition;
        Vector2 viewportSize = viewportRect.rect.size;

        float minX = Mathf.Clamp01(-position.x / contentSize.x);
        float maxX = Mathf.Clamp01((-position.x + viewportSize.x) / contentSize.x);
        float minY = Mathf.Clamp01(position.y / contentSize.y);
        float maxY = Mathf.Clamp01((position.y + viewportSize.y) / contentSize.y);

        normalizedRect = Rect.MinMaxRect(minX, minY, maxX, maxY);
        return true;
    }

    private float GetMinMapZoomForViewport(GridGenerator gridGenerator)
    {
        if (gridGenerator == null || gridGenerator.gridParent == null)
        {
            return 1f;
        }

        RectTransform viewportRect = gridGenerator.gridParent.parent as RectTransform;

        if (viewportRect == null || getMapWidth() <= 0 || getMapHeight() <= 0 || gridGenerator.cellSize <= 0f)
        {
            return 1f;
        }

        float contentWidth = getMapWidth() * gridGenerator.cellSize;
        float contentHeight = getMapHeight() * gridGenerator.cellSize;

        if (contentWidth <= 0f || contentHeight <= 0f)
        {
            return 1f;
        }

        float fitWidthZoom = viewportRect.rect.width / contentWidth;
        float fitHeightZoom = viewportRect.rect.height / contentHeight;
        return Mathf.Min(1f, Mathf.Min(fitWidthZoom, fitHeightZoom));
    }

    private Vector2 GetScaledRectSize(RectTransform rect)
    {
        if (rect == null)
        {
            return Vector2.zero;
        }

        return new Vector2(rect.sizeDelta.x * rect.localScale.x, rect.sizeDelta.y * rect.localScale.y);
    }

    private float ClampAxisToViewport(float position, float contentSize, float viewportSize)
    {
        if (contentSize <= viewportSize)
        {
            return 0f;
        }

        float maxOffset = (contentSize - viewportSize) * 0.5f;
        return Mathf.Clamp(position, -maxOffset, maxOffset);
    }

    private static Camera GetEventCamera(RectTransform rect)
    {
        Canvas canvas = rect == null ? null : rect.GetComponentInParent<Canvas>();
        return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
    }
}
