using UnityEngine;
using UnityEngine.InputSystem;

public class UIInputController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private GameObject craftingPanel;
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private GameObject pausePanel;

    [Header("Settings")]
    [SerializeField] private bool closeOtherPanelsWhenOpen = true;
    [SerializeField] private bool blockGameplayWhenPanelOpen = true;

    public bool IsAnyPanelOpen =>
        IsPanelOpen(inventoryPanel) ||
        IsPanelOpen(mapPanel) ||
        IsPanelOpen(craftingPanel) ||
        IsPanelOpen(statsPanel) ||
        IsPanelOpen(pausePanel);

    private void Start()
    {
        CloseAllPanels();
    }

    public void ToggleInventory(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        TogglePanel(inventoryPanel);
    }

    public void ToggleMap(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        TogglePanel(mapPanel);
    }

    public void ToggleCrafting(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        TogglePanel(craftingPanel);
    }

    public void ToggleStats(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        TogglePanel(statsPanel);
    }

    public void TogglePause(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        TogglePanel(pausePanel);
    }

    public void CloseAll(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        CloseAllPanels();
    }

    public void CloseAllPanels()
    {
        SetPanel(inventoryPanel, false);
        SetPanel(mapPanel, false);
        SetPanel(craftingPanel, false);
        SetPanel(statsPanel, false);
        SetPanel(pausePanel, false);

        UpdateTimeScale();
    }

    private void TogglePanel(GameObject targetPanel)
    {
        if (targetPanel == null) return;

        bool willOpen = !targetPanel.activeSelf;

        if (willOpen && closeOtherPanelsWhenOpen)
            CloseAllPanels();

        SetPanel(targetPanel, willOpen);
        UpdateTimeScale();
    }

    private void SetPanel(GameObject panel, bool state)
    {
        if (panel != null)
            panel.SetActive(state);
    }

    private bool IsPanelOpen(GameObject panel)
    {
        return panel != null && panel.activeSelf;
    }

    private void UpdateTimeScale()
    {
        if (!blockGameplayWhenPanelOpen)
            return;

        Time.timeScale = IsAnyPanelOpen ? 0f : 1f;
    }
}