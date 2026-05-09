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
    public Image iconImage;
    public Button skillButton;

    [Header("Runtime References")]
    [SerializeField] private SkillEffectApplier skillEffectApplier;
    [SerializeField] private SkillResearchRadialPanelUI researchRadialPanelUI;

    private void Awake()
    {
        if (iconImage == null)
            iconImage = GetComponent<Image>();

        if (skillButton == null)
            skillButton = GetComponent<Button>();

        if (skillEffectApplier == null)
            skillEffectApplier = FindObjectOfType<SkillEffectApplier>();

        if (researchRadialPanelUI == null)
            researchRadialPanelUI = FindObjectOfType<SkillResearchRadialPanelUI>();

        if (skillButton != null)
            skillButton.onClick.AddListener(Buy);
        else
            Debug.LogWarning($"Skill on {gameObject.name}: Button не назначен.");
    }

    public void CheckDependencies()
    {
        if (skillSO == null)
            return;

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
                if (previousSkill == null)
                    continue;

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
                if (previousSkill == null)
                    continue;

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
        if (skillSO == null)
            return;

        if (lockStatusText != null)
        {
            lockStatusText.text = !skillSO.isLocked ? "Unlocked" :
                                  skillSO.canBeUnlocked ? "Unlockable" : "Locked";
        }

        if (titleText != null)
        {
            titleText.text = !string.IsNullOrWhiteSpace(skillSO.skillName)
                ? skillSO.skillName
                : skillSO.name;
        }

        if (costText != null)
            costText.text = $"{skillSO.cost} coins";

        if (iconImage != null && skillSO.skillIcon != null)
            iconImage.sprite = skillSO.skillIcon;

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

        if (skillButton != null)
        {
            bool hasEnoughCoins = CoinSystem.Instance != null &&
                                  CoinSystem.Instance.CurrentCoins >= skillSO.cost;

            skillButton.interactable =
                skillSO.canBeUnlocked &&
                skillSO.isLocked &&
                !skillSO.permanentlyLocked &&
                hasEnoughCoins;
        }
    }

    public void Buy()
    {
        if (skillSO == null)
            return;

        if (!skillSO.canBeUnlocked || !skillSO.isLocked || skillSO.permanentlyLocked)
            return;

        if (CoinSystem.Instance == null)
        {
            Debug.LogWarning("CoinSystem.Instance не найден.");
            return;
        }

        if (!CoinSystem.Instance.SpendCoins(skillSO.cost))
            return;

        skillSO.Unlock();

        if (skillEffectApplier == null)
            skillEffectApplier = FindObjectOfType<SkillEffectApplier>();

        if (skillEffectApplier != null)
        {
            skillEffectApplier.ApplySkill(skillSO);
        }
        else
        {
            Debug.LogWarning($"Skill {skillSO.name}: SkillEffectApplier не найден на сцене.");
        }

        if (researchRadialPanelUI == null)
            researchRadialPanelUI = FindObjectOfType<SkillResearchRadialPanelUI>();

        if (researchRadialPanelUI != null)
            researchRadialPanelUI.ShowSkill(skillSO);
        else
            Debug.LogWarning("SkillResearchRadialPanelUI не найден на сцене.");

        if (SkillTree.skillTree != null)
            SkillTree.skillTree.UpdateAllSkillUI();

        if (CoinSystem.Instance != null)
            CoinSystem.Instance.UpdateUI();
    }
}