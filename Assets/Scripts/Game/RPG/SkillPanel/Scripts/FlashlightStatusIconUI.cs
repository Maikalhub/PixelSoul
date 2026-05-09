using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FlashlightStatusIconUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SkillEffectApplier skillEffectApplier;
    [SerializeField] private FlashlightController flashlight;

    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image radialImage;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private TMP_Text chargeText;

    [Header("Icon")]
    [SerializeField] private Sprite flashlightIcon;

    [Header("Colors")]
    [SerializeField] private Color usingColor = Color.white;
    [SerializeField] private Color rechargeColor = Color.yellow;
    [SerializeField] private Color readyColor = Color.green;
    [SerializeField] private Color lockedColor = Color.gray;

    [Header("Options")]
    [SerializeField] private bool hideUntilUnlocked = true;
    [SerializeField] private bool searchReferencesAutomatically = true;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        SetupImages();
        ResolveReferences();
        Refresh();
    }

    private void Update()
    {
        ResolveReferences();
        Refresh();
    }

    private void ResolveReferences()
    {
        if (!searchReferencesAutomatically)
            return;

        if (skillEffectApplier == null)
            skillEffectApplier = FindObjectOfType<SkillEffectApplier>();

        if (flashlight == null && skillEffectApplier != null)
            flashlight = skillEffectApplier.Flashlight;

        if (flashlight == null)
            flashlight = FindObjectOfType<FlashlightController>(true);
    }

    private void SetupImages()
    {
        if (backgroundImage != null)
        {
            if (flashlightIcon != null)
                backgroundImage.sprite = flashlightIcon;

            backgroundImage.preserveAspect = true;

            Color color = backgroundImage.color;
            color.a = 0.25f;
            backgroundImage.color = color;
        }

        if (radialImage != null)
        {
            if (flashlightIcon != null)
                radialImage.sprite = flashlightIcon;

            radialImage.preserveAspect = true;
            radialImage.type = Image.Type.Filled;
            radialImage.fillMethod = Image.FillMethod.Radial360;
            radialImage.fillOrigin = (int)Image.Origin360.Top;
            radialImage.fillClockwise = true;
            radialImage.fillAmount = 0f;
        }
    }

    private void Refresh()
    {
        bool hasFlashlight = flashlight != null;
        bool unlocked = hasFlashlight && flashlight.IsUnlocked;

        if (hideUntilUnlocked)
            SetVisible(unlocked);
        else
            SetVisible(true);

        if (!hasFlashlight)
        {
            SetVisual(0f, lockedColor, "Нет", "0/0");
            return;
        }

        if (!unlocked)
        {
            SetVisual(0f, lockedColor, "Закрыт", "0/0");
            return;
        }

        float chargeNormalized = Mathf.Clamp01(flashlight.ChargeNormalized);

        string chargeString =
            $"{Mathf.CeilToInt(flashlight.CurrentCharge)}/{Mathf.CeilToInt(flashlight.MaxCharge)}";

        if (flashlight.IsOn)
        {
            SetVisual(chargeNormalized, usingColor, "Свет", chargeString);
        }
        else if (flashlight.IsReady)
        {
            SetVisual(1f, readyColor, "Готов", chargeString);
        }
        else if (flashlight.IsRecharging)
        {
            SetVisual(chargeNormalized, rechargeColor, "Заряд", chargeString);
        }
        else
        {
            SetVisual(chargeNormalized, rechargeColor, "Ожид.", chargeString);
        }
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    private void SetVisual(float fillAmount, Color color, string state, string charge)
    {
        if (radialImage != null)
        {
            radialImage.fillAmount = Mathf.Clamp01(fillAmount);
            radialImage.color = color;
        }

        if (stateText != null)
        {
            stateText.text = state;
            stateText.color = color;
        }

        if (chargeText != null)
            chargeText.text = charge;
    }
}