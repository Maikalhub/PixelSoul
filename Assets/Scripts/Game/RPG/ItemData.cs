using UnityEngine;

public enum ItemType
{
    Bust,
    Armor,
    Consumable,
    Misc
}

public enum ItemUseEffectType
{
    None,
    Damage,
    Defense,
    MaxHealth,
    RestoreHealth,
    MaxStamina,
    RestoreStamina,
    MoveSpeed,
    JumpPower,
    SprintMultiplier,
    DashSpeed
}

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    [Header("Base Info")]
    public string itemName;
    public Sprite icon;
    [TextArea] public string description;
    public ItemType itemType;

    [Header("Stats (для тултипа / общих данных)")]
    public int damage;
    public int defense;
    public int health;

    [Header("Use Settings")]
    public bool canUse = false;
    public bool consumeOnUse = true;
    public ItemUseEffectType useEffectType = ItemUseEffectType.None;
    public float useValue = 0f;

    [Header("Buff Settings")]
    public bool isTemporaryBuff = false;
    public float buffDuration = 0f;

    [Header("Stack Settings")]
    public bool stackable = true;
    public int maxStack = 10;
}
