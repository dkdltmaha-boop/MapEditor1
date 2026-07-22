public static class MapSaveDataValidator
{
    public static bool IsValid(MapSaveData saveData, int maxMapSize)
    {
        if (saveData == null || saveData.tiles == null)
        {
            return false;
        }

        if (saveData.width <= 0 || saveData.height <= 0 || saveData.width > maxMapSize || saveData.height > maxMapSize)
        {
            return false;
        }

        return saveData.tiles.Length >= saveData.width * saveData.height;
    }
}
