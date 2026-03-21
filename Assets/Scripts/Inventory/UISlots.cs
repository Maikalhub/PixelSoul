using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class UISlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image icon;
    public TMP_Text amountText;
    public Button button; // добавляем кнопку для клика

    private InventorySlot currentSlot;
    private float lastClickTime;
    private float doubleClickThreshold = 0.3f;

    public void Setup(InventorySlot slot)
    {
        if (slot == null)
        {
            Debug.LogWarning("UISlot.Setup: передан null slot");
            return;
        }

        currentSlot = slot;

        if (icon != null && slot.item != null)
            icon.sprite = slot.item.icon;

        if (amountText != null)
            amountText.text = slot.amount > 1 ? $"x{slot.amount}" : "";

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }

        UpdateHighlight();
    }

    void OnClick()
    {
        float timeSinceLastClick = Time.time - lastClickTime;

        if (timeSinceLastClick <= doubleClickThreshold)
        {
            // 🔹 двойной клик → снимаем выделение, если выбран
            if (SelectionManager.Instance.selectedItems.Contains(currentSlot.item))
                SelectionManager.Instance.DeselectItem(currentSlot.item);
            else
                SelectionManager.Instance.SelectItem(currentSlot.item);
        }
        else
        {
            // 🔹 одинарный клик → выделяем
            if (!SelectionManager.Instance.selectedItems.Contains(currentSlot.item))
                SelectionManager.Instance.SelectItem(currentSlot.item);
        }

        lastClickTime = Time.time;
        UpdateHighlight();
    }

    public void UpdateHighlight()
    {
        if (currentSlot != null)
        {
            bool selected = SelectionManager.Instance.selectedItems.Contains(currentSlot.item);
            if (icon != null)
                icon.color = selected ? Color.yellow : Color.white;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentSlot != null && currentSlot.item != null && TooltipUI.Instance != null)
        {
            TooltipUI.Instance.Show(currentSlot.item);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipUI.Instance != null)
            TooltipUI.Instance.Hide();
    }
}