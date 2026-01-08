using UnityEngine;

public class TabSpawnPrefab : MonoBehaviour
{
    [SerializeField] private GameObject prefab;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && prefab != null)
        {
            Instantiate(prefab, Vector3.zero, Quaternion.identity);
        }
    }
}
