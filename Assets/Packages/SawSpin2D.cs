using UnityEngine;

public class SawSpin2D : MonoBehaviour
{
    public float spinSpeed = 1080f;
    public bool clockwise = true;

    private void Update()
    {
        float direction = clockwise ? -1f : 1f;
        transform.Rotate(0f, 0f, direction * spinSpeed * Time.deltaTime);
    }
}