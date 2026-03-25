using UnityEngine;

public class EnemyDrop : MonoBehaviour
{
    [System.Serializable]
    public class DropItem
    {
        public GameObject prefab;
        [Range(0f, 1f)] public float dropChance = 1f; // шанс выпадения
    }

    [Header("Drop Settings")]
    public DropItem[] drops;

    [Header("Spawn Settings")]
    public Vector2 spawnOffsetMin = new Vector2(-0.5f, 0.2f);
    public Vector2 spawnOffsetMax = new Vector2(0.5f, 1f);

    [Header("Physics Settings")]
    public LayerMask collisionLayers; // с какими слоями взаимодействует
    public float minForce = 2f;       // минимальная сила броска
    public float maxForce = 5f;       // максимальная сила броска

    /// <summary>
    /// Вызывается для спавна всех предметов из списка drops
    /// </summary>
    public void Drop()
    {
        foreach (var drop in drops)
        {
            if (drop.prefab == null) continue;

            if (Random.value <= drop.dropChance)
            {
                // случайная позиция спавна относительно врага
                Vector2 offset = new Vector2(
                    Random.Range(spawnOffsetMin.x, spawnOffsetMax.x),
                    Random.Range(spawnOffsetMin.y, spawnOffsetMax.y)
                );

                GameObject obj = Instantiate(drop.prefab, (Vector2)transform.position + offset, Quaternion.identity);

                SetupPhysics(obj);
            }
        }
    }

    /// <summary>
    /// Настройка физики и слоя для предмета
    /// </summary>
    private void SetupPhysics(GameObject obj)
    {
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        Collider2D col = obj.GetComponent<Collider2D>();

        if (rb != null)
        {
            // 180° вверх: предметы летят только в верхнем полукруге
            float angle = Random.Range(0f, 180f) * Mathf.Deg2Rad;
            float forceMagnitude = Random.Range(minForce, maxForce);
            Vector2 force = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * forceMagnitude;

            rb.AddForce(force, ForceMode2D.Impulse);
        }

        if (col != null)
        {
            // Назначаем слой безопасно
            int layer = LayerMaskToLayer(collisionLayers);
            if (layer >= 0 && layer <= 31)
                obj.layer = layer;
        }
    }

    /// <summary>
    /// Преобразует LayerMask в первый выбранный слой
    /// </summary>
    private int LayerMaskToLayer(LayerMask mask)
    {
        int layerNumber = 0;
        int layer = mask.value;
        while (layer > 1)
        {
            layer = layer >> 1;
            layerNumber++;
        }
        return layerNumber;
    }
}