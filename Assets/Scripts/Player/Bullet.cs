using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Damage")]
    public float damage = 10f;
    public bool applyStun = false;

    [Header("Collision Settings")]
    public LayerMask destroyOnLayers;

    [Header("Splash Damage")]
    [SerializeField] private bool splashEnabled = false;
    [SerializeField] private float splashDamage = 0f;
    [SerializeField] private float splashRadius = 0f;

    private Animator animator;
    private Rigidbody2D rb;
    private Collider2D col;

    private bool isDead = false;
    private bool splashTriggered = false;

    private void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    public void SetupSkillEffects(bool enableSplash, float radiusDamage, float radius)
    {
        splashEnabled = enableSplash;
        splashDamage = radiusDamage;
        splashRadius = radius;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead)
            return;

        if (collision.CompareTag("Enemy"))
        {
            EnemyAI enemy = collision.GetComponent<EnemyAI>();

            if (enemy == null)
                enemy = collision.GetComponentInParent<EnemyAI>();

            if (enemy != null)
            {
                enemy.TakeDamageMethod(damage, applyStun);
                ApplySplashDamage(enemy);
                Die();
                return;
            }
        }

        if (((1 << collision.gameObject.layer) & destroyOnLayers) != 0)
        {
            ApplySplashDamage(null);
            Die();
        }
    }

    private void ApplySplashDamage(EnemyAI mainTarget)
    {
        if (splashTriggered || !splashEnabled || splashRadius <= 0f || splashDamage <= 0f)
            return;

        splashTriggered = true;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, splashRadius);
        HashSet<EnemyAI> damagedEnemies = new HashSet<EnemyAI>();

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            EnemyAI enemy = hit.GetComponent<EnemyAI>();

            if (enemy == null)
                enemy = hit.GetComponentInParent<EnemyAI>();

            if (enemy == null)
                continue;

            if (enemy == mainTarget)
                continue;

            if (damagedEnemies.Contains(enemy))
                continue;

            damagedEnemies.Add(enemy);
            enemy.TakeDamageMethod(splashDamage, false);
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (col != null)
            col.enabled = false;

        if (animator != null)
        {
            animator.SetTrigger("Death");
            Destroy(gameObject, 0.5f);
        }
        else
        {
            Destroy(gameObject, 0.5f);
        }
    }

    public void DestroySelf()
    {
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (!splashEnabled || splashRadius <= 0f)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, splashRadius);
    }
}