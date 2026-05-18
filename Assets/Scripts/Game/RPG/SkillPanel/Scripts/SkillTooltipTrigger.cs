using UnityEngine;
using UnityEngine.EventSystems;

public class SkillTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Skill skill;

    private void Awake()
    {
        if (skill == null)
            skill = GetComponent<Skill>();

        if (skill == null)
            skill = GetComponentInParent<Skill>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (skill == null)
            return;

        if (SkillTooltipUI.Instance == null)
            return;

        SkillTooltipUI.Instance.Show(skill);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    private void OnDisable()
    {
        HideTooltip();
    }

    private void HideTooltip()
    {
        if (SkillTooltipUI.Instance == null)
            return;

        SkillTooltipUI.Instance.Hide();
    }
}
