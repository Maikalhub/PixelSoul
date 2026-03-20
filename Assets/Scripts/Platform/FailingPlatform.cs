using UnityEngine;
using System.Collections;

public class FallingPlatform : MonoBehaviour
{
    [Header("Режим падения")]
    public FallMode fallMode = FallMode.Immediate;
    public enum FallMode
    {
        Immediate,           // Падает сразу при касании
        Delayed,             // Падает через задержку после касания
        StepCount,           // Падает после определенного количества шагов
        ShakeThenFall        // Сначала трясется, потом падает
    }

    [Header("Настройки падения")]
    public float fallDelay = 0.5f;              // Задержка перед падением
    public float fallSpeed = 5f;                 // Скорость падения
    public float destroyDelay = 2f;               // Задержка перед уничтожением после падения
    public int maxSteps = 3;                       // Количество шагов до падения

    [Header("Эффекты тряски")]
    public bool enableShake = true;                // Включить тряску перед падением
    public float shakeAmount = 0.1f;               // Интенсивность тряски
    public float shakeSpeed = 10f;                  // Скорость тряски

    [Header("Визуальные эффекты")]
    public GameObject fallEffect;                   // Эффект при падении
    public GameObject disappearEffect;              // Эффект при исчезновении
    public AudioClip shakeSound;                     // Звук тряски
    public AudioClip fallSound;                      // Звук падения
    public AudioClip disappearSound;                 // Звук исчезновения
    public AudioClip reappearSound;                 // Звук исчезновения

    [Header("Визуальные индикаторы")]
    public SpriteRenderer spriteRenderer;
    public Color warningColor = Color.red;          // Цвет предупреждения
    public Color criticalColor = Color.magenta;     // Цвет критического состояния
    public Animator animator;
    public string fallTrigger = "Fall";

    [Header("Возрождение")]
    public bool respawnAfterFall = false;           // Возрождаться ли после падения
    public float respawnDelay = 3f;                  // Задержка перед возрождением
    public GameObject respawnEffect;                 // Эффект при возрождении

    // Приватные переменные
    private Rigidbody2D rb;
    private Collider2D platformCollider;
    private Vector3 startPosition;
    private bool isFalling = false;
    private int stepCount = 0;
    private bool isPlayerOnPlatform = false;
    private Vector3 originalScale;
    private Color originalColor;
    private float shakeTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        platformCollider = GetComponent<Collider2D>();
        startPosition = transform.position;
        originalScale = transform.localScale;

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // Если нет Rigidbody2D - добавляем
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic; // Начинаем как кинематическая
        }

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (isFalling) return;

        // Эффект тряски если нужно
        if (enableShake && isPlayerOnPlatform && fallMode == FallMode.ShakeThenFall)
        {
            ShakePlatform();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !isFalling)
        {
            isPlayerOnPlatform = true;

            switch (fallMode)
            {
                case FallMode.Immediate:
                    StartFalling();
                    break;

                case FallMode.Delayed:
                    StartCoroutine(FallAfterDelay());
                    break;

                case FallMode.StepCount:
                    stepCount++;
                    UpdatePlatformColor();

                    if (stepCount >= maxSteps)
                    {
                        StartFalling();
                    }
                    break;

                case FallMode.ShakeThenFall:
                    StartCoroutine(ShakeAndFall());
                    break;
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerOnPlatform = false;
            shakeTimer = 0f;

            // Возвращаем нормальную позицию после тряски
            if (fallMode == FallMode.ShakeThenFall && !isFalling)
            {
                transform.position = startPosition;
            }
        }
    }

    void ShakePlatform()
    {
        if (shakeTimer < fallDelay)
        {
            shakeTimer += Time.deltaTime;

            // Эффект тряски
            float shakeX = Mathf.Sin(Time.time * shakeSpeed) * shakeAmount;
            float shakeY = Mathf.Cos(Time.time * shakeSpeed) * shakeAmount * 0.5f;
            transform.position = startPosition + new Vector3(shakeX, shakeY, 0);

            // Меняем цвет в зависимости от времени тряски
            if (spriteRenderer != null)
            {
                float t = shakeTimer / fallDelay;
                spriteRenderer.color = Color.Lerp(originalColor, warningColor, t);
            }

            // Звук тряски
            if (shakeSound != null && GetComponent<AudioSource>() != null && !GetComponent<AudioSource>().isPlaying)
            {
                GetComponent<AudioSource>().PlayOneShot(shakeSound);
            }
        }
    }

    IEnumerator ShakeAndFall()
    {
        yield return new WaitForSeconds(fallDelay);
        StartFalling();
    }

    IEnumerator FallAfterDelay()
    {
        yield return new WaitForSeconds(fallDelay);
        StartFalling();
    }

    void StartFalling()
    {
        if (isFalling) return;

        isFalling = true;

        // Меняем физику на динамическую для падения
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
            rb.linearVelocity = Vector2.down * fallSpeed;
        }

        // Эффект падения
        if (fallEffect != null)
        {
            Instantiate(fallEffect, transform.position, Quaternion.identity);
        }

        // Звук падения
        if (fallSound != null && GetComponent<AudioSource>() != null)
        {
            GetComponent<AudioSource>().PlayOneShot(fallSound);
        }

        // Анимация
        if (animator != null)
        {
            animator.SetTrigger(fallTrigger);
        }

        // Отключаем коллайдер чтобы игрок провалился
        if (platformCollider != null)
        {
            platformCollider.enabled = false;
        }

        // Запускаем исчезновение
        StartCoroutine(DestroyAfterFall());
    }

    IEnumerator DestroyAfterFall()
    {
        yield return new WaitForSeconds(destroyDelay);

        // Эффект исчезновения
        if (disappearEffect != null)
        {
            Instantiate(disappearEffect, transform.position, Quaternion.identity);
        }

        // Звук исчезновения
        if (disappearSound != null && GetComponent<AudioSource>() != null)
        {
            GetComponent<AudioSource>().PlayOneShot(disappearSound);
        }

        if (respawnAfterFall)
        {
            // Скрываем платформу
            if (spriteRenderer != null)
                spriteRenderer.enabled = false;

            // Ждем и возрождаем
            yield return new WaitForSeconds(respawnDelay);
            RespawnPlatform();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void RespawnPlatform()
    {
        // Возвращаем на стартовую позицию
        transform.position = startPosition;

        // Сбрасываем физику
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        // Включаем коллайдер
        if (platformCollider != null)
        {
            platformCollider.enabled = true;
        }

        // Показываем спрайт
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = originalColor;
        }

        // Сбрасываем переменные
        isFalling = false;
        stepCount = 0;
        shakeTimer = 0f;

        // Эффект возрождения
        if (respawnEffect != null)
        {
            Instantiate(respawnEffect, transform.position, Quaternion.identity);
        }

        // Звук возрождения (опционально)
        if (reappearSound != null && GetComponent<AudioSource>() != null)
        {
            GetComponent<AudioSource>().PlayOneShot(reappearSound);
        }
    }

    void UpdatePlatformColor()
    {
        if (spriteRenderer != null)
        {
            float progress = (float)stepCount / maxSteps;

            if (progress >= 0.7f)
            {
                spriteRenderer.color = criticalColor;
            }
            else if (progress >= 0.3f)
            {
                spriteRenderer.color = warningColor;
            }
        }
    }

    // Публичный метод для принудительного сброса
    public void ResetPlatform()
    {
        StopAllCoroutines();
        RespawnPlatform();
    }

    // Для визуализации в редакторе
    private void OnDrawGizmosSelected()
    {
        // Рисуем путь падения
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 5f);

        // Отмечаем зону уничтожения
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawCube(transform.position + Vector3.down * destroyDelay * fallSpeed, new Vector3(2f, 0.5f, 0f));
    }
}