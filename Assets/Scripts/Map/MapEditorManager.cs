using UnityEngine;

public class MapEditorManager : MonoBehaviour
{
    public static MapEditorManager Instance { get; private set; }

    [Header("맵 설정")]
    public int mapWidth = 16;
    public int mapHeight = 16;

    [Header("타일")]
    public int selectedTileId = 0;
    public TileDatabase tileDatabase;

    [Header("도구")]
    public BrushTool brushTool;
    public EraserTool eraserTool;

    public MapData CurrentMapData { get; private set; }

    private void Awake()
    {
        Instance = this;
        CurrentMapData = new MapData(mapWidth, mapHeight);
    }

    public void SelectTile(int tileId)
    {
        selectedTileId = tileId;
        Debug.Log("선택된 타일 ID: " + tileId);
    }

    public void UseCurrentTool(GridCell cell)
    {
        switch (EditorToolController.Instance.CurrentTool)
        {
            case EditorToolType.Brush:
                brushTool.Use(cell);
                break;

            case EditorToolType.Eraser:
                eraserTool.Use(cell);
                break;
        }
    }

    public void PaintCell(GridCell cell)
    {
        CurrentMapData.SetTile(cell.X, cell.Y, selectedTileId);

        Color color = tileDatabase.GetTileColor(selectedTileId);
        cell.SetColor(color);
    }

    public void EraseCell(GridCell cell)
    {
        CurrentMapData.SetTile(cell.X, cell.Y, -1);
        cell.SetColor(Color.white);
    }
}