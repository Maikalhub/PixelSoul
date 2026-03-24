using System.Collections;
using UnityEngine;

public class BossAttack : MonoBehaviour
{
    private BossAI bossAI;

    [Header("Knockback Settings")]
    public float knockbackX = 5f;
    public float knockbackY = 7f;

    private void Start()
    {
        bossAI = GetComponentInParent<BossAI>();
        if (bossAI == null)
            Debug.LogError("BossAttack: BossAI not found on parent!");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (bossAI == null) return;

        if (other.CompareTag("Player"))
        {
            PlayerMovement player = other.GetComponent<PlayerMovement>();
            if (player != null)
            {
                Rigidbody2D playerRb = player.rb;
                if (playerRb != null)
                {
                    Vector2 direction = (playerRb.position - (Vector2)transform.position).normalized;
                    Vector2 knockback = new Vector2(
                        Mathf.Sign(direction.x) * knockbackX,
                        Mathf.Sign(direction.y) * knockbackY
                    );

                    playerRb.linearVelocity = Vector2.zero;
                    playerRb.AddForce(knockback, ForceMode2D.Impulse);
                }

                player.TakeDamage(bossAI.attackDamage);
                Debug.Log($"Boss hit player for {bossAI.attackDamage} damage with knockback!");
            }
        }
    }
}