using UnityEngine;
using UnityEngine.UI;

public class MapEditorSelectionClipboardService
{
    private readonly MapEditorMapEditingService mapEditing;
    private readonly System.Func<MapData> getMapData;
    private readonly System.Func<GridCell> getHoveredCell;
    private readonly System.Action<Vector2Int, int, int> ensureMapContainsRect;
    private readonly System.Action refreshAllCells;
    private readonly System.Action configureMapViewportVisual;
    private readonly System.Action updateBrushCursorPreview;
    private readonly System.Action refreshMinimap;

    private Vector2Int? selectionStart;
    private RectInt? selectedRect;
    private bool isSelecting;
    private MapEditorClipboard clipboard;

    public MapEditorSelectionClipboardService(
        MapEditorMapEditingService mapEditing,
        System.Func<MapData> getMapData,
        System.Func<GridCell> getHoveredCell,
        System.Action<Vector2Int, int, int> ensureMapContainsRect,
        System.Action refreshAllCells,
        System.Action configureMapViewportVisual,
        System.Action updateBrushCursorPreview,
        System.Action refreshMinimap)
    {
        this.mapEditing = mapEditing;
        this.getMapData = getMapData;
        this.getHoveredCell = getHoveredCell;
        this.ensureMapContainsRect = ensureMapContainsRect;
        this.refreshAllCells = refreshAllCells;
        this.configureMapViewportVisual = configureMapViewportVisual;
        this.updateBrushCursorPreview = updateBrushCursorPreview;
        this.refreshMinimap = refreshMinimap;
    }

    public RectInt? SelectionPreviewRect => selectedRect;

    public void ClearSelection()
    {
        selectedRect = null;
        selectionStart = null;
        isSelecting = false;
        updateBrushCursorPreview();
    }

    public void CancelActiveDrag()
    {
        if (!isSelecting && !selectionStart.HasValue)
        {
            return;
        }

        selectionStart = null;
        isSelecting = false;
        updateBrushCursorPreview();
    }

    public void SetSelectionRect(RectInt rect)
    {
        selectedRect = ClampSelectionRect(rect);
        selectionStart = null;
        isSelecting = false;
        updateBrushCursorPreview();
    }

    public void BeginSelectionDrag(GridCell cell)
    {
        if (cell == null)
        {
            return;
        }

        Vector2Int point = new Vector2Int(cell.X, cell.Y);
        selectionStart = point;
        selectedRect = new RectInt(point.x, point.y, 1, 1);
        isSelecting = true;
        updateBrushCursorPreview();
    }

    public void UpdateSelectionDrag(GridCell cell)
    {
        if (!isSelecting || !selectionStart.HasValue || cell == null)
        {
            return;
        }

        selectedRect = ClampSelectionRect(CreateRect(selectionStart.Value, new Vector2Int(cell.X, cell.Y)));
        updateBrushCursorPreview();
    }

    public void EndSelectionDrag(GridCell cell)
    {
        if (!isSelecting)
        {
            return;
        }

        if (cell != null)
        {
            UpdateSelectionDrag(cell);
        }

        isSelecting = false;
        selectionStart = null;
        updateBrushCursorPreview();

        if (selectedRect.HasValue)
        {
            Debug.Log("Selection: " + selectedRect.Value.width + "x" + selectedRect.Value.height);
        }
    }

    public void CopySelection()
    {
        if (!selectedRect.HasValue)
        {
            Debug.Log("No map selection to copy.");
            return;
        }

        clipboard = mapEditing.CopyRect(selectedRect.Value);
        Debug.Log("Selection copied: " + selectedRect.Value.width + "x" + selectedRect.Value.height);
    }

    public void CutSelection()
    {
        if (!selectedRect.HasValue)
        {
            Debug.Log("No map selection to cut.");
            return;
        }

        clipboard = mapEditing.CopyRect(selectedRect.Value);
        mapEditing.ClearRect(selectedRect.Value);
        selectedRect = null;
        selectionStart = null;
        updateBrushCursorPreview();
        refreshMinimap();
        Debug.Log("Selection cut.");
    }

    public void PasteClipboardAtCurrentTarget()
    {
        if (clipboard == null)
        {
            Debug.Log("Clipboard is empty.");
            return;
        }

        Vector2Int topLeft = GetPasteTopLeft();
        ensureMapContainsRect(topLeft, clipboard.width, clipboard.height);
        mapEditing.PasteClipboard(topLeft, clipboard);
        selectedRect = ClampSelectionRect(new RectInt(topLeft.x, topLeft.y, clipboard.width, clipboard.height));
        selectionStart = null;
        Canvas.ForceUpdateCanvases();
        refreshAllCells();
        configureMapViewportVisual();
        updateBrushCursorPreview();
        refreshMinimap();
        Debug.Log("Clipboard pasted at " + topLeft + " size " + clipboard.width + "x" + clipboard.height);
    }

    public Vector2Int GetPasteTopLeft()
    {
        GridCell hoveredCell = getHoveredCell();

        if (hoveredCell != null)
        {
            return new Vector2Int(hoveredCell.X, hoveredCell.Y);
        }

        if (selectedRect.HasValue)
        {
            return new Vector2Int(selectedRect.Value.xMin, selectedRect.Value.yMin);
        }

        return Vector2Int.zero;
    }

    private RectInt CreateRect(Vector2Int a, Vector2Int b)
    {
        int minX = Mathf.Min(a.x, b.x);
        int maxX = Mathf.Max(a.x, b.x);
        int minY = Mathf.Min(a.y, b.y);
        int maxY = Mathf.Max(a.y, b.y);
        return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private RectInt ClampSelectionRect(RectInt rect)
    {
        MapData mapData = getMapData();

        if (mapData == null)
        {
            return rect;
        }

        int minX = Mathf.Clamp(rect.xMin, 0, mapData.width - 1);
        int minY = Mathf.Clamp(rect.yMin, 0, mapData.height - 1);
        int maxX = Mathf.Clamp(rect.xMax - 1, 0, mapData.width - 1);
        int maxY = Mathf.Clamp(rect.yMax - 1, 0, mapData.height - 1);
        return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }
}
