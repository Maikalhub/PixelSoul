using UnityEngine;
using UnityEngine.UI;

public class SkillConnection : MonoBehaviour
{
    public Skill fromSkill;
    public Skill toSkill;

    private RectTransform rect;
    private Image image;

    private void Init()
    {
        if (rect == null)
            rect = GetComponent<RectTransform>();

        if (image == null)
            image = GetComponent<Image>();
    }

    public void Setup(Skill from, Skill to)
    {
        fromSkill = from;
        toSkill = to;

        Init(); // 🔥 ВАЖНО

        if (fromSkill == null || toSkill == null)
        {
            Debug.LogError("SkillConnection: fromSkill or toSkill is NULL");
            return;
        }

        UpdatePosition();
    }

    public void UpdateConnection()
    {
        Init();

        if (fromSkill == null || toSkill == null) return;

        if (toSkill.skillSO.permanentlyLocked)
            image.color = Color.gray;
        else if (!fromSkill.skillSO.isLocked)
            image.color = Color.green;
        else
            image.color = Color.white;
    }

    public void UpdatePosition()
    {
        Init();

        if (fromSkill == null || toSkill == null) return;

        Vector3 start = fromSkill.transform.position;
        Vector3 end = toSkill.transform.position;

        Vector3 dir = end - start;
        float distance = dir.magnitude;

        rect.position = start + dir / 2f;
        rect.sizeDelta = new Vector2(distance, 5f);

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rect.rotation = Quaternion.Euler(0, 0, angle);
    }
}