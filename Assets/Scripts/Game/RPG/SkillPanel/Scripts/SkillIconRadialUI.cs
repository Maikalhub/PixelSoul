using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SkillIconRadialUI : MonoBehaviour
{
    public static SkillIconRadialUI Instance;

    [Header("UI")]
    [SerializeField] private Image skillIcon;

    [Header("Settings")]
    [SerializeField] private float defaultDuration = 2f;
    [SerializeField] private bool hideOnStart = true;
    [SerializeField] private bool useUnscaledTime = false;

    private Coroutine currentRoutine;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        if (skillIcon != null)
        {
            skillIcon.type = Image.Type.Filled;
            skillIcon.fillMethod = Image.FillMethod.Radial360;
            skillIcon.fillOrigin = (int)Image.Origin360.Top;
            skillIcon.fillClockwise = false;
            skillIcon.fillAmount = 1f;
        }

        if (hideOnStart)
            HideInstant();
    }

    public void ShowIcon(Sprite icon, float duration = -1f)
    {
        if (skillIcon == null || icon == null)
            return;

        if (duration <= 0f)
            duration = defaultDuration;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        skillIcon.sprite = icon;
        skillIcon.color = Color.white;
        skillIcon.fillAmount = 1f;
        skillIcon.gameObject.SetActive(true);

        currentRoutine = StartCoroutine(RadialHideRoutine(duration));
    }

    private IEnumerator RadialHideRoutine(float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            time += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            float progress = Mathf.Clamp01(time / duration);
            skillIcon.fillAmount = 1f - progress;

            yield return null;
        }

        skillIcon.fillAmount = 0f;
        skillIcon.gameObject.SetActive(false);
        currentRoutine = null;
    }

    public void HideInstant()
    {
        if (skillIcon == null)
            return;

        skillIcon.fillAmount = 0f;
        skillIcon.gameObject.SetActive(false);
    }
}