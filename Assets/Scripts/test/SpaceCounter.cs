using UnityEngine;

public class SpaceCounter : MonoBehaviour
{
    private int count;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            count--;
            Debug.Log($"Space count: {count}");
        }
    }
}
