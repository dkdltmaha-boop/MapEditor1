using UnityEngine;

[System.Serializable]
public class MapData
{
    public int width;
    public int height;

    public int[,] tileMap;

    public MapData(int width, int height)
    {
        this.width = width;
        this.height = height;

        tileMap = new int[width, height];

        Clear();
    }

    public void SetTile(int x, int y, int tileId)
    {
        if (!IsInside(x, y)) return;

        tileMap[x, y] = tileId;
    }

    public int GetTile(int x, int y)
    {
        if (!IsInside(x, y)) return -1;

        return tileMap[x, y];
    }

    public void Clear()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                tileMap[x, y] = -1;
            }
        }
    }

    public bool IsInside(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
}