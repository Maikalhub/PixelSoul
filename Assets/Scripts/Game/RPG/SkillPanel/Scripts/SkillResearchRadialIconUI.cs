using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillResearchRadialIconUI : MonoBehaviour
{
    [Header("Child Icon")]
    [SerializeField] private Image iconImage;

    [Header("Optional Text")]
    [SerializeField] private TMP_Text skillNameText;

    [Header("Mask Settings")]
    [SerializeField] private Image.Origin360 fillOrigin = Image.Origin360.Top;
    [SerializeField] private bool clockwise = true;

    [Header("Time")]
    [SerializeField] private bool useUnscaledTime = true;

    private SkillResearchRadialPanelUI owner;

    private Mask parentMask;
    private Image parentMaskImage;

    private float duration = 2f;
    private float timer;
    private bool isFinished;

    public RectTransform LayoutRectTransform
    {
        get
        {
            if (parentMask != null)
                return parentMask.transform as RectTransform;

            Mask mask = GetComponentInParent<Mask>();

            if (mask != null)
                return mask.transform as RectTransform;

            return transform as RectTransform;
        }
    }

    private void Awake()
    {
        CacheReferences();
        SetupParentMask();
    }

    public void Setup(SkillSO skill, float duration, SkillResearchRadialPanelUI owner)
    {
        this.owner = owner;
        this.duration = Mathf.Max(0.01f, duration);

        timer = 0f;
        isFinished = false;

        CacheReferences();
        SetupParentMask();

        if (skill != null && iconImage != null)
        {
            iconImage.sprite = skill.skillIcon;
            iconImage.preserveAspect = true;
        }

        if (skill != null && skillNameText != null)
        {
            skillNameText.text = !string.IsNullOrWhiteSpace(skill.skillName)
                ? skill.skillName
                : skill.name;
        }

        if (parentMaskImage != null)
            parentMaskImage.fillAmount = 1f;
    }

    private void Update()
    {
        if (isFinished)
            return;

        if (parentMaskImage == null)
        {
            CacheReferences();
            SetupParentMask();

            if (parentMaskImage == null)
                return;
        }

        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        timer += deltaTime;

        float progress = Mathf.Clamp01(timer / duration);

        parentMaskImage.fillAmount = 1f - progress;

        if (progress >= 1f)
            Finish();
    }

    private void CacheReferences()
    {
        if (iconImage == null)
            iconImage = GetComponent<Image>();

        if (parentMask == null)
            parentMask = GetComponentInParent<Mask>();

        if (parentMask != null && parentMaskImage == null)
            parentMaskImage = parentMask.GetComponent<Image>();
    }

    private void SetupParentMask()
    {
        if (parentMask == null)
        {
            Debug.LogWarning($"{gameObject.name}: родительская Mask не найдена.");
            return;
        }

        if (parentMaskImage == null)
        {
            Debug.LogWarning($"{parentMask.gameObject.name}: на объекте Mask нет Image.");
            return;
        }

        parentMask.showMaskGraphic = false;

        parentMaskImage.type = Image.Type.Filled;
        parentMaskImage.fillMethod = Image.FillMethod.Radial360;
        parentMaskImage.fillOrigin = (int)fillOrigin;
        parentMaskImage.fillClockwise = clockwise;
        parentMaskImage.fillAmount = 1f;
    }

    private void Finish()
    {
        if (isFinished)
            return;

        isFinished = true;

        if (owner != null)
            owner.RemoveIcon(this);

        if (parentMask != null)
            Destroy(parentMask.gameObject);
        else
            Destroy(gameObject);
    }
}