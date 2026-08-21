using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class MapEditorAnimationFramePaletteGraphic : MaskableGraphic
{
    private int gridSize = 16;
    private readonly HashSet<int> selected = new HashSet<int>();
    private readonly HashSet<int> occupied = new HashSet<int>();

    public void Configure(int size, IEnumerable<int> selectedTiles, IEnumerable<int> occupiedTiles)
    {
        gridSize = MapEditorManager.NormalizePngPaletteGridSize(size);
        selected.Clear();
        occupied.Clear();
        if (selectedTiles != null) foreach (int id in selectedTiles) selected.Add(id);
        if (occupiedTiles != null) foreach (int id in occupiedTiles) occupied.Add(id);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = rectTransform.rect;
        float cellWidth = rect.width / gridSize;
        float cellHeight = rect.height / gridSize;

        foreach (int id in occupied)
        {
            if (!selected.Contains(id)) AddCellQuad(vh, rect, cellWidth, cellHeight, id, new Color32(150, 65, 42, 115));
        }
        foreach (int id in selected)
        {
            AddCellQuad(vh, rect, cellWidth, cellHeight, id, new Color32(46, 122, 242, 145));
        }

        Color32 lineColor = gridSize <= 32
            ? new Color32(255, 255, 255, 82)
            : gridSize == 64
                ? new Color32(255, 255, 255, 72)
                : new Color32(255, 255, 255, 58);
        const float thickness = 1f;
        for (int i = 0; i <= gridSize; i++)
        {
            float x = Mathf.Round(rect.xMin + i * cellWidth);
            AddQuad(vh, new Vector2(x - thickness * 0.5f, rect.yMin), new Vector2(x + thickness * 0.5f, rect.yMax), lineColor);
            float y = Mathf.Round(rect.yMin + i * cellHeight);
            AddQuad(vh, new Vector2(rect.xMin, y - thickness * 0.5f), new Vector2(rect.xMax, y + thickness * 0.5f), lineColor);
        }
    }

    private void AddCellQuad(VertexHelper vh, Rect rect, float cellWidth, float cellHeight, int id, Color32 color)
    {
        if (id < 0 || id >= gridSize * gridSize) return;
        int x = id % gridSize;
        int rowFromTop = id / gridSize;
        float xMin = rect.xMin + x * cellWidth;
        float yMax = rect.yMax - rowFromTop * cellHeight;
        AddQuad(vh, new Vector2(xMin, yMax - cellHeight), new Vector2(xMin + cellWidth, yMax), color);
    }

    private static void AddQuad(VertexHelper vh, Vector2 min, Vector2 max, Color32 color)
    {
        int start = vh.currentVertCount;
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;
        vertex.position = new Vector3(min.x, min.y); vh.AddVert(vertex);
        vertex.position = new Vector3(min.x, max.y); vh.AddVert(vertex);
        vertex.position = new Vector3(max.x, max.y); vh.AddVert(vertex);
        vertex.position = new Vector3(max.x, min.y); vh.AddVert(vertex);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }
}
