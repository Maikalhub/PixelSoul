using UnityEngine;

public class SpriteMiracle : MonoBehaviour
{
    [Header("? Time Settings")]
    public float duration = 5f; // Время действия в секундах

    [Header("?? Mask")]
    public Transform maskTransform;

    private float timer;

    void Start()
    {
        timer = duration;
        maskTransform.localRotation = Quaternion.Euler(0, 0, 0);
    }

    void Update()
    {
        if (timer <= 0f)
        {
            timer = 0f;
            return;
        }

        timer -= Time.deltaTime;

        float progress = timer / duration; // 1 ? 0
        float angle = progress * 360f;

        // по часовой стрелке
        maskTransform.localRotation = Quaternion.Euler(0, 0, -angle);
    }
}
