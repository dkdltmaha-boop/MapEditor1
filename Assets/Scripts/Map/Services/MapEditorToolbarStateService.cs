using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class MapEditorToolbarStateService
{
    private static readonly Color SelectedButtonColor = new Color(0.18f, 0.48f, 0.95f, 1f);
    private static readonly Color NormalButtonColor = new Color(0.25f, 0.25f, 0.25f, 1f);

    private readonly Dictionary<EditorToolType, Image> toolButtonImages = new Dictionary<EditorToolType, Image>();
    private readonly Dictionary<MapEditorLayerType, Image> layerButtonImages = new Dictionary<MapEditorLayerType, Image>();

    private MapEditorManager manager;
    private Image brushPreviewImage;
    private Text brushPreviewText;
    private Text validationStatusText;
    private Transform recentPngListRoot;
    private Image runnerSpawnButtonImage;
    private Image seekerSpawnButtonImage;

    public void EnsureToolbar(MapEditorManager manager, Vector2 offset, IReadOnlyList<string> recentPngPaths)
    {
        this.manager = manager;
        MapEditorToolbarRefs toolbarRefs = MapEditorSceneUiBuilder.EnsureToolToolbar(manager, offset, recentPngPaths);
        toolButtonImages.Clear();

        if (toolbarRefs.toolButtonImages != null)
        {
            foreach (KeyValuePair<EditorToolType, Image> pair in toolbarRefs.toolButtonImages)
            {
                toolButtonImages[pair.Key] = pair.Value;
            }
        }

        brushPreviewImage = toolbarRefs.brushPreviewImage;
        brushPreviewText = toolbarRefs.brushPreviewText;
        validationStatusText = toolbarRefs.validationStatusText;
        recentPngListRoot = toolbarRefs.recentPngListRoot;
        runnerSpawnButtonImage = toolbarRefs.runnerSpawnButtonImage;
        seekerSpawnButtonImage = toolbarRefs.seekerSpawnButtonImage;

        layerButtonImages.Clear();
        Dictionary<MapEditorLayerType, Image> layerRefs = MapEditorLayerPanelBuilder.Ensure(manager, offset);

        foreach (KeyValuePair<MapEditorLayerType, Image> pair in layerRefs)
        {
            layerButtonImages[pair.Key] = pair.Value;
        }
    }

    public void RefreshRecentPngList(MapEditorManager manager, IReadOnlyList<string> recentPngPaths)
    {
        MapEditorSceneUiBuilder.RefreshRecentPngList(recentPngListRoot, manager, recentPngPaths);
    }

    public void RefreshToolSelection()
    {
        EditorToolType? currentTool = EditorToolController.Instance == null
            ? (EditorToolType?)null
            : EditorToolController.Instance.CurrentTool;

        if (manager != null && manager.showPlayerScaleGuide)
        {
            currentTool = null;
        }

        foreach (KeyValuePair<EditorToolType, Image> pair in toolButtonImages)
        {
            if (pair.Value == null)
            {
                continue;
            }

            bool selected = currentTool.HasValue && pair.Key == currentTool.Value;

            pair.Value.color = selected ? SelectedButtonColor : NormalButtonColor;
        }

        bool spawnToolSelected = currentTool == EditorToolType.Spawn;
        SetButtonSelected(runnerSpawnButtonImage, spawnToolSelected && manager != null && manager.SelectedSpawnRole == "Runner");
        SetButtonSelected(seekerSpawnButtonImage, spawnToolSelected && manager != null && manager.SelectedSpawnRole == "Seeker");

        RefreshLayerSelection();
    }

    public void RefreshLayerSelection()
    {
        if (manager == null)
        {
            return;
        }

        HashSet<Image> refreshedImages = new HashSet<Image>();
        foreach (KeyValuePair<MapEditorLayerType, Image> pair in layerButtonImages)
        {
            if (pair.Value == null || !refreshedImages.Add(pair.Value))
            {
                continue;
            }

            int canvasIndex = MapEditorLayerUtility.GetCanvasIndex(pair.Key);
            bool selected = canvasIndex >= 0 && canvasIndex == manager.ActiveCanvasIndex;
            pair.Value.color = selected ? SelectedButtonColor : NormalButtonColor;
        }
    }

    public void UpdateBrushPreview(
        Sprite selectedImageBrush,
        Color selectedColor,
        int selectedImageRotation,
        int brushSize,
        bool useWallTileBrush,
        bool previewFlipX,
        bool previewFlipY)
    {
        if (brushPreviewImage == null || brushPreviewText == null)
        {
            return;
        }

        if (selectedImageBrush != null)
        {
            brushPreviewImage.sprite = selectedImageBrush;
            brushPreviewImage.color = Color.white;
            ResetPreviewTransform(brushPreviewImage);
            brushPreviewImage.rectTransform.localScale = new Vector3(
                previewFlipX ? -1f : 1f,
                previewFlipY ? -1f : 1f,
                1f);
            brushPreviewText.text = (useWallTileBrush ? "벽 이미지" : "이미지") + " R" + selectedImageRotation + "\n크기 " + brushSize;
            return;
        }

        brushPreviewImage.sprite = null;
        brushPreviewImage.color = selectedColor;
        ResetPreviewTransform(brushPreviewImage);
        brushPreviewText.text = (useWallTileBrush ? "벽" : "색상") + "\n크기 " + brushSize;
    }

    public void UpdateValidationStatus(PixelChromaMapValidationReport report)
    {
        if (validationStatusText == null || report == null)
        {
            return;
        }

        validationStatusText.text =
            (report.isValid ? "검사 합격" : "검사 불합격") +
            "\n실패 " + report.errors.Count + " / 경고 " + report.warnings.Count;
        validationStatusText.color = report.isValid
            ? new Color(0.5f, 1f, 0.5f, 1f)
            : new Color(1f, 0.35f, 0.3f, 1f);
    }

    private static void ResetPreviewTransform(Image image)
    {
        RectTransform rect = image.GetComponent<RectTransform>();

        if (rect == null)
        {
            return;
        }

        rect.localEulerAngles = Vector3.zero;
        rect.localScale = Vector3.one;
    }

    private static void SetButtonSelected(Image image, bool selected)
    {
        if (image != null)
        {
            image.color = selected ? SelectedButtonColor : NormalButtonColor;
        }
    }
}
