using System.Collections.Generic;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance;
    public List<ItemData> selectedItems = new List<ItemData>();
    public int maxSelection = 3;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void SelectItem(ItemData item)
    {
        if (!selectedItems.Contains(item) && selectedItems.Count < maxSelection)
            selectedItems.Add(item);
    }

    public void DeselectItem(ItemData item)
    {
        if (selectedItems.Contains(item))
            selectedItems.Remove(item);
    }

    public void ClearSelection()
    {
        selectedItems.Clear();
    }
}