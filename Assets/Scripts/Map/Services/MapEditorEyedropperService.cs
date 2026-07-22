using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MapEditorEyedropperService
{
    public void PickUnderMouse(MapEditorManager manager)
    {
        if (EventSystem.current == null || manager == null)
        {
            return;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        if (TryPickPngPaletteColor(manager, results))
        {
            return;
        }

        if (TryPickGridCell(manager, results))
        {
            return;
        }

        TryPickUiImageColor(manager, results);
    }

    private static bool TryPickPngPaletteColor(MapEditorManager manager, List<RaycastResult> results)
    {
        foreach (RaycastResult result in results)
        {
            PngPaletteTile pngTile = result.gameObject.GetComponentInParent<PngPaletteTile>();

            if (pngTile != null && pngTile.TryPickColor(Input.mousePosition, result.module.eventCamera, out Color pickedColor))
            {
                manager.SelectColor(pickedColor);
                return true;
            }
        }

        return false;
    }

    private static bool TryPickGridCell(MapEditorManager manager, List<RaycastResult> results)
    {
        foreach (RaycastResult result in results)
        {
            GridCell cell = result.gameObject.GetComponentInParent<GridCell>();

            if (cell == null)
            {
                continue;
            }

            if (cell.CurrentSprite != null)
            {
                manager.SelectImageBrush(
                    cell.CurrentSprite,
                    cell.CurrentImagePath,
                    cell.CurrentImageIndex,
                    cell.CurrentImageRotation,
                    cell.CurrentImageFlipX,
                    cell.CurrentImageFlipY
                );

                if (cell.TileId == MapEditorManager.WallTileId)
                {
                    manager.SetWallTileTool();
                }

                manager.UpdateBrushPreview();
            }
            else
            {
                manager.SelectColor(cell.CurrentColor);

                if (cell.TileId == MapEditorManager.WallTileId)
                {
                    manager.SetWallTileTool();
                }
            }

            return true;
        }

        return false;
    }

    private static void TryPickUiImageColor(MapEditorManager manager, List<RaycastResult> results)
    {
        foreach (RaycastResult result in results)
        {
            Image image = result.gameObject.GetComponent<Image>();

            if (image != null)
            {
                manager.SelectColor(image.color);
                return;
            }
        }
    }
}
