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

        long expectedCells = (long)saveData.width * saveData.height;
        if (expectedCells > maxMapSize * (long)maxMapSize || saveData.tiles.LongLength > expectedCells * 2L)
        {
            return false;
        }

        if (saveData.layerTiles != null && saveData.layerTiles.Length > 128)
        {
            return false;
        }

        if (saveData.importedTilesets != null && saveData.importedTilesets.Length > 256)
        {
            return false;
        }

        if (saveData.embeddedPngAssets != null && saveData.embeddedPngAssets.Length > 512)
        {
            return false;
        }

        if (saveData.movingRegions != null && saveData.movingRegions.Length > 128)
        {
            return false;
        }

        if (saveData.previewRegions != null && saveData.previewRegions.Length > 64)
        {
            return false;
        }

        return saveData.tiles.Length >= saveData.width * saveData.height;
    }
}
