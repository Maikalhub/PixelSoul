using System;
using System.Collections.Generic;
using UnityEngine;

public enum SkillUpgradeType
{
    BaseStats,
    SpecialEffect
}

public enum SkillValueMode
{
    Add,
    Multiply,
    Set
}

public enum SkillStatType
{
    MaxHealth,
    MaxStamina,
    AttackDamage,
    Defense,
    MoveSpeed,
    JumpPower,
    SprintMultiplier,
    DashSpeed,
    ThrowForce,
    ThrowCooldown,
    FlashlightActiveDuration,
    FlashlightRechargeDuration,
    EnemyDetectionRadius,
    FlashlightDetectionBonus
}

public enum SkillSpecialEffectType
{
    None,
    EnableShooting,
    Revive,
    ShieldBubble,
    ExtraProjectiles,
    BulletSplashDamage,
    EnableFlashlight
}

[Serializable]
public class SkillStatModifier
{
    public SkillStatType statType;
    public SkillValueMode valueMode = SkillValueMode.Add;
    public float value = 0f;
}

[Serializable]
public class SkillSpecialEffectData
{
    public SkillSpecialEffectType effectType = SkillSpecialEffectType.None;

    [TextArea]
    public string description;

    public float amount = 0f;
    public float radius = 0f;
    public float duration = 0f;
    public int charges = 0;
    public bool enabledOnUnlock = true;
}

[CreateAssetMenu(menuName = "Skill", fileName = "New Skill")]
public class SkillSO : ScriptableObject
{
    [Header("Info")]
    public string skillId;
    public string skillName;

    [TextArea]
    public string description;

    public Sprite skillIcon;
    public int cost = 1;

    [Header("Default State")]
    [Tooltip("Locked-состояние по умолчанию. К нему навык вернётся при сбросе.")]
    public bool defaultIsLocked = true;

    [Tooltip("Can Be Unlocked по умолчанию. Обычно лучше оставить false, потому что доступность пересчитывается через зависимости.")]
    public bool defaultCanBeUnlocked = false;

    [Tooltip("Permanently Locked по умолчанию.")]
    public bool defaultPermanentlyLocked = false;

    [Header("Runtime State")]
    public bool isLocked = true;
    public bool canBeUnlocked = false;
    public bool permanentlyLocked = false;

    [Header("Save / Reset")]
    [Tooltip("Если выключено, ResetSkill() будет возвращать навык в Default State при старте дерева навыков.")]
    public bool saveProgress = true;

    [Tooltip("Если включено, при выходе из Play Mode / закрытии игры навык принудительно вернётся в Default State.")]
    public bool resetToDefaultOnExit = true;

    [Header("Dependencies")]
    public SkillSO[] requiredSkills;
    public SkillSO[] exclusiveSkills;
    public bool requireAll = true;

    [Header("Upgrade Data")]
    public SkillUpgradeType upgradeType = SkillUpgradeType.BaseStats;
    public List<SkillStatModifier> statModifiers = new List<SkillStatModifier>();
    public List<SkillSpecialEffectData> specialEffects = new List<SkillSpecialEffectData>();

    public bool HasStatModifiers =>
        upgradeType == SkillUpgradeType.BaseStats &&
        statModifiers != null &&
        statModifiers.Count > 0;

    public bool HasSpecialEffects =>
        upgradeType == SkillUpgradeType.SpecialEffect &&
        specialEffects != null &&
        specialEffects.Count > 0;

    private void OnEnable()
    {
        Application.quitting -= HandleApplicationQuitting;
        Application.quitting += HandleApplicationQuitting;
    }

    private void OnDisable()
    {
        Application.quitting -= HandleApplicationQuitting;

        if (!Application.isPlaying)
            return;

        if (resetToDefaultOnExit)
            ResetToDefaultState();
    }

    private void HandleApplicationQuitting()
    {
        if (resetToDefaultOnExit)
            ResetToDefaultState();
    }

    public void Unlock()
    {
        if (permanentlyLocked)
            return;

        isLocked = false;

        if (exclusiveSkills != null)
        {
            foreach (SkillSO skill in exclusiveSkills)
            {
                if (skill == null)
                    continue;

                skill.permanentlyLocked = true;
                skill.isLocked = true;
                skill.canBeUnlocked = false;
            }
        }
    }

    public void ResetSkill()
    {
        if (!saveProgress)
        {
            ResetToDefaultState();
        }
    }

    public void ForceResetSkill()
    {
        ResetToDefaultState();
    }

    public void ResetToDefaultState()
    {
        isLocked = defaultIsLocked;
        canBeUnlocked = defaultCanBeUnlocked;
        permanentlyLocked = defaultPermanentlyLocked;
    }

    [ContextMenu("Set Current State As Default")]
    public void SetCurrentStateAsDefault()
    {
        defaultIsLocked = isLocked;
        defaultCanBeUnlocked = canBeUnlocked;
        defaultPermanentlyLocked = permanentlyLocked;
    }

    [ContextMenu("Reset To Default State")]
    public void ResetToDefaultStateFromContextMenu()
    {
        ResetToDefaultState();
    }
}