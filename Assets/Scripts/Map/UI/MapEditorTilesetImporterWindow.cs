#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class MapEditorTilesetImporterWindow : EditorWindow
{
    private MapEditorManager manager;
    private string sourcePath = string.Empty;
    private string displayName = "새 타일셋";
    private int tileWidth = 16;
    private int tileHeight = 16;
    private int margin;
    private int spacing;
    private MapEditorLayerType defaultLayer = MapEditorLayerType.Ground;
    private bool collision;
    private Vector2 scroll;
    private Texture2D preview;

    public static void Open(MapEditorManager manager)
    {
        MapEditorTilesetImporterWindow window = GetWindow<MapEditorTilesetImporterWindow>(true, "타일셋 보관함", true);
        window.manager = manager;
        window.minSize = new Vector2(420f, 520f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("타일셋 가져오기", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.TextField("PNG", sourcePath);
            if (GUILayout.Button("찾아보기", GUILayout.Width(72f)))
            {
                Browse();
            }
        }

        displayName = EditorGUILayout.TextField("이름", displayName);
        tileWidth = Mathf.Max(1, EditorGUILayout.IntField("타일 너비", tileWidth));
        tileHeight = Mathf.Max(1, EditorGUILayout.IntField("타일 높이", tileHeight));
        margin = Mathf.Max(0, EditorGUILayout.IntField("바깥 여백", margin));
        spacing = Mathf.Max(0, EditorGUILayout.IntField("타일 간격", spacing));
        string[] layerNames = { "바닥", "오브젝트", "벽 모양", "벽 충돌", "시작 위치", "구역" };
        defaultLayer = (MapEditorLayerType)EditorGUILayout.Popup("기본 레이어", (int)defaultLayer, layerNames);
        collision = EditorGUILayout.Toggle("충돌 사용", collision);

        DrawPreview();

        GUI.enabled = manager != null && File.Exists(sourcePath);
        if (GUILayout.Button("가져와서 사용", GUILayout.Height(30f)))
        {
            manager.ImportTileset(sourcePath, displayName, tileWidth, tileHeight, margin, spacing, defaultLayer, collision);
        }
        GUI.enabled = true;

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("가져온 타일셋", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll);

        if (manager != null)
        {
            var definitions = manager.GetImportedTilesets();
            for (int i = 0; i < definitions.Count; i++)
            {
                MapEditorTilesetDefinition definition = definitions[i];
                if (definition == null)
                {
                    continue;
                }

                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(
                        definition.displayName + "  " + definition.tileWidth + "x" + definition.tileHeight + "px",
                        GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("사용", GUILayout.Width(52f)))
                    {
                        manager.UseImportedTileset(definition.id);
                    }
                    if (GUILayout.Button("삭제", GUILayout.Width(62f)))
                    {
                        manager.RemoveImportedTileset(definition.id);
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void Browse()
    {
        string path = EditorUtility.OpenFilePanel("타일셋 PNG 가져오기", string.Empty, "png");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        sourcePath = path;
        displayName = Path.GetFileNameWithoutExtension(path);
        LoadPreview(path);
    }

    private void LoadPreview(string path)
    {
        if (preview != null)
        {
            DestroyImmediate(preview);
        }

        preview = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!preview.LoadImage(File.ReadAllBytes(path), false))
        {
            DestroyImmediate(preview);
            preview = null;
        }
    }

    private void DrawPreview()
    {
        Rect rect = GUILayoutUtility.GetRect(200f, 200f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));
        if (preview == null)
        {
            GUI.Label(rect, "PNG 타일셋을 선택하세요", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        Rect fitted = Fit(rect, preview.width, preview.height);
        EditorGUI.DrawPreviewTexture(fitted, preview, null, ScaleMode.ScaleToFit);
        DrawGrid(fitted);
    }

    private void DrawGrid(Rect rect)
    {
        if (preview == null || tileWidth <= 0 || tileHeight <= 0)
        {
            return;
        }

        float scaleX = rect.width / preview.width;
        float scaleY = rect.height / preview.height;
        Handles.BeginGUI();
        Handles.color = new Color(1f, 0.85f, 0f, 0.8f);

        for (int x = margin; x + tileWidth <= preview.width - margin; x += tileWidth + spacing)
        {
            float screenX = rect.x + x * scaleX;
            Handles.DrawLine(new Vector3(screenX, rect.y), new Vector3(screenX, rect.yMax));
        }

        for (int y = margin; y + tileHeight <= preview.height - margin; y += tileHeight + spacing)
        {
            float screenY = rect.y + y * scaleY;
            Handles.DrawLine(new Vector3(rect.x, screenY), new Vector3(rect.xMax, screenY));
        }

        Handles.EndGUI();
    }

    private static Rect Fit(Rect area, float width, float height)
    {
        float scale = Mathf.Min(area.width / width, area.height / height);
        Vector2 size = new Vector2(width * scale, height * scale);
        return new Rect(area.center - size * 0.5f, size);
    }

    private void OnDisable()
    {
        if (preview != null)
        {
            DestroyImmediate(preview);
            preview = null;
        }
    }
}
#endif
