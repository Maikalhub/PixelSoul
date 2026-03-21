using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class LayoutStep
{
    public int maxItems;          // до скольки предметов действует правило
    public int columns;           // сколько в строке
    public Vector2 cellSize;      // размер слота
}

public class InventoryUI : MonoBehaviour
{
    public GameObject slotPrefab;
    public Transform slotParent;

    public LayoutStep[] layoutSteps; // 🔥 настраивается в инспекторе

    private GridLayoutGroup grid;

    void Awake()
    {
        grid = slotParent.GetComponent<GridLayoutGroup>();
    }

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
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

        // если больше всех значений
        var last = layoutSteps[layoutSteps.Length - 1];
        grid.constraintCount = last.columns;
        grid.cellSize = last.cellSize;
    }
}