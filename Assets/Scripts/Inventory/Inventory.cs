using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    public int maxSlots = 20;

    public List<InventorySlot> items = new List<InventorySlot>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public bool AddItem(ItemData item)
    {
        // поиск стака
        foreach (InventorySlot slot in items)
        {
            if (slot.item == item && item.stackable && slot.amount < item.maxStack)
            {
                slot.amount++;
                return true;
            }
        }

        // добавление нового
        if (items.Count < maxSlots)
        {
            items.Add(new InventorySlot(item, 1));
            return true;
        }

        return false;
    }

    public void RemoveItem(ItemData item)
    {
        foreach (InventorySlot slot in items)
        {
            if (slot.item == item)
            {
                slot.amount--;

                if (slot.amount <= 0)
                    items.Remove(slot);

                return;
            }
        }
    }
}