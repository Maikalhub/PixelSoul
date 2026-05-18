using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillActPanelNavigator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LevelManager levelManager;

    [Header("Act Roots")]
    [Tooltip("Сюда по порядку ставь объекты актов: ACT 1, ACT 2, ACT 3.")]
    [SerializeField] private GameObject[] actRoots;

    [Header("Navigation Buttons")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;

    [Header("Unlock Rules")]
    [Tooltip("Индекс уровня, с которого открывается каждый акт. Например: 0, 10, 20.")]
    [SerializeField] private int[] unlockFromLevelIndex;

    [Header("Connection Prefab")]
    [Tooltip("Prefab линии. Это должен быть UI Image с компонентом SkillConnection.")]
    [SerializeField] private SkillConnection connectionPrefab;

    [Header("Connection Root")]
    [SerializeField] private string connectionsRootName = "Connections";

    [Header("Behaviour")]
    [SerializeField] private bool forceNewestActOnLevelChange = true;
    [SerializeField] private bool rebuildConnectionsOnActChange = true;

    private int currentVisibleActIndex;
    private int lastKnownLevelIndex = -999;
    private Coroutine rebuildRoutine;

    private void Awake()
    {
        if (levelManager == null)
            levelManager = LevelManager.Instance;

        if (previousButton != null)
        {
            previousButton.onClick.RemoveListener(ShowPreviousAct);
            previousButton.onClick.AddListener(ShowPreviousAct);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(ShowNextAct);
            nextButton.onClick.AddListener(ShowNextAct);
        }
    }

    private void Start()
    {
        RefreshFromLevel(true);
    }

    private void OnEnable()
    {
        RefreshFromLevel(true);
    }

    private void Update()
    {
        if (levelManager == null)
            levelManager = LevelManager.Instance;

        if (levelManager == null)
            return;

        int currentLevelIndex = levelManager.CurrentLevelIndex;

        if (currentLevelIndex != lastKnownLevelIndex)
            RefreshFromLevel(forceNewestActOnLevelChange);
    }

    private void RefreshFromLevel(bool forceNewestAct)
    {
        if (!HasValidActs())
            return;

        if (levelManager == null)
            levelManager = LevelManager.Instance;

        int currentLevelIndex = levelManager != null ? levelManager.CurrentLevelIndex : 0;
        lastKnownLevelIndex = currentLevelIndex;

        int newestUnlockedActIndex = GetNewestUnlockedActIndex(currentLevelIndex);

        if (forceNewestAct)
        {
            ShowAct(newestUnlockedActIndex);
            return;
        }

        if (!IsValidActIndex(currentVisibleActIndex))
        {
            ShowAct(newestUnlockedActIndex);
            return;
        }

        if (!IsActUnlocked(currentVisibleActIndex, currentLevelIndex))
        {
            ShowAct(newestUnlockedActIndex);
            return;
        }

        UpdateNavigationButtons(currentLevelIndex);
        RebuildConnectionsInsideCurrentAct();
    }

    public void ShowPreviousAct()
    {
        int targetIndex = currentVisibleActIndex - 1;

        if (!IsValidActIndex(targetIndex))
            return;

        int currentLevelIndex = levelManager != null ? levelManager.CurrentLevelIndex : 0;

        if (!IsActUnlocked(targetIndex, currentLevelIndex))
            return;

        ShowAct(targetIndex);
    }

    public void ShowNextAct()
    {
        int targetIndex = currentVisibleActIndex + 1;

        if (!IsValidActIndex(targetIndex))
            return;

        int currentLevelIndex = levelManager != null ? levelManager.CurrentLevelIndex : 0;

        if (!IsActUnlocked(targetIndex, currentLevelIndex))
            return;

        ShowAct(targetIndex);
    }

    private void ShowAct(int actIndex)
    {
        if (!IsValidActIndex(actIndex))
            return;

        for (int i = 0; i < actRoots.Length; i++)
        {
            if (actRoots[i] != null)
                actRoots[i].SetActive(i == actIndex);
        }

        currentVisibleActIndex = actIndex;

        int currentLevelIndex = levelManager != null ? levelManager.CurrentLevelIndex : 0;
        UpdateNavigationButtons(currentLevelIndex);

        RebuildConnectionsInsideCurrentAct();
    }

    private void RebuildConnectionsInsideCurrentAct()
    {
        if (!rebuildConnectionsOnActChange)
            return;

        if (connectionPrefab == null)
        {
            Debug.LogWarning("SkillActPanelNavigator: connectionPrefab не назначен.");
            return;
        }

        if (!IsValidActIndex(currentVisibleActIndex))
            return;

        if (rebuildRoutine != null)
            StopCoroutine(rebuildRoutine);

        rebuildRoutine = StartCoroutine(RebuildConnectionsRoutine());
    }

    private IEnumerator RebuildConnectionsRoutine()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        RebuildConnectionsNow();

        rebuildRoutine = null;
    }

    private void RebuildConnectionsNow()
    {
        if (!IsValidActIndex(currentVisibleActIndex))
            return;

        GameObject actRoot = actRoots[currentVisibleActIndex];

        if (actRoot == null)
            return;

        Canvas.ForceUpdateCanvases();

        RectTransform actRect = actRoot.GetComponent<RectTransform>();

        if (actRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(actRect);

        RectTransform connectionsRoot = GetOrCreateConnectionsRootInsideAct(actRoot);

        ClearConnectionsRoot(connectionsRoot);

        SpawnConnectionsInsideAct(actRoot, connectionsRoot);

        Canvas.ForceUpdateCanvases();
    }

    private RectTransform GetOrCreateConnectionsRootInsideAct(GameObject actRoot)
    {
        Transform existing = actRoot.transform.Find(connectionsRootName);

        if (existing != null)
        {
            RectTransform existingRect = existing.GetComponent<RectTransform>();

            if (existingRect != null)
            {
                existingRect.SetAsFirstSibling();
                return existingRect;
            }
        }

        GameObject connectionsObject = new GameObject(connectionsRootName, typeof(RectTransform));
        connectionsObject.transform.SetParent(actRoot.transform, false);

        RectTransform rect = connectionsObject.GetComponent<RectTransform>();

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);

        rect.SetAsFirstSibling();

        return rect;
    }

    private void ClearConnectionsRoot(RectTransform connectionsRoot)
    {
        if (connectionsRoot == null)
            return;

        for (int i = connectionsRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(connectionsRoot.GetChild(i).gameObject);
        }
    }

    private void SpawnConnectionsInsideAct(GameObject actRoot, RectTransform connectionsRoot)
    {
        if (actRoot == null || connectionsRoot == null)
            return;

        Skill[] skills = actRoot.GetComponentsInChildren<Skill>(true);

        Dictionary<SkillSO, Skill> skillsBySO = new Dictionary<SkillSO, Skill>();

        foreach (Skill skill in skills)
        {
            if (skill == null)
                continue;

            if (skill.skillSO == null)
                continue;

            if (!skillsBySO.ContainsKey(skill.skillSO))
                skillsBySO.Add(skill.skillSO, skill);
        }

        foreach (Skill toSkill in skills)
        {
            if (toSkill == null)
                continue;

            if (toSkill.skillSO == null)
                continue;

            SkillSO toSkillSO = toSkill.skillSO;

            if (toSkillSO.requiredSkills == null)
                continue;

            foreach (SkillSO requiredSkillSO in toSkillSO.requiredSkills)
            {
                if (requiredSkillSO == null)
                    continue;

                if (!skillsBySO.TryGetValue(requiredSkillSO, out Skill fromSkill))
                    continue;

                SkillConnection connection = Instantiate(connectionPrefab, connectionsRoot);
                connection.gameObject.SetActive(true);
                connection.name = $"Connection_{fromSkill.name}_to_{toSkill.name}";
                connection.transform.SetAsFirstSibling();
                connection.Setup(fromSkill, toSkill);
                connection.UpdateConnection();
            }
        }
    }

    private void UpdateNavigationButtons(int currentLevelIndex)
    {
        bool hasPrevious =
            IsValidActIndex(currentVisibleActIndex - 1) &&
            IsActUnlocked(currentVisibleActIndex - 1, currentLevelIndex);

        bool hasNext =
            IsValidActIndex(currentVisibleActIndex + 1) &&
            IsActUnlocked(currentVisibleActIndex + 1, currentLevelIndex);

        if (previousButton != null)
            previousButton.gameObject.SetActive(hasPrevious);

        if (nextButton != null)
            nextButton.gameObject.SetActive(hasNext);
    }

    private int GetNewestUnlockedActIndex(int currentLevelIndex)
    {
        int result = 0;

        for (int i = 0; i < actRoots.Length; i++)
        {
            if (IsActUnlocked(i, currentLevelIndex))
                result = i;
        }

        return result;
    }

    private bool IsActUnlocked(int actIndex, int currentLevelIndex)
    {
        if (!IsValidActIndex(actIndex))
            return false;

        if (unlockFromLevelIndex == null || unlockFromLevelIndex.Length == 0)
            return actIndex == 0;

        if (actIndex >= unlockFromLevelIndex.Length)
            return actIndex == 0;

        return currentLevelIndex >= unlockFromLevelIndex[actIndex];
    }

    private bool IsValidActIndex(int actIndex)
    {
        return actRoots != null &&
               actIndex >= 0 &&
               actIndex < actRoots.Length &&
               actRoots[actIndex] != null;
    }

    private bool HasValidActs()
    {
        return actRoots != null && actRoots.Length > 0;
    }

    [ContextMenu("Rebuild Connections Inside Current Act")]
    public void RebuildConnectionsInsideCurrentActContext()
    {
        RebuildConnectionsInsideCurrentAct();
    }
}
