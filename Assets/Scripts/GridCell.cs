using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GridCell : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
{
    public int X { get; private set; }
    public int Y { get; private set; }

    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    public void Init(int x, int y)
    {
        X = x;
        Y = y;
        SetColor(Color.white);
    }

    public void SetColor(Color color)
    {
        image.color = color;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        MapEditorManager.Instance.UseCurrentTool(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Input.GetMouseButton(0))
        {
            MapEditorManager.Instance.UseCurrentTool(this);
        }
    }
}