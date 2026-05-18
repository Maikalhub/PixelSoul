using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SkillEffectApplier : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement player;
    [SerializeField] private PlayerEnemyDetectionRadius enemyDetection;
    [SerializeField] private FlashlightController flashlight;

    [Header("Flashlight Creation")]
    [SerializeField] private GameObject flashlightPrefab;
    [SerializeField] private Transform flashlightParent;

    [Header("Skill Trigger UI")]
    [SerializeField] private SkillIconRadialUI skillIconRadialUI;
    [SerializeField] private float skillIconShowDuration = 2f;

    [Header("Message UI")]
    [SerializeField] private CanvasMessageManager messageManager;
    [SerializeField] private bool findMessageManagerAutomatically = true;
    [SerializeField] private bool showSkillEffectMessages = true;

    [Header("Applied / Available Messages")]
    [SerializeField] private bool showMessagesWhenSkillApplied = true;
    [SerializeField] private string skillAppliedMessageFormat = "Skill effect applied: {0}";
    [SerializeField] private string shootingReadyMessage = "Skill ready: Shooting";
    [SerializeField] private string flashlightReadyMessage = "Skill ready: Flashlight";
    [SerializeField] private string reviveReadyMessageFormat = "Skill ready: Revive | Charges: {0}";
    [SerializeField] private string shieldReadyMessageFormat = "Skill ready: Shield | Shield: +{0}";
    [SerializeField] private string extraProjectilesReadyMessageFormat = "Skill ready: Extra projectiles | +{0}";
    [SerializeField] private string bulletSplashReadyMessageFormat = "Skill ready: Bullet splash | Damage: {0} | Radius: {1}";

    [Header("Used / Triggered Messages")]
    [SerializeField] private bool showMessagesWhenSkillUsed = true;
    [SerializeField] private string genericSkillUsedMessageFormat = "Skill used: {0}";
    [SerializeField] private string unavailableSkillMessageFormat = "{0} is not available";
    [SerializeField] private string shieldUsedMessageFormat = "Shield absorbed {0} damage | Shield: {1:0.#}/{2:0.#}";
    [SerializeField] private string reviveUsedMessageFormat = "Revive used | Charges left: {0}";
    [SerializeField] private string shootingUsedMessage = "Shooting skill used";
    [SerializeField] private string flashlightUsedMessage = "Flashlight skill used";
    [SerializeField] private string bulletSplashUsedMessage = "Bullet splash triggered";
    [SerializeField] private string extraProjectilesUsedMessageFormat = "Extra projectiles fired: +{0}";

    [Header("Message Colors")]
    [SerializeField] private Color appliedMessageColor = Color.cyan;
    [SerializeField] private Color usedMessageColor = Color.white;
    [SerializeField] private Color warningMessageColor = Color.yellow;

    [Header("Message Duration")]
    [SerializeField] private float appliedMessageDuration = 1.5f;
    [SerializeField] private float usedMessageDuration = 1.2f;
    [SerializeField] private float warningMessageDuration = 1.3f;

    [Header("Runtime Special Effects")]
    [SerializeField] private bool canShoot = false;
    [SerializeField] private bool hasFlashlight = false;
    [SerializeField] private int reviveCharges = 0;

    [SerializeField] private float currentShield = 0f;
    [SerializeField] private float maxShield = 0f;

    [SerializeField] private int extraProjectiles = 0;
    [SerializeField] private float projectileSpreadAngle = 8f;

    [SerializeField] private bool bulletSplashEnabled = false;
    [SerializeField] private float bulletSplashDamage = 0f;
    [SerializeField] private float bulletSplashRadius = 0f;

    private readonly HashSet<string> appliedSkillIds = new HashSet<string>();

    private struct SkillTriggerIconData
    {
        public Sprite icon;
        public float duration;

        public SkillTriggerIconData(Sprite icon, float duration)
        {
            this.icon = icon;
            this.duration = duration;
        }
    }

    private readonly Dictionary<SkillSpecialEffectType, SkillTriggerIconData> triggerIcons =
        new Dictionary<SkillSpecialEffectType, SkillTriggerIconData>();

    public bool CanShoot => canShoot;
    public bool HasFlashlight => hasFlashlight && flashlight != null && flashlight.IsUnlocked;
    public FlashlightController Flashlight => flashlight;

    public int ExtraProjectiles => extraProjectiles;
    public float ProjectileSpreadAngle => projectileSpreadAngle;

    public bool BulletSplashEnabled => bulletSplashEnabled;
    public float BulletSplashDamage => bulletSplashDamage;
    public float BulletSplashRadius => bulletSplashRadius;

    public float CurrentShield => currentShield;
    public float MaxShield => maxShield;
    public int ReviveCharges => reviveCharges;

    private void Awake()
    {
        if (player == null)
            player = GetComponent<PlayerMovement>();

        if (enemyDetection == null)
            enemyDetection = GetComponent<PlayerEnemyDetectionRadius>();

        if (flashlight == null)
            flashlight = GetComponentInChildren<FlashlightController>(true);

        if (skillIconRadialUI == null)
            skillIconRadialUI = SkillIconRadialUI.Instance;

        if (messageManager == null && findMessageManagerAutomatically)
            messageManager = FindObjectOfType<CanvasMessageManager>();

        if (flashlight != null)
        {
            flashlight.SetUnlocked(false);
            ConnectFlashlight();
        }
    }

    public void ResetRuntimeEffects()
    {
        appliedSkillIds.Clear();
        triggerIcons.Clear();

        canShoot = false;
        hasFlashlight = false;
        reviveCharges = 0;

        currentShield = 0f;
        maxShield = 0f;

        extraProjectiles = 0;
        projectileSpreadAngle = 8f;

        bulletSplashEnabled = false;
        bulletSplashDamage = 0f;
        bulletSplashRadius = 0f;

        if (flashlight != null)
            flashlight.SetUnlocked(false);
    }

    public bool ApplySkill(SkillSO skill)
    {
        if (skill == null)
            return false;

        string uniqueId = !string.IsNullOrWhiteSpace(skill.skillId)
            ? skill.skillId
            : skill.name;

        if (appliedSkillIds.Contains(uniqueId))
            return false;

        if (skill.upgradeType == SkillUpgradeType.BaseStats && skill.statModifiers != null)
        {
            foreach (SkillStatModifier modifier in skill.statModifiers)
                ApplyStatModifier(modifier);
        }

        if (skill.upgradeType == SkillUpgradeType.SpecialEffect && skill.specialEffects != null)
        {
            foreach (SkillSpecialEffectData effect in skill.specialEffects)
                ApplySpecialEffect(effect);
        }

        RegisterTriggerIcons(skill);

        appliedSkillIds.Add(uniqueId);

        if (showMessagesWhenSkillApplied)
        {
            string skillName = !string.IsNullOrWhiteSpace(skill.skillName)
                ? skill.skillName
                : skill.name;

            ShowMessage(
                string.Format(skillAppliedMessageFormat, skillName),
                appliedMessageColor,
                appliedMessageDuration
            );
        }

        Debug.Log($"Skill applied: {uniqueId}");
        return true;
    }

    private void RegisterTriggerIcons(SkillSO skill)
    {
        if (skill == null)
            return;

        if (skill.skillIcon == null)
            return;

        if (skill.specialEffects == null)
            return;

        foreach (SkillSpecialEffectData effect in skill.specialEffects)
        {
            if (effect == null)
                continue;

            if (effect.effectType == SkillSpecialEffectType.None)
                continue;

            float duration = effect.duration > 0f
                ? effect.duration
                : skillIconShowDuration;

            triggerIcons[effect.effectType] =
                new SkillTriggerIconData(skill.skillIcon, duration);
        }
    }

    private void ShowTriggeredSkillIcon(SkillSpecialEffectType effectType)
    {
        if (!triggerIcons.TryGetValue(effectType, out SkillTriggerIconData data))
            return;

        if (data.icon == null)
            return;

        if (skillIconRadialUI == null)
            skillIconRadialUI = SkillIconRadialUI.Instance;

        if (skillIconRadialUI == null)
            return;

        skillIconRadialUI.ShowIcon(data.icon, data.duration);
    }

    private void ApplyStatModifier(SkillStatModifier modifier)
    {
        if (player == null || modifier == null)
            return;

        switch (modifier.statType)
        {
            case SkillStatType.MaxHealth:
                {
                    int oldMax = player.maxHealth;
                    player.maxHealth = ApplyInt(player.maxHealth, modifier.value, modifier.valueMode, 1);

                    int delta = player.maxHealth - oldMax;
                    player.currentHealth = Mathf.Clamp(
                        player.currentHealth + Mathf.Max(delta, 0),
                        0,
                        player.maxHealth
                    );

                    break;
                }

            case SkillStatType.MaxStamina:
                {
                    float oldMax = player.maxStamina;
                    player.maxStamina = ApplyFloat(player.maxStamina, modifier.value, modifier.valueMode, 1f);

                    float delta = player.maxStamina - oldMax;
                    player.currentStamina = Mathf.Clamp(
                        player.currentStamina + Mathf.Max(delta, 0f),
                        0f,
                        player.maxStamina
                    );

                    break;
                }

            case SkillStatType.AttackDamage:
                player.attackDamage = ApplyInt(player.attackDamage, modifier.value, modifier.valueMode, 1);
                break;

            case SkillStatType.Defense:
                player.defenseStat = ApplyInt(player.defenseStat, modifier.value, modifier.valueMode, 0);
                break;

            case SkillStatType.MoveSpeed:
                player.moveSpeed = ApplyFloat(player.moveSpeed, modifier.value, modifier.valueMode, 0.1f);
                break;

            case SkillStatType.JumpPower:
                player.jumpPower = ApplyFloat(player.jumpPower, modifier.value, modifier.valueMode, 0.1f);
                break;

            case SkillStatType.SprintMultiplier:
                player.sprintMultiplier = ApplyFloat(player.sprintMultiplier, modifier.value, modifier.valueMode, 1f);
                break;

            case SkillStatType.DashSpeed:
                player.dashSpeed = ApplyFloat(player.dashSpeed, modifier.value, modifier.valueMode, 0.1f);
                break;

            case SkillStatType.ThrowForce:
                player.throwForce = ApplyFloat(player.throwForce, modifier.value, modifier.valueMode, 0.1f);
                break;

            case SkillStatType.ThrowCooldown:
                player.cooldown = ApplyFloat(player.cooldown, modifier.value, modifier.valueMode, 0.01f);
                break;

            case SkillStatType.FlashlightActiveDuration:
                if (EnsureFlashlightExists(true))
                    flashlight.ModifyActiveDuration(modifier.value, modifier.valueMode);
                break;

            case SkillStatType.FlashlightRechargeDuration:
                if (EnsureFlashlightExists(true))
                    flashlight.ModifyRechargeDuration(modifier.value, modifier.valueMode);
                break;

            case SkillStatType.EnemyDetectionRadius:
                if (enemyDetection != null)
                    enemyDetection.ModifyBaseRadius(modifier.value, modifier.valueMode);
                break;

            case SkillStatType.FlashlightDetectionBonus:
                if (enemyDetection != null)
                    enemyDetection.ModifyFlashlightDetectionBonus(modifier.value, modifier.valueMode);
                break;
        }
    }

    private void ApplySpecialEffect(SkillSpecialEffectData effect)
    {
        if (effect == null)
            return;

        switch (effect.effectType)
        {
            case SkillSpecialEffectType.EnableShooting:
                canShoot = effect.enabledOnUnlock;

                if (canShoot)
                    ShowAppliedMessage(shootingReadyMessage);

                break;

            case SkillSpecialEffectType.EnableFlashlight:
                SetFlashlightUnlocked(effect.enabledOnUnlock);

                if (effect.enabledOnUnlock)
                    ShowAppliedMessage(flashlightReadyMessage);

                break;

            case SkillSpecialEffectType.Revive:
                {
                    int charges = Mathf.Max(effect.charges, 1);
                    reviveCharges += charges;

                    ShowAppliedMessage(
                        string.Format(reviveReadyMessageFormat, reviveCharges)
                    );

                    break;
                }

            case SkillSpecialEffectType.ShieldBubble:
                {
                    float shieldAmount = effect.amount > 0f ? effect.amount : 1f;
                    maxShield += shieldAmount;
                    currentShield += shieldAmount;

                    ShowAppliedMessage(
                        string.Format(shieldReadyMessageFormat, shieldAmount.ToString("0.#"))
                    );

                    break;
                }

            case SkillSpecialEffectType.ExtraProjectiles:
                {
                    int projectiles = Mathf.Max(effect.charges, 1);
                    extraProjectiles += projectiles;

                    if (effect.amount > 0f)
                        projectileSpreadAngle = effect.amount;

                    ShowAppliedMessage(
                        string.Format(extraProjectilesReadyMessageFormat, projectiles)
                    );

                    break;
                }

            case SkillSpecialEffectType.BulletSplashDamage:
                bulletSplashEnabled = effect.enabledOnUnlock;
                bulletSplashDamage = Mathf.Max(bulletSplashDamage, effect.amount);
                bulletSplashRadius = Mathf.Max(bulletSplashRadius, effect.radius);

                if (bulletSplashEnabled)
                {
                    ShowAppliedMessage(
                        string.Format(
                            bulletSplashReadyMessageFormat,
                            bulletSplashDamage.ToString("0.#"),
                            bulletSplashRadius.ToString("0.#")
                        )
                    );
                }

                break;
        }
    }

    private void SetFlashlightUnlocked(bool unlocked)
    {
        if (unlocked)
        {
            if (!EnsureFlashlightExists(true))
            {
                Debug.LogWarning("Нельзя разблокировать фонарь: FlashlightController не найден и prefab не назначен.");
                return;
            }

            hasFlashlight = true;
            flashlight.SetUnlocked(true);
            ConnectFlashlight();
            return;
        }

        hasFlashlight = false;

        if (flashlight != null)
            flashlight.SetUnlocked(false);
    }

    private bool EnsureFlashlightExists(bool allowCreate)
    {
        if (flashlight != null)
            return true;

        flashlight = GetComponentInChildren<FlashlightController>(true);

        if (flashlight != null)
        {
            ConnectFlashlight();
            return true;
        }

        if (!allowCreate || flashlightPrefab == null)
            return false;

        Transform parent = flashlightParent != null
            ? flashlightParent
            : transform;

        GameObject createdFlashlight = Instantiate(flashlightPrefab, parent);
        flashlight = createdFlashlight.GetComponentInChildren<FlashlightController>(true);

        if (flashlight == null)
        {
            Debug.LogWarning("В созданном prefab фонаря нет FlashlightController.");
            Destroy(createdFlashlight);
            return false;
        }

        flashlight.SetUnlocked(false);
        ConnectFlashlight();
        return true;
    }

    private void ConnectFlashlight()
    {
        if (player != null)
            player.flashlight = flashlight;

        if (enemyDetection != null)
            enemyDetection.SetFlashlight(flashlight);
    }

    public int AbsorbDamage(int incomingDamage)
    {
        if (incomingDamage <= 0)
            return 0;

        if (currentShield <= 0f)
            return incomingDamage;

        float absorbed = Mathf.Min(currentShield, incomingDamage);
        currentShield -= absorbed;

        if (absorbed > 0f)
        {
            ShowTriggeredSkillIcon(SkillSpecialEffectType.ShieldBubble);

            if (showMessagesWhenSkillUsed)
            {
                ShowMessage(
                    string.Format(
                        shieldUsedMessageFormat,
                        absorbed.ToString("0.#"),
                        currentShield,
                        maxShield
                    ),
                    usedMessageColor,
                    usedMessageDuration
                );
            }
        }

        int remainingDamage = incomingDamage - Mathf.RoundToInt(absorbed);
        return Mathf.Max(remainingDamage, 0);
    }

    public bool TryUseRevive()
    {
        if (reviveCharges <= 0)
            return false;

        reviveCharges--;

        ShowTriggeredSkillIcon(SkillSpecialEffectType.Revive);

        if (showMessagesWhenSkillUsed)
        {
            ShowMessage(
                string.Format(reviveUsedMessageFormat, reviveCharges),
                usedMessageColor,
                usedMessageDuration
            );
        }

        return true;
    }

    public void NotifySkillUsed(SkillSpecialEffectType effectType)
    {
        if (!showMessagesWhenSkillUsed)
            return;

        ShowTriggeredSkillIcon(effectType);

        switch (effectType)
        {
            case SkillSpecialEffectType.EnableShooting:
                if (canShoot)
                {
                    ShowMessage(shootingUsedMessage, usedMessageColor, usedMessageDuration);
                }
                else
                {
                    ShowUnavailableMessage("Shooting");
                }

                break;

            case SkillSpecialEffectType.EnableFlashlight:
                if (HasFlashlight)
                {
                    ShowMessage(flashlightUsedMessage, usedMessageColor, usedMessageDuration);
                }
                else
                {
                    ShowUnavailableMessage("Flashlight");
                }

                break;

            case SkillSpecialEffectType.ExtraProjectiles:
                if (extraProjectiles > 0)
                {
                    ShowMessage(
                        string.Format(extraProjectilesUsedMessageFormat, extraProjectiles),
                        usedMessageColor,
                        usedMessageDuration
                    );
                }
                else
                {
                    ShowUnavailableMessage("Extra projectiles");
                }

                break;

            case SkillSpecialEffectType.BulletSplashDamage:
                if (bulletSplashEnabled)
                {
                    ShowMessage(bulletSplashUsedMessage, usedMessageColor, usedMessageDuration);
                }
                else
                {
                    ShowUnavailableMessage("Bullet splash");
                }

                break;

            case SkillSpecialEffectType.ShieldBubble:
                if (currentShield > 0f)
                {
                    ShowMessage(
                        string.Format(genericSkillUsedMessageFormat, "Shield"),
                        usedMessageColor,
                        usedMessageDuration
                    );
                }
                else
                {
                    ShowUnavailableMessage("Shield");
                }

                break;

            case SkillSpecialEffectType.Revive:
                if (reviveCharges > 0)
                {
                    ShowMessage(
                        string.Format(genericSkillUsedMessageFormat, "Revive"),
                        usedMessageColor,
                        usedMessageDuration
                    );
                }
                else
                {
                    ShowUnavailableMessage("Revive");
                }

                break;

            default:
                ShowMessage(
                    string.Format(genericSkillUsedMessageFormat, effectType),
                    usedMessageColor,
                    usedMessageDuration
                );

                break;
        }
    }

    private void ShowAppliedMessage(string message)
    {
        if (!showMessagesWhenSkillApplied)
            return;

        ShowMessage(message, appliedMessageColor, appliedMessageDuration);
    }

    private void ShowUnavailableMessage(string skillName)
    {
        ShowMessage(
            string.Format(unavailableSkillMessageFormat, skillName),
            warningMessageColor,
            warningMessageDuration
        );
    }

    private void ShowMessage(string message, Color color, float duration)
    {
        if (!showSkillEffectMessages)
            return;

        if (string.IsNullOrWhiteSpace(message))
            return;

        if (messageManager == null && findMessageManagerAutomatically)
            messageManager = FindObjectOfType<CanvasMessageManager>();

        if (messageManager == null)
        {
            Debug.LogWarning("SkillEffectApplier: CanvasMessageManager не найден.");
            return;
        }

        messageManager.ShowMessage(message, color, duration);
    }

    private int ApplyInt(int currentValue, float amount, SkillValueMode mode, int minValue)
    {
        int result = currentValue;

        switch (mode)
        {
            case SkillValueMode.Add:
                result = currentValue + Mathf.RoundToInt(amount);
                break;

            case SkillValueMode.Multiply:
                result = Mathf.RoundToInt(currentValue * amount);
                break;

            case SkillValueMode.Set:
                result = Mathf.RoundToInt(amount);
                break;
        }

        return Mathf.Max(result, minValue);
    }

    private float ApplyFloat(float currentValue, float amount, SkillValueMode mode, float minValue)
    {
        float result = currentValue;

        switch (mode)
        {
            case SkillValueMode.Add:
                result = currentValue + amount;
                break;

            case SkillValueMode.Multiply:
                result = currentValue * amount;
                break;

            case SkillValueMode.Set:
                result = amount;
                break;
        }

        return Mathf.Max(result, minValue);
    }
}
