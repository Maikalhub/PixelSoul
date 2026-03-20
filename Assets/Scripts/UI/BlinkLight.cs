using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BlinkLight : MonoBehaviour
{
    private SpriteRenderer sr;
    private Vector3 startScale;

    [Header("Alpha (Brightness)")]
    [Range(0f, 1f)] public float minAlpha = 0.4f;
    [Range(0f, 1f)] public float maxAlpha = 1f;

    [Header("Scale")]
    public float minScale = 0.9f;
    public float maxScale = 1.1f;

    [Header("Flicker Speed")]
    public float flickerSpeed = 3f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        startScale = transform.localScale;
    }

    void Update()
    {
        // ћ€гкое живое мерцание
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);

        // яркость (альфа)
        Color c = sr.color;
        c.a = Mathf.Lerp(minAlpha, maxAlpha, noise);
        sr.color = c;

        // –азмер
        float scale = Mathf.Lerp(minScale, maxScale, noise);
        transform.localScale = startScale * scale;
    }
}
