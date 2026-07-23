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

        foreach (KeyValuePair<EditorToolType, Image> pair in toolButtonImages)
        {
            if (pair.Value == null)
            {
                continue;
            }

            bool selected = currentTool.HasValue && pair.Key == currentTool.Value;

            pair.Value.color = selected ? SelectedButtonColor : NormalButtonColor;
        }

        RefreshLayerSelection();
    }

    public void RefreshLayerSelection()
    {
        if (manager == null)
        {
            return;
        }

        foreach (KeyValuePair<MapEditorLayerType, Image> pair in layerButtonImages)
        {
            if (pair.Value == null)
            {
                continue;
            }

            pair.Value.color = pair.Key == manager.ActiveLayer ? SelectedButtonColor : NormalButtonColor;
        }
    }

    public void UpdateBrushPreview(Sprite selectedImageBrush, Color selectedColor, int selectedImageRotation, int brushSize, bool useWallTileBrush)
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
            "맵 검사: " + (report.isValid ? "통과" : "수정 필요") +
            "\n오류 " + report.errors.Count + " / 경고 " + report.warnings.Count;
        validationStatusText.color = report.isValid
            ? new Color(0.5f, 1f, 0.5f, 1f)
            : new Color(1f, 0.74f, 0.32f, 1f);
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
}
