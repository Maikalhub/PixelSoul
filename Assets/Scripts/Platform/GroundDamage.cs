using UnityEngine;

public class GroundDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damageAmount = 20;
    public float knockbackForce = 15f;
    public float knockbackDuration = 0.3f;

    [Header("Knockback Direction")]
    public Vector2 customKnockbackDirection = new Vector2(1, 1); // Направление по умолчанию (вправо-вверх)
    public bool useCustomDirection = true; // Включить/выключить кастомное направление
    public bool normalizeDirection = true; // Нормализовать ли направление

    [Header("References")]
    private PlayerMovement playerMovement;
    private Rigidbody2D playerRb;

    // Проверка, был ли игрок уже отброшен (чтобы не отбрасывать повторно за одно касание)
    private bool isKnockingBack = false;
    private float knockbackTimer = 0f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            ApplyDamageAndKnockback(collision.gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ApplyDamageAndKnockback(other.gameObject);
        }
    }

    private void ApplyDamageAndKnockback(GameObject player)
    {
        // Получаем компоненты игрока
        playerMovement = player.GetComponent<PlayerMovement>();
        playerRb = player.GetComponent<Rigidbody2D>();

        if (playerMovement != null && playerRb != null && !isKnockingBack)
        {
            // Наносим урон
            playerMovement.TakeDamage(damageAmount);

            // Определяем направление отбрасывания
            Vector2 knockbackDir;

            if (useCustomDirection)
            {
                // Используем заданное направление
                knockbackDir = customKnockbackDirection;

                // Нормализуем, если нужно
                if (normalizeDirection)
                {
                    knockbackDir = knockbackDir.normalized;
                }
            }
            else
            {
                // Старое поведение - направление от объекта
                knockbackDir = (player.transform.position - transform.position).normalized;
                knockbackDir.y = Mathf.Abs(knockbackDir.y) + 1f;
            }

            // Применяем отбрасывание
            StartCoroutine(KnockbackPlayer(playerRb, knockbackDir));
        }
    }

    private System.Collections.IEnumerator KnockbackPlayer(Rigidbody2D playerRb, Vector2 direction)
    {
        isKnockingBack = true;
        knockbackTimer = 0f;

        // Сохраняем исходные настройки игрока
        PlayerMovement playerMove = playerRb.GetComponent<PlayerMovement>();
        float originalGravity = playerRb.gravityScale;

        // Отключаем гравитацию на время отбрасывания
        playerRb.gravityScale = 0f;

        // Применяем силу отбрасывания
        while (knockbackTimer < knockbackDuration)
        {
            playerRb.linearVelocity = direction * knockbackForce * (1 - knockbackTimer / knockbackDuration);
            knockbackTimer += Time.deltaTime;
            yield return null;
        }

        // Восстанавливаем настройки
        playerRb.gravityScale = originalGravity;
        playerRb.linearVelocity = Vector2.zero;
        isKnockingBack = false;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Дополнительная защита от застревания в опасной зоне
        if (collision.gameObject.CompareTag("Player") && !isKnockingBack)
        {
            ApplyDamageAndKnockback(collision.gameObject);
        }
    }

    // Метод для динамического изменения направления (можно вызывать из других скриптов)
    public void SetKnockbackDirection(Vector2 newDirection)
    {
        customKnockbackDirection = newDirection;
    }

    // Метод для установки направления в зависимости от стороны света
    public void SetDirectionFromString(string direction)
    {
        switch (direction.ToLower())
        {
            case "up":
                customKnockbackDirection = Vector2.up;
                break;
            case "down":
                customKnockbackDirection = Vector2.down;
                break;
            case "left":
                customKnockbackDirection = Vector2.left;
                break;
            case "right":
                customKnockbackDirection = Vector2.right;
                break;
            case "upleft":
                customKnockbackDirection = new Vector2(-1, 1);
                break;
            case "upright":
                customKnockbackDirection = new Vector2(1, 1);
                break;
            case "downleft":
                customKnockbackDirection = new Vector2(-1, -1);
                break;
            case "downright":
                customKnockbackDirection = new Vector2(1, -1);
                break;
        }

        if (normalizeDirection)
        {
            customKnockbackDirection = customKnockbackDirection.normalized;
        }
    }
}