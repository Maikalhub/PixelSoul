using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FlashlightController : MonoBehaviour
{
    [Header("Ability")]
    [SerializeField] private float activeDuration = 6f;       // Максимальный заряд в секундах
    [SerializeField] private float rechargeDuration = 8f;     // За сколько секунд зарядится с 0 до full
    [SerializeField] private bool startReady = true;
    [SerializeField] private float minChargeToTurnOn = 0.05f; // Минимум заряда для включения

    [Header("Light")]
    [SerializeField] private Light unityLight;
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

    private bool isOn;
    private float currentCharge;

    public bool IsOn => isOn;
    public bool IsReady => currentCharge >= activeDuration - 0.01f;
    public bool IsRecharging => currentCharge < activeDuration - 0.01f && !isOn;

    public float CurrentCharge => currentCharge;
    public float MaxCharge => activeDuration;
    public float ChargeNormalized => activeDuration > 0f ? currentCharge / activeDuration : 0f;

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
        currentCharge = startReady ? activeDuration : 0f;
        isOn = false;

        SetLightIntensity(offIntensity);
        HideAllEnemiesInside();
    }

    private void Update()
    {
        UpdateCharge();
        UpdateLightVisual();
    }

    public void ToggleLight()
    {
        if (isOn)
        {
            TurnOff(false);
            return;
        }

        if (currentCharge <= minChargeToTurnOn)
        {
            Debug.Log($"[Flashlight] Недостаточно заряда: {currentCharge:F1}/{activeDuration:F1}");
            return;
        }

        TurnOn();
    }

    private void UpdateCharge()
    {
        if (isOn)
        {
            currentCharge -= Time.deltaTime;

            if (currentCharge <= 0f)
            {
                currentCharge = 0f;
                Debug.Log("[Flashlight] Заряд закончился.");
                TurnOff(true);
            }
        }
        else
        {
            if (currentCharge < activeDuration)
            {
                float rechargeRate = rechargeDuration > 0f
                    ? activeDuration / rechargeDuration
                    : activeDuration;

                float previousCharge = currentCharge;

                currentCharge += rechargeRate * Time.deltaTime;
                currentCharge = Mathf.Min(currentCharge, activeDuration);

                if (previousCharge < activeDuration && currentCharge >= activeDuration)
                {
                    Debug.Log("[Flashlight] Полностью заряжен.");
                }
            }
        }
    }

    private void TurnOn()
    {
        if (isOn)
            return;

        isOn = true;
        RevealAllEnemiesInside();

        Debug.Log($"[Flashlight] Включен. Заряд: {currentCharge:F1}/{activeDuration:F1}");
    }

    private void TurnOff(bool autoOff)
    {
        if (!isOn)
            return;

        isOn = false;
        HideAllEnemiesInside();

        if (autoOff)
        {
            Debug.Log("[Flashlight] Автоматически выключен: заряд пуст.");
        }
        else
        {
            Debug.Log($"[Flashlight] Выключен вручную. Остаток заряда: {currentCharge:F1}/{activeDuration:F1}");
        }
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
        enemy.SetLightVisible(isOn);
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

    private void UpdateLightVisual()
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
        float newIntensity = Mathf.Lerp(
            GetLightIntensity(),
            targetIntensity,
            Time.deltaTime * intensityLerpSpeed
        );

        SetLightIntensity(newIntensity);
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

            PropertyInfo property = component.GetType().GetProperty(
                "intensity",
                BindingFlags.Public | BindingFlags.Instance
            );

            if (property != null &&
                property.CanRead &&
                property.CanWrite &&
                property.PropertyType == typeof(float))
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