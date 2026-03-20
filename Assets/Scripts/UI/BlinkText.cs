using UnityEngine;
using TMPro;

public class BlinkText : MonoBehaviour
{
    private TextMeshProUGUI text;

    [Header("Colors")]
    public Color[] colors =
    {
        Color.red,
        Color.green,
        Color.blue,
        Color.magenta
    };

    [Header("Color Speed")]
    public float normalColorSpeed = 1f;
    public float fastColorSpeed = 4f;

    [Header("Blink (Alpha)")]
    public float blinkSpeed = 2f;
    [Range(0f, 1f)] public float minAlpha = 0.4f;
    [Range(0f, 1f)] public float maxAlpha = 0.9f;

    private float currentColorSpeed;
    private int colorIndex;
    private float colorT;

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
        ResetBlink();
    }

    private void Update()
    {
        // ---------- COLOR FLOW ----------
        colorT += Time.deltaTime * currentColorSpeed;

        if (colorT >= 1f)
        {
            colorT = 0f;
            colorIndex = (colorIndex + 1) % colors.Length;
        }

        Color color = Color.Lerp(
            colors[colorIndex],
            colors[(colorIndex + 1) % colors.Length],
            colorT
        );

        // ---------- BLINK (ALPHA) ----------
        float blink = Mathf.Sin(Time.time * blinkSpeed) * 0.5f + 0.5f;
        color.a = Mathf.Lerp(minAlpha, maxAlpha, blink);

        text.color = color;
    }

    // ---------- PUBLIC CONTROL ----------

    public void SetNormalColorSpeed()
    {
        currentColorSpeed = normalColorSpeed;
    }

    public void SetFastColorSpeed()
    {
        currentColorSpeed = fastColorSpeed;
    }

    public void ResetBlink()
    {
        colorT = 0f;
        colorIndex = 0;
        currentColorSpeed = normalColorSpeed;
    }
}
