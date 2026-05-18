using System.Collections;
using TMPro;
using UnityEngine;

public class CanvasTextInteractionTrigger : MonoBehaviour
{
    [System.Serializable]
    public class DialogueMessage
    {
        [Header("Текст сообщения")]
        [TextArea(3, 10)]
        public string text;

        [Header("Кнопка продолжения для этого сообщения")]
        public KeyCode continueKey = KeyCode.E;

        [Tooltip("Спрайт/объект подсказки продолжения для этого сообщения.")]
        public GameObject continueSprite;

        [Header("Настройки текста для этого сообщения")]
        public bool overrideTextSettings = true;
        public TextAlignmentOptions textAlignment = TextAlignmentOptions.Center;
        public float fontSize = 36f;
        public Color textColor = Color.white;

        [Header("Положение текста для этого сообщения")]
        public bool overrideTextRectTransform = true;
        public Vector2 anchoredPosition = Vector2.zero;
        public Vector2 sizeDelta = new Vector2(600f, 200f);
    }

    [Header("Игрок")]
    [SerializeField] private string playerTag = "Player";

    [Header("Кнопка начала диалога")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Кнопка выхода из диалога")]
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;
    [SerializeField] private GameObject closeSprite;

    [Header("Спрайт подсказки взаимодействия")]
    [SerializeField] private GameObject interactionSprite;

    [Header("Canvas и текст")]
    [SerializeField] private GameObject canvasObject;
    [SerializeField] private TMP_Text textObject;

    [Header("Сообщения диалога")]
    [SerializeField] private DialogueMessage[] messages = new DialogueMessage[1];

    [Header("Эффект печати")]
    [SerializeField] private bool useTypingEffect = true;
    [SerializeField] private float typingSpeed = 0.04f;

    [Header("Повторное взаимодействие")]
    [SerializeField] private bool canInteractAgain = true;
    [SerializeField] private float repeatDelay = 0f;

    [Header("Поведение")]
    [SerializeField] private bool closeOnExit = true;
    [SerializeField] private bool hideInteractionSpriteWhileTextOpen = true;
    [SerializeField] private bool allowContinueOnlyAfterTyping = true;
    [SerializeField] private bool pressContinueKeyToSkipTyping = true;
    [SerializeField] private bool closeAfterLastMessage = true;

    [Header("Поведение кнопки выхода")]
    [SerializeField] private bool allowCloseOnlyAfterTyping = false;
    [SerializeField] private bool pressCloseKeyToSkipTyping = false;
    [SerializeField] private bool showCloseSpriteWhileDialogueOpen = true;
    [SerializeField] private bool showCloseSpriteOnlyAfterTyping = false;

    private bool playerInsideTrigger;
    private bool textOpened;
    private bool typingFinished = true;
    private bool canUse = true;
    private bool wasUsedOnce;

    private int currentMessageIndex = -1;

    private Collider2D currentPlayer;
    private Coroutine typingCoroutine;
    private Coroutine repeatDelayCoroutine;

    private void Start()
    {
        if (interactionSprite != null)
            interactionSprite.SetActive(false);

        if (closeSprite != null)
            closeSprite.SetActive(false);

        HideAllContinueSprites();

        if (canvasObject != null)
            canvasObject.SetActive(false);

        if (textObject != null)
            textObject.text = "";
    }

    private void Update()
    {
        if (!playerInsideTrigger) return;
        if (currentPlayer == null) return;

        if (!textOpened)
        {
            if (Input.GetKeyDown(interactKey))
                TryOpenDialogue();

            return;
        }

        HandleCloseInput();

        if (!textOpened) return;

        DialogueMessage currentMessage = GetCurrentMessage();
        if (currentMessage == null) return;

        if (Input.GetKeyDown(currentMessage.continueKey))
            HandleContinuePressed();
    }

    private void HandleCloseInput()
    {
        if (!Input.GetKeyDown(closeKey)) return;

        if (!typingFinished && pressCloseKeyToSkipTyping)
        {
            FinishTypingImmediately();
            return;
        }

        if (allowCloseOnlyAfterTyping && !typingFinished)
            return;

        CloseText();
    }

    private void TryOpenDialogue()
    {
        if (!canUse) return;

        if (wasUsedOnce && !canInteractAgain)
            return;

        if (messages == null || messages.Length == 0)
        {
            Debug.LogWarning($"{name}: массив messages пустой.");
            return;
        }

        OpenDialogue();
    }

    private void OpenDialogue()
    {
        textOpened = true;
        wasUsedOnce = true;
        currentMessageIndex = 0;

        if (hideInteractionSpriteWhileTextOpen && interactionSprite != null)
            interactionSprite.SetActive(false);

        if (canvasObject != null)
            canvasObject.SetActive(true);

        UpdateCloseSprite();

        ShowCurrentMessage();
    }

    private void ShowCurrentMessage()
    {
        DialogueMessage message = GetCurrentMessage();

        if (message == null)
        {
            CloseText();
            return;
        }

        HideAllContinueSprites();

        typingFinished = false;

        UpdateCloseSprite();

        if (textObject == null)
        {
            FinishTyping();
            return;
        }

        ApplyMessageSettings(message);

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (useTypingEffect)
            typingCoroutine = StartCoroutine(TypeText(message.text));
        else
            ShowTextImmediately(message.text);
    }

    private IEnumerator TypeText(string text)
    {
        textObject.text = "";

        if (string.IsNullOrEmpty(text))
        {
            FinishTyping();
            yield break;
        }

        foreach (char letter in text)
        {
            textObject.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        typingCoroutine = null;
        FinishTyping();
    }

    private void ShowTextImmediately(string text)
    {
        textObject.text = text;
        FinishTyping();
    }

    private void HandleContinuePressed()
    {
        if (!typingFinished && pressContinueKeyToSkipTyping)
        {
            FinishTypingImmediately();
            return;
        }

        if (allowContinueOnlyAfterTyping && !typingFinished)
            return;

        GoToNextMessageOrClose();
    }

    private void FinishTypingImmediately()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        DialogueMessage message = GetCurrentMessage();

        if (textObject != null && message != null)
            textObject.text = message.text;

        FinishTyping();
    }

    private void FinishTyping()
    {
        typingFinished = true;

        DialogueMessage message = GetCurrentMessage();

        if (message != null && message.continueSprite != null)
            message.continueSprite.SetActive(true);

        UpdateCloseSprite();
    }

    private void GoToNextMessageOrClose()
    {
        HideAllContinueSprites();

        if (messages != null && currentMessageIndex < messages.Length - 1)
        {
            currentMessageIndex++;
            ShowCurrentMessage();
            return;
        }

        if (closeAfterLastMessage)
            CloseText();
    }

    private void CloseText()
    {
        bool wasOpen = textOpened;

        textOpened = false;
        typingFinished = true;
        currentMessageIndex = -1;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (textObject != null)
            textObject.text = "";

        if (canvasObject != null)
            canvasObject.SetActive(false);

        HideAllContinueSprites();

        if (closeSprite != null)
            closeSprite.SetActive(false);

        if (!wasOpen)
        {
            UpdateInteractionSprite();
            return;
        }

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
        repeatDelayCoroutine = null;

        UpdateInteractionSprite();
    }

    private void ApplyMessageSettings(DialogueMessage message)
    {
        if (textObject == null || message == null) return;

        if (message.overrideTextSettings)
        {
            textObject.alignment = message.textAlignment;
            textObject.fontSize = message.fontSize;
            textObject.color = message.textColor;
        }

        if (message.overrideTextRectTransform)
        {
            RectTransform rectTransform = textObject.GetComponent<RectTransform>();

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = message.anchoredPosition;
                rectTransform.sizeDelta = message.sizeDelta;
            }
        }
    }

    private DialogueMessage GetCurrentMessage()
    {
        if (messages == null) return null;
        if (currentMessageIndex < 0) return null;
        if (currentMessageIndex >= messages.Length) return null;

        return messages[currentMessageIndex];
    }

    private void HideAllContinueSprites()
    {
        if (messages == null) return;

        foreach (DialogueMessage message in messages)
        {
            if (message != null && message.continueSprite != null)
                message.continueSprite.SetActive(false);
        }
    }

    private void UpdateCloseSprite()
    {
        if (closeSprite == null) return;

        bool shouldShow =
            textOpened &&
            showCloseSpriteWhileDialogueOpen &&
            (!showCloseSpriteOnlyAfterTyping || typingFinished);

        closeSprite.SetActive(shouldShow);
    }

    private void UpdateInteractionSprite()
    {
        if (interactionSprite == null) return;

        bool hasMessages = messages != null && messages.Length > 0;

        bool shouldShow =
            hasMessages &&
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

        if (currentPlayer != other) return;

        playerInsideTrigger = false;
        currentPlayer = null;

        if (interactionSprite != null)
            interactionSprite.SetActive(false);

        if (closeOnExit && textOpened)
            CloseText();
        else
        {
            HideAllContinueSprites();

            if (closeSprite != null)
                closeSprite.SetActive(false);
        }
    }

    private void OnDisable()
    {
        HideAllContinueSprites();

        if (interactionSprite != null)
            interactionSprite.SetActive(false);

        if (closeSprite != null)
            closeSprite.SetActive(false);

        if (canvasObject != null)
            canvasObject.SetActive(false);
    }
}