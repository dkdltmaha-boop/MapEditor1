using System;
using UnityEngine;

public sealed class MapEditorPaintOperationService
{
    private readonly Action<int, int, int, Color, Sprite, string, int, int, bool, bool, bool> setCellTile;

    private Vector2Int? areaFillStart;

    public MapEditorPaintOperationService(Action<int, int, int, Color, Sprite, string, int, int, bool, bool, bool> setCellTile)
    {
        this.setCellTile = setCellTile;
    }

    public void Clear()
    {
        areaFillStart = null;
    }

    public void PaintCell(GridCell cell, MapEditorPaintSelection selection)
    {
        if (cell == null)
        {
            return;
        }

        if (selection.useWallTileBrush)
        {
            Color wallColor = selection.selectedImageBrush == null ? selection.selectedColor : Color.white;
            PaintBrushArea(cell.X, cell.Y, MapEditorManager.WallTileId, wallColor, selection.selectedImageBrush, selection.selectedImagePath, selection.selectedImageIndex, selection.selectedImageRotation, selection.selectedImageFlipX, selection.selectedImageFlipY, selection.brushSize, true);
            return;
        }

        if (selection.selectedImageBrush != null)
        {
            PaintBrushArea(cell.X, cell.Y, MapEditorManager.CustomImageTileId, Color.white, selection.selectedImageBrush, selection.selectedImagePath, selection.selectedImageIndex, selection.selectedImageRotation, selection.selectedImageFlipX, selection.selectedImageFlipY, selection.brushSize, true);
            return;
        }

        if (selection.useSelectedColor)
        {
            PaintBrushArea(cell.X, cell.Y, MapEditorManager.CustomColorTileId, selection.selectedColor, null, string.Empty, -1, 0, false, false, selection.brushSize, true);
        }
    }

    public void EraseCell(GridCell cell, int brushSize)
    {
        if (cell == null)
        {
            return;
        }

        PaintBrushArea(cell.X, cell.Y, -1, Color.white, null, string.Empty, -1, 0, false, false, brushSize, true);
    }

    public void HandleAreaFill(GridCell cell, MapEditorPaintSelection selection, Action beginTransaction, Action commitTransaction)
    {
        if (cell == null)
        {
            return;
        }

        Vector2Int point = new Vector2Int(cell.X, cell.Y);

        if (!areaFillStart.HasValue)
        {
            areaFillStart = point;
            Debug.Log("Area fill start: " + point);
            return;
        }

        beginTransaction?.Invoke();
        FillRect(areaFillStart.Value, point, selection);
        commitTransaction?.Invoke();
        Debug.Log("Area fill end: " + point);
        areaFillStart = null;
    }

    public bool TryGetAreaFillRect(GridCell currentCell, out RectInt areaRect)
    {
        areaRect = new RectInt();

        if (!areaFillStart.HasValue || currentCell == null)
        {
            return false;
        }

        Vector2Int start = areaFillStart.Value;
        int minX = Mathf.Min(start.x, currentCell.X);
        int maxX = Mathf.Max(start.x, currentCell.X);
        int minY = Mathf.Min(start.y, currentCell.Y);
        int maxY = Mathf.Max(start.y, currentCell.Y);
        areaRect = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        return true;
    }

    private void PaintBrushArea(int centerX, int centerY, int tileId, Color color, Sprite sprite, string imagePath, int imageIndex, int imageRotation, bool imageFlipX, bool imageFlipY, int brushSize, bool recordUndo)
    {
        int startX = centerX - brushSize / 2;
        int startY = centerY - brushSize / 2;
        int endX = startX + brushSize - 1;
        int endY = startY + brushSize - 1;

        for (int y = startY; y <= endY; y++)
        {
            for (int x = startX; x <= endX; x++)
            {
                setCellTile(x, y, tileId, color, sprite, imagePath, imageIndex, imageRotation, imageFlipX, imageFlipY, recordUndo);
            }
        }
    }

    private void FillRect(Vector2Int a, Vector2Int b, MapEditorPaintSelection selection)
    {
        int minX = Mathf.Min(a.x, b.x);
        int maxX = Mathf.Max(a.x, b.x);
        int minY = Mathf.Min(a.y, b.y);
        int maxY = Mathf.Max(a.y, b.y);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (selection.useWallTileBrush)
                {
                    Color wallColor = selection.selectedImageBrush == null ? selection.selectedColor : Color.white;
                    setCellTile(x, y, MapEditorManager.WallTileId, wallColor, selection.selectedImageBrush, selection.selectedImagePath, selection.selectedImageIndex, selection.selectedImageRotation, selection.selectedImageFlipX, selection.selectedImageFlipY, true);
                }
                else if (selection.selectedImageBrush != null)
                {
                    setCellTile(x, y, MapEditorManager.CustomImageTileId, Color.white, selection.selectedImageBrush, selection.selectedImagePath, selection.selectedImageIndex, selection.selectedImageRotation, selection.selectedImageFlipX, selection.selectedImageFlipY, true);
                }
                else
                {
                    setCellTile(x, y, MapEditorManager.CustomColorTileId, selection.selectedColor, null, string.Empty, -1, 0, false, false, true);
                }
            }
        }
    }
}
