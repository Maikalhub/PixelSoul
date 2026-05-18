using System.Collections.Generic;
using UnityEngine;

public class SkillResearchRadialPanelUI : MonoBehaviour
{
    [Header("References For Flashlight Check")]
    [SerializeField] private SkillEffectApplier skillEffectApplier;
    [SerializeField] private FlashlightController flashlight;
    [SerializeField] private bool searchReferencesAutomatically = true;

    [Header("Prefab")]
    [SerializeField] private GameObject iconPrefab;

    [Header("Layout")]
    [SerializeField] private float spacingX = 90f;
    [SerializeField] private bool nextIconGoesLeft = false;

    [Header("Position When Flashlight Is NOT Researched")]
    [Tooltip("Где будут появляться исследования/бусты, пока фонарик НЕ изучен.")]
    [SerializeField] private Vector2 positionWhenFlashlightLocked = Vector2.zero;

    [Header("Position When Flashlight IS Researched")]
    [Tooltip("Где будут появляться исследования/бусты, когда фонарик УЖЕ изучен. Например, чуть правее фонарика.")]
    [SerializeField] private Vector2 positionWhenFlashlightUnlocked = new Vector2(90f, 0f);

    [Header("Optional UI Points")]
    [Tooltip("Можно использовать не координаты, а готовые UI-точки.")]
    [SerializeField] private bool usePointPositions = false;

    [SerializeField] private RectTransform pointWhenFlashlightLocked;
    [SerializeField] private RectTransform pointWhenFlashlightUnlocked;

    [Header("Timing")]
    [SerializeField] private float iconDuration = 2f;

    private readonly List<SkillResearchRadialIconUI> activeIcons = new List<SkillResearchRadialIconUI>();

    private bool lastFlashlightUnlockedState;
    private bool hasLastFlashlightState;

    private void Awake()
    {
        ResolveReferences();

        lastFlashlightUnlockedState = IsFlashlightUnlocked();
        hasLastFlashlightState = true;
    }

    private void Update()
    {
        ResolveReferences();

        bool currentFlashlightUnlockedState = IsFlashlightUnlocked();

        if (!hasLastFlashlightState || currentFlashlightUnlockedState != lastFlashlightUnlockedState)
        {
            lastFlashlightUnlockedState = currentFlashlightUnlockedState;
            hasLastFlashlightState = true;

            RebuildLayout();
        }
    }

    public void ShowSkill(SkillSO skill)
    {
        if (skill == null)
            return;

        SkillResearchRadialIconUI icon = CreateIcon();

        if (icon == null)
            return;

        activeIcons.Add(icon);

        icon.Setup(skill, iconDuration, this);

        RebuildLayout();
    }

    public void ShowBoost(Sprite boostIcon, string boostName, float boostDuration)
    {
        if (boostIcon == null)
            return;

        SkillResearchRadialIconUI icon = CreateIcon();

        if (icon == null)
            return;

        activeIcons.Add(icon);

        icon.SetupBoost(boostIcon, boostName, boostDuration, this);

        RebuildLayout();
    }

    private SkillResearchRadialIconUI CreateIcon()
    {
        if (iconPrefab == null)
        {
            Debug.LogWarning("SkillResearchRadialPanelUI: iconPrefab не назначен.");
            return null;
        }

        GameObject createdObject = Instantiate(iconPrefab, transform);

        SkillResearchRadialIconUI icon = createdObject.GetComponentInChildren<SkillResearchRadialIconUI>(true);

        if (icon == null)
        {
            Debug.LogWarning("В prefab не найден SkillResearchRadialIconUI.");
            Destroy(createdObject);
            return null;
        }

        return icon;
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
        Vector2 startPosition = GetStartPosition();

        for (int i = 0; i < activeIcons.Count; i++)
        {
            if (activeIcons[i] == null)
                continue;

            RectTransform rect = activeIcons[i].LayoutRectTransform;

            if (rect == null)
                continue;

            float direction = nextIconGoesLeft ? -1f : 1f;
            float x = startPosition.x + spacingX * i * direction;

            rect.anchoredPosition = new Vector2(x, startPosition.y);
        }
    }

    private Vector2 GetStartPosition()
    {
        bool flashlightUnlocked = IsFlashlightUnlocked();

        if (usePointPositions)
        {
            if (flashlightUnlocked && pointWhenFlashlightUnlocked != null)
                return pointWhenFlashlightUnlocked.anchoredPosition;

            if (!flashlightUnlocked && pointWhenFlashlightLocked != null)
                return pointWhenFlashlightLocked.anchoredPosition;
        }

        return flashlightUnlocked
            ? positionWhenFlashlightUnlocked
            : positionWhenFlashlightLocked;
    }

    private bool IsFlashlightUnlocked()
    {
        if (flashlight == null)
            return false;

        return flashlight.IsUnlocked;
    }

    private void ResolveReferences()
    {
        if (!searchReferencesAutomatically)
            return;

        if (skillEffectApplier == null)
            skillEffectApplier = FindObjectOfType<SkillEffectApplier>();

        if (flashlight == null && skillEffectApplier != null)
            flashlight = skillEffectApplier.Flashlight;

        if (flashlight == null)
            flashlight = FindObjectOfType<FlashlightController>(true);
    }
}