using UnityEngine;

public sealed class MapEditorBackgroundRotator : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = 10f;

    private void Update()
    {
        transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
    }
}
