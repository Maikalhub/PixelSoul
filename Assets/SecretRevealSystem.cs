using UnityEngine;

public class SecretRevealSystem : MonoBehaviour
{
    [Header("Player")]
    public Transform player;

    [Header("Detection Settings")]
    public float revealRadius = 3f;

    [Header("Secret Objects")]
    public GameObject[] secretObjects;

    [Header("Fade Settings")]
    public float fadeSpeed = 3f;

    [Header("Behavior")]
    public bool hideAgain = false;

    private SpriteRenderer[] renderers;
    private float[] targetAlphas;

    void Start()
    {
        renderers = new SpriteRenderer[secretObjects.Length];
        targetAlphas = new float[secretObjects.Length];

        for (int i = 0; i < secretObjects.Length; i++)
        {
            if (secretObjects[i] == null) continue;

            renderers[i] = secretObjects[i].GetComponent<SpriteRenderer>();

            if (renderers[i] != null)
            {
                SetAlpha(renderers[i], 0f); // скрываем в начале
                targetAlphas[i] = 0f;
            }
        }
    }

    void Update()
    {
        if (player == null) return;

        for (int i = 0; i < secretObjects.Length; i++)
        {
            if (secretObjects[i] == null || renderers[i] == null) continue;

            float distance = Vector2.Distance(player.position, secretObjects[i].transform.position);

            if (distance <= revealRadius)
            {
                targetAlphas[i] = 1f;
            }
            else if (hideAgain)
            {
                targetAlphas[i] = 0f;
            }

            // Плавное изменение прозрачности
            float newAlpha = Mathf.Lerp(renderers[i].color.a, targetAlphas[i], Time.deltaTime * fadeSpeed);
            SetAlpha(renderers[i], newAlpha);
        }
    }

    void SetAlpha(SpriteRenderer sr, float a)
    {
        Color c = sr.color;
        c.a = a;
        sr.color = c;
    }

    // Для отладки — рисует радиус в сцене
    void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player.position, revealRadius);
    }
}