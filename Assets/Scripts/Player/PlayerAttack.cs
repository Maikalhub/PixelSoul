using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Hit Settings")]
    [SerializeField] private float hitCooldownPerEnemy = 0.25f;
    [SerializeField] private bool applyStun = false;

    private PlayerMovement playerMovement;
    private readonly Dictionary<EnemyAI, float> nextHitTime = new Dictionary<EnemyAI, float>();

    private void Awake()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();

        if (playerMovement == null)
        {
            Debug.LogError("PlayerAttack: PlayerMovement not found on parent!");
        }
    }

    private void OnEnable()
    {
        nextHitTime.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDealDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDealDamage(other);
    }

    private void TryDealDamage(Collider2D other)
    {
        if (playerMovement == null || other == null)
            return;

        EnemyAI enemy = GetEnemyByTag(other);
        if (enemy == null)
            return;

        if (nextHitTime.TryGetValue(enemy, out float allowedTime))
        {
            if (Time.time < allowedTime)
                return;
        }

        enemy.TakeDamageMethod(playerMovement.attackDamage, applyStun);
        nextHitTime[enemy] = Time.time + hitCooldownPerEnemy;

        Debug.Log($"Player dealt {playerMovement.attackDamage} damage to {enemy.name}");
    }

    private EnemyAI GetEnemyByTag(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            EnemyAI enemyOnSelf = other.GetComponent<EnemyAI>();
            if (enemyOnSelf != null)
                return enemyOnSelf;

            EnemyAI enemyInParent = other.GetComponentInParent<EnemyAI>();
            if (enemyInParent != null)
                return enemyInParent;
        }

        Transform root = other.transform.root;
        if (root != null && root.CompareTag("Enemy"))
        {
            EnemyAI enemyOnRoot = root.GetComponent<EnemyAI>();
            if (enemyOnRoot != null)
                return enemyOnRoot;

            EnemyAI enemyInRootParent = other.GetComponentInParent<EnemyAI>();
            if (enemyInRootParent != null)
                return enemyInRootParent;
        }

        return null;
    }
}