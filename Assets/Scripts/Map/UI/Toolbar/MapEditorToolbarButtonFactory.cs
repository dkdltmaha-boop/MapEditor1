using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public static class MapEditorToolbarButtonFactory
{
    private const float ToolbarButtonHeight = 18f;
    private const int ToolbarButtonFontSize = 10;
    private const int ToolbarShortcutFontSize = 9;

    public static Button CreateActionButton(Transform parent, MapEditorManager manager, string label, string shortcut, string objectName = null)
    {
        return CreateActionButton(parent, manager, label, shortcut, GetToolbarAction(label), objectName);
    }

    public static Button CreateActionButton(Transform parent, MapEditorManager manager, string label, string shortcut, MapEditorToolbarAction action, string objectName = null)
    {
        GameObject buttonObject = new GameObject(objectName ?? label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, ToolbarButtonHeight);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;

        MapEditorToolbarButton toolbarButton = buttonObject.AddComponent<MapEditorToolbarButton>();
        ConfigureToolbarButton(toolbarButton, manager, action);
        EnsureButtonText(buttonObject.transform, label, shortcut);
        return button;
    }

    public static void ConfigureActionButton(Transform buttonTransform, MapEditorManager manager, string label, string shortcut)
    {
        ConfigureActionButton(buttonTransform, manager, label, shortcut, GetToolbarAction(label));
    }

    public static void ConfigureActionButton(Transform buttonTransform, MapEditorManager manager, string label, string shortcut, MapEditorToolbarAction action)
    {
        Image image = buttonTransform.GetComponent<Image>();

        if (image == null)
        {
            image = buttonTransform.gameObject.AddComponent<Image>();
            image.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        }

        Button button = buttonTransform.GetComponent<Button>();

        if (button == null)
        {
            button = buttonTransform.gameObject.AddComponent<Button>();
        }

        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;

        MapEditorToolbarButton toolbarButton = buttonTransform.GetComponent<MapEditorToolbarButton>();

        if (toolbarButton == null)
        {
            toolbarButton = buttonTransform.gameObject.AddComponent<MapEditorToolbarButton>();
        }

        ConfigureToolbarButton(toolbarButton, manager, action);
        EnsureButtonText(buttonTransform, label, shortcut);
    }

    public static Button CreateRecentPngButton(Transform parent, MapEditorManager manager, string path)
    {
        string label = manager == null ? Path.GetFileNameWithoutExtension(path) : manager.GetRecentResourceDisplayName(path);

        if (string.IsNullOrEmpty(label))
        {
            label = "PNG";
        }

        GameObject rowObject = new GameObject("RecentResource_" + Path.GetFileName(path), typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rowObject.transform.SetParent(parent, false);
        rowObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, ToolbarButtonHeight);
        HorizontalLayoutGroup row = rowObject.GetComponent<HorizontalLayoutGroup>();
        row.spacing = 2f;
        row.childControlWidth = false;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = true;

        string typeLabel = manager != null && manager.IsRegisteredTilesetPath(path) ? "TILE" : "PNG";
        Button button = CreateActionButton(rowObject.transform, manager, typeLabel, string.Empty, MapEditorToolbarAction.LoadRecentPng);
        button.name = "RecentLoad_" + Path.GetFileName(path);
        button.GetComponent<RectTransform>().sizeDelta = new Vector2(38f, ToolbarButtonHeight);

        MapEditorToolbarButton toolbarButton = button.GetComponent<MapEditorToolbarButton>();

        if (toolbarButton != null)
        {
            toolbarButton.action = MapEditorToolbarAction.LoadRecentPng;
            toolbarButton.stringArgument = path;
        }

        GameObject inputObject = new GameObject("RecentName", typeof(RectTransform), typeof(Image), typeof(InputField));
        inputObject.transform.SetParent(rowObject.transform, false);
        inputObject.GetComponent<RectTransform>().sizeDelta = new Vector2(126f, ToolbarButtonHeight);
        Image inputBackground = inputObject.GetComponent<Image>();
        inputBackground.color = new Color(0.09f, 0.09f, 0.09f, 1f);
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(inputObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(4f, 0f);
        textRect.offsetMax = new Vector2(-4f, 0f);
        Text inputText = textObject.GetComponent<Text>();
        inputText.font = MapEditorFontProvider.Default;
        inputText.fontSize = ToolbarShortcutFontSize;
        inputText.alignment = TextAnchor.MiddleLeft;
        inputText.color = Color.white;
        InputField input = inputObject.GetComponent<InputField>();
        input.targetGraphic = inputBackground;
        input.textComponent = inputText;
        input.text = label;
        input.characterLimit = 24;
        MapEditorRecentResourceNameInput rename = inputObject.AddComponent<MapEditorRecentResourceNameInput>();
        rename.manager = manager;
        rename.path = path;

        return button;
    }

    public static void CacheToolButton(Transform toolbar, Dictionary<EditorToolType, Image> buttonImages, string objectName, EditorToolType toolType)
    {
        Transform button = toolbar.Find(objectName);

        if (button == null)
        {
            Transform[] descendants = toolbar.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                if (descendants[i].name == objectName)
                {
                    button = descendants[i];
                    break;
                }
            }

            if (button == null) return;
        }

        Image image = button.GetComponent<Image>();

        if (image != null)
        {
            buttonImages[toolType] = image;
        }
    }

    private static void ConfigureToolbarButton(MapEditorToolbarButton toolbarButton, MapEditorManager manager, MapEditorToolbarAction action)
    {
        toolbarButton.manager = manager;
        toolbarButton.action = action;
        toolbarButton.stringArgument = string.Empty;

        Button button = toolbarButton.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveListener(toolbarButton.InvokeAction);
            button.onClick.AddListener(toolbarButton.InvokeAction);
        }
    }

    private static void EnsureButtonText(Transform buttonTransform, string label, string shortcut)
    {
        Transform textTransform = buttonTransform.Find("Text");
        Text text;

        if (textTransform == null)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(buttonTransform, false);

            RectTransform createdTextRect = textObject.GetComponent<RectTransform>();
            createdTextRect.anchorMin = Vector2.zero;
            createdTextRect.anchorMax = Vector2.one;
            createdTextRect.offsetMin = new Vector2(8f, 0f);
            createdTextRect.offsetMax = new Vector2(-8f, 0f);
            text = textObject.GetComponent<Text>();
        }
        else
        {
            text = textTransform.GetComponent<Text>();

            if (text == null)
            {
                text = textTransform.gameObject.AddComponent<Text>();
            }
        }

        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(6f, 0f);
        textRect.offsetMax = string.IsNullOrEmpty(shortcut) ? new Vector2(-6f, 0f) : new Vector2(-48f, 0f);

        text.font = MapEditorFontProvider.Default;
        text.fontSize = ToolbarButtonFontSize;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.white;
        text.raycastTarget = false;
        text.text = label;

        EnsureShortcutText(buttonTransform, shortcut);
    }

    private static void EnsureShortcutText(Transform buttonTransform, string shortcut)
    {
        Transform shortcutTransform = buttonTransform.Find("Shortcut");
        Text text;

        if (shortcutTransform == null)
        {
            GameObject shortcutObject = new GameObject("Shortcut", typeof(RectTransform), typeof(Text));
            shortcutObject.transform.SetParent(buttonTransform, false);

            RectTransform shortcutRect = shortcutObject.GetComponent<RectTransform>();
            shortcutRect.anchorMin = new Vector2(1f, 0f);
            shortcutRect.anchorMax = Vector2.one;
            shortcutRect.pivot = new Vector2(1f, 0.5f);
            shortcutRect.offsetMin = new Vector2(-48f, 0f);
            shortcutRect.offsetMax = new Vector2(-5f, 0f);
            text = shortcutObject.GetComponent<Text>();
        }
        else
        {
            text = shortcutTransform.GetComponent<Text>();

            if (text == null)
            {
                text = shortcutTransform.gameObject.AddComponent<Text>();
            }
        }

        text.font = MapEditorFontProvider.Default;
        text.fontSize = ToolbarShortcutFontSize;
        text.alignment = TextAnchor.MiddleRight;
        text.color = new Color(0.82f, 0.82f, 0.82f, 1f);
        text.raycastTarget = false;
        text.text = string.IsNullOrEmpty(shortcut) ? string.Empty : shortcut;
    }

    private static MapEditorToolbarAction GetToolbarAction(string label)
    {
        label = string.IsNullOrEmpty(label) ? string.Empty : label.Trim();

        switch (label)
        {
            case "Brush":
                return MapEditorToolbarAction.Brush;
            case "Wall":
                return MapEditorToolbarAction.Wall;
            case "Eraser":
            case "Erase Layer":
            case "EraseLayer":
                return MapEditorToolbarAction.Eraser;
            case "Select":
                return MapEditorToolbarAction.Select;
            case "Rotate":
            case "TileRotate":
                return MapEditorToolbarAction.Rotate;
            case "Flip H":
            case "FlipH":
                return MapEditorToolbarAction.FlipH;
            case "Flip V":
            case "FlipV":
                return MapEditorToolbarAction.FlipV;
            case "Copy":
                return MapEditorToolbarAction.Copy;
            case "Cut":
                return MapEditorToolbarAction.Cut;
            case "Paste":
                return MapEditorToolbarAction.Paste;
            case "Spawn":
            case "Set Spawn":
            case "SetSpawn":
                return MapEditorToolbarAction.SetSpawn;
            case "Eyedrop":
            case "Eyedropper":
                return MapEditorToolbarAction.Eyedropper;
            case "Undo":
                return MapEditorToolbarAction.Undo;
            case "Redo":
                return MapEditorToolbarAction.Redo;
            case "Save":
            case "Save Project":
            case "SaveProject":
            case "Save Edit":
            case "SaveEdit":
                return MapEditorToolbarAction.Save;
            case "Load":
            case "Load Project":
            case "LoadProject":
            case "Load Edit":
            case "LoadEdit":
                return MapEditorToolbarAction.Load;
            case "Import Game":
            case "ImportGame":
            case "Import Map":
            case "ImportMap":
            case "Import PixelChroma":
            case "ImportPixelChroma":
            case "Import PixelChroma Map":
            case "ImportPixelChromaMap":
                return MapEditorToolbarAction.ImportPixelChromaMap;
            case "Tilesets":
            case "Import Tileset":
            case "ImportTileset":
                return MapEditorToolbarAction.OpenTilesetLibrary;
            case "Load PNG":
            case "PNGLoad":
                return MapEditorToolbarAction.PngLoad;
            case "Paste PNG":
            case "PastePNG":
                return MapEditorToolbarAction.PastePng;
            case "Validate":
            case "Validate Map":
            case "ValidateMap":
                return MapEditorToolbarAction.ValidateMap;
            case "Export Game":
            case "ExportGame":
            case "Export PixelChroma":
            case "ExportPixelChroma":
            case "Game Out":
            case "GameOut":
                return MapEditorToolbarAction.ExportPixelChroma;
            case "Workshop":
            case "Export Workshop":
            case "ExportWorkshop":
                return MapEditorToolbarAction.ExportWorkshop;
            case "Clear":
                return MapEditorToolbarAction.Clear;
            default:
                return MapEditorToolbarAction.None;
        }
    }
}
