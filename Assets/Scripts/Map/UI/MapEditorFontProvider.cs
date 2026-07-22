using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MapEditorFontProvider
{
    private const string PaperlogyResourcePath = "Fonts/Paperlogy-4Regular";
    private static Font cachedFont;

    public static Font Default
    {
        get
        {
            if (cachedFont == null)
            {
                cachedFont = Resources.Load<Font>(PaperlogyResourcePath);
            }

            return cachedFont != null
                ? cachedFont
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }

    public static void ApplyToScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        Font font = Default;
        GameObject[] roots = scene.GetRootGameObjects();

        for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
        {
            Text[] labels = roots[rootIndex].GetComponentsInChildren<Text>(true);
            for (int labelIndex = 0; labelIndex < labels.Length; labelIndex++)
            {
                labels[labelIndex].font = font;
            }
        }
    }
}
