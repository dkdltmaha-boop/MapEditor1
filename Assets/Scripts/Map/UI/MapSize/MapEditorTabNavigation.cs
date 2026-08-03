using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public sealed class MapEditorTabNavigation : MonoBehaviour, IUpdateSelectedHandler
{
    public Selectable next;
    public Selectable previous;

    public void Configure(Selectable nextSelectable, Selectable previousSelectable)
    {
        next = nextSelectable;
        previous = previousSelectable;
    }

    public void OnUpdateSelected(BaseEventData eventData)
    {
        if (!Input.GetKeyDown(KeyCode.Tab))
        {
            return;
        }

        Selectable target = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
            ? previous
            : next;

        if (target == null || !target.IsInteractable())
        {
            return;
        }

        target.Select();
        eventData.Use();
    }
}
