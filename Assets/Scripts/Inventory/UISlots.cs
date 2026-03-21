using UnityEngine;
using UnityEngine.UI;
using TMPro; // 🔥 ВАЖНО

public class UISlot : MonoBehaviour
{
    public Image icon;
    public TMP_Text amountText; // 🔥 вместо Text

    public void Setup(InventorySlot slot)
    {
        icon.sprite = slot.item.icon;

        // если 1 — не показываем число
        amountText.text = slot.amount > 1 ? slot.amount.ToString() : "";
    }
}