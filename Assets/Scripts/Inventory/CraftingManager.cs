using System.Collections.Generic;
using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    public RecipeData[] allRecipes;

    public void TryCraft()
    {
        var selected = SelectionManager.Instance.selectedItems;

        foreach (var recipe in allRecipes)
        {
            if (CheckRecipeMatch(recipe, selected))
            {
                Debug.Log($"Crafted: {recipe.result.name}");
                Inventory.Instance.AddItem(recipe.result);
                // Удаляем использованные предметы
                foreach (var item in selected)
                    Inventory.Instance.RemoveItem(item);

                SelectionManager.Instance.ClearSelection();
                return;
            }
        }

        Debug.Log("No matching recipe.");
    }

    bool CheckRecipeMatch(RecipeData recipe, List<ItemData> selected)
    {
        if (recipe.inputs.Length != selected.Count) return false;

        if (recipe.orderMatters)
        {
            for (int i = 0; i < recipe.inputs.Length; i++)
            {
                if (recipe.inputs[i] != selected[i])
                    return false;
            }
            return true;
        }
        else
        {
            List<ItemData> temp = new List<ItemData>(selected);
            foreach (var input in recipe.inputs)
            {
                if (!temp.Remove(input))
                    return false;
            }
            return true;
        }
    }
}