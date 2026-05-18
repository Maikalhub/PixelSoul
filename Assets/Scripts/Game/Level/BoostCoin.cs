using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class BoostCoin : MonoBehaviour
{
    public enum BoostType
    {
        Speed,
        Stamina,
        Health,
        Attack,
        Random
    }

    public enum SpawnMode
    {
        Static,
        Respawn
    }

    [Header("Boost Settings")]
    public BoostType boostType;
    public float boostAmount = 5f;
    public float boostDuration = 5f;

    [Header("Visual Objects")]
    public GameObject speedVisual;
    public GameObject staminaVisual;
    public GameObject attackVisual;
    public GameObject healthVisual;

    [Header("Radial UI")]
    [SerializeField] private SkillResearchRadialPanelUI radialPanelUI;
    [SerializeField] private bool findRadialPanelAutomatically = true;

    [Header("Radial Icons")]
    [SerializeField] private Sprite speedIcon;
    [SerializeField] private Sprite staminaIcon;
    [SerializeField] private Sprite attackIcon;
    [SerializeField] private Sprite healthIcon;

    [Header("Radial Names")]
    [SerializeField] private string speedName = "Speed";
    [SerializeField] private string staminaName = "Stamina";
    [SerializeField] private string attackName = "Attack";
    [SerializeField] private string healthName = "Health";

    [Header("Health UI Duration")]
    [SerializeField] private float healthRadialDuration = 1f;

    [Header("Message UI")]
    [SerializeField] private CanvasMessageManager messageManager;
    [SerializeField] private bool findMessageManagerAutomatically = true;
    [SerializeField] private bool showBoostMessages = true;

    [Header("Boost Message Text")]
    [SerializeField] private string speedMessageFormat = "Speed boost: +{0} for {1:0.#}s";
    [SerializeField] private string staminaMessageFormat = "Stamina boost: +{0} for {1:0.#}s";
    [SerializeField] private string attackMessageFormat = "Attack boost: +{0} for {1:0.#}s";
    [SerializeField] private string healthMessageFormat = "Health restored: +{0}";

    [Header("Boost Message Colors")]
    [SerializeField] private Color speedMessageColor = Color.white;
    [SerializeField] private Color staminaMessageColor = Color.white;
    [SerializeField] private Color attackMessageColor = Color.white;
    [SerializeField] private Color healthMessageColor = Color.white;

    [Header("Spawn Settings")]
    public SpawnMode spawnMode = SpawnMode.Static;
    public float respawnDelay = 10f;
    public Vector2 spawnMin;
    public Vector2 spawnMax;

    private Collider2D col;
    private SpriteRenderer sr;

    private bool isCollected;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();

        col.isTrigger = true;
    }

    private void Start()
    {
        if (radialPanelUI == null && findRadialPanelAutomatically)
            radialPanelUI = FindObjectOfType<SkillResearchRadialPanelUI>();

        if (messageManager == null && findMessageManagerAutomatically)
            messageManager = FindObjectOfType<CanvasMessageManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected)
            return;

        PlayerMovement player = other.GetComponent<PlayerMovement>();

        if (player == null || player.isDead)
            return;

        isCollected = true;

        BoostType finalType = boostType;

        if (boostType == BoostType.Random)
            finalType = (BoostType)Random.Range(0, 4);

        ShowBoostRadialUI(finalType);
        ShowBoostMessage(finalType);

        player.StartCoroutine(ApplyBoost(player, finalType));

        if (spawnMode == SpawnMode.Static)
        {
            Destroy(gameObject);
        }
        else
        {
            StartCoroutine(RespawnRoutine());
        }
    }

    private void ShowBoostRadialUI(BoostType type)
    {
        if (radialPanelUI == null && findRadialPanelAutomatically)
            radialPanelUI = FindObjectOfType<SkillResearchRadialPanelUI>();

        if (radialPanelUI == null)
        {
            Debug.LogWarning("BoostCoin: radialPanelUI не найден.");
            return;
        }

        Sprite icon = GetBoostIcon(type);

        if (icon == null)
        {
            Debug.LogWarning($"BoostCoin: иконка для буста {type} не назначена.");
            return;
        }

        string boostName = GetBoostName(type);
        float duration = GetRadialDuration(type);

        radialPanelUI.ShowBoost(icon, boostName, duration);
    }

    private void ShowBoostMessage(BoostType type)
    {
        if (!showBoostMessages)
            return;

        if (messageManager == null && findMessageManagerAutomatically)
            messageManager = FindObjectOfType<CanvasMessageManager>();

        if (messageManager == null)
        {
            Debug.LogWarning("BoostCoin: CanvasMessageManager не найден.");
            return;
        }

        string message = GetBoostMessage(type);
        Color color = GetBoostMessageColor(type);

        messageManager.ShowMessage(message, color);
    }

    private string GetBoostMessage(BoostType type)
    {
        string amountText = GetBoostAmountText(type);
        float duration = GetRadialDuration(type);

        switch (type)
        {
            case BoostType.Speed:
                return string.Format(speedMessageFormat, amountText, duration);

            case BoostType.Stamina:
                return string.Format(staminaMessageFormat, amountText, duration);

            case BoostType.Attack:
                return string.Format(attackMessageFormat, amountText, duration);

            case BoostType.Health:
                return string.Format(healthMessageFormat, amountText, duration);

            default:
                return "";
        }
    }

    private string GetBoostAmountText(BoostType type)
    {
        switch (type)
        {
            case BoostType.Speed:
                return boostAmount.ToString("0.#");

            case BoostType.Stamina:
            case BoostType.Attack:
            case BoostType.Health:
                return Mathf.RoundToInt(boostAmount).ToString();

            default:
                return boostAmount.ToString("0.#");
        }
    }

    private Color GetBoostMessageColor(BoostType type)
    {
        switch (type)
        {
            case BoostType.Speed:
                return speedMessageColor;

            case BoostType.Stamina:
                return staminaMessageColor;

            case BoostType.Attack:
                return attackMessageColor;

            case BoostType.Health:
                return healthMessageColor;

            default:
                return Color.white;
        }
    }

    private float GetRadialDuration(BoostType type)
    {
        switch (type)
        {
            case BoostType.Health:
                return healthRadialDuration;

            case BoostType.Speed:
            case BoostType.Stamina:
            case BoostType.Attack:
                return boostDuration;

            default:
                return boostDuration;
        }
    }

    private Sprite GetBoostIcon(BoostType type)
    {
        switch (type)
        {
            case BoostType.Speed:
                return speedIcon;

            case BoostType.Stamina:
                return staminaIcon;

            case BoostType.Attack:
                return attackIcon;

            case BoostType.Health:
                return healthIcon;

            default:
                return null;
        }
    }

    private string GetBoostName(BoostType type)
    {
        switch (type)
        {
            case BoostType.Speed:
                return speedName;

            case BoostType.Stamina:
                return staminaName;

            case BoostType.Attack:
                return attackName;

            case BoostType.Health:
                return healthName;

            default:
                return "";
        }
    }

    private IEnumerator ApplyBoost(PlayerMovement player, BoostType type)
    {
        GameObject visual = GetVisual(type);

        if (visual != null)
            visual.SetActive(true);

        switch (type)
        {
            case BoostType.Speed:
                player.moveSpeed += boostAmount;

                yield return new WaitForSeconds(boostDuration);

                player.moveSpeed -= boostAmount;
                break;

            case BoostType.Stamina:
                int staminaAmount = Mathf.RoundToInt(boostAmount);

                player.maxStamina += staminaAmount;
                player.currentStamina += staminaAmount;

                yield return new WaitForSeconds(boostDuration);

                player.maxStamina -= staminaAmount;
                player.currentStamina = Mathf.Min(player.currentStamina, player.maxStamina);
                break;

            case BoostType.Health:
                player.currentHealth += Mathf.RoundToInt(boostAmount);
                player.currentHealth = Mathf.Min(player.currentHealth, player.maxHealth);

                yield return new WaitForSeconds(healthRadialDuration);
                break;

            case BoostType.Attack:
                int attackAmount = Mathf.RoundToInt(boostAmount);

                player.attackDamage += attackAmount;

                yield return new WaitForSeconds(boostDuration);

                player.attackDamage -= attackAmount;
                break;
        }

        if (visual != null)
            visual.SetActive(false);
    }

    private GameObject GetVisual(BoostType type)
    {
        switch (type)
        {
            case BoostType.Speed:
                return speedVisual;

            case BoostType.Stamina:
                return staminaVisual;

            case BoostType.Attack:
                return attackVisual;

            case BoostType.Health:
                return healthVisual;

            default:
                return null;
        }
    }

    private IEnumerator RespawnRoutine()
    {
        col.enabled = false;

        if (sr != null)
            sr.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        Vector2 randomPos = new Vector2(
            Random.Range(spawnMin.x, spawnMax.x),
            Random.Range(spawnMin.y, spawnMax.y)
        );

        transform.position = randomPos;

        col.enabled = true;

        if (sr != null)
            sr.enabled = true;

        isCollected = false;
    }
}