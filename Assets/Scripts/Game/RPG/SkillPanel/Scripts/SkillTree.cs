using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillTree : MonoBehaviour
{
    public static SkillTree skillTree;

    private void Awake()
    {
        skillTree = this;
    }

    [Header("Act Roots")]
    [Tooltip("Сюда поставь ACT 1, ACT 2, ACT 3. Именно объекты актов, внутри которых лежат Skill.")]
    [SerializeField] private GameObject[] actRoots;

    [Header("Connection")]
    [Tooltip("Prefab линии. Это должен быть UI Image с компонентом SkillConnection.")]
    [SerializeField] private GameObject connectionPrefab;

    [SerializeField] private string connectionsRootName = "Connections";

    [Header("Options")]
    [SerializeField] private bool rebuildConnectionsOnStart = true;
    [SerializeField] private bool includeInactiveSkills = true;

    private readonly List<Skill> allSkills = new List<Skill>();
    private readonly List<SkillConnection> allConnections = new List<SkillConnection>();

    private void Start()
    {
        RebuildSkillTree();
    }

    private void OnEnable()
    {
        UpdateAllSkillUI();
    }

    [ContextMenu("Rebuild Skill Tree")]
    public void RebuildSkillTree()
    {
        allSkills.Clear();
        allConnections.Clear();

        CollectAllSkills();

        if (rebuildConnectionsOnStart)
            RebuildAllConnectionsInsideActs();

        UpdateAllSkillUI();
    }

    private void CollectAllSkills()
    {
        if (actRoots == null)
            return;

        foreach (GameObject actRoot in actRoots)
        {
            if (actRoot == null)
                continue;

            Skill[] skills = actRoot.GetComponentsInChildren<Skill>(includeInactiveSkills);

            foreach (Skill skill in skills)
            {
                if (skill == null)
                    continue;

                if (skill.skillSO == null)
                    continue;

                if (!allSkills.Contains(skill))
                    allSkills.Add(skill);
            }
        }
    }

    [ContextMenu("Rebuild All Connections Inside Acts")]
    public void RebuildAllConnectionsInsideActs()
    {
        allConnections.Clear();

        if (actRoots == null)
            return;

        foreach (GameObject actRoot in actRoots)
        {
            if (actRoot == null)
                continue;

            RebuildConnectionsInsideAct(actRoot);
        }
    }

    public void RebuildConnectionsInsideAct(GameObject actRoot)
    {
        if (actRoot == null)
            return;

        if (connectionPrefab == null)
        {
            Debug.LogWarning("SkillTree: connectionPrefab не назначен.");
            return;
        }

        Canvas.ForceUpdateCanvases();

        RectTransform actRect = actRoot.GetComponent<RectTransform>();

        if (actRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(actRect);

        RectTransform connectionsRoot = GetOrCreateConnectionsRoot(actRoot);

        ClearConnectionsRoot(connectionsRoot);

        Skill[] actSkills = actRoot.GetComponentsInChildren<Skill>(true);

        Dictionary<SkillSO, Skill> skillMap = new Dictionary<SkillSO, Skill>();

        foreach (Skill skill in actSkills)
        {
            if (skill == null)
                continue;

            if (skill.skillSO == null)
                continue;

            if (!skillMap.ContainsKey(skill.skillSO))
                skillMap.Add(skill.skillSO, skill);
        }

        foreach (Skill toSkill in actSkills)
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

                if (!skillMap.TryGetValue(requiredSkillSO, out Skill fromSkill))
                    continue;

                CreateConnection(connectionsRoot, fromSkill, toSkill);
            }
        }

        Canvas.ForceUpdateCanvases();
    }

    private RectTransform GetOrCreateConnectionsRoot(GameObject actRoot)
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

    private void CreateConnection(RectTransform connectionsRoot, Skill fromSkill, Skill toSkill)
    {
        if (connectionsRoot == null)
            return;

        GameObject line = Instantiate(connectionPrefab, connectionsRoot);
        line.name = $"Connection_{fromSkill.name}_to_{toSkill.name}";
        line.SetActive(true);
        line.transform.SetAsFirstSibling();

        SkillConnection connection = line.GetComponent<SkillConnection>();

        if (connection == null)
        {
            Debug.LogWarning("SkillTree: connectionPrefab должен иметь компонент SkillConnection.");
            Destroy(line);
            return;
        }

        connection.Setup(fromSkill, toSkill);
        connection.UpdateConnection();
        connection.UpdatePosition();

        allConnections.Add(connection);
    }

    public void CheckAllSkillDependencies()
    {
        foreach (Skill skill in allSkills)
        {
            if (skill == null)
                continue;

            skill.CheckDependencies();
        }
    }

    public void UpdateAllSkillUI()
    {
        if (allSkills.Count == 0)
            CollectAllSkills();

        CheckAllSkillDependencies();

        foreach (Skill skill in allSkills)
        {
            if (skill == null)
                continue;

            skill.UpdateUI();
        }

        RefreshAllConnections();
    }

    public void RefreshAllConnections()
    {
        allConnections.RemoveAll(connection => connection == null);

        foreach (SkillConnection connection in allConnections)
        {
            if (connection == null)
                continue;

            connection.UpdateConnection();
            connection.UpdatePosition();
        }
    }

    public void RefreshConnectionsForAct(GameObject actRoot)
    {
        if (actRoot == null)
            return;

        SkillConnection[] connections = actRoot.GetComponentsInChildren<SkillConnection>(true);

        foreach (SkillConnection connection in connections)
        {
            if (connection == null)
                continue;

            connection.UpdateConnection();
            connection.UpdatePosition();
        }
    }
}