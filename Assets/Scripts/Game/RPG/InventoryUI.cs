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
    public GameObject slotPrefab;
    public Transform slotParent;
    public LayoutStep[] layoutSteps;

    private GridLayoutGroup grid;
    private bool initialized = false;

    void Awake()
    {
        Init();
    }

    void OnEnable()
    {
        Init();
        Refresh();
    }

    void Init()
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

        foreach (Transform child in slotParent)
            Destroy(child.gameObject);

        int count = Inventory.Instance.items.Count;

        UpdateLayout(count);

        foreach (InventorySlot slot in Inventory.Instance.items)
        {
            GameObject obj = Instantiate(slotPrefab, slotParent);
            UISlot uiSlot = obj.GetComponent<UISlot>();
            uiSlot.Setup(slot);
        }
    }

    void UpdateLayout(int itemCount)
    {
        if (layoutSteps == null || layoutSteps.Length == 0)
            return;

        foreach (var step in layoutSteps)
        {
            if (itemCount <= step.maxItems)
            {
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = step.columns;
                grid.cellSize = step.cellSize;
                return;
            }
        }

        var last = layoutSteps[layoutSteps.Length - 1];
        grid.constraintCount = last.columns;
        grid.cellSize = last.cellSize;
    }
}
