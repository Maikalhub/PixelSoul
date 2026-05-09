using System.Collections.Generic;
using UnityEngine;

public class SkillResearchRadialPanelUI : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject iconPrefab;

    [Header("Layout")]
    [SerializeField] private Vector2 firstIconPosition = Vector2.zero;
    [SerializeField] private float spacingX = 90f;
    [SerializeField] private bool nextIconGoesLeft = true;

    [Header("Timing")]
    [SerializeField] private float iconDuration = 2f;

    private readonly List<SkillResearchRadialIconUI> activeIcons = new List<SkillResearchRadialIconUI>();

    public void ShowSkill(SkillSO skill)
    {
        if (skill == null)
            return;

        if (iconPrefab == null)
        {
            Debug.LogWarning("SkillResearchRadialPanelUI: iconPrefab не назначен.");
            return;
        }

        GameObject createdObject = Instantiate(iconPrefab, transform);

        SkillResearchRadialIconUI icon = createdObject.GetComponentInChildren<SkillResearchRadialIconUI>(true);

        if (icon == null)
        {
            Debug.LogWarning("В prefab не найден SkillResearchRadialIconUI.");
            Destroy(createdObject);
            return;
        }

        activeIcons.Add(icon);

        icon.Setup(skill, iconDuration, this);

        RebuildLayout();
    }

    public void RemoveIcon(SkillResearchRadialIconUI icon)
    {
        if (icon == null)
            return;

        activeIcons.Remove(icon);
        RebuildLayout();
    }

    private void RebuildLayout()
    {
        for (int i = 0; i < activeIcons.Count; i++)
        {
            if (activeIcons[i] == null)
                continue;

            RectTransform rect = activeIcons[i].LayoutRectTransform;

            if (rect == null)
                continue;

            float direction = nextIconGoesLeft ? -1f : 1f;
            float x = firstIconPosition.x + spacingX * i * direction;

            rect.anchoredPosition = new Vector2(x, firstIconPosition.y);
        }
    }
}