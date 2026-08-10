using System;
using UnityEngine;

public sealed class MapEditorBrushSelectionService
{
    private readonly Func<string, int, int, bool, bool, Sprite> getTransformedSprite;

    public MapEditorBrushSelectionService(Func<string, int, int, bool, bool, Sprite> getTransformedSprite)
    {
        this.getTransformedSprite = getTransformedSprite;
    }

    public void SelectColor(MapEditorManager manager, Color color)
    {
        manager.selectedColor = color;
        manager.useSelectedColor = true;
        ClearImageBrush(manager);
    }

    public void ClearImageBrush(MapEditorManager manager)
    {
        manager.selectedImageBrush = null;
        manager.selectedImagePath = string.Empty;
        manager.selectedImageIndex = -1;
        manager.selectedImageRotation = 0;
        manager.selectedImageFlipX = false;
        manager.selectedImageFlipY = false;
        manager.useSelectedColor = true;
    }

    public bool SelectImageBrush(MapEditorManager manager, Sprite sprite, string imagePath, int imageIndex)
    {
        return SelectImageBrush(manager, sprite, imagePath, imageIndex, 0, false, false);
    }

    public bool SelectImageBrush(MapEditorManager manager, Sprite sprite, string imagePath, int imageIndex, int rotation, bool flipX, bool flipY)
    {
        if (sprite == null)
        {
            return false;
        }

        manager.selectedImagePath = imagePath;
        manager.selectedImageIndex = imageIndex;
        manager.selectedImageRotation = MapEditorRotationUtility.NormalizeQuarterTurn(rotation);
        manager.selectedImageFlipX = flipX;
        manager.selectedImageFlipY = flipY;
        manager.selectedImageBrush = GetSelectedImageSprite(manager, sprite);
        manager.useSelectedColor = false;
        return true;
    }

    public void ChangeBrushSize(MapEditorManager manager, int delta)
    {
        int[] sizes = { 1, 2, 4, 8 };
        int currentIndex = 0;

        for (int i = 1; i < sizes.Length; i++)
        {
            if (Mathf.Abs(manager.brushSize - sizes[i]) < Mathf.Abs(manager.brushSize - sizes[currentIndex]))
            {
                currentIndex = i;
            }
        }

        manager.brushSize = sizes[Mathf.Clamp(currentIndex + (delta < 0 ? -1 : 1), 0, sizes.Length - 1)];
    }

    public bool RotateSelectedImageBrush(MapEditorManager manager)
    {
        if (manager.selectedImageBrush == null)
        {
            return false;
        }

        manager.selectedImageRotation = MapEditorRotationUtility.NormalizeQuarterTurn(manager.selectedImageRotation + 90);
        RefreshSelectedImageBrush(manager);
        return true;
    }

    public bool FlipSelectedImageBrushHorizontal(MapEditorManager manager)
    {
        if (manager.selectedImageBrush == null)
        {
            return false;
        }

        manager.selectedImageFlipX = !manager.selectedImageFlipX;
        RefreshSelectedImageBrush(manager);
        return true;
    }

    public bool FlipSelectedImageBrushVertical(MapEditorManager manager)
    {
        if (manager.selectedImageBrush == null)
        {
            return false;
        }

        manager.selectedImageFlipY = !manager.selectedImageFlipY;
        RefreshSelectedImageBrush(manager);
        return true;
    }

    public void RefreshSelectedImageBrush(MapEditorManager manager)
    {
        if (manager.selectedImageBrush == null)
        {
            return;
        }

        manager.selectedImageBrush = GetSelectedImageSprite(manager, manager.selectedImageBrush);
    }

    private Sprite GetSelectedImageSprite(MapEditorManager manager, Sprite fallback)
    {
        if (string.IsNullOrEmpty(manager.selectedImagePath) || manager.selectedImageIndex < 0)
        {
            return fallback;
        }

        Sprite transformedSprite = getTransformedSprite(
            manager.selectedImagePath,
            manager.selectedImageIndex,
            manager.selectedImageRotation,
            manager.selectedImageFlipX,
            manager.selectedImageFlipY
        );

        return transformedSprite != null ? transformedSprite : fallback;
    }
}
