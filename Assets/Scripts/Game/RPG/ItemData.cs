using UnityEngine;

public enum ItemType
{
    Bust,
    Armor,
    Consumable,
    Misc
}

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;

    [TextArea] public string description;

    public ItemType itemType;

    [Header("Stats")]
    public int damage;
    public int defense;
    public int health;

    public bool stackable = true;
    public int maxStack = 10;
}