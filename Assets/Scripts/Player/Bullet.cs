using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Collision Settings")]
    public LayerMask destroyOnLayers;  // слои, при столкновении с которыми пуля умирает

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Проверяем, входит ли слой объекта в destroyOnLayers
        if (((1 << collision.gameObject.layer) & destroyOnLayers) != 0)
        {
            // Можно добавить эффект попадания, звук и т.д.
            Destroy(gameObject);
        }
    }
}
