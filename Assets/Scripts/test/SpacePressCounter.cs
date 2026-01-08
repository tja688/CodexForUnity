using UnityEngine;

public class SpacePressCounter : MonoBehaviour
{
    [SerializeField]
    private int count;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            count++;
        }
    }
}
