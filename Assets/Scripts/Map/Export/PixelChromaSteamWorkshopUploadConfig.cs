[System.Serializable]
public sealed class PixelChromaSteamWorkshopUploadConfig
{
    public string format = "PixelChromaSteamWorkshopUploadConfig";
    public int version = 1;
    public string mapId;
    public string title;
    public string description;
    public string contentFolder = ".";
    public string previewFile = "preview.png";
    public string[] additionalPreviewFiles = System.Array.Empty<string>();
    public string visibility = "Public";
    public string[] tags = new string[] { "Map" };
}
