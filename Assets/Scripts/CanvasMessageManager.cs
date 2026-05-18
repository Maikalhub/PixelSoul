using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class CanvasMessageManager : MonoBehaviour
{
    public static CanvasMessageManager Instance { get; private set; }

    private struct MessageData
    {
        public string text;
        public Color color;
        public float customVisibleDuration;

        public MessageData(string text, Color color, float customVisibleDuration)
        {
            this.text = text;
            this.color = color;
            this.customVisibleDuration = customVisibleDuration;
        }
    }

    [Header("Text Object")]
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private bool findTextInChildrenAutomatically = true;

    [Header("Default Text Settings")]
    [SerializeField] private Color defaultTextColor = Color.white;

    [Header("Typing Effect")]
    [SerializeField] private bool useTypingEffect = true;

    [Tooltip("Сколько секунд уходит на одну букву. Чем меньше значение, тем быстрее печать.")]
    [SerializeField] private float secondsPerCharacter = 0.035f;

    [Tooltip("Минимальная скорость печати при большом количестве сообщений.")]
    [SerializeField] private float minSecondsPerCharacter = 0.005f;

    [Header("Visible Time")]
    [Tooltip("Сколько сообщение висит после печати.")]
    [SerializeField] private float visibleDuration = 2f;

    [Tooltip("Минимальное время показа при большом количестве сообщений.")]
    [SerializeField] private float minVisibleDuration = 0.35f;

    [Header("Fade Effect")]
    [SerializeField] private float fadeInDuration = 0.15f;
    [SerializeField] private float fadeOutDuration = 0.4f;
    [SerializeField] private float minFadeDuration = 0.08f;

    [Header("Message Buffer")]
    [Tooltip("Максимальное количество сообщений в очереди. Если сообщений больше, старые удаляются.")]
    [SerializeField] private int maxQueueSize = 10;

    [Tooltip("Чем больше сообщений в очереди, тем быстрее они показываются. 0.5 = ускорение на 50% за каждое сообщение в очереди.")]
    [SerializeField] private float speedIncreasePerQueuedMessage = 0.5f;

    [Tooltip("Ограничение максимального ускорения.")]
    [SerializeField] private int maxMessagesForAcceleration = 8;

    [Header("Time")]
    [Tooltip("Если true, сообщения будут работать даже при Time.timeScale = 0.")]
    [SerializeField] private bool useUnscaledTime = false;

    private readonly Queue<MessageData> messageQueue = new Queue<MessageData>();

    private CanvasGroup canvasGroup;
    private Coroutine processCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("CanvasMessageManager: в сцене найдено несколько менеджеров сообщений.");
        }

        canvasGroup = GetComponent<CanvasGroup>();

        if (messageText == null && findTextInChildrenAutomatically)
            messageText = GetComponentInChildren<TMP_Text>(true);

        if (messageText != null)
        {
            messageText.text = "";
            defaultTextColor = messageText.color;
        }

        SetCanvasVisible(false);
    }

    public void ShowMessage(string text)
    {
        ShowMessage(text, defaultTextColor, -1f);
    }

    public void ShowMessage(string text, Color color)
    {
        ShowMessage(text, color, -1f);
    }

    public void ShowMessage(string text, Color color, float customVisibleDuration)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (messageText == null)
        {
            Debug.LogWarning("CanvasMessageManager: TMP_Text не назначен и не найден в дочерних объектах.");
            return;
        }

        if (maxQueueSize > 0)
        {
            while (messageQueue.Count >= maxQueueSize)
                messageQueue.Dequeue();
        }

        messageQueue.Enqueue(new MessageData(text, color, customVisibleDuration));

        if (processCoroutine == null)
            processCoroutine = StartCoroutine(ProcessMessages());
    }

    public void ClearMessages()
    {
        messageQueue.Clear();

        if (processCoroutine != null)
        {
            StopCoroutine(processCoroutine);
            processCoroutine = null;
        }

        if (messageText != null)
            messageText.text = "";

        SetCanvasVisible(false);
    }

    private IEnumerator ProcessMessages()
    {
        while (messageQueue.Count > 0)
        {
            MessageData message = messageQueue.Dequeue();
            yield return ShowMessageRoutine(message);
        }

        processCoroutine = null;

        if (messageText != null)
            messageText.text = "";

        SetCanvasVisible(false);
    }

    private IEnumerator ShowMessageRoutine(MessageData message)
    {
        SetCanvasVisible(true);

        canvasGroup.alpha = 0f;

        messageText.text = "";
        messageText.color = message.color;

        yield return FadeCanvas(0f, 1f, fadeInDuration, minFadeDuration);
        yield return TypeText(message.text);

        float duration = message.customVisibleDuration > 0f
            ? message.customVisibleDuration
            : visibleDuration;

        yield return WaitAdjusted(duration, minVisibleDuration);
        yield return FadeCanvas(canvasGroup.alpha, 0f, fadeOutDuration, minFadeDuration);

        messageText.text = "";
    }

    private IEnumerator TypeText(string text)
    {
        if (!useTypingEffect || secondsPerCharacter <= 0f)
        {
            messageText.text = text;
            yield break;
        }

        messageText.text = "";

        foreach (char letter in text)
        {
            messageText.text += letter;
            yield return WaitAdjusted(secondsPerCharacter, minSecondsPerCharacter);
        }
    }

    private IEnumerator FadeCanvas(float from, float to, float baseDuration, float minDuration)
    {
        float duration = GetAdjustedDuration(baseDuration, minDuration);

        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += GetDeltaTime();

            float t = Mathf.Clamp01(timer / duration);
            canvasGroup.alpha = Mathf.Lerp(from, to, t);

            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private IEnumerator WaitAdjusted(float baseDuration, float minDuration)
    {
        float duration = GetAdjustedDuration(baseDuration, minDuration);
        float timer = 0f;

        while (timer < duration)
        {
            timer += GetDeltaTime();
            yield return null;
        }
    }

    private float GetAdjustedDuration(float baseDuration, float minDuration)
    {
        if (baseDuration <= 0f)
            return 0f;

        int messagePressure = Mathf.Clamp(messageQueue.Count + 1, 1, maxMessagesForAcceleration);
        float multiplier = 1f + ((messagePressure - 1) * speedIncreasePerQueuedMessage);

        return Mathf.Max(minDuration, baseDuration / multiplier);
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private void SetCanvasVisible(bool visible)
    {
        if (canvasGroup == null)
            return;

        if (!visible)
            canvasGroup.alpha = 0f;

        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}