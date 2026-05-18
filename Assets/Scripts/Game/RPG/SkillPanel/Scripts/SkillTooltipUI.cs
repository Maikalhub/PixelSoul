using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class SkillTooltipUI : MonoBehaviour
{
    public enum TooltipPosition
    {
        Top,
        Bottom,
        Left,
        Right,
        Custom
    }

    [Header("Tooltip Text")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text effectsText;
    [SerializeField] private TMP_Text requirementsText;
    [SerializeField] private TMP_Text footerText;

    [Header("Canvas")]
    [SerializeField] private Canvas canvas;

    [Header("Position")]
    [SerializeField] private TooltipPosition position = TooltipPosition.Right;
    [SerializeField] private Vector2 customOffset = new Vector2(40f, -30f);
    [SerializeField] private bool clampToCanvas = true;

    [Header("Behaviour")]
    [SerializeField] private bool followMouse = true;

    public static SkillTooltipUI Instance { get; private set; }

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Skill currentSkill;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        gameObject.SetActive(false);
    }

    public void Show(Skill skill)
    {
        if (skill == null || skill.skillSO == null)
            return;

        if (canvas == null)
            return;

        currentSkill = skill;

        skill.CheckDependencies();

        SkillSO skillSO = skill.skillSO;

        gameObject.SetActive(true);
        canvasGroup.alpha = 0f;

        SetText(nameText, GetSkillName(skillSO));
        SetText(statusText, GetStatusText(skillSO));
        SetText(costText, GetCostText(skillSO));
        SetText(descriptionText, GetDescriptionText(skillSO));
        SetText(effectsText, GetEffectsText(skillSO));
        SetText(requirementsText, GetRequirementsText(skillSO));
        SetText(footerText, GetFooterText(skillSO));

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        UpdatePosition();

        canvasGroup.alpha = 1f;
    }

    public void Hide()
    {
        currentSkill = null;

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!gameObject.activeSelf)
            return;

        if (!followMouse)
            return;

        if (canvas == null || rectTransform == null)
            return;

        UpdatePosition();
    }

    private void UpdatePosition()
    {
        RectTransform canvasRect = canvas.transform as RectTransform;

        if (canvasRect == null)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            Input.mousePosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 localPoint
        );

        Vector2 anchoredPos = localPoint + GetOffset();

        if (clampToCanvas)
        {
            Vector2 minPosition = new Vector2(
                -canvasRect.rect.width * 0.5f + rectTransform.rect.width * 0.5f,
                -canvasRect.rect.height * 0.5f + rectTransform.rect.height * 0.5f
            );

            Vector2 maxPosition = new Vector2(
                canvasRect.rect.width * 0.5f - rectTransform.rect.width * 0.5f,
                canvasRect.rect.height * 0.5f - rectTransform.rect.height * 0.5f
            );

            anchoredPos.x = Mathf.Clamp(anchoredPos.x, minPosition.x, maxPosition.x);
            anchoredPos.y = Mathf.Clamp(anchoredPos.y, minPosition.y, maxPosition.y);
        }

        rectTransform.anchoredPosition = anchoredPos;
    }

    private Vector2 GetOffset()
    {
        switch (position)
        {
            case TooltipPosition.Top:
                return new Vector2(0f, rectTransform.rect.height * 0.5f + 20f);

            case TooltipPosition.Bottom:
                return new Vector2(0f, -rectTransform.rect.height * 0.5f - 20f);

            case TooltipPosition.Left:
                return new Vector2(-rectTransform.rect.width * 0.5f - 20f, 0f);

            case TooltipPosition.Right:
                return new Vector2(rectTransform.rect.width * 0.5f + 20f, 0f);

            case TooltipPosition.Custom:
                return customOffset;
        }

        return customOffset;
    }

    private string GetSkillName(SkillSO skillSO)
    {
        if (skillSO == null)
            return "Unknown Skill";

        if (!string.IsNullOrWhiteSpace(skillSO.skillName))
            return skillSO.skillName;

        return skillSO.name;
    }

    private string GetStatusText(SkillSO skillSO)
    {
        if (skillSO == null)
            return "";

        if (skillSO.permanentlyLocked)
            return "Status: Permanently Locked";

        if (!skillSO.isLocked)
            return "Status: Unlocked";

        if (skillSO.canBeUnlocked)
            return "Status: Available";

        return "Status: Locked";
    }

    private string GetCostText(SkillSO skillSO)
    {
        if (skillSO == null)
            return "";

        if (!skillSO.isLocked)
            return "Cost: Already unlocked";

        if (CoinSystem.Instance == null)
            return $"Cost: {skillSO.cost} coins";

        int currentCoins = CoinSystem.Instance.CurrentCoins;

        return $"Cost: {skillSO.cost} coins | You have: {currentCoins}";
    }

    private string GetDescriptionText(SkillSO skillSO)
    {
        if (skillSO == null)
            return "";

        if (string.IsNullOrWhiteSpace(skillSO.description))
            return "";

        return skillSO.description;
    }

    private string GetEffectsText(SkillSO skillSO)
    {
        if (skillSO == null)
            return "";

        StringBuilder builder = new StringBuilder();

        if (skillSO.upgradeType == SkillUpgradeType.BaseStats)
        {
            builder.AppendLine("Effect:");

            if (skillSO.statModifiers == null || skillSO.statModifiers.Count == 0)
            {
                builder.AppendLine("- No stat modifier");
            }
            else
            {
                foreach (SkillStatModifier modifier in skillSO.statModifiers)
                {
                    if (modifier == null)
                        continue;

                    builder.AppendLine($"- {GetStatName(modifier.statType)} {GetValueText(modifier.valueMode, modifier.value)}");
                }
            }
        }
        else if (skillSO.upgradeType == SkillUpgradeType.SpecialEffect)
        {
            builder.AppendLine("Effect:");

            if (skillSO.specialEffects == null || skillSO.specialEffects.Count == 0)
            {
                builder.AppendLine("- No special effect");
            }
            else
            {
                foreach (SkillSpecialEffectData effect in skillSO.specialEffects)
                {
                    if (effect == null)
                        continue;

                    builder.AppendLine(GetSpecialEffectText(effect));
                }
            }
        }

        return builder.ToString().TrimEnd();
    }

    private string GetRequirementsText(SkillSO skillSO)
    {
        if (skillSO == null)
            return "";

        if (skillSO.requiredSkills == null || skillSO.requiredSkills.Length == 0)
            return "Requires: None";

        StringBuilder builder = new StringBuilder();

        builder.Append(skillSO.requireAll ? "Requires all:" : "Requires one:");

        foreach (SkillSO requiredSkill in skillSO.requiredSkills)
        {
            if (requiredSkill == null)
                continue;

            string requiredName = GetSkillName(requiredSkill);
            string state = requiredSkill.isLocked ? "Locked" : "Unlocked";

            builder.AppendLine();
            builder.Append($"- {requiredName} ({state})");
        }

        return builder.ToString();
    }

    private string GetFooterText(SkillSO skillSO)
    {
        if (skillSO == null)
            return "";

        if (skillSO.permanentlyLocked)
            return "This skill is locked by another choice.";

        if (!skillSO.isLocked)
            return "Already researched.";

        if (!skillSO.canBeUnlocked)
            return "Unlock required skills first.";

        if (CoinSystem.Instance != null && CoinSystem.Instance.CurrentCoins < skillSO.cost)
            return "Not enough coins.";

        return "Click to research.";
    }

    private string GetStatName(SkillStatType statType)
    {
        switch (statType)
        {
            case SkillStatType.MaxHealth:
                return "Max Health";

            case SkillStatType.MaxStamina:
                return "Max Stamina";

            case SkillStatType.AttackDamage:
                return "Attack Damage";

            case SkillStatType.Defense:
                return "Defense";

            case SkillStatType.MoveSpeed:
                return "Move Speed";

            case SkillStatType.JumpPower:
                return "Jump Power";

            case SkillStatType.SprintMultiplier:
                return "Sprint";

            case SkillStatType.DashSpeed:
                return "Dash Speed";

            case SkillStatType.ThrowForce:
                return "Throw Force";

            case SkillStatType.ThrowCooldown:
                return "Throw Cooldown";

            case SkillStatType.FlashlightActiveDuration:
                return "Flashlight Duration";

            case SkillStatType.FlashlightRechargeDuration:
                return "Flashlight Recharge";

            case SkillStatType.EnemyDetectionRadius:
                return "Detection Radius";

            case SkillStatType.FlashlightDetectionBonus:
                return "Flashlight Detection";
        }

        return statType.ToString();
    }

    private string GetValueText(SkillValueMode mode, float value)
    {
        string valueText = value.ToString("0.##");

        switch (mode)
        {
            case SkillValueMode.Add:
                return value >= 0f ? $"+{valueText}" : valueText;

            case SkillValueMode.Multiply:
                return $"x{valueText}";

            case SkillValueMode.Set:
                return $"= {valueText}";
        }

        return valueText;
    }

    private string GetSpecialEffectText(SkillSpecialEffectData effect)
    {
        if (effect == null)
            return "";

        string baseText = "";

        switch (effect.effectType)
        {
            case SkillSpecialEffectType.EnableShooting:
                baseText = "- Unlocks bone throwing";
                break;

            case SkillSpecialEffectType.EnableFlashlight:
                baseText = "- Unlocks lantern";
                break;

            case SkillSpecialEffectType.Revive:
                baseText = $"- Revive charges: {Mathf.Max(effect.charges, 1)}";
                break;

            case SkillSpecialEffectType.ShieldBubble:
                baseText = $"- Shield: +{effect.amount:0.##}";
                break;

            case SkillSpecialEffectType.ExtraProjectiles:
                baseText = $"- Extra projectiles: +{Mathf.Max(effect.charges, 1)}";
                break;

            case SkillSpecialEffectType.BulletSplashDamage:
                baseText = $"- Splash damage: {effect.amount:0.##} | Radius: {effect.radius:0.##}";
                break;

            case SkillSpecialEffectType.None:
                baseText = "- No special effect";
                break;

            default:
                baseText = $"- {effect.effectType}";
                break;
        }

        if (!string.IsNullOrWhiteSpace(effect.description))
            baseText += $"\n  {effect.description}";

        return baseText;
    }

    private void SetText(TMP_Text targetText, string value)
    {
        if (targetText == null)
            return;

        targetText.text = value;

        bool hasText = !string.IsNullOrWhiteSpace(value);
        targetText.gameObject.SetActive(hasText);
    }
}
