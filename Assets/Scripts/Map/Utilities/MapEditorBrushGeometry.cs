using System;
using UnityEngine;

public static class MapEditorBrushGeometry
{
    public static Vector2Int GetRotatedSize(int width, int height, int rotation)
    {
        int quarterTurn = MapEditorRotationUtility.NormalizeQuarterTurn(rotation) / 90;
        return quarterTurn % 2 == 0
            ? new Vector2Int(width, height)
            : new Vector2Int(height, width);
    }

    public static Vector2Int MapOutputToSource(
        int outputX,
        int outputY,
        int sourceWidth,
        int sourceHeight,
        int rotation,
        bool flipX,
        bool flipY)
    {
        Vector2Int outputSize = GetRotatedSize(sourceWidth, sourceHeight, rotation);
        int transformedX = flipX ? outputSize.x - 1 - outputX : outputX;
        int transformedY = flipY ? outputSize.y - 1 - outputY : outputY;
        int quarterTurn = MapEditorRotationUtility.NormalizeQuarterTurn(rotation) / 90;

        switch (quarterTurn)
        {
            case 1:
                return new Vector2Int(transformedY, sourceHeight - 1 - transformedX);
            case 2:
                return new Vector2Int(sourceWidth - 1 - transformedX, sourceHeight - 1 - transformedY);
            case 3:
                return new Vector2Int(sourceWidth - 1 - transformedY, transformedX);
            default:
                return new Vector2Int(transformedX, transformedY);
        }
    }

    public static void RasterizeLine(Vector2Int start, Vector2Int end, Action<Vector2Int> visit)
    {
        if (visit == null)
        {
            return;
        }

        int x = start.x;
        int y = start.y;
        int deltaX = Mathf.Abs(end.x - start.x);
        int deltaY = Mathf.Abs(end.y - start.y);
        int stepX = start.x < end.x ? 1 : -1;
        int stepY = start.y < end.y ? 1 : -1;
        int error = deltaX - deltaY;

        while (true)
        {
            visit(new Vector2Int(x, y));

            if (x == end.x && y == end.y)
            {
                return;
            }

            int doubledError = error * 2;

            if (doubledError > -deltaY)
            {
                error -= deltaY;
                x += stepX;
            }

            if (doubledError < deltaX)
            {
                error += deltaX;
                y += stepY;
            }
        }
    }
}
