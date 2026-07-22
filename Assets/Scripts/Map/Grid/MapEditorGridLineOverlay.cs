using UnityEngine;
using UnityEngine.UI;

public sealed class MapEditorGridLineOverlay : Graphic
{
    public GridGenerator gridGenerator;
    public Color lineColor = new Color(0f, 0f, 0f, 0.24f);
    public float screenLineThickness = 1f;

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    public void Configure(GridGenerator generator)
    {
        gridGenerator = generator;
        color = lineColor;
        raycastTarget = false;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (gridGenerator == null || gridGenerator.width <= 0 || gridGenerator.height <= 0 || gridGenerator.cellSize <= 0f)
        {
            return;
        }

        float width = gridGenerator.width * gridGenerator.cellSize;
        float height = gridGenerator.height * gridGenerator.cellSize;
        float thickness = GetLocalLineThickness();

        for (int x = 0; x <= gridGenerator.width; x++)
        {
            float lineX = x * gridGenerator.cellSize;
            AddQuad(vh, lineX - thickness * 0.5f, -height, lineX + thickness * 0.5f, 0f);
        }

        for (int y = 0; y <= gridGenerator.height; y++)
        {
            float lineY = -y * gridGenerator.cellSize;
            AddQuad(vh, 0f, lineY - thickness * 0.5f, width, lineY + thickness * 0.5f);
        }
    }

    private float GetLocalLineThickness()
    {
        float scale = Mathf.Max(0.001f, transform.lossyScale.x);
        return Mathf.Max(0.25f, screenLineThickness / scale);
    }

    private void AddQuad(VertexHelper vh, float left, float bottom, float right, float top)
    {
        int startIndex = vh.currentVertCount;
        Color32 vertexColor = lineColor;

        vh.AddVert(new Vector3(left, bottom), vertexColor, Vector2.zero);
        vh.AddVert(new Vector3(left, top), vertexColor, Vector2.zero);
        vh.AddVert(new Vector3(right, top), vertexColor, Vector2.zero);
        vh.AddVert(new Vector3(right, bottom), vertexColor, Vector2.zero);
        vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
    }
}
