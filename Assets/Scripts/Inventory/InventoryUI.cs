using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public GameObject slotPrefab;
    public Transform slotParent;

    void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        foreach (Transform child in slotParent)
            Destroy(child.gameObject);

        foreach (InventorySlot slot in Inventory.Instance.items)
        {
            GameObject obj = Instantiate(slotPrefab, slotParent);

            UISlot uiSlot = obj.GetComponent<UISlot>();
            uiSlot.Setup(slot);
        }
    }
}