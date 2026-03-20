using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class BounceText : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Text")]
    [SerializeField] private TextMeshProUGUI buttonText;

    [Header("Bounce Settings")]
    [SerializeField] private float bounceHeight = 15f;
    [SerializeField] private float bounceDuration = 0.15f;

    private Vector3 startPos;
    private Coroutine bounceCoroutine;

    private void Awake()
    {
        if (buttonText == null)
            buttonText = GetComponentInChildren<TextMeshProUGUI>();

        startPos = buttonText.rectTransform.anchoredPosition;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StartBounce(startPos + Vector3.up * bounceHeight);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StartBounce(startPos);
    }

    private void StartBounce(Vector3 targetPos)
    {
        if (bounceCoroutine != null)
            StopCoroutine(bounceCoroutine);

        bounceCoroutine = StartCoroutine(BounceRoutine(targetPos));
    }

    private IEnumerator BounceRoutine(Vector3 target)
    {
        Vector3 current = buttonText.rectTransform.anchoredPosition;
        float time = 0f;

        while (time < bounceDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = time / bounceDuration;
            buttonText.rectTransform.anchoredPosition =
                Vector3.Lerp(current, target, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }

        buttonText.rectTransform.anchoredPosition = target;
    }
}
