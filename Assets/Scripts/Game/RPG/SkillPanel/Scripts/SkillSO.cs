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

    [Header("State")]
    public bool isLocked = true;
    public bool canBeUnlocked = false;
    public bool saveProgress = true;
    public bool permanentlyLocked = false;

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
            isLocked = true;
            canBeUnlocked = false;
            permanentlyLocked = false;
        }
    }
}