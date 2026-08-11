using System.Collections.Generic;

[System.Serializable]
public sealed class PixelChromaWorkshopPackageReport
{
    public string format = "PixelChromaWorkshopPackageReport";
    public int version = 1;
    public string mapId;
    public bool isValid;
    public string loaderStrategy = "RuntimeTilemapLoader";
    public string mapFormat = "PixelChromaMap";
    public int mapFormatVersion = 3;
    public int width;
    public int height;
    public int minX;
    public int minY;
    public int maxX;
    public int maxY;
    public int paintedTileCount;
    public int wallTileCount;
    public int colorTileCount;
    public int imageTileCount;
    public int animatedTileCount;
    public int animationDefinitionCount;
    public int invalidAnimationCount;
    public int tilesetCount;
    public int spawnPointCount;
    public int zoneCount;
    public int spawnX;
    public int spawnY;
    public bool manifestExists;
    public bool mapFileExists;
    public bool previewExists;
    public bool tilesetFolderExists;
    public bool steamUploadConfigExists;
    public bool readmeExists;
    public List<PixelChromaWorkshopPackageFileReport> files = new List<PixelChromaWorkshopPackageFileReport>();
    public List<string> errors = new List<string>();
    public List<string> warnings = new List<string>();
}

[System.Serializable]
public sealed class PixelChromaWorkshopPackageFileReport
{
    public string path;
    public long bytes;
    public string sha256;
}
