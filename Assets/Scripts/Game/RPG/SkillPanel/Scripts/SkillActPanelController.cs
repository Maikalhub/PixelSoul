using UnityEngine;
using UnityEngine.UI;

public class SkillActPanelController : MonoBehaviour
{
    [System.Serializable]
    public class SkillActPanel
    {
        [Header("Act Info")]
        public string actName;

        [Tooltip("Объект внутри SkillPanel, где лежат Skill-объекты этого акта.")]
        public GameObject actRoot;

        [Tooltip("Кнопка, которая вручную показывает этот акт.")]
        public Button tabButton;

        [Tooltip("Необязательно: объект замка/оверлея, если акт ещё закрыт.")]
        public GameObject lockedOverlay;

        [Header("Level Rules")]
        [Tooltip("С какого индекса уровня этот акт можно открыть кнопкой. Индекс начинается с 0.")]
        public int unlockFromLevelIndex = 0;

        [Tooltip("С какого индекса уровня этот акт должен включаться принудительно.")]
        public int forceFromLevelIndex = 0;
    }

    [Header("References")]
    [SerializeField] private LevelManager levelManager;

    [Header("Act Panels")]
    [SerializeField] private SkillActPanel[] acts;

    [Header("Behaviour")]
    [SerializeField] private int defaultActIndex = 0;

    [Tooltip("Если true, при достижении нужного уровня панель сама переключится на нужный акт.")]
    [SerializeField] private bool forceActByCurrentLevel = true;

    [Tooltip("Если true, игрок может кнопками смотреть прошлые уже открытые акты.")]
    [SerializeField] private bool allowViewingPreviousActs = true;

    [Tooltip("Если true, кнопки закрытых актов будут неактивны.")]
    [SerializeField] private bool disableLockedButtons = true;

    private int currentVisibleActIndex = -1;
    private int lastKnownLevelIndex = int.MinValue;

    private void Awake()
    {
        if (levelManager == null)
            levelManager = LevelManager.Instance;

        RegisterButtons();
    }

    private void OnEnable()
    {
        RefreshPanels(true);
    }

    private void Update()
    {
        if (levelManager == null)
            levelManager = LevelManager.Instance;

        if (levelManager == null)
            return;

        int currentLevelIndex = levelManager.CurrentLevelIndex;

        if (currentLevelIndex != lastKnownLevelIndex)
        {
            RefreshPanels(true);
        }
    }

    private void RegisterButtons()
    {
        if (acts == null)
            return;

        for (int i = 0; i < acts.Length; i++)
        {
            int index = i;

            if (acts[i] != null && acts[i].tabButton != null)
            {
                acts[i].tabButton.onClick.RemoveAllListeners();
                acts[i].tabButton.onClick.AddListener(() => ShowActByIndex(index));
            }
        }
    }

    public void ShowActByIndex(int actIndex)
    {
        if (!IsValidActIndex(actIndex))
            return;

        if (levelManager == null)
            levelManager = LevelManager.Instance;

        int currentLevelIndex = levelManager != null ? levelManager.CurrentLevelIndex : 0;
        int forcedActIndex = GetForcedActIndex(currentLevelIndex);

        if (!IsActUnlocked(actIndex, currentLevelIndex))
        {
            Debug.Log($"Акт закрыт: {acts[actIndex].actName}");
            return;
        }

        if (!allowViewingPreviousActs && actIndex != forcedActIndex)
        {
            Debug.Log($"Сейчас доступен только акт: {acts[forcedActIndex].actName}");
            return;
        }

        ShowOnlyAct(actIndex);
        UpdateButtonsAndLocks(currentLevelIndex, forcedActIndex);
    }

    public void ShowCurrentLevelAct()
    {
        RefreshPanels(true);
    }

    private void RefreshPanels(bool allowForceSwitch)
    {
        if (acts == null || acts.Length == 0)
            return;

        if (levelManager == null)
            levelManager = LevelManager.Instance;

        int currentLevelIndex = levelManager != null ? levelManager.CurrentLevelIndex : 0;
        lastKnownLevelIndex = currentLevelIndex;

        int forcedActIndex = GetForcedActIndex(currentLevelIndex);

        if (!IsValidActIndex(forcedActIndex))
            forcedActIndex = Mathf.Clamp(defaultActIndex, 0, acts.Length - 1);

        UpdateButtonsAndLocks(currentLevelIndex, forcedActIndex);

        if (forceActByCurrentLevel && allowForceSwitch)
        {
            ShowOnlyAct(forcedActIndex);
            return;
        }

        if (!IsValidActIndex(currentVisibleActIndex))
        {
            ShowOnlyAct(forcedActIndex);
            return;
        }

        if (!IsActUnlocked(currentVisibleActIndex, currentLevelIndex))
        {
            ShowOnlyAct(forcedActIndex);
        }
    }

    private int GetForcedActIndex(int currentLevelIndex)
    {
        int result = defaultActIndex;

        if (acts == null)
            return result;

        for (int i = 0; i < acts.Length; i++)
        {
            if (acts[i] == null)
                continue;

            if (currentLevelIndex >= acts[i].forceFromLevelIndex)
            {
                result = i;
            }
        }

        return result;
    }

    private bool IsActUnlocked(int actIndex, int currentLevelIndex)
    {
        if (!IsValidActIndex(actIndex))
            return false;

        return currentLevelIndex >= acts[actIndex].unlockFromLevelIndex;
    }

    private void ShowOnlyAct(int actIndex)
    {
        if (!IsValidActIndex(actIndex))
            return;

        for (int i = 0; i < acts.Length; i++)
        {
            if (acts[i] == null || acts[i].actRoot == null)
                continue;

            acts[i].actRoot.SetActive(i == actIndex);
        }

        currentVisibleActIndex = actIndex;
    }

    private void UpdateButtonsAndLocks(int currentLevelIndex, int forcedActIndex)
    {
        if (acts == null)
            return;

        for (int i = 0; i < acts.Length; i++)
        {
            if (acts[i] == null)
                continue;

            bool unlocked = IsActUnlocked(i, currentLevelIndex);
            bool canClick = unlocked;

            if (!allowViewingPreviousActs)
                canClick = unlocked && i == forcedActIndex;

            if (acts[i].tabButton != null && disableLockedButtons)
                acts[i].tabButton.interactable = canClick;

            if (acts[i].lockedOverlay != null)
                acts[i].lockedOverlay.SetActive(!unlocked);
        }
    }

    private bool IsValidActIndex(int index)
    {
        return acts != null &&
               index >= 0 &&
               index < acts.Length &&
               acts[index] != null;
    }
}