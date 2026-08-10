using UnityEngine;
using UnityEngine.UI;

public class GridGenerator : MonoBehaviour
{
    private const string GridLineOverlayObjectName = "MapEditor_GridLineOverlay";
    private const int ChunkRenderingCellThreshold = 0;

    [Header("Grid")]
    public int width = 16;
    public int height = 16;
    public float cellSize = 32f;

    [Header("References")]
    public Transform gridParent;
    public GridCell cellPrefab;
    public MapEditorManager mapEditorManager;

    public bool UsesChunkRendering { get; private set; }
    public MapEditorChunkRenderer ChunkRenderer { get; private set; }

    private void Start()
    {
        GenerateGrid();
    }

    public void GenerateGrid()
    {
        if (gridParent == null)
        {
            Debug.LogWarning("GridGenerator에 gridParent가 연결되지 않았습니다.");
            return;
        }

        if (cellPrefab == null)
        {
            Debug.LogWarning("GridGenerator에 cellPrefab이 연결되지 않았습니다.");
            return;
        }

        if (mapEditorManager == null)
        {
            mapEditorManager = MapEditorManager.Instance;
        }

        if (mapEditorManager != null)
        {
            width = mapEditorManager.mapWidth;
            height = mapEditorManager.mapHeight;
            mapEditorManager.ClearRegisteredCells();
        }

        for (int i = gridParent.childCount - 1; i >= 0; i--)
        {
            Transform child = gridParent.GetChild(i);

            if (child != null && child.name != GridLineOverlayObjectName)
            {
                MapEditorObjectUtility.DestroyObject(child.gameObject);
            }
        }

        ApplyLayoutSize();

        UsesChunkRendering = (long)width * height > ChunkRenderingCellThreshold;

        if (UsesChunkRendering)
        {
            EnsureChunkRendering();
            EnsureGridLineOverlay();
            return;
        }

        DisableChunkRendering();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GridCell cell = Instantiate(cellPrefab, gridParent);
                ConfigureCellRect(cell.transform as RectTransform, x, y);
                cell.Init(x, y);

                if (mapEditorManager != null)
                {
                    mapEditorManager.RegisterCell(cell);
                    mapEditorManager.RefreshCell(cell);
                }
            }
        }

        EnsureGridLineOverlay();
    }

    public void ApplyLayoutSize()
    {
        if (gridParent == null)
        {
            return;
        }

        EnsureGridContentMask();

        GridLayoutGroup gridLayout = gridParent.GetComponent<GridLayoutGroup>();

        if (gridLayout != null)
        {
            gridLayout.enabled = false;
        }

        RectTransform rect = gridParent.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.sizeDelta = new Vector2(width * cellSize, height * cellSize);
        }

        for (int i = 0; i < gridParent.childCount; i++)
        {
            GridCell cell = gridParent.GetChild(i).GetComponent<GridCell>();

            if (cell != null)
            {
                ConfigureCellRect(cell.transform as RectTransform, cell.X, cell.Y);
            }
        }

        ChunkRenderer?.RefreshLayout();

        EnsureGridLineOverlay();
    }

    private void EnsureChunkRendering()
    {
        ChunkRenderer = gridParent.GetComponent<MapEditorChunkRenderer>();

        if (ChunkRenderer == null)
        {
            ChunkRenderer = gridParent.gameObject.AddComponent<MapEditorChunkRenderer>();
        }

        MapEditorGridInputSurface inputSurface = gridParent.GetComponent<MapEditorGridInputSurface>();
        if (inputSurface == null)
        {
            inputSurface = gridParent.gameObject.AddComponent<MapEditorGridInputSurface>();
        }

        Image raycastImage = gridParent.GetComponent<Image>();
        if (raycastImage == null)
        {
            raycastImage = gridParent.gameObject.AddComponent<Image>();
            raycastImage.color = Color.clear;
        }

        raycastImage.raycastTarget = true;
        inputSurface.Configure(this, mapEditorManager);
        ChunkRenderer.Configure(this, mapEditorManager, width, height);
    }

    private void DisableChunkRendering()
    {
        if (ChunkRenderer == null && gridParent != null)
        {
            ChunkRenderer = gridParent.GetComponent<MapEditorChunkRenderer>();
        }

        ChunkRenderer?.Deactivate();

        MapEditorGridInputSurface inputSurface = gridParent == null ? null : gridParent.GetComponent<MapEditorGridInputSurface>();
        if (inputSurface != null)
        {
            inputSurface.enabled = false;
        }
    }

    public void EnsureGridContentMask()
    {
        RectMask2D mask = gridParent.GetComponent<RectMask2D>();

        if (mask == null)
        {
            mask = gridParent.gameObject.AddComponent<RectMask2D>();
        }

        mask.padding = Vector4.zero;
        mask.softness = Vector2Int.zero;
    }

    private void ConfigureCellRect(RectTransform rect, int x, int y)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(cellSize, cellSize);
        rect.anchoredPosition = new Vector2(x * cellSize, -y * cellSize);
    }

    private void EnsureGridLineOverlay()
    {
        if (gridParent == null)
        {
            return;
        }

        Transform existing = gridParent.Find(GridLineOverlayObjectName);

        if (existing == null)
        {
            GameObject overlayObject = new GameObject(GridLineOverlayObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(MapEditorGridLineOverlay));
            overlayObject.transform.SetParent(gridParent, false);
            existing = overlayObject.transform;
        }

        RectTransform rect = existing as RectTransform;

        if (rect != null)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width * cellSize, height * cellSize);
        }

        MapEditorGridLineOverlay overlay = existing.GetComponent<MapEditorGridLineOverlay>();

        if (overlay == null)
        {
            overlay = existing.gameObject.AddComponent<MapEditorGridLineOverlay>();
        }

        overlay.Configure(this);
        existing.SetAsLastSibling();
    }
}
