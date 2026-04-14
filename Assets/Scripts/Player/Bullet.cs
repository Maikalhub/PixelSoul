using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Damage")]
    public float damage = 10f;
    public bool applyStun = false;

    [Header("Collision Settings")]
    public LayerMask destroyOnLayers;

    private Animator animator;
    private Rigidbody2D rb;
    private Collider2D col;

    private bool isDead = false;

    private void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead) return;

        // Урон врагу по тегу Enemy
        if (collision.CompareTag("Enemy"))
        {
            EnemyAI enemy = collision.GetComponent<EnemyAI>();

            if (enemy == null)
                enemy = collision.GetComponentInParent<EnemyAI>();

            if (enemy != null)
            {
                enemy.TakeDamageMethod(damage, applyStun);
                Die();
                return;
            }
        }

        // Уничтожение о другие слои
        if (((1 << collision.gameObject.layer) & destroyOnLayers) != 0)
        {
            Die();
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

    // Вызывается из Animation Event в анимации Death
    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}