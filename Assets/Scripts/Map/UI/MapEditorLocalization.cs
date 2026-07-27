using UnityEngine;

public enum MapEditorLanguage
{
    Korean,
    English
}

public static class MapEditorLocalization
{
    private const string LanguagePreferenceKey = "MapEditor.Language";

    public static MapEditorLanguage Current
    {
        get
        {
            return (MapEditorLanguage)Mathf.Clamp(
                PlayerPrefs.GetInt(LanguagePreferenceKey, (int)MapEditorLanguage.Korean),
                (int)MapEditorLanguage.Korean,
                (int)MapEditorLanguage.English);
        }
    }

    public static string Choose(string korean, string english)
    {
        return Current == MapEditorLanguage.Korean ? korean : english;
    }

    public static void Toggle()
    {
        MapEditorLanguage next = Current == MapEditorLanguage.Korean
            ? MapEditorLanguage.English
            : MapEditorLanguage.Korean;
        PlayerPrefs.SetInt(LanguagePreferenceKey, (int)next);
        PlayerPrefs.Save();
    }
}
