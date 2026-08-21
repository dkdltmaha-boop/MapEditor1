using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(InputField))]
public class MapEditorCanvasNameInput : MonoBehaviour
{
    public MapEditorManager manager;
    public int canvasIndex;
    private InputField input;

    private void OnEnable()
    {
        input = GetComponent<InputField>();
        input.onEndEdit.RemoveListener(ApplyName);
        input.onEndEdit.AddListener(ApplyName);
    }

    private void OnDisable()
    {
        if (input != null) input.onEndEdit.RemoveListener(ApplyName);
    }

    private void ApplyName(string value)
    {
        MapEditorManager target = manager != null ? manager : MapEditorManager.Instance;
        if (target != null) target.SetCanvasDisplayName(canvasIndex, value);
    }
}
