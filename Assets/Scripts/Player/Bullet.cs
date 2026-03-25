using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Collision Settings")]
    public LayerMask destroyOnLayers;  // слои, при столкновении с которыми пуля умирает

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
        // если уже умираем — игнор
        if (isDead) return;

        // проверка слоя
        if (((1 << collision.gameObject.layer) & destroyOnLayers) != 0)
        {
            isDead = true;

            // останавливаем пулю
            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            // отключаем коллайдер
            if (col != null)
                col.enabled = false;

            // запускаем анимацию смерти
            if (animator != null)
                animator.SetTrigger("Death");

            Destroy(gameObject, 0.5f);


            // если вдруг нет анимации — подстраховка
            if (animator == null)
                Destroy(gameObject, 0.5f);
        }
    }

    // ?? вызывается из Animation Event в конце Death анимации
    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}