using System.Collections;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    private EnemyAI enemyAI;

    [Header("Knockback Settings")]
    public float knockbackX = 5f;
    public float knockbackY = 7f;

    private void Start()
    {
        enemyAI = GetComponentInParent<EnemyAI>();
        if (enemyAI == null)
            Debug.LogError("EnemyAttack: EnemyAI not found on parent!");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (enemyAI == null) return;

        if (other.CompareTag("Player"))
        {
            PlayerMovement player = other.GetComponent<PlayerMovement>();
            if (player != null)
            {
                Rigidbody2D playerRb = player.rb;
                if (playerRb != null)
                {
                    // Вектор от врага к игроку
                    Vector2 direction = (playerRb.position - (Vector2)transform.position).normalized;

                    // Knockback с отдельной силой по X и Y
                    Vector2 knockback = new Vector2(
                        Mathf.Sign(direction.x) * knockbackX,
                        Mathf.Sign(direction.y) * knockbackY
                    );

                    // Обнуляем скорость и добавляем силу
                    playerRb.linearVelocity = Vector2.zero;
                    playerRb.AddForce(knockback, ForceMode2D.Impulse);

                    StartCoroutine(ResetPlayerCollider(playerRb));
                }

                player.TakeDamage(enemyAI.attackDamage);
                Debug.Log($"Enemy hit player for {enemyAI.attackDamage} damage with knockback!");
            }
        }
    }

    private IEnumerator ResetPlayerCollider(Rigidbody2D playerRb)
    {
        Collider2D col = playerRb.GetComponent<Collider2D>();
        if (col == null) yield break;

        col.enabled = false;
        yield return new WaitForSeconds(0.1f);
        col.enabled = true;
    }
}