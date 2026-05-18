using UnityEngine;
using UnityEngine.UI;

public class UIPanelSwitcher : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject skillPanel;
    [SerializeField] private GameObject inventoryPanel;

    [Header("Keyboard")]
    [SerializeField] private KeyCode skillToggleKey = KeyCode.Tab;
    [SerializeField] private KeyCode inventoryToggleKey = KeyCode.B;

    [Header("Skill Buttons")]
    [Tooltip("Кнопки, которые открывают/закрывают панель навыков.")]
    [SerializeField] private Button[] skillToggleButtons;

    [Header("Inventory Buttons")]
    [Tooltip("Кнопки, которые открывают/закрывают инвентарь.")]
    [SerializeField] private Button[] inventoryToggleButtons;

    [Header("Close Buttons")]
    [Tooltip("Кнопки, которые закрывают обе панели. Можно оставить пустым.")]
    [SerializeField] private Button[] closeAllButtons;

    [Header("Start State")]
    [SerializeField] private bool closeAllOnStart = true;

    private void Awake()
    {
        RegisterButtons();
    }

    private void Start()
    {
        if (closeAllOnStart)
            CloseAllPanels();
        else
            ForceOnlyOnePanelActive();
    }

    private void Update()
    {
        if (Input.GetKeyDown(skillToggleKey))
            ToggleSkillPanel();

        if (Input.GetKeyDown(inventoryToggleKey))
            ToggleInventoryPanel();
    }

    private void RegisterButtons()
    {
        if (skillToggleButtons != null)
        {
            foreach (Button button in skillToggleButtons)
            {
                if (button == null)
                    continue;

                button.onClick.RemoveListener(ToggleSkillPanel);
                button.onClick.AddListener(ToggleSkillPanel);
            }
        }

        if (inventoryToggleButtons != null)
        {
            foreach (Button button in inventoryToggleButtons)
            {
                if (button == null)
                    continue;

                button.onClick.RemoveListener(ToggleInventoryPanel);
                button.onClick.AddListener(ToggleInventoryPanel);
            }
        }

        if (closeAllButtons != null)
        {
            foreach (Button button in closeAllButtons)
            {
                if (button == null)
                    continue;

                button.onClick.RemoveListener(CloseAllPanels);
                button.onClick.AddListener(CloseAllPanels);
            }
        }
    }

    public void ToggleSkillPanel()
    {
        if (IsActive(skillPanel))
        {
            CloseAllPanels();
            return;
        }

        OpenSkillPanel();
    }

    public void ToggleInventoryPanel()
    {
        if (IsActive(inventoryPanel))
        {
            CloseAllPanels();
            return;
        }

        OpenInventoryPanel();
    }

    public void OpenSkillPanel()
    {
        if (skillPanel != null)
            skillPanel.SetActive(true);

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    public void OpenInventoryPanel()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(true);

        if (skillPanel != null)
            skillPanel.SetActive(false);
    }

    public void CloseAllPanels()
    {
        if (skillPanel != null)
            skillPanel.SetActive(false);

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    private void ForceOnlyOnePanelActive()
    {
        bool skillIsActive = IsActive(skillPanel);
        bool inventoryIsActive = IsActive(inventoryPanel);

        if (skillIsActive && inventoryIsActive)
        {
            inventoryPanel.SetActive(false);
        }
    }

    private bool IsActive(GameObject panel)
    {
        return panel != null && panel.activeSelf;
    }
}