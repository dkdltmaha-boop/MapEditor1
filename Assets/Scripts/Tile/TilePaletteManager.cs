using UnityEngine;
using UnityEngine.UI;

public class TilePaletteManager : MonoBehaviour
{
    public TileDatabase tileDatabase;
    public Transform contentParent;

    private void Start()
    {
        CreatePalette();
    }

    private void CreatePalette()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        foreach (TileInfo tile in tileDatabase.tiles)
        {
            GameObject obj = new GameObject("TileButton_" + tile.id);
            obj.transform.SetParent(contentParent, false);

            Image image = obj.AddComponent<Image>();
            image.color = tile.color;
            image.raycastTarget = true;

            Button button = obj.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;

            int id = tile.id;
            button.onClick.AddListener(() =>
            {
                MapEditorManager.Instance.SelectTile(id);
            });
        }
    }
}