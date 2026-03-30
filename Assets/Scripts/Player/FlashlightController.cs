using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FlashlightController : MonoBehaviour
{
    [Header("Light")]
    [SerializeField] private Light unityLight;
    [SerializeField] private bool isOn = true;
    [SerializeField] private float onIntensity = 2.2f;
    [SerializeField] private float offIntensity = 0f;
    [SerializeField] private float intensityLerpSpeed = 10f;

    [Header("Flicker While Moving")]
    [SerializeField] private float movingFlickerAmount = 0.15f;
    [SerializeField] private float movingFlickerSpeed = 16f;

    [Header("Flicker While Idle")]
    [SerializeField] private float idleFlickerAmount = 0.015f;
    [SerializeField] private float idleFlickerSpeed = 2f;

    private PlayerMovement playerMovement;
    private Collider2D revealTrigger;
    private readonly HashSet<EnemyAI> enemiesInside = new HashSet<EnemyAI>();

    private Component resolvedLightComponent;
    private PropertyInfo intensityProperty;
    private float noiseSeed;

    private void Awake()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
        revealTrigger = GetComponent<Collider2D>();
        revealTrigger.isTrigger = true;

        ResolveLightComponent();
        noiseSeed = Random.Range(0f, 1000f);
    }

    private void Start()
    {
        SetLightIntensity(isOn ? onIntensity : offIntensity);
    }

    private void Update()
    {
        if (resolvedLightComponent == null)
            return;

        bool isMoving = playerMovement != null && playerMovement.IsActuallyMoving;

        float flickerAmount = isMoving ? movingFlickerAmount : idleFlickerAmount;
        float flickerSpeed = isMoving ? movingFlickerSpeed : idleFlickerSpeed;

        float baseIntensity = isOn ? onIntensity : offIntensity;
        float flicker = 0f;

        if (isOn)
        {
            float noise = Mathf.PerlinNoise(noiseSeed, Time.time * flickerSpeed);
            flicker = (noise - 0.5f) * 2f * flickerAmount;
        }

        float targetIntensity = Mathf.Max(0f, baseIntensity + flicker);
        float newIntensity = Mathf.Lerp(GetLightIntensity(), targetIntensity, Time.deltaTime * intensityLerpSpeed);

        SetLightIntensity(newIntensity);
    }

    public void ToggleLight()
    {
        isOn = !isOn;

        if (isOn)
            RevealAllEnemiesInside();
        else
            HideAllEnemiesInside();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnemyAI enemy = other.GetComponentInParent<EnemyAI>();
        if (enemy == null) return;

        enemiesInside.Add(enemy);

        if (isOn)
            enemy.SetLightVisible(true);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        EnemyAI enemy = other.GetComponentInParent<EnemyAI>();
        if (enemy == null) return;

        enemiesInside.Add(enemy);

        if (isOn)
            enemy.SetLightVisible(true);
        else
            enemy.SetLightVisible(false);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        EnemyAI enemy = other.GetComponentInParent<EnemyAI>();
        if (enemy == null) return;

        enemiesInside.Remove(enemy);
        enemy.SetLightVisible(false);
    }

    private void RevealAllEnemiesInside()
    {
        enemiesInside.RemoveWhere(enemy => enemy == null);

        foreach (EnemyAI enemy in enemiesInside)
            enemy.SetLightVisible(true);
    }

    private void HideAllEnemiesInside()
    {
        enemiesInside.RemoveWhere(enemy => enemy == null);

        foreach (EnemyAI enemy in enemiesInside)
            enemy.SetLightVisible(false);
    }

    private void ResolveLightComponent()
    {
        if (unityLight == null)
            unityLight = GetComponent<Light>();

        if (unityLight != null)
        {
            resolvedLightComponent = unityLight;
            intensityProperty = typeof(Light).GetProperty(nameof(Light.intensity));
            return;
        }

        Component[] components = GetComponents<Component>();
        foreach (Component component in components)
        {
            if (component == null) continue;
            if (component is Transform) continue;
            if (component is Collider2D) continue;
            if (component is FlashlightController) continue;

            PropertyInfo property = component.GetType().GetProperty("intensity", BindingFlags.Public | BindingFlags.Instance);
            if (property != null && property.CanRead && property.CanWrite && property.PropertyType == typeof(float))
            {
                resolvedLightComponent = component;
                intensityProperty = property;
                return;
            }
        }

        Debug.LogWarning("На объекте света не найден компонент Light/Light2D с полем intensity.");
    }

    private float GetLightIntensity()
    {
        if (resolvedLightComponent == null || intensityProperty == null)
            return 0f;

        object value = intensityProperty.GetValue(resolvedLightComponent);
        return value is float f ? f : 0f;
    }

    private void SetLightIntensity(float value)
    {
        if (resolvedLightComponent == null || intensityProperty == null)
            return;

        intensityProperty.SetValue(resolvedLightComponent, value);
    }
}