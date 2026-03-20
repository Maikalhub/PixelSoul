using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    private EnemyAI enemyAI;
    private bool hasDealtDamage;

    private void Start()
    {
        // Находим компонент EnemyAI на родительском объекте
        enemyAI = GetComponentInParent<EnemyAI>();
        if (enemyAI == null)
        {
            Debug.LogError("EnemyAttackCheck: EnemyAI not found on parent!");
        }
    }

    private void OnEnable()
    {
        // Сбрасываем флаг при каждом включении триггера
        hasDealtDamage = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (enemyAI == null || hasDealtDamage) return;

        // Проверяем, что столкнулись с игроком
        if (other.CompareTag("Player"))
        {
            // Получаем компонент PlayerMovement у игрока
            PlayerMovement player = other.GetComponent<PlayerMovement>();
            if (player != null)
            {
                // Наносим урон используя attackDamage из EnemyAI
                player.TakeDamage((int)enemyAI.attackDamage);
                hasDealtDamage = true;

                Debug.Log($"Enemy dealt {enemyAI.attackDamage} damage to player!");
            }
        }
    }
}