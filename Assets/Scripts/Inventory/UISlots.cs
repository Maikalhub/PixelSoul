using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UISlot : MonoBehaviour
{
    public Image icon;
    public TMP_Text amountText;

    public void Setup(InventorySlot slot)
    {
        icon.sprite = slot.item.icon;

        if (slot.amount >= 1)
            amountText.text = slot.amount.ToString();
        else
            amountText.text = "";
    }
}