using UnityEngine;

public class InventoryUseButton : MonoBehaviour
{
    public PlayerMovement player;

    public void UseSelectedItem()
    {
        if (Inventory.Instance == null)
        {
            Debug.LogWarning("Inventory не найден.");
            return;
        }

        if (player == null)
            player = FindFirstObjectByType<PlayerMovement>();

        if (player == null)
        {
            Debug.LogWarning("PlayerMovement не найден.");
            return;
        }

        Inventory.Instance.UseSelectedItem(player);
    }
}
