using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Inventory/Recipe")]
public class RecipeData : ScriptableObject
{
    [Header("Inputs")]
    public ItemData[] inputs; // Можно любое количество предметов для рецепта

    [Header("Output")]
    public ItemData result;

    [Header("Boosts")]
    public int damageBoost;
    public int defenseBoost;
    public int healthBoost;

    [Tooltip("Можно ли комбинировать предметы в любом порядке?")]
    public bool orderMatters = false;
}