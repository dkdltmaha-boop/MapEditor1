using UnityEngine;
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
    Line
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
    WallVisualExtra8 = 29
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
            case MapEditorToolbarAction.ExportPng:
                target.ExportMapPng();
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
        }
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
