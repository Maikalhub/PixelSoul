using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private bool hasDealtDamage;

    private void Start()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();

        if (playerMovement == null)
        {
            Debug.LogError("PlayerAttack: PlayerMovement not found on parent!");
        }
    }

    private void OnEnable()
    {
        hasDealtDamage = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (playerMovement == null || hasDealtDamage) return;

        EnemyAI enemy = other.GetComponentInParent<EnemyAI>();
        if (enemy != null)
        {
            enemy.TakeDamageMethod(playerMovement.attackDamage);
            hasDealtDamage = true;

            Debug.Log($"Player dealt {playerMovement.attackDamage} damage to enemy!");
        }
    }
}