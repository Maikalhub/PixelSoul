using UnityEngine;

[CreateAssetMenu(menuName = "Skill", fileName = "New Skill")]
public class SkillSO : ScriptableObject
{
    [Header("Skill State")]
    public bool isLocked = true;
    public bool canBeUnlocked = false;

    [Header("Dependencies")]
    public SkillSO[] requiredSkills;
    public SkillSO[] exclusiveSkills;

    [Header("Dependency Logic")]
    public bool requireAll = true; // true = И, false = ИЛИ

    [Header("Cost & Icon")]
    public int cost = 1;
    public Sprite skillIcon;

    [Header("Progress Settings")]
    public bool saveProgress = true;

    [Header("Permanent Lock")]
    public bool permanentlyLocked = false;

    public void Unlock()
    {
        if (permanentlyLocked) return;

        isLocked = false;

        if (exclusiveSkills != null)
        {
            foreach (SkillSO skill in exclusiveSkills)
            {
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