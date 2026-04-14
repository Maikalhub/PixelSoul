using System.Collections.Generic;
using UnityEngine;

public class SkillTree : MonoBehaviour
{
    public static SkillTree skillTree;
    private void Awake() => skillTree = this;

    [Header("References")]
    public GameObject SkillHolder;
    public GameObject connectionPrefab;

    private List<Skill> SkillList = new List<Skill>();
    private List<SkillConnection> connections = new List<SkillConnection>();

    private Dictionary<SkillSO, Skill> skillMap = new Dictionary<SkillSO, Skill>();

    private void Start()
    {
        foreach (Skill skill in SkillHolder.GetComponentsInChildren<Skill>())
        {
            SkillList.Add(skill);
            skill.skillSO.ResetSkill();

            if (!skillMap.ContainsKey(skill.skillSO))
                skillMap.Add(skill.skillSO, skill);
        }

        GenerateConnections();
        UpdateAllSkillUI();
    }

    void GenerateConnections()
    {
        foreach (Skill skill in SkillList)
        {
            if (skill.skillSO.requiredSkills == null) continue;

            foreach (SkillSO required in skill.skillSO.requiredSkills)
            {
                if (!skillMap.ContainsKey(required)) continue;

                Skill from = skillMap[required];
                Skill to = skill;

                GameObject line = Instantiate(connectionPrefab, SkillHolder.transform);
                line.transform.SetAsFirstSibling();

                SkillConnection connection = line.GetComponent<SkillConnection>();
                connection.Setup(from, to);

                connections.Add(connection);
            }
        }
    }

    public void CheckAllSkillDependencies()
    {
        foreach (Skill skill in SkillList)
        {
            skill.CheckDependencies();
        }
    }

    public void UpdateAllSkillUI()
    {
        CheckAllSkillDependencies();

        foreach (Skill skill in SkillList)
        {
            skill.UpdateUI();
        }

        foreach (SkillConnection connection in connections)
        {
            connection.UpdateConnection();
            connection.UpdatePosition();
        }
    }
}