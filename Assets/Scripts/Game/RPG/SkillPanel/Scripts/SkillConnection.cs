using UnityEngine;
using UnityEngine.UI;

public class SkillConnection : MonoBehaviour
{
    [Header("Connection")]
    [SerializeField] private Skill fromSkill;
    [SerializeField] private Skill toSkill;

    [Header("Visual")]
    [SerializeField] private float thickness = 5f;
    [SerializeField] private float buttonPadding = 8f;

    [Header("Colors")]
    [SerializeField] private Color lockedColor = Color.white;
    [SerializeField] private Color unlockedColor = Color.green;
    [SerializeField] private Color permanentlyLockedColor = Color.gray;

    private RectTransform rect;
    private RectTransform parentRect;
    private Image image;

    private void Awake()
    {
        Init();
    }

    private void OnEnable()
    {
        UpdateConnection();
    }

    private void Init()
    {
        if (rect == null)
            rect = GetComponent<RectTransform>();

        if (image == null)
            image = GetComponent<Image>();

        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        if (transform.parent != null)
            parentRect = transform.parent.GetComponent<RectTransform>();

        if (image != null)
            image.raycastTarget = false;
    }

    public void Setup(Skill from, Skill to)
    {
        fromSkill = from;
        toSkill = to;

        Init();

        if (fromSkill == null || toSkill == null)
        {
            Debug.LogWarning("SkillConnection: fromSkill or toSkill is null.");
            return;
        }

        transform.SetAsFirstSibling();

        UpdateConnection();
    }

    public void UpdateConnection()
    {
        Init();

        if (fromSkill == null || toSkill == null)
            return;

        if (fromSkill.skillSO == null || toSkill.skillSO == null)
            return;

        UpdateColor();
        UpdatePosition();
    }

    private void UpdateColor()
    {
        if (image == null)
            return;

        if (toSkill.skillSO.permanentlyLocked)
        {
            image.color = permanentlyLockedColor;
            return;
        }

        if (!fromSkill.skillSO.isLocked)
        {
            image.color = unlockedColor;
            return;
        }

        image.color = lockedColor;
    }

    public void UpdatePosition()
    {
        Init();

        if (fromSkill == null || toSkill == null)
            return;

        if (rect == null || parentRect == null)
            return;

        RectTransform fromRect = fromSkill.GetComponent<RectTransform>();
        RectTransform toRect = toSkill.GetComponent<RectTransform>();

        if (fromRect == null || toRect == null)
        {
            Debug.LogWarning("SkillConnection: Skill objects must have RectTransform.");
            return;
        }

        Vector2 fromCenterWorld = fromRect.TransformPoint(fromRect.rect.center);
        Vector2 toCenterWorld = toRect.TransformPoint(toRect.rect.center);

        Vector2 worldDirection = toCenterWorld - fromCenterWorld;

        if (worldDirection.magnitude <= 0.001f)
            return;

        Vector2 dirNormalized = worldDirection.normalized;

        Vector2 startWorld = GetRectEdgePoint(fromRect, dirNormalized) + dirNormalized * buttonPadding;
        Vector2 endWorld = GetRectEdgePoint(toRect, -dirNormalized) - dirNormalized * buttonPadding;

        Vector2 startLocal = parentRect.InverseTransformPoint(startWorld);
        Vector2 endLocal = parentRect.InverseTransformPoint(endWorld);

        Vector2 localDirection = endLocal - startLocal;
        float distance = localDirection.magnitude;

        if (distance <= 0.001f)
        {
            rect.sizeDelta = new Vector2(0f, thickness);
            return;
        }

        rect.anchoredPosition = (startLocal + endLocal) * 0.5f;
        rect.sizeDelta = new Vector2(distance, thickness);

        float angle = Mathf.Atan2(localDirection.y, localDirection.x) * Mathf.Rad2Deg;
        rect.localRotation = Quaternion.Euler(0f, 0f, angle);

        transform.SetAsFirstSibling();
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