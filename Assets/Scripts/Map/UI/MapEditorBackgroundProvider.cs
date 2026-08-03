using System;
using System.Linq;
using UnityEngine;

public static class MapEditorBackgroundProvider
{
    private const string TitleResourceFolder = "MapEditorTitle/";
    private const string LatestSky = TitleResourceFolder + "PixelChroma_TitleSky_20260803";
    private const string LatestGround = TitleResourceFolder + "PixelChroma_TitleGround_20260803";
    private const string LatestClouds = TitleResourceFolder + "PixelChroma_TitleClouds_20260803";
    private const string LatestCharacters = TitleResourceFolder + "PixelChroma_TitleCharacters_20260803";
    private const string LatestLogo = TitleResourceFolder + "PixelChroma_TitleLogo_20260803";
    private const string BackgroundFolder = "MapEditorBackgrounds";
    private const string LegacyBackground = "PixelChroma_Background";
    private const string LogoResource = "MapEditorLogo";

    public static Texture2D LoadLatest()
    {
        Texture2D latestSky = Resources.Load<Texture2D>(LatestSky);

        if (latestSky != null)
        {
            return latestSky;
        }

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
        return Resources.Load<Texture2D>(LatestLogo)
            ?? Resources.Load<Texture2D>(LogoResource);
    }

    public static Texture2D LoadGround() => Resources.Load<Texture2D>(LatestGround);

    public static Texture2D LoadClouds() => Resources.Load<Texture2D>(LatestClouds);

    public static Texture2D LoadCharacters() => Resources.Load<Texture2D>(LatestCharacters);
}
