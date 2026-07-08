using UnityEngine;

public class BrushTool : MonoBehaviour
{
    public void Use(GridCell cell)
    {
        MapEditorManager.Instance.PaintCell(cell);
    }
}