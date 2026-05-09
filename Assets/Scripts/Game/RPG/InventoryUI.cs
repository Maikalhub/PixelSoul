using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class LayoutStep
{
    public int maxItems;
    public int columns;
    public Vector2 cellSize;
}

public class InventoryUI : MonoBehaviour
{
    [Header("Slots")]
    public GameObject slotPrefab;
    public Transform slotParent;

    [Header("Layout")]
    public LayoutStep[] layoutSteps;

    private GridLayoutGroup grid;
    private bool initialized = false;

    private void Awake()
    {
        Init();
    }

    private void OnEnable()
    {
        Init();
        Refresh();
    }

    private void OnDisable()
    {
        HideTooltip();
    }

    private void Init()
    {
        if (initialized) return;

        if (slotParent == null)
        {
            Debug.LogError("slotParent НЕ назначен!");
            return;
        }

        grid = slotParent.GetComponent<GridLayoutGroup>();

        if (grid == null)
        {
            Debug.LogError("Нет GridLayoutGroup на slotParent!");
            return;
        }

        initialized = true;
    }

    public void Refresh()
    {
        if (!initialized || Inventory.Instance == null)
            return;

        HideTooltip();

        foreach (Transform child in slotParent)
        {
            Destroy(child.gameObject);
        }

        int count = Inventory.Instance.items.Count;

        UpdateLayout(count);

        foreach (InventorySlot slot in Inventory.Instance.items)
        {
            GameObject obj = Instantiate(slotPrefab, slotParent);

            UISlot uiSlot = obj.GetComponent<UISlot>();

            if (uiSlot != null)
            {
                uiSlot.Setup(slot);
            }
            else
            {
                Debug.LogError("На slotPrefab нет компонента UISlot!");
            }
        }
    }

    private void UpdateLayout(int itemCount)
    {
        if (layoutSteps == null || layoutSteps.Length == 0)
            return;

        foreach (LayoutStep step in layoutSteps)
        {
            if (itemCount <= step.maxItems)
            {
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = step.columns;
                grid.cellSize = step.cellSize;
                return;
            }
        }

        LayoutStep last = layoutSteps[layoutSteps.Length - 1];

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = last.columns;
        grid.cellSize = last.cellSize;
    }

    public void CloseInventory()
    {
        HideTooltip();
        gameObject.SetActive(false);
    }

    private void HideTooltip()
    {
        if (TooltipUI.Instance != null)
        {
            TooltipUI.Instance.Hide();
        }
    }
}