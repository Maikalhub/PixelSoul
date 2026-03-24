using UnityEngine;

public class SkillTreeUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject skillTreePanel; // Панель с деревом навыков
    public KeyCode toggleKey = KeyCode.Tab; // Клавиша для открытия/закрытия

    private bool isOpen = false;

    private void Start()
    {
        if (skillTreePanel != null)
            skillTreePanel.SetActive(isOpen); // Изначально закрыта
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            TogglePanel();
        }
    }

    public void TogglePanel()
    {
        isOpen = !isOpen;
        if (skillTreePanel != null)
            skillTreePanel.SetActive(isOpen);
    }
}