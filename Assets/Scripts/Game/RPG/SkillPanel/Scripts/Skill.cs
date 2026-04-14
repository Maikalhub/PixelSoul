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

    [Header("Assigned References")]
    public Image iconImage;     // сюда назначаешь любой Image, в том числе дочерний
    public Button skillButton;  // сюда тоже можно назначить кнопку вручную

    private void Awake()
    {
        // Если не назначил вручную — попробуем взять с текущего объекта
        if (iconImage == null)
            iconImage = GetComponent<Image>();

        if (skillButton == null)
            skillButton = GetComponent<Button>();

        if (skillButton != null)
            skillButton.onClick.AddListener(Buy);
        else
            Debug.LogWarning($"Skill on {gameObject.name}: Button не назначен.");
    }

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
        if (lockStatusText != null)
        {
            lockStatusText.text = !skillSO.isLocked ? "Unlocked" :
                                  skillSO.canBeUnlocked ? "Unlockable" : "Locked";
        }

        if (titleText != null)
            titleText.text = skillSO.name;

        if (costText != null)
            costText.text = $"{skillSO.cost} coins";

        // Иконка навыка
        if (iconImage != null && skillSO.skillIcon != null)
        {
            iconImage.sprite = skillSO.skillIcon;
        }

        // Цвет иконки/объекта изображения
        if (iconImage != null)
        {
            if (skillSO.permanentlyLocked)
                iconImage.color = Color.gray;
            else if (!skillSO.isLocked)
                iconImage.color = Color.green;
            else if (skillSO.canBeUnlocked)
                iconImage.color = Color.yellow;
            else
                iconImage.color = Color.white;
        }

        // Доступность кнопки
        if (skillButton != null)
        {
            skillButton.interactable = skillSO.canBeUnlocked &&
                                       skillSO.isLocked &&
                                       !skillSO.permanentlyLocked &&
                                       CoinSystem.Instance.CurrentCoins >= skillSO.cost;
        }
    }

    public void Buy()
    {
        if (!skillSO.canBeUnlocked || !skillSO.isLocked || skillSO.permanentlyLocked)
            return;

        if (CoinSystem.Instance.SpendCoins(skillSO.cost))
        {
            skillSO.Unlock();
            SkillTree.skillTree.UpdateAllSkillUI();
            CoinSystem.Instance.UpdateUI();
        }
    }
}