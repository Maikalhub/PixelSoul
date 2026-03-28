using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    public int maxSlots = 20;
    public List<InventorySlot> items = new List<InventorySlot>();
    public InventoryUI inventoryUI;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public bool AddItem(ItemData item)
    {
        if (item == null) return false;

        for (int i = 0; i < items.Count; i++)
        {
            InventorySlot slot = items[i];

            if (slot.item == item && item.stackable && slot.amount < item.maxStack)
            {
                slot.amount++;
                inventoryUI?.Refresh();
                return true;
            }
        }

        if (items.Count < maxSlots)
        {
            items.Add(new InventorySlot(item, 1));
            inventoryUI?.Refresh();
            return true;
        }

        return false;
    }

    public void RemoveItem(ItemData item)
    {
        if (item == null) return;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].item == item)
            {
                items[i].amount--;

                if (items[i].amount <= 0)
                    items.RemoveAt(i);

                inventoryUI?.Refresh();
                return;
            }
        }
    }

    public bool ContainsItem(ItemData item)
    {
        if (item == null) return false;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].item == item && items[i].amount > 0)
                return true;
        }

        return false;
    }

    public bool UseSelectedItem(PlayerMovement player)
    {
        if (player == null)
        {
            Debug.LogWarning("Inventory.UseSelectedItem: PlayerMovement не найден.");
            return false;
        }

        if (SelectionManager.Instance == null)
        {
            Debug.LogWarning("Inventory.UseSelectedItem: SelectionManager не найден.");
            return false;
        }

        ItemData selectedItem = SelectionManager.Instance.GetSingleSelectedItem();

        if (selectedItem == null)
        {
            Debug.Log("Для использования нужно выбрать ровно 1 предмет.");
            return false;
        }

        if (!selectedItem.canUse)
        {
            Debug.Log($"Предмет {selectedItem.itemName} нельзя использовать.");
            return false;
        }

        if (!ContainsItem(selectedItem))
        {
            Debug.LogWarning("Предмет не найден в инвентаре.");
            return false;
        }

        bool used = player.TryUseInventoryItem(selectedItem);
        if (!used)
            return false;

        if (selectedItem.consumeOnUse)
            RemoveItem(selectedItem);

        SelectionManager.Instance.ClearSelection();
        inventoryUI?.Refresh();

        Debug.Log($"Used item: {selectedItem.itemName}");
        return true;
    }
}
