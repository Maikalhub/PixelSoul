using UnityEngine;

[RequireComponent(typeof(Light))]
public class DynamicSpotlight : MonoBehaviour
{
    private Light spotLight;

    [Header("Mode")]
    public bool useTemperature = true;

    [Header("Intensity (Brightness)")]
    public float minIntensity = 0.5f;
    public float maxIntensity = 2f;

    [Header("Temperature")]
    public float minTemperature = 2000f;
    public float maxTemperature = 8000f;

    [Header("Color (if not using temperature)")]
    public Color colorA = Color.red;
    public Color colorB = Color.blue;

    [Header("Flicker Speed")]
    public float flickerSpeed = 3f;

    void Awake()
    {
        spotLight = GetComponent<Light>();
    }

    void Update()
    {
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);

        // Intensity
        float intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
        spotLight.intensity = intensity;

        if (useTemperature)
        {
            spotLight.useColorTemperature = true;
            float temp = Mathf.Lerp(minTemperature, maxTemperature, noise);
            spotLight.colorTemperature = temp;
        }
        else
        {
            spotLight.useColorTemperature = false;
            Color col = Color.Lerp(colorA, colorB, noise);
            spotLight.color = col;
        }
    }
}