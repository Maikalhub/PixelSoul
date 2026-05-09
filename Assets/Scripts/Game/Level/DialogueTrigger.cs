using System.Collections;
using TMPro;
using UnityEngine;

public class CanvasTextInteractionTrigger : MonoBehaviour
{
    [Header("Игрок")]
    [SerializeField] private string playerTag = "Player";

    [Header("Кнопки")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private KeyCode closeKey = KeyCode.E;

    [Header("Объекты подсказок")]
    [SerializeField] private GameObject interactionSprite;
    [SerializeField] private GameObject closeSprite;

    [Header("Canvas и текст")]
    [SerializeField] private GameObject canvasObject;
    [SerializeField] private TMP_Text textObject;

    [Header("Текст")]
    [TextArea(3, 10)]
    [SerializeField] private string textToShow;

    [SerializeField] private bool useTypingEffect = true;
    [SerializeField] private float typingSpeed = 0.04f;

    [Header("Настройки текста из Inspector")]
    [SerializeField] private bool overrideTextSettings = true;
    [SerializeField] private TextAlignmentOptions textAlignment = TextAlignmentOptions.Center;
    [SerializeField] private float fontSize = 36f;
    [SerializeField] private Color textColor = Color.white;

    [Header("Положение текста")]
    [SerializeField] private bool overrideTextRectTransform = true;
    [SerializeField] private Vector2 anchoredPosition = Vector2.zero;
    [SerializeField] private Vector2 sizeDelta = new Vector2(600f, 200f);

    [Header("Повторное взаимодействие")]
    [SerializeField] private bool canInteractAgain = true;
    [SerializeField] private float repeatDelay = 0f;

    [Header("Поведение")]
    [SerializeField] private bool closeOnExit = true;
    [SerializeField] private bool hideInteractionSpriteWhileTextOpen = true;
    [SerializeField] private bool allowCloseOnlyAfterTyping = true;
    [SerializeField] private bool pressCloseKeyToSkipTyping = true;

    private bool playerInsideTrigger;
    private bool textOpened;
    private bool typingFinished = true;
    private bool canUse = true;
    private bool wasUsedOnce;

    private Collider2D currentPlayer;
    private Coroutine typingCoroutine;
    private Coroutine repeatDelayCoroutine;

    private void Start()
    {
        if (interactionSprite != null)
            interactionSprite.SetActive(false);

        if (closeSprite != null)
            closeSprite.SetActive(false);

        if (canvasObject != null)
            canvasObject.SetActive(false);

        if (textObject != null)
        {
            textObject.text = "";
            ApplyTextSettings();
        }
    }

    private void Update()
    {
        if (!playerInsideTrigger) return;
        if (currentPlayer == null) return;

        if (!textOpened)
        {
            if (Input.GetKeyDown(interactKey))
            {
                TryOpenText();
            }
        }
        else
        {
            if (Input.GetKeyDown(closeKey))
            {
                if (!typingFinished && pressCloseKeyToSkipTyping)
                {
                    FinishTypingImmediately();
                    return;
                }

                if (allowCloseOnlyAfterTyping && !typingFinished)
                    return;

                CloseText();
            }
        }
    }

    private void TryOpenText()
    {
        if (!canUse) return;

        if (wasUsedOnce && !canInteractAgain)
            return;

        OpenText();
    }

    private void OpenText()
    {
        textOpened = true;
        typingFinished = false;
        wasUsedOnce = true;

        if (hideInteractionSpriteWhileTextOpen && interactionSprite != null)
            interactionSprite.SetActive(false);

        if (closeSprite != null)
            closeSprite.SetActive(false);

        if (canvasObject != null)
            canvasObject.SetActive(true);

        if (textObject != null)
        {
            ApplyTextSettings();

            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            if (useTypingEffect)
                typingCoroutine = StartCoroutine(TypeText());
            else
                ShowTextImmediately();
        }
    }

    private IEnumerator TypeText()
    {
        textObject.text = "";

        foreach (char letter in textToShow)
        {
            textObject.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        FinishTyping();
    }

    private void ShowTextImmediately()
    {
        textObject.text = textToShow;
        FinishTyping();
    }

    private void FinishTypingImmediately()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (textObject != null)
            textObject.text = textToShow;

        FinishTyping();
    }

    private void FinishTyping()
    {
        typingFinished = true;

        if (closeSprite != null)
            closeSprite.SetActive(true);
    }

    private void CloseText()
    {
        textOpened = false;
        typingFinished = true;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (textObject != null)
            textObject.text = "";

        if (canvasObject != null)
            canvasObject.SetActive(false);

        if (closeSprite != null)
            closeSprite.SetActive(false);

        if (canInteractAgain && repeatDelay > 0f)
        {
            canUse = false;

            if (repeatDelayCoroutine != null)
                StopCoroutine(repeatDelayCoroutine);

            repeatDelayCoroutine = StartCoroutine(RepeatDelayRoutine());
        }
        else
        {
            canUse = canInteractAgain;
        }

        UpdateInteractionSprite();
    }

    private IEnumerator RepeatDelayRoutine()
    {
        UpdateInteractionSprite();

        yield return new WaitForSeconds(repeatDelay);

        canUse = true;
        UpdateInteractionSprite();
    }

    private void ApplyTextSettings()
    {
        if (textObject == null) return;

        if (overrideTextSettings)
        {
            textObject.alignment = textAlignment;
            textObject.fontSize = fontSize;
            textObject.color = textColor;
        }

        if (overrideTextRectTransform)
        {
            RectTransform rectTransform = textObject.GetComponent<RectTransform>();

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = anchoredPosition;
                rectTransform.sizeDelta = sizeDelta;
            }
        }
    }

    private void UpdateInteractionSprite()
    {
        if (interactionSprite == null) return;

        bool shouldShow =
            playerInsideTrigger &&
            !textOpened &&
            canUse &&
            (canInteractAgain || !wasUsedOnce);

        interactionSprite.SetActive(shouldShow);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInsideTrigger = true;
        currentPlayer = other;

        UpdateInteractionSprite();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (currentPlayer == other)
        {
            playerInsideTrigger = false;
            currentPlayer = null;

            if (interactionSprite != null)
                interactionSprite.SetActive(false);

            if (closeOnExit)
                CloseText();
        }
    }
}