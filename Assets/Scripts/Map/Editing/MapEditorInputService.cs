using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MapEditorInputService
{
    private readonly MapEditorManager manager;
    private bool isMapPanning;
    private Vector3 lastPanMousePosition;

    public MapEditorInputService(MapEditorManager manager)
    {
        this.manager = manager;
    }

    public void Tick()
    {
        if (Input.GetMouseButtonUp(0))
        {
            manager.EndPointerDrag(null);
            manager.CommitEditTransaction();
        }

        HandleMapZoomInput();
        HandleMapPanInput();

        if (Input.GetKeyDown(manager.eyedropperKey) || Input.GetKeyDown(KeyCode.I))
        {
            manager.PickColorUnderMouse();
        }

        if (Input.GetKeyDown(KeyCode.F1))
        {
            manager.ShowPackageSaveGuide();
        }

        if (!manager.enableKeyboardShortcuts)
        {
            return;
        }

        HandleKeyboardShortcuts();
    }

    public static bool IsAreaFillModifierPressed()
    {
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    private void HandleKeyboardShortcuts()
    {
        if (MapEditorTileCreatorWindow.IsOpen)
        {
            return;
        }

        if (EventSystem.current != null
            && EventSystem.current.currentSelectedGameObject != null
            && EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() != null)
        {
            return;
        }

        bool control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Delete))
        {
            manager.ClearSelection();
        }

        if (!control)
        {
            if (manager.IsSelectionToolActive())
            {
                Vector2Int move = Vector2Int.zero;

                if (Input.GetKeyDown(KeyCode.UpArrow)) move = Vector2Int.down;
                else if (Input.GetKeyDown(KeyCode.RightArrow)) move = Vector2Int.right;
                else if (Input.GetKeyDown(KeyCode.DownArrow)) move = Vector2Int.up;
                else if (Input.GetKeyDown(KeyCode.LeftArrow)) move = Vector2Int.left;

                if (move != Vector2Int.zero)
                {
                    manager.MoveSelection(move);
                    return;
                }
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                manager.SetBrushTool();
            }
            else if (Input.GetKeyDown(KeyCode.L))
            {
                manager.SetLineTool();
            }
            else if (Input.GetKeyDown(KeyCode.G))
            {
                manager.SetRectangleFillTool();
            }
            else if (Input.GetKeyDown(KeyCode.W))
            {
                manager.SetWallTileTool();
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                manager.SetEraserTool();
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                manager.SetSelectionTool();
            }
            else if (Input.GetKeyDown(KeyCode.P))
            {
                manager.SetPreviewRegionTool();
            }
            else if (Input.GetKeyDown(KeyCode.LeftBracket))
            {
                manager.ChangeBrushSize(-1);
            }
            else if (Input.GetKeyDown(KeyCode.RightBracket))
            {
                manager.ChangeBrushSize(1);
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                manager.RotateSelectedImageBrush();
            }
            else if (Input.GetKeyDown(KeyCode.H))
            {
                manager.FlipSelectedImageBrushHorizontal();
            }
            else if (Input.GetKeyDown(KeyCode.V))
            {
                manager.FlipSelectedImageBrushVertical();
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            manager.CopySelection();
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            manager.CutSelection();
        }
        else if (Input.GetKeyDown(KeyCode.V))
        {
            manager.PasteClipboardAtHoveredCell();
        }
        else if (Input.GetKeyDown(KeyCode.P))
        {
            manager.PasteLoadedPngToMap();
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            manager.SaveMap();
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {
            manager.LoadMap();
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            manager.Undo();
        }
        else if (Input.GetKeyDown(KeyCode.Y))
        {
            manager.Redo();
        }
    }

    private void HandleMapZoomInput()
    {
        if (manager.GridGenerator != null && manager.GridGenerator.UsesChunkRendering)
        {
            return;
        }

        float scroll = Input.mouseScrollDelta.y;

        if (Mathf.Approximately(scroll, 0f) || !IsPointerOverMapGrid())
        {
            return;
        }

        manager.ZoomMap(scroll > 0f ? 1f : -1f);
    }

    private void HandleMapPanInput()
    {
        if (Input.GetMouseButtonDown(2) && IsPointerOverMapGrid())
        {
            isMapPanning = true;
            lastPanMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(2))
        {
            isMapPanning = false;
        }

        if (!isMapPanning)
        {
            return;
        }

        Vector3 mousePosition = Input.mousePosition;
        Vector3 delta = mousePosition - lastPanMousePosition;
        lastPanMousePosition = mousePosition;

        manager.PanMap(new Vector2(delta.x, delta.y));
    }

    private bool IsPointerOverMapGrid()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject.GetComponentInParent<GridCell>() != null
                || result.gameObject.GetComponentInParent<MapEditorGridInputSurface>() != null)
            {
                return true;
            }
        }

        return false;
    }
}
