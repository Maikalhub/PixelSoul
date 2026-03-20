using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    private PlayerMovement player;

    [Header("Knockback Settings")]
    public float knockbackX = 5f;
    public float knockbackY = 7f;

    private void Start()
    {
        player = GetComponentInParent<PlayerMovement>();

        if (player == null)
            Debug.LogError("PlayerAttack: PlayerMovement not found on parent!");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (player == null) return;

        if (other.CompareTag("Enemy"))
        {
            EnemyAI enemy = other.GetComponentInParent<EnemyAI>();

            if (enemy != null)
            {
                Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();

                if (enemyRb != null)
                {
                    Vector2 knockbackDir = new Vector2(
                        transform.position.x < enemyRb.position.x ? knockbackX : -knockbackX,
                        knockbackY
                    );

                    enemyRb.linearVelocity = Vector2.zero;
                    enemyRb.AddForce(knockbackDir, ForceMode2D.Impulse);

                    StartCoroutine(ResetEnemyCollider(enemyRb));
                }

                enemy.TakeDamageMethod(player.attackDamage);

                Debug.Log($"Player hit enemy for {player.attackDamage} damage with knockback!");
            }
        }
    }

    private IEnumerator ResetEnemyCollider(Rigidbody2D enemyRb)
    {
        Collider2D col = enemyRb.GetComponent<Collider2D>();
        if (col == null) yield break;

        col.enabled = false;
        yield return new WaitForSeconds(0.1f);
        col.enabled = true;
    }
}