using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public class FlashlightController : MonoBehaviour
{
    [Header("Unlock")]
    [SerializeField] private bool startUnlocked = false;
    [SerializeField] private GameObject visualRoot;

    [Header("Ability")]
    [SerializeField] private float activeDuration = 6f;
    [SerializeField] private float rechargeDuration = 8f;
    [SerializeField] private bool startReady = true;
    [SerializeField] private float minChargeToTurnOn = 0.05f;

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

    private Component resolvedLightComponent;
    private PropertyInfo intensityProperty;

    private float noiseSeed;
    private bool initialized;

    private bool isUnlocked;
    private bool isOn;
    private float currentCharge;

    public bool IsUnlocked => isUnlocked;
    public bool IsOn => isOn;
    public bool IsReady => currentCharge >= activeDuration - 0.01f;
    public bool IsRecharging => currentCharge < activeDuration - 0.01f && !isOn;

    public float CurrentCharge => currentCharge;
    public float MaxCharge => activeDuration;
    public float ChargeNormalized => activeDuration > 0f ? currentCharge / activeDuration : 0f;

    private void Awake()
    {
        Initialize();
        SetUnlocked(startUnlocked);
    }

    private void OnEnable()
    {
        Initialize();
        UpdateLightVisual(true);
    }

    private void Update()
    {
        if (!isUnlocked)
        {
            if (isOn)
                TurnOff(false);

            UpdateLightVisual(false);
            return;
        }

        UpdateCharge();
        UpdateLightVisual(false);
    }

    private void Initialize()
    {
        if (initialized)
            return;

        initialized = true;

        playerMovement = GetComponentInParent<PlayerMovement>();
        ResolveLightComponent();

        noiseSeed = Random.Range(0f, 1000f);
        currentCharge = startReady ? activeDuration : 0f;

        SetLightIntensity(offIntensity);

        if (visualRoot != null)
            visualRoot.SetActive(startUnlocked);
    }

    public void SetUnlocked(bool unlocked)
    {
        Initialize();

        isUnlocked = unlocked;

        if (!isUnlocked)
        {
            TurnOff(false);

            if (visualRoot != null)
                visualRoot.SetActive(false);

            SetLightIntensity(offIntensity);
            return;
        }

        if (visualRoot != null)
            visualRoot.SetActive(true);

        if (startReady && currentCharge <= 0f)
            currentCharge = activeDuration;

        Debug.Log("[Flashlight] Фонарь разблокирован.");
    }

    public void ToggleLight()
    {
        if (!isUnlocked)
        {
            Debug.Log("[Flashlight] Фонарь ещё не разблокирован навыком.");
            return;
        }

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

    private void TurnOn()
    {
        if (isOn)
            return;

        isOn = true;
        Debug.Log($"[Flashlight] Включен. Заряд: {currentCharge:F1}/{activeDuration:F1}");
    }

    private void TurnOff(bool autoOff)
    {
        if (!isOn)
            return;

        isOn = false;

        if (autoOff)
            Debug.Log("[Flashlight] Автоматически выключен: заряд пуст.");
        else
            Debug.Log($"[Flashlight] Выключен. Остаток заряда: {currentCharge:F1}/{activeDuration:F1}");
    }

    private void UpdateCharge()
    {
        if (isOn)
        {
            currentCharge -= Time.deltaTime;

            if (currentCharge <= 0f)
            {
                currentCharge = 0f;
                TurnOff(true);
            }

            return;
        }

        if (currentCharge >= activeDuration)
            return;

        float rechargeRate = rechargeDuration > 0f
            ? activeDuration / rechargeDuration
            : activeDuration;

        currentCharge += rechargeRate * Time.deltaTime;
        currentCharge = Mathf.Min(currentCharge, activeDuration);
    }

    private void UpdateLightVisual(bool instant)
    {
        if (resolvedLightComponent == null)
            return;

        bool canShowLight = isUnlocked && isOn;
        bool isMoving = playerMovement != null && playerMovement.IsActuallyMoving;

        float flickerAmount = isMoving ? movingFlickerAmount : idleFlickerAmount;
        float flickerSpeed = isMoving ? movingFlickerSpeed : idleFlickerSpeed;

        float baseIntensity = canShowLight ? onIntensity : offIntensity;
        float flicker = 0f;

        if (canShowLight)
        {
            float noise = Mathf.PerlinNoise(noiseSeed, Time.time * flickerSpeed);
            flicker = (noise - 0.5f) * 2f * flickerAmount;
        }

        float targetIntensity = Mathf.Max(0f, baseIntensity + flicker);

        if (instant)
        {
            SetLightIntensity(targetIntensity);
            return;
        }

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
            if (component == null)
                continue;

            if (component is Transform)
                continue;

            if (component is Collider2D)
                continue;

            if (component is FlashlightController)
                continue;

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

        Debug.LogWarning("На объекте фонаря не найден компонент Light или Light2D с intensity.");
    }

    private float GetLightIntensity()
    {
        if (resolvedLightComponent == null || intensityProperty == null)
            return 0f;

        object value = intensityProperty.GetValue(resolvedLightComponent);
        return value is float floatValue ? floatValue : 0f;
    }

    private void SetLightIntensity(float value)
    {
        if (resolvedLightComponent == null || intensityProperty == null)
            return;

        intensityProperty.SetValue(resolvedLightComponent, value);
    }

    public void ModifyActiveDuration(float value, SkillValueMode mode)
    {
        bool wasFull = currentCharge >= activeDuration - 0.01f;

        activeDuration = ApplyValue(activeDuration, value, mode, 0.1f);

        if (wasFull)
            currentCharge = activeDuration;
        else
            currentCharge = Mathf.Clamp(currentCharge, 0f, activeDuration);
    }

    public void ModifyRechargeDuration(float value, SkillValueMode mode)
    {
        rechargeDuration = ApplyValue(rechargeDuration, value, mode, 0.01f);
    }

    private float ApplyValue(float current, float value, SkillValueMode mode, float minValue)
    {
        float result = current;

        switch (mode)
        {
            case SkillValueMode.Add:
                result = current + value;
                break;

            case SkillValueMode.Multiply:
                result = current * value;
                break;

            case SkillValueMode.Set:
                result = value;
                break;
        }

        return Mathf.Max(result, minValue);
    }
}