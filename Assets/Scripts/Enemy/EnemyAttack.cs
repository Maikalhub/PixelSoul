using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyAttack : MonoBehaviour
{
    private EnemyAI enemyAI;
    private Collider2D hitbox;
    private readonly HashSet<Collider2D> hitTargets = new HashSet<Collider2D>();

    private bool attackWindowActive;

    [Header("Knockback")]
    public float knockbackX = 6f;
    public float knockbackY = 3f;

    private void Awake()
    {
        hitbox = GetComponent<Collider2D>();

        if (hitbox != null)
            hitbox.enabled = false;
    }

    private void Start()
    {
        enemyAI = GetComponentInParent<EnemyAI>();

        if (enemyAI == null)
            Debug.LogError("EnemyAttack: EnemyAI not found on parent!");
    }

    public void BeginAttackWindow()
    {
        attackWindowActive = true;
        hitTargets.Clear();

        if (hitbox != null)
            hitbox.enabled = true;
    }

    public void EndAttackWindow()
    {
        attackWindowActive = false;
        hitTargets.Clear();

        if (hitbox != null)
            hitbox.enabled = false;
    }

    private void OnDisable()
    {
        EndAttackWindow();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other);
    }

    private void TryHit(Collider2D other)
    {
        if (!attackWindowActive || enemyAI == null)
            return;

        if (!other.CompareTag("Player"))
            return;

        if (hitTargets.Contains(other))
            return;

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player == null)
            return;

        hitTargets.Add(other);
        player.TakeDamage(enemyAI.attackDamage);

        if (player.rb != null)
        {
            float dir = other.transform.position.x > transform.position.x ? 1f : -1f;
            player.rb.linearVelocity = new Vector2(dir * knockbackX, knockbackY);
        }
    }
}