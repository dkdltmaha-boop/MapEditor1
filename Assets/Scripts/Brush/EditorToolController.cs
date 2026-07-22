using System;
using UnityEngine;

public enum EditorToolType
{
    Brush,
    Eraser,
    Selection,
    Wall,
    Spawn
}

[ExecuteAlways]
public class EditorToolController : MonoBehaviour
{
    public static EditorToolController Instance { get; private set; }

    public EditorToolType CurrentTool { get; private set; } = EditorToolType.Brush;
    public event Action<EditorToolType> ToolChanged;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        Instance = this;
    }

    public void SetBrushTool()
    {
        SetTool(EditorToolType.Brush);
    }

    public void SetWallTool()
    {
        SetTool(EditorToolType.Wall);
    }

    public void SetEraserTool()
    {
        SetTool(EditorToolType.Eraser);
    }

    public void SetSelectionTool()
    {
        SetTool(EditorToolType.Selection);
    }

    public void SetSpawnTool()
    {
        SetTool(EditorToolType.Spawn);
    }

    public void SetTool(EditorToolType tool)
    {
        if (CurrentTool == tool)
        {
            return;
        }

        CurrentTool = tool;
        ToolChanged?.Invoke(CurrentTool);
        Debug.Log("Selected tool: " + CurrentTool);
    }
}
