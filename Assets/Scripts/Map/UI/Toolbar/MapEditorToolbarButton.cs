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
    Clear,
    UploadWorkshop
}

public enum MapEditorLayerType
{
    Ground,
    Object,
    WallVisual,
    WallCollision,
    Spawn,
    Zone
}

[RequireComponent(typeof(Button))]
public class MapEditorToolbarButton : MonoBehaviour
{
    public MapEditorManager manager;
    public MapEditorToolbarAction action;
    public string stringArgument;
    public int intArgument;

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
                target.ResizeMap(intArgument, intArgument);
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
            case MapEditorToolbarAction.Clear:
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
        }
    }
}
