using UnityEngine;
using UnityEngine.EventSystems;

public sealed class MapEditorRecentResourceDragItem : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    public MapEditorManager manager;
    public string path;
    private CanvasGroup canvasGroup;

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        MapEditorRecentResourceDragItem target = eventData.pointerCurrentRaycast.gameObject == null
            ? null
            : eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<MapEditorRecentResourceDragItem>();
        MapEditorManager targetManager = manager != null ? manager : MapEditorManager.Instance;
        if (targetManager != null && target != null && target != this)
        {
            targetManager.ReorderRecentResource(path, target.path);
        }
    }
}
