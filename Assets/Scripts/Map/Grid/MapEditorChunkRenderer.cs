using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MapEditorChunkRenderer : MonoBehaviour
{
    public const int ChunkSize = 16;
    private const int PixelsPerTile = MapEditorManager.MaxExportCellPixels;
    private const int ChunksPerFrame = 2;
    private const float AnimationRefreshInterval = 0.1f;
    private const string RootName = "MapEditor_ChunkRoot";

    private sealed class ChunkVisual
    {
        public Vector2Int coordinate;
        public int tileWidth;
        public int tileHeight;
        public RectTransform rect;
        public RawImage image;
        public Texture2D texture;
        public Color32[] pixels;
        public bool hasAnimation;
    }

    private readonly Dictionary<Vector2Int, ChunkVisual> chunks = new Dictionary<Vector2Int, ChunkVisual>();
    private readonly Queue<Vector2Int> dirtyQueue = new Queue<Vector2Int>();
    private readonly HashSet<Vector2Int> dirtySet = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> animatedChunks = new HashSet<Vector2Int>();

    private GridGenerator generator;
    private MapEditorManager manager;
    private RectTransform root;
    private int mapWidth;
    private int mapHeight;
    private float nextAnimationRefreshTime;

    public bool IsActive { get; private set; }
    public int ChunkCount => chunks.Count;

    public void Configure(GridGenerator owner, MapEditorManager mapEditor, int width, int height)
    {
        DisposeVisuals();
        generator = owner;
        manager = mapEditor;
        mapWidth = Mathf.Max(1, width);
        mapHeight = Mathf.Max(1, height);
        IsActive = true;

        GameObject rootObject = new GameObject(RootName, typeof(RectTransform));
        rootObject.transform.SetParent(generator.gridParent, false);
        root = rootObject.GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = Vector2.zero;
        root.sizeDelta = new Vector2(mapWidth * generator.cellSize, mapHeight * generator.cellSize);
        root.SetAsFirstSibling();

        int chunkColumns = Mathf.CeilToInt(mapWidth / (float)ChunkSize);
        int chunkRows = Mathf.CeilToInt(mapHeight / (float)ChunkSize);

        for (int chunkY = 0; chunkY < chunkRows; chunkY++)
        {
            for (int chunkX = 0; chunkX < chunkColumns; chunkX++)
            {
                CreateChunk(chunkX, chunkY);
            }
        }

        MarkAllDirty();
    }

    public void Deactivate()
    {
        IsActive = false;
        DisposeVisuals();
    }

    public void RefreshLayout()
    {
        if (!IsActive || generator == null)
        {
            return;
        }

        if (root != null)
        {
            root.sizeDelta = new Vector2(mapWidth * generator.cellSize, mapHeight * generator.cellSize);
        }

        foreach (ChunkVisual chunk in chunks.Values)
        {
            PositionChunk(chunk);
        }
    }

    public void MarkCellDirty(int mapX, int mapY)
    {
        if (!IsActive || mapX < 0 || mapY < 0 || mapX >= mapWidth || mapY >= mapHeight)
        {
            return;
        }

        EnqueueDirty(new Vector2Int(mapX / ChunkSize, mapY / ChunkSize));
    }

    public void MarkAllDirty()
    {
        if (!IsActive)
        {
            return;
        }

        foreach (Vector2Int coordinate in chunks.Keys)
        {
            EnqueueDirty(coordinate);
        }
    }

    public void RenderAllNow()
    {
        while (dirtyQueue.Count > 0)
        {
            RenderNextChunk();
        }
    }

    public void RenderOneNow()
    {
        if (dirtyQueue.Count > 0)
        {
            RenderNextChunk();
        }
    }

    private void LateUpdate()
    {
        if (!IsActive)
        {
            return;
        }

        if (Time.unscaledTime >= nextAnimationRefreshTime)
        {
            foreach (Vector2Int coordinate in animatedChunks)
            {
                EnqueueDirty(coordinate);
            }

            nextAnimationRefreshTime = Time.unscaledTime + AnimationRefreshInterval;
        }

        for (int i = 0; i < ChunksPerFrame && dirtyQueue.Count > 0; i++)
        {
            RenderNextChunk();
        }
    }

    private void CreateChunk(int chunkX, int chunkY)
    {
        int tileWidth = Mathf.Min(ChunkSize, mapWidth - chunkX * ChunkSize);
        int tileHeight = Mathf.Min(ChunkSize, mapHeight - chunkY * ChunkSize);
        int textureWidth = tileWidth * PixelsPerTile;
        int textureHeight = tileHeight * PixelsPerTile;
        Vector2Int coordinate = new Vector2Int(chunkX, chunkY);

        GameObject chunkObject = new GameObject(
            $"Chunk_{chunkX}_{chunkY}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage));
        chunkObject.transform.SetParent(root, false);

        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
        {
            name = $"MapChunk_{chunkX}_{chunkY}",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        ChunkVisual chunk = new ChunkVisual
        {
            coordinate = coordinate,
            tileWidth = tileWidth,
            tileHeight = tileHeight,
            rect = chunkObject.GetComponent<RectTransform>(),
            image = chunkObject.GetComponent<RawImage>(),
            texture = texture,
            pixels = new Color32[textureWidth * textureHeight]
        };

        chunk.image.texture = texture;
        chunk.image.color = Color.white;
        chunk.image.raycastTarget = false;
        chunk.image.uvRect = new Rect(0f, 0f, 1f, 1f);
        PositionChunk(chunk);
        chunks.Add(coordinate, chunk);
    }

    private void PositionChunk(ChunkVisual chunk)
    {
        chunk.rect.anchorMin = new Vector2(0f, 1f);
        chunk.rect.anchorMax = new Vector2(0f, 1f);
        chunk.rect.pivot = new Vector2(0f, 1f);
        chunk.rect.anchoredPosition = new Vector2(
            chunk.coordinate.x * ChunkSize * generator.cellSize,
            -chunk.coordinate.y * ChunkSize * generator.cellSize);
        chunk.rect.sizeDelta = new Vector2(
            chunk.tileWidth * generator.cellSize,
            chunk.tileHeight * generator.cellSize);
    }

    private void EnqueueDirty(Vector2Int coordinate)
    {
        if (chunks.ContainsKey(coordinate) && dirtySet.Add(coordinate))
        {
            dirtyQueue.Enqueue(coordinate);
        }
    }

    private void RenderNextChunk()
    {
        Vector2Int coordinate = dirtyQueue.Dequeue();
        dirtySet.Remove(coordinate);

        if (!chunks.TryGetValue(coordinate, out ChunkVisual chunk) || manager == null)
        {
            return;
        }

        int textureWidth = chunk.tileWidth * PixelsPerTile;
        int chunkStartX = coordinate.x * ChunkSize;
        int chunkStartY = coordinate.y * ChunkSize;
        bool hasAnimation = false;

        for (int localY = 0; localY < chunk.tileHeight; localY++)
        {
            for (int localX = 0; localX < chunk.tileWidth; localX++)
            {
                int textureOffsetY = (chunk.tileHeight - localY - 1) * PixelsPerTile;
                hasAnimation |= manager.WriteCompositeCellPixels(
                    chunkStartX + localX,
                    chunkStartY + localY,
                    PixelsPerTile,
                    chunk.pixels,
                    textureWidth,
                    localX * PixelsPerTile,
                    textureOffsetY);

                string spawnRole = manager.GetSpawnRoleAt(chunkStartX + localX, chunkStartY + localY);
                if (!string.IsNullOrEmpty(spawnRole))
                {
                    DrawSpawnMarker(chunk.pixels, textureWidth, localX * PixelsPerTile, textureOffsetY, spawnRole);
                }
            }
        }

        chunk.texture.SetPixels32(chunk.pixels);
        chunk.texture.Apply(false, false);
        chunk.hasAnimation = hasAnimation;

        if (hasAnimation)
        {
            animatedChunks.Add(coordinate);
        }
        else
        {
            animatedChunks.Remove(coordinate);
        }
    }

    private static void DrawSpawnMarker(Color32[] pixels, int width, int offsetX, int offsetY, string role)
    {
        bool seeker = string.Equals(role, "Seeker", System.StringComparison.OrdinalIgnoreCase);
        Color32 marker = seeker
            ? new Color32(255, 71, 64, 255)
            : new Color32(26, 230, 255, 255);
        int center = PixelsPerTile / 2;

        for (int i = 3; i < PixelsPerTile - 3; i++)
        {
            if (seeker)
            {
                pixels[(offsetY + i) * width + offsetX + i] = marker;
                pixels[(offsetY + i) * width + offsetX + PixelsPerTile - 1 - i] = marker;
            }
            else
            {
                pixels[(offsetY + center) * width + offsetX + i] = marker;
                pixels[(offsetY + i) * width + offsetX + center] = marker;
            }
        }
    }

    private void DisposeVisuals()
    {
        foreach (ChunkVisual chunk in chunks.Values)
        {
            if (chunk.texture != null)
            {
                MapEditorObjectUtility.DestroyObject(chunk.texture);
            }
        }

        chunks.Clear();
        dirtyQueue.Clear();
        dirtySet.Clear();
        animatedChunks.Clear();

        if (root != null)
        {
            MapEditorObjectUtility.DestroyObject(root.gameObject);
            root = null;
        }
    }

    private void OnDestroy()
    {
        DisposeVisuals();
    }
}
