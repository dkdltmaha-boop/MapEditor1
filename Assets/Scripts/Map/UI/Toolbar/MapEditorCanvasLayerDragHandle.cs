using UnityEngine;
using UnityEngine.EventSystems;

public sealed class MapEditorCanvasLayerDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public MapEditorManager manager;
    public int canvasIndex;
    private CanvasGroup rowCanvasGroup;

    public void OnBeginDrag(PointerEventData eventData)
    {
        rowCanvasGroup = GetComponent<CanvasGroup>();
        if (rowCanvasGroup == null) rowCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        rowCanvasGroup.alpha = 0.55f;
        rowCanvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData)
    {
        MapEditorCanvasLayerDragHandle targetHandle = FindDropTarget(eventData);
        if (rowCanvasGroup != null)
        {
            rowCanvasGroup.alpha = 1f;
            rowCanvasGroup.blocksRaycasts = true;
        }

        if (manager != null && targetHandle != null)
        {
            manager.MoveCanvasLayer(canvasIndex, targetHandle.canvasIndex);
        }
    }

    private MapEditorCanvasLayerDragHandle FindDropTarget(PointerEventData eventData)
    {
        GameObject raycastTarget = eventData.pointerCurrentRaycast.gameObject;
        MapEditorCanvasLayerDragHandle raycastHandle = raycastTarget == null
            ? null
            : raycastTarget.GetComponentInParent<MapEditorCanvasLayerDragHandle>();
        if (raycastHandle != null && raycastHandle != this) return raycastHandle;

        Transform list = transform.parent;
        if (list == null) return null;

        MapEditorCanvasLayerDragHandle closest = null;
        float closestDistance = float.MaxValue;
        Camera eventCamera = eventData.pressEventCamera;
        for (int i = 0; i < list.childCount; i++)
        {
            MapEditorCanvasLayerDragHandle candidate = list.GetChild(i).GetComponent<MapEditorCanvasLayerDragHandle>();
            if (candidate == null || candidate == this) continue;
            RectTransform candidateRect = candidate.transform as RectTransform;
            if (candidateRect == null) continue;
            Vector3 worldCenter = candidateRect.TransformPoint(candidateRect.rect.center);
            float screenY = RectTransformUtility.WorldToScreenPoint(eventCamera, worldCenter).y;
            float distance = Mathf.Abs(eventData.position.y - screenY);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = candidate;
            }
        }

        return closest;
    }
}
