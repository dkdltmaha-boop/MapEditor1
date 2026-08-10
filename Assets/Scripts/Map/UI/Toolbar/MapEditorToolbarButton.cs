using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum MapEditorToolbarAction
{
    None,
    Brush,
    Wall,
    Eraser,
    Select,
    Rotate,
    FlipH,
    FlipV,
    Copy,
    Cut,
    Paste,
    SetSpawn,
    PreviewRegion = 13,
    Eyedropper,
    Undo,
    Redo,
    Save,
    Load,
    ImportPixelChromaMap,
    OpenTilesetLibrary,
    PngLoad,
    PastePng,
    ExportPng,
    ValidateMap,
    ExportPixelChroma,
    ExportWorkshop,
    PackageGuide,
    OpenSteamWorkshop,
    LoadRecentPng,
    MapPresetSquare,
    ExportCellPixels,
    WholeTilePaint,
    PngPaletteGridSize,
    SetLayer,
    ToggleLayerVisible,
    AddLayer,
    DeleteLayer,
    Clear,
    UploadWorkshop,
    OpenTileCreator,
    Line,
    BrushEraser,
    RectangleFill,
    ClearAll,
    ToggleBrushRoleMenu,
    SetBrushRole,
    SetCanvas,
    ToggleCanvasVisible,
    AddCanvas,
    DeleteCanvas
}

public enum MapEditorLayerType
{
    Ground = 0,
    Object = 1,
    WallVisual = 2,
    WallCollision = 3,
    Spawn = 4,
    Zone = 5,
    GroundExtra = 6,
    ObjectExtra = 7,
    WallVisualExtra = 8,
    GroundExtra2 = 9,
    GroundExtra3 = 10,
    GroundExtra4 = 11,
    GroundExtra5 = 12,
    GroundExtra6 = 13,
    GroundExtra7 = 14,
    GroundExtra8 = 15,
    ObjectExtra2 = 16,
    ObjectExtra3 = 17,
    ObjectExtra4 = 18,
    ObjectExtra5 = 19,
    ObjectExtra6 = 20,
    ObjectExtra7 = 21,
    ObjectExtra8 = 22,
    WallVisualExtra2 = 23,
    WallVisualExtra3 = 24,
    WallVisualExtra4 = 25,
    WallVisualExtra5 = 26,
    WallVisualExtra6 = 27,
    WallVisualExtra7 = 28,
    WallVisualExtra8 = 29,
    GroundExtra9 = 30,
    GroundExtra10 = 31,
    GroundExtra11 = 32,
    GroundExtra12 = 33,
    GroundExtra13 = 34,
    GroundExtra14 = 35,
    GroundExtra15 = 36,
    GroundExtra16 = 37,
    GroundExtra17 = 38,
    GroundExtra18 = 39,
    GroundExtra19 = 40,
    GroundExtra20 = 41,
    GroundExtra21 = 42,
    GroundExtra22 = 43,
    GroundExtra23 = 44,
    GroundExtra24 = 45,
    GroundExtra25 = 46,
    GroundExtra26 = 47,
    GroundExtra27 = 48,
    GroundExtra28 = 49,
    GroundExtra29 = 50,
    GroundExtra30 = 51,
    GroundExtra31 = 52,
    ObjectExtra9 = 53,
    ObjectExtra10 = 54,
    ObjectExtra11 = 55,
    ObjectExtra12 = 56,
    ObjectExtra13 = 57,
    ObjectExtra14 = 58,
    ObjectExtra15 = 59,
    ObjectExtra16 = 60,
    ObjectExtra17 = 61,
    ObjectExtra18 = 62,
    ObjectExtra19 = 63,
    ObjectExtra20 = 64,
    ObjectExtra21 = 65,
    ObjectExtra22 = 66,
    ObjectExtra23 = 67,
    ObjectExtra24 = 68,
    ObjectExtra25 = 69,
    ObjectExtra26 = 70,
    ObjectExtra27 = 71,
    ObjectExtra28 = 72,
    ObjectExtra29 = 73,
    ObjectExtra30 = 74,
    ObjectExtra31 = 75,
    WallVisualExtra9 = 76,
    WallVisualExtra10 = 77,
    WallVisualExtra11 = 78,
    WallVisualExtra12 = 79,
    WallVisualExtra13 = 80,
    WallVisualExtra14 = 81,
    WallVisualExtra15 = 82,
    WallVisualExtra16 = 83,
    WallVisualExtra17 = 84,
    WallVisualExtra18 = 85,
    WallVisualExtra19 = 86,
    WallVisualExtra20 = 87,
    WallVisualExtra21 = 88,
    WallVisualExtra22 = 89,
    WallVisualExtra23 = 90,
    WallVisualExtra24 = 91,
    WallVisualExtra25 = 92,
    WallVisualExtra26 = 93,
    WallVisualExtra27 = 94,
    WallVisualExtra28 = 95,
    WallVisualExtra29 = 96,
    WallVisualExtra30 = 97,
    WallVisualExtra31 = 98
}

[RequireComponent(typeof(Button))]
public class MapEditorToolbarButton : MonoBehaviour
{
    public MapEditorManager manager;
    public MapEditorToolbarAction action;
    public string stringArgument;
    public int intArgument;
    public int intArgument2;

    private Button button;

    private void OnEnable()
    {
        button = GetComponent<Button>();
        button.onClick.RemoveListener(InvokeAction);
        button.onClick.AddListener(InvokeAction);
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(InvokeAction);
        }
    }

    public void InvokeAction()
    {
        MapEditorManager target = manager != null ? manager : MapEditorManager.Instance;

        if (target == null)
        {
            return;
        }

        switch (action)
        {
            case MapEditorToolbarAction.Brush:
                target.SetBrushTool();
                break;
            case MapEditorToolbarAction.Wall:
                target.SetWallTileTool();
                break;
            case MapEditorToolbarAction.Eraser:
                target.SetEraserTool();
                break;
            case MapEditorToolbarAction.BrushEraser:
                target.SetBrushEraserTool();
                break;
            case MapEditorToolbarAction.RectangleFill:
                target.SetRectangleFillTool();
                break;
            case MapEditorToolbarAction.Select:
                target.SetSelectionTool();
                break;
            case MapEditorToolbarAction.Rotate:
                target.RotateSelectedImageBrush();
                break;
            case MapEditorToolbarAction.FlipH:
                target.FlipSelectedImageBrushHorizontal();
                break;
            case MapEditorToolbarAction.FlipV:
                target.FlipSelectedImageBrushVertical();
                break;
            case MapEditorToolbarAction.Copy:
                target.CopySelection();
                break;
            case MapEditorToolbarAction.Cut:
                target.CutSelection();
                break;
            case MapEditorToolbarAction.Paste:
                target.PasteClipboardAtHoveredCell();
                break;
            case MapEditorToolbarAction.SetSpawn:
                target.SetSpawnTool();
                break;
            case MapEditorToolbarAction.PreviewRegion:
                target.SetPreviewRegionTool();
                break;
            case MapEditorToolbarAction.Eyedropper:
                target.PickColorUnderMouse();
                break;
            case MapEditorToolbarAction.Undo:
                target.Undo();
                break;
            case MapEditorToolbarAction.Redo:
                target.Redo();
                break;
            case MapEditorToolbarAction.Save:
                target.SaveMap();
                break;
            case MapEditorToolbarAction.Load:
                target.LoadMap();
                break;
            case MapEditorToolbarAction.ImportPixelChromaMap:
                target.ImportPixelChromaMap();
                break;
            case MapEditorToolbarAction.OpenTilesetLibrary:
                target.OpenTilesetLibrary();
                break;
            case MapEditorToolbarAction.PngLoad:
                target.LoadPngPalette();
                break;
            case MapEditorToolbarAction.PastePng:
                target.PasteLoadedPngToMap();
                break;
            case MapEditorToolbarAction.ValidateMap:
                target.ValidatePixelChromaMap();
                break;
            case MapEditorToolbarAction.ExportPixelChroma:
                target.ExportForPixelChroma();
                break;
            case MapEditorToolbarAction.ExportWorkshop:
                target.ExportWorkshopPackage();
                break;
            case MapEditorToolbarAction.PackageGuide:
                target.ShowPackageSaveGuide();
                break;
            case MapEditorToolbarAction.OpenSteamWorkshop:
                target.OpenSteamWorkshopPage();
                break;
            case MapEditorToolbarAction.LoadRecentPng:
                target.LoadRecentPngPalette(stringArgument);
                break;
            case MapEditorToolbarAction.MapPresetSquare:
                target.ResizeMap(intArgument, intArgument2 > 0 ? intArgument2 : intArgument);
                break;
            case MapEditorToolbarAction.ExportCellPixels:
                target.SetExportCellPixels(intArgument);
                break;
            case MapEditorToolbarAction.WholeTilePaint:
                target.SetWholeTilePaintMode();
                break;
            case MapEditorToolbarAction.PngPaletteGridSize:
                target.SetPngPaletteGridSize(intArgument);
                break;
            case MapEditorToolbarAction.SetLayer:
                target.SetActiveLayer((MapEditorLayerType)intArgument);
                break;
            case MapEditorToolbarAction.ToggleLayerVisible:
                target.ToggleLayerVisible((MapEditorLayerType)intArgument);
                break;
            case MapEditorToolbarAction.AddLayer:
                target.AddUserLayer((MapEditorLayerType)intArgument);
                break;
            case MapEditorToolbarAction.DeleteLayer:
                target.DeleteActiveUserLayer();
                break;
            case MapEditorToolbarAction.Clear:
                target.ClearActiveLayer();
                break;
            case MapEditorToolbarAction.ClearAll:
                target.ClearMap();
                break;
            case MapEditorToolbarAction.UploadWorkshop:
                PixelChromaRuntimeWorkshopUploader uploader =
                    Object.FindFirstObjectByType<PixelChromaRuntimeWorkshopUploader>();

                if (uploader != null)
                {
                    uploader.ValidateAndUpload();
                }
                else
                {
                    Debug.LogWarning("창작마당 업로더를 찾을 수 없습니다.");
                }
                break;
            case MapEditorToolbarAction.OpenTileCreator:
                target.OpenTileCreator();
                break;
            case MapEditorToolbarAction.Line:
                target.SetLineTool();
                break;
            case MapEditorToolbarAction.ToggleBrushRoleMenu:
                target.ToggleBrushRoleMenu();
                break;
            case MapEditorToolbarAction.SetBrushRole:
                target.SetBrushLayerRole((MapEditorLayerType)intArgument);
                break;
            case MapEditorToolbarAction.SetCanvas:
                target.SetActiveCanvas(intArgument);
                break;
            case MapEditorToolbarAction.ToggleCanvasVisible:
                target.ToggleCanvasVisible(intArgument);
                break;
            case MapEditorToolbarAction.AddCanvas:
                target.AddCanvasLayer();
                break;
            case MapEditorToolbarAction.DeleteCanvas:
                target.DeleteActiveCanvasLayer();
                break;
        }
    }
}

[RequireComponent(typeof(InputField))]
public class MapEditorCanvasNameInput : MonoBehaviour
{
    public MapEditorManager manager;
    public int canvasIndex;

    private InputField input;

    private void OnEnable()
    {
        input = GetComponent<InputField>();
        input.onEndEdit.RemoveListener(ApplyName);
        input.onEndEdit.AddListener(ApplyName);
    }

    private void OnDisable()
    {
        if (input != null) input.onEndEdit.RemoveListener(ApplyName);
    }

    private void ApplyName(string value)
    {
        MapEditorManager target = manager != null ? manager : MapEditorManager.Instance;
        if (target != null) target.SetCanvasDisplayName(canvasIndex, value);
    }
}

public sealed class MapEditorCanvasLayerDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public MapEditorManager manager;
    public int canvasIndex;

    private CanvasGroup rowCanvasGroup;
    public void OnBeginDrag(PointerEventData eventData)
    {
        rowCanvasGroup = GetComponent<CanvasGroup>();
        if (rowCanvasGroup == null) rowCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        rowCanvasGroup.alpha = 0.55f;
        rowCanvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        MapEditorCanvasLayerDragHandle targetHandle = FindDropTarget(eventData);

        if (rowCanvasGroup != null)
        {
            rowCanvasGroup.alpha = 1f;
            rowCanvasGroup.blocksRaycasts = true;
        }

        if (manager != null && targetHandle != null)
        {
            manager.MoveCanvasLayer(canvasIndex, targetHandle.canvasIndex);
        }
    }

    private MapEditorCanvasLayerDragHandle FindDropTarget(PointerEventData eventData)
    {
        GameObject raycastTarget = eventData.pointerCurrentRaycast.gameObject;
        MapEditorCanvasLayerDragHandle raycastHandle = raycastTarget == null
            ? null
            : raycastTarget.GetComponentInParent<MapEditorCanvasLayerDragHandle>();
        if (raycastHandle != null && raycastHandle != this)
        {
            return raycastHandle;
        }

        Transform list = transform.parent;
        if (list == null)
        {
            return null;
        }

        MapEditorCanvasLayerDragHandle closest = null;
        float closestDistance = float.MaxValue;
        Camera eventCamera = eventData.pressEventCamera;

        for (int i = 0; i < list.childCount; i++)
        {
            MapEditorCanvasLayerDragHandle candidate = list.GetChild(i).GetComponent<MapEditorCanvasLayerDragHandle>();
            if (candidate == null || candidate == this)
            {
                continue;
            }

            RectTransform candidateRect = candidate.transform as RectTransform;
            if (candidateRect == null)
            {
                continue;
            }

            Vector3 worldCenter = candidateRect.TransformPoint(candidateRect.rect.center);
            float screenY = RectTransformUtility.WorldToScreenPoint(eventCamera, worldCenter).y;
            float distance = Mathf.Abs(eventData.position.y - screenY);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = candidate;
            }
        }

        return closest;
    }
}

[RequireComponent(typeof(InputField))]
public sealed class MapEditorRecentResourceNameInput : MonoBehaviour
{
    public MapEditorManager manager;
    public string path;
    private InputField input;

    private void OnEnable()
    {
        input = GetComponent<InputField>();
        input.onEndEdit.RemoveListener(ApplyName);
        input.onEndEdit.AddListener(ApplyName);
    }

    private void OnDisable()
    {
        if (input != null) input.onEndEdit.RemoveListener(ApplyName);
    }

    private void ApplyName(string value)
    {
        MapEditorManager target = manager != null ? manager : MapEditorManager.Instance;
        if (target != null) target.RenameRecentResource(path, value);
    }
}

[RequireComponent(typeof(InputField))]
public class MapEditorLayerNameInput : MonoBehaviour
{
    public MapEditorManager manager;
    public MapEditorLayerType layerType;

    private InputField input;

    private void OnEnable()
    {
        input = GetComponent<InputField>();
        input.onEndEdit.RemoveListener(ApplyName);
        input.onEndEdit.AddListener(ApplyName);
    }

    private void OnDisable()
    {
        if (input != null)
        {
            input.onEndEdit.RemoveListener(ApplyName);
        }
    }

    private void ApplyName(string value)
    {
        MapEditorManager target = manager != null ? manager : MapEditorManager.Instance;

        if (target != null)
        {
            target.SetLayerDisplayName(layerType, value);
        }
    }
}
