using UnityEngine;
using UnityEngine.UI;

public class SkillConnection : MonoBehaviour
{
    [Header("Connection")]
    public Skill fromSkill;
    public Skill toSkill;

    [SerializeField] private float thickness = 5f;
    [SerializeField] private float buttonPadding = 8f;

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

        Init();

        if (fromSkill == null || toSkill == null)
        {
            Debug.LogError("SkillConnection: fromSkill or toSkill is NULL");
            return;
        }

        transform.SetAsFirstSibling();

        if (image != null)
            image.raycastTarget = false;

        UpdatePosition();
    }

    public void UpdateConnection()
    {
        Init();

        if (fromSkill == null || toSkill == null)
            return;

        transform.SetAsFirstSibling();

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

        if (fromSkill == null || toSkill == null)
            return;

        RectTransform fromRect = fromSkill.GetComponent<RectTransform>();
        RectTransform toRect = toSkill.GetComponent<RectTransform>();

        if (fromRect == null || toRect == null)
        {
            Debug.LogError("SkillConnection: Skill objects must have RectTransform.");
            return;
        }

        transform.SetAsFirstSibling();

        Vector2 fromCenter = fromRect.TransformPoint(fromRect.rect.center);
        Vector2 toCenter = toRect.TransformPoint(toRect.rect.center);

        Vector2 direction = toCenter - fromCenter;
        float fullDistance = direction.magnitude;

        if (fullDistance <= 0.001f)
            return;

        Vector2 dirNormalized = direction.normalized;

        Vector2 startPoint = GetRectEdgePoint(fromRect, dirNormalized) + dirNormalized * buttonPadding;
        Vector2 endPoint = GetRectEdgePoint(toRect, -dirNormalized) - dirNormalized * buttonPadding;

        float distance = Vector2.Distance(startPoint, endPoint);

        if (distance <= 0.001f)
        {
            rect.sizeDelta = new Vector2(0f, thickness);
            return;
        }

        rect.position = (startPoint + endPoint) * 0.5f;
        rect.sizeDelta = new Vector2(distance, thickness);

        float angle = Mathf.Atan2(endPoint.y - startPoint.y, endPoint.x - startPoint.x) * Mathf.Rad2Deg;
        rect.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private Vector2 GetRectEdgePoint(RectTransform targetRect, Vector2 worldDirection)
    {
        Vector3 localDir3 = targetRect.InverseTransformVector(worldDirection);
        Vector2 localDir = new Vector2(localDir3.x, localDir3.y).normalized;

        Vector2 halfSize = targetRect.rect.size * 0.5f;
        Vector2 localCenter = targetRect.rect.center;

        float tx = Mathf.Approximately(localDir.x, 0f)
            ? float.MaxValue
            : halfSize.x / Mathf.Abs(localDir.x);

        float ty = Mathf.Approximately(localDir.y, 0f)
            ? float.MaxValue
            : halfSize.y / Mathf.Abs(localDir.y);

        float t = Mathf.Min(tx, ty);

        Vector2 localEdgePoint = localCenter + localDir * t;
        Vector3 worldPoint = targetRect.TransformPoint(localEdgePoint);

        return worldPoint;
    }
}