using UnityEngine;
using System.Collections;

public class PlayerShoot : MonoBehaviour
{
    [Header("Projectile Settings")]
    public GameObject bulletPrefab;   // префаб пули
    public Transform firePoint;       // точка, откуда бросаем
    public float throwForce = 15f;
    public float cooldown = 0.5f;

    [Header("Collision Settings")]
    public LayerMask destroyLayers;   // слои, при столкновении с которыми пуля умирает

    private bool canThrow = true;

    // Метод вызывается для броска пули
    public void Throw()
    {
        if (!canThrow || bulletPrefab == null || firePoint == null)
            return;

        // Позиция мыши в мире
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        // Направление броска
        Vector2 direction = (mouseWorldPos - firePoint.position).normalized;

        // Создаём пулю
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

        // Присваиваем скорость Rigidbody2D
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = direction * throwForce;

        // Поворачиваем пулю в сторону движения
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Прокидываем слои, при которых пуля умирает
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
            bulletScript.destroyOnLayers = destroyLayers;

        // Старт кулдауна
        canThrow = false;
        StartCoroutine(ResetThrowCooldown());
    }

    private IEnumerator ResetThrowCooldown()
    {
        yield return new WaitForSeconds(cooldown);
        canThrow = true;
    }
}
