using UnityEngine;

public class EraserTool : MonoBehaviour
{
    public void Use(GridCell cell)
    {
        MapEditorManager.Instance.EraseCell(cell);
    }
}