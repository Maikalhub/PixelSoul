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

    [Header("Message UI")]
    [SerializeField] private CanvasMessageManager messageManager;
    [SerializeField] private bool findMessageManagerAutomatically = true;
    [SerializeField] private bool showSkillMessages = true;

    [Header("Button Behaviour")]
    [Tooltip("Если true, кнопку можно нажать даже когда скилл недоступен, чтобы показать сообщение с причиной.")]
    [SerializeField] private bool allowClickWhenUnavailableForMessages = true;

    [Header("Skill Messages")]
    [SerializeField] private string unlockedMessageFormat = "Skill unlocked: {0}";
    [SerializeField] private string alreadyUnlockedMessageFormat = "Skill already unlocked: {0}";
    [SerializeField] private string permanentlyLockedMessageFormat = "Skill locked: {0}";
    [SerializeField] private string requirementsMissingMessageFormat = "Skill unavailable: {0} | Unlock required skills first";
    [SerializeField] private string notEnoughCoinsMessageFormat = "Not enough coins for {0} | Need {1}, you have {2}";
    [SerializeField] private string coinSystemMissingMessage = "Cannot buy skill | Coin system not found";
    [SerializeField] private string effectApplierMissingMessageFormat = "Skill unlocked: {0} | Effect applier not found";
    [SerializeField] private string skillDataMissingMessage = "Cannot buy skill | Skill data missing";

    [Header("Available Upgrade Message")]
    [SerializeField] private bool showAvailableMessage = true;

    [Tooltip("Если true, сообщение появится даже при первом обновлении UI, если скилл уже доступен.")]
    [SerializeField] private bool showAvailableMessageOnFirstUIUpdate = false;

    [SerializeField] private string availableMessageFormat = "Upgrade available: {0}";
    [SerializeField] private Color availableMessageColor = Color.cyan;
    [SerializeField] private float availableMessageDuration = 1.5f;

    [Header("Message Colors")]
    [SerializeField] private Color successMessageColor = Color.green;
    [SerializeField] private Color warningMessageColor = Color.yellow;
    [SerializeField] private Color errorMessageColor = Color.red;
    [SerializeField] private Color infoMessageColor = Color.white;

    [Header("Message Duration")]
    [SerializeField] private float successMessageDuration = 1.5f;
    [SerializeField] private float warningMessageDuration = 1.5f;
    [SerializeField] private float errorMessageDuration = 1.5f;
    [SerializeField] private float infoMessageDuration = 1.3f;

    private bool uiWasInitialized;
    private bool wasAvailableForPurchase;

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

        if (messageManager == null && findMessageManagerAutomatically)
            messageManager = FindObjectOfType<CanvasMessageManager>();

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

        bool hasEnoughCoins = CoinSystem.Instance != null &&
                              CoinSystem.Instance.CurrentCoins >= skillSO.cost;

        bool canActuallyBuy =
            skillSO.canBeUnlocked &&
            skillSO.isLocked &&
            !skillSO.permanentlyLocked &&
            hasEnoughCoins;

        if (skillButton != null)
        {
            if (allowClickWhenUnavailableForMessages)
            {
                skillButton.interactable = true;
            }
            else
            {
                skillButton.interactable = canActuallyBuy;
            }
        }

        CheckAvailableMessage(canActuallyBuy);
    }

    public void Buy()
    {
        if (skillSO == null)
        {
            ShowMessage(skillDataMissingMessage, errorMessageColor, errorMessageDuration);
            return;
        }

        string skillName = GetSkillName();

        if (!skillSO.isLocked)
        {
            ShowMessage(
                string.Format(alreadyUnlockedMessageFormat, skillName),
                infoMessageColor,
                infoMessageDuration
            );

            return;
        }

        if (skillSO.permanentlyLocked)
        {
            ShowMessage(
                string.Format(permanentlyLockedMessageFormat, skillName),
                errorMessageColor,
                errorMessageDuration
            );

            return;
        }

        if (!skillSO.canBeUnlocked)
        {
            ShowMessage(
                string.Format(requirementsMissingMessageFormat, skillName),
                warningMessageColor,
                warningMessageDuration
            );

            return;
        }

        if (CoinSystem.Instance == null)
        {
            ShowMessage(coinSystemMissingMessage, errorMessageColor, errorMessageDuration);
            Debug.LogWarning("CoinSystem.Instance не найден.");
            return;
        }

        int currentCoins = CoinSystem.Instance.CurrentCoins;

        if (currentCoins < skillSO.cost)
        {
            ShowMessage(
                string.Format(notEnoughCoinsMessageFormat, skillName, skillSO.cost, currentCoins),
                warningMessageColor,
                warningMessageDuration
            );

            return;
        }

        if (!CoinSystem.Instance.SpendCoins(skillSO.cost))
        {
            ShowMessage(
                string.Format(notEnoughCoinsMessageFormat, skillName, skillSO.cost, currentCoins),
                warningMessageColor,
                warningMessageDuration
            );

            return;
        }

        skillSO.Unlock();

        if (skillEffectApplier == null)
            skillEffectApplier = FindObjectOfType<SkillEffectApplier>();

        if (skillEffectApplier != null)
        {
            skillEffectApplier.ApplySkill(skillSO);

            ShowMessage(
                string.Format(unlockedMessageFormat, skillName),
                successMessageColor,
                successMessageDuration
            );
        }
        else
        {
            ShowMessage(
                string.Format(effectApplierMissingMessageFormat, skillName),
                warningMessageColor,
                warningMessageDuration
            );

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

    private void CheckAvailableMessage(bool canActuallyBuy)
    {
        if (!showAvailableMessage)
        {
            wasAvailableForPurchase = canActuallyBuy;
            uiWasInitialized = true;
            return;
        }

        bool becameAvailableNow =
            uiWasInitialized &&
            !wasAvailableForPurchase &&
            canActuallyBuy;

        bool availableOnFirstUpdate =
            !uiWasInitialized &&
            showAvailableMessageOnFirstUIUpdate &&
            canActuallyBuy;

        if (becameAvailableNow || availableOnFirstUpdate)
        {
            ShowMessage(
                string.Format(availableMessageFormat, GetSkillName()),
                availableMessageColor,
                availableMessageDuration
            );
        }

        wasAvailableForPurchase = canActuallyBuy;
        uiWasInitialized = true;
    }

    private string GetSkillName()
    {
        if (skillSO == null)
            return "Unknown Skill";

        if (!string.IsNullOrWhiteSpace(skillSO.skillName))
            return skillSO.skillName;

        return skillSO.name;
    }

    private void ShowMessage(string message, Color color, float duration)
    {
        if (!showSkillMessages)
            return;

        if (string.IsNullOrWhiteSpace(message))
            return;

        if (messageManager == null && findMessageManagerAutomatically)
            messageManager = FindObjectOfType<CanvasMessageManager>();

        if (messageManager == null)
        {
            Debug.LogWarning("Skill: CanvasMessageManager не найден.");
            return;
        }

        messageManager.ShowMessage(message, color, duration);
    }
}