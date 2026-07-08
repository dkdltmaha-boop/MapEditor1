using UnityEngine;

public enum EditorToolType
{
    Brush,
    Eraser
}

public class EditorToolController : MonoBehaviour
{
    public static EditorToolController Instance { get; private set; }

    public EditorToolType CurrentTool { get; private set; } = EditorToolType.Brush;

    private void Awake()
    {
        Instance = this;
    }

    public void SetBrushTool()
    {
        CurrentTool = EditorToolType.Brush;
        Debug.Log("브러시 선택");
    }

    public void SetEraserTool()
    {
        CurrentTool = EditorToolType.Eraser;
        Debug.Log("지우개 선택");
    }
}