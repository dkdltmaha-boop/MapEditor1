using System;
using System.Linq;
using UnityEngine;

public static class MapEditorBackgroundProvider
{
    private const string BackgroundFolder = "MapEditorBackgrounds";
    private const string LegacyBackground = "PixelChroma_Background";
    private const string LogoResource = "MapEditorLogo";

    public static Texture2D LoadLatest()
    {
        Texture2D[] candidates = Resources.LoadAll<Texture2D>(BackgroundFolder);

        if (candidates != null && candidates.Length > 0)
        {
            return candidates
                .Where(texture => texture != null)
                .OrderByDescending(texture => texture.name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }

        return Resources.Load<Texture2D>(LegacyBackground);
    }

    public static Texture2D LoadLogo()
    {
        return Resources.Load<Texture2D>(LogoResource);
    }
}
