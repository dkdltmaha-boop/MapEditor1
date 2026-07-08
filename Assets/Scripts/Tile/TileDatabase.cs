using UnityEngine;

[System.Serializable]
public class TileInfo
{
    public int id;
    public string tileName;
    public Color color;
}

public class TileDatabase : MonoBehaviour
{
    public TileInfo[] tiles;

    public TileInfo GetTile(int id)
    {
        foreach (TileInfo tile in tiles)
        {
            if (tile.id == id)
            {
                return tile;
            }
        }

        return null;
    }

    public Color GetTileColor(int id)
    {
        TileInfo tile = GetTile(id);

        if (tile == null)
        {
            return Color.white;
        }

        return tile.color;
    }
}