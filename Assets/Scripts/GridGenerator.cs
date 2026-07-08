using UnityEngine;
using UnityEngine.UI;

public class GridGenerator : MonoBehaviour
{
    [Header("그리드 설정")]
    public int width = 16;
    public int height = 16;
    public float cellSize = 32f;

    [Header("연결")]
    public Transform gridParent;
    public GridCell cellPrefab;

    private void Start()
    {
        GenerateGrid();
    }

    public void GenerateGrid()
    {
        foreach (Transform child in gridParent)
        {
            Destroy(child.gameObject);
        }

        GridLayoutGroup gridLayout = gridParent.GetComponent<GridLayoutGroup>();
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = width;
        gridLayout.cellSize = new Vector2(cellSize, cellSize);
        gridLayout.spacing = Vector2.zero;

        RectTransform rect = gridParent.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width * cellSize, height * cellSize);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GridCell cell = Instantiate(cellPrefab, gridParent);
                cell.Init(x, y);
            }
        }
    }
}