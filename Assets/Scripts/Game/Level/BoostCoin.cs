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

    [Header("Spawn Settings")]
    public SpawnMode spawnMode = SpawnMode.Static;
    public float respawnDelay = 10f;
    public Vector2 spawnMin;
    public Vector2 spawnMax;

    private Collider2D col;
    private SpriteRenderer sr;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player == null || player.isDead) return;

        BoostType finalType = boostType;

        if (boostType == BoostType.Random)
            finalType = (BoostType)Random.Range(0, 4);

        player.StartCoroutine(ApplyBoost(player, finalType));

        if (spawnMode == SpawnMode.Static)
            Destroy(gameObject);
        else
            StartCoroutine(RespawnRoutine());
    }

    IEnumerator ApplyBoost(PlayerMovement player, BoostType type)
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
                yield return new WaitForSeconds(1f);
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

    GameObject GetVisual(BoostType type)
    {
        switch (type)
        {
            case BoostType.Speed: return speedVisual;
            case BoostType.Stamina: return staminaVisual;
            case BoostType.Attack: return attackVisual;
            case BoostType.Health: return healthVisual;
            default: return null;
        }
    }

    IEnumerator RespawnRoutine()
    {
        col.enabled = false;
        if (sr != null) sr.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        Vector2 randomPos = new Vector2(
            Random.Range(spawnMin.x, spawnMax.x),
            Random.Range(spawnMin.y, spawnMax.y)
        );

        transform.position = randomPos;

        col.enabled = true;
        if (sr != null) sr.enabled = true;
    }
}