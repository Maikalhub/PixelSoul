using UnityEngine;

public class SkillPanel : MonoBehaviour
{
    public GameObject inventoryPanel;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            inventoryPanel.SetActive(true);
        }
    }
}
