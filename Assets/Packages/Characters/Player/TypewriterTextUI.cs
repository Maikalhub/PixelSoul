using System.Collections;
using TMPro;
using UnityEngine;

public class TypewriterTextUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text textField;

    [Tooltip("Главный объект/панель, который будет включаться перед показом и выключаться после исчезновения.")]
    [SerializeField] private GameObject rootObject;

    [Header("Text")]
    [TextArea(3, 10)]
    [SerializeField] private string textToWrite;

    [Header("Settings")]
    [SerializeField] private float letterDelay = 0.04f;

    [Tooltip("Сколько секунд текст будет висеть после полной печати.")]
    [SerializeField] private float holdDuration = 1.2f;

    [SerializeField] private bool playOnStart = false;
    [SerializeField] private bool clearTextOnStart = true;
    [SerializeField] private bool useUnscaledTime = true;

    private Coroutine typingCoroutine;
    private bool isTyping;

    public bool IsTyping => isTyping;

    private void Awake()
    {
        if (rootObject == null)
            rootObject = gameObject;

        if (textField == null)
            textField = GetComponentInChildren<TMP_Text>(true);

        if (clearTextOnStart && textField != null)
            textField.text = "";
    }

    private void Start()
    {
        if (playOnStart)
            Play();
    }

    public void Play()
    {
        StartTypingOnly(textToWrite);
    }

    public void PlayText(string newText)
    {
        textToWrite = newText;
        StartTypingOnly(textToWrite);
    }

    public void PlayTextThenHide(string newText)
    {
        textToWrite = newText;

        if (rootObject != null)
            rootObject.SetActive(true);

        if (textField == null)
        {
            textField = GetComponentInChildren<TMP_Text>(true);
        }

        if (textField == null)
        {
            Debug.LogWarning($"{gameObject.name}: TMP_Text не назначен.");
            return;
        }

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeThenEraseAndHide());
    }

    public void Skip()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (textField != null)
            textField.text = textToWrite;

        isTyping = false;
        typingCoroutine = null;
    }

    public void Clear()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (textField != null)
            textField.text = "";

        isTyping = false;
        typingCoroutine = null;
    }

    private void StartTypingOnly(string text)
    {
        if (rootObject != null)
            rootObject.SetActive(true);

        if (textField == null)
        {
            textField = GetComponentInChildren<TMP_Text>(true);
        }

        if (textField == null)
        {
            Debug.LogWarning($"{gameObject.name}: TMP_Text не назначен.");
            return;
        }

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        textToWrite = text;
        typingCoroutine = StartCoroutine(TypeTextOnly());
    }

    private IEnumerator TypeTextOnly()
    {
        isTyping = true;
        textField.text = "";

        for (int i = 0; i < textToWrite.Length; i++)
        {
            textField.text += textToWrite[i];

            if (useUnscaledTime)
                yield return new WaitForSecondsRealtime(letterDelay);
            else
                yield return new WaitForSeconds(letterDelay);
        }

        isTyping = false;
        typingCoroutine = null;
    }

    private IEnumerator TypeThenEraseAndHide()
    {
        isTyping = true;
        textField.text = "";

        for (int i = 0; i < textToWrite.Length; i++)
        {
            textField.text += textToWrite[i];

            if (useUnscaledTime)
                yield return new WaitForSecondsRealtime(letterDelay);
            else
                yield return new WaitForSeconds(letterDelay);
        }

        if (useUnscaledTime)
            yield return new WaitForSecondsRealtime(holdDuration);
        else
            yield return new WaitForSeconds(holdDuration);

        for (int i = textToWrite.Length; i >= 0; i--)
        {
            textField.text = textToWrite.Substring(0, i);

            if (useUnscaledTime)
                yield return new WaitForSecondsRealtime(letterDelay);
            else
                yield return new WaitForSeconds(letterDelay);
        }

        textField.text = "";

        isTyping = false;
        typingCoroutine = null;

        if (rootObject != null)
            rootObject.SetActive(false);
    }
}