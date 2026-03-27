using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Skill : MonoBehaviour
{
    [Header("Skill Data")]
    public SkillSO skillSO;

    [Header("UI References")]
    public TMP_Text lockStatusText;
    public TMP_Text titleText;
    public TMP_Text costText;

    private Image iconImage;
    private Button skillButton;

    private void Awake()
    {
        iconImage = GetComponent<Image>();
        skillButton = GetComponent<Button>();
        skillButton.onClick.AddListener(Buy);
    }

    // Проверяем зависимости и permanent lock
    public void CheckDependencies()
    {
        if (skillSO.permanentlyLocked)
        {
            skillSO.canBeUnlocked = false;
            return;
        }

        if (skillSO.requiredSkills == null || skillSO.requiredSkills.Length == 0)
        {
            skillSO.canBeUnlocked = true;
            return;
        }

        if (skillSO.requireAll)
        {
            // Логика И (все навыки должны быть изучены)
            bool canBeUnlocked = true;
            foreach (SkillSO previousSkill in skillSO.requiredSkills)
            {
                if (previousSkill.isLocked)
                {
                    canBeUnlocked = false;
                    break;
                }
            }
            skillSO.canBeUnlocked = canBeUnlocked;
        }
        else
        {
            // Логика ИЛИ (хотя бы один навык изучен)
            bool canBeUnlocked = false;
            foreach (SkillSO previousSkill in skillSO.requiredSkills)
            {
                if (!previousSkill.isLocked)
                {
                    canBeUnlocked = true;
                    break;
                }
            }
            skillSO.canBeUnlocked = canBeUnlocked;
        }
    }

    public void UpdateUI()
    {
        lockStatusText.text = !skillSO.isLocked ? "Unlocked" :
                              skillSO.canBeUnlocked ? "Unlockable" : "Locked";

        titleText.text = skillSO.name;
        costText.text = $"Cost: {skillSO.cost} coins";

        // Иконка навыка
        if (skillSO.skillIcon != null)
        {
            iconImage.sprite = skillSO.skillIcon;
        }

        // Цвет кнопки
        if (skillSO.permanentlyLocked) iconImage.color = Color.gray;
        else if (!skillSO.isLocked) iconImage.color = Color.green;
        else if (skillSO.canBeUnlocked) iconImage.color = Color.yellow;
        else iconImage.color = Color.white;

        // Доступность кнопки
        skillButton.interactable = skillSO.canBeUnlocked && skillSO.isLocked &&
                                   !skillSO.permanentlyLocked &&
                                   CoinSystem.Instance.CurrentCoins >= skillSO.cost;
    }

    public void Buy()
    {
        if (!skillSO.canBeUnlocked || !skillSO.isLocked || skillSO.permanentlyLocked) return;

        if (CoinSystem.Instance.SpendCoins(skillSO.cost))
        {
            skillSO.Unlock();
            SkillTree.skillTree.UpdateAllSkillUI();
            CoinSystem.Instance.UpdateUI();
        }
    }
}