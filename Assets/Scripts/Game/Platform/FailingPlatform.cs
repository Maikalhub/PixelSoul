using UnityEngine;
using System.Collections;
using System;

public class FallingPlatform : MonoBehaviour
{
    [Header("Режим падения")]
    public FallMode fallMode = FallMode.Immediate;
    public enum FallMode
    {
        Immediate,
        Delayed,
        StepCount,
        ShakeThenFall
    }

    [Header("Настройки падения")]
    public float fallDelay = 0.5f;
    public float fallSpeed = 5f;
    public float destroyDelay = 2f;
    public int maxSteps = 3;

    [Header("Эффекты тряски")]
    public bool enableShake = true;
    public float shakeAmount = 0.1f;
    public float shakeSpeed = 10f;

    [Header("Визуальные эффекты")]
    public GameObject fallEffect;
    public GameObject disappearEffect;
    public AudioClip shakeSound;
    public AudioClip fallSound;
    public AudioClip disappearSound;
    public AudioClip reappearSound;

    [Header("Визуальные индикаторы")]
    public SpriteRenderer spriteRenderer;
    public Color warningColor = Color.red;
    public Color criticalColor = Color.magenta;
    public Animator animator;
    public string fallTrigger = "Fall";

    [Header("Возрождение")]
    public bool respawnAfterFall = false;
    public float respawnDelay = 3f;
    public GameObject respawnEffect;

    // 💥 УРОН
    [Header("Damage Settings")]
    public bool dealDamageOnFall = true;
    public int damageAmount = 1;
    public string[] damageTags = { "Player", "Enemy", "Boss" };

    [Tooltip("Trigger collider для нанесения урона")]
    public Collider2D damageTrigger;

    private Rigidbody2D rb;
    private Collider2D platformCollider;
    private Vector3 startPosition;
    private bool isFalling = false;
    private int stepCount = 0;
    private bool isPlayerOnPlatform = false;
    private Color originalColor;
    private float shakeTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        platformCollider = GetComponent<Collider2D>();
        startPosition = transform.position;

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (animator == null)
            animator = GetComponent<Animator>();

        if (damageTrigger == null)
            Debug.LogWarning("DamageTrigger не назначен!");
    }

    void Update()
    {
        if (isFalling) return;

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
                        StartFalling();
                    break;

                case FallMode.ShakeThenFall:
                    StartCoroutine(ShakeAndFall());
                    break;
            }
        }
    }

    private void UpdatePlatformColor()
    {
        throw new NotImplementedException();
    }

    // 💥 ВОТ ГЛАВНОЕ — УРОН ЧЕРЕЗ TRIGGER
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isFalling || !dealDamageOnFall) return;

        if (IsDamageTarget(other.gameObject))
        {
            // 👇 проверка что падаем вниз
            if (rb.linearVelocity.y < -0.1f)
            {
                DealDamage(other.gameObject);
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerOnPlatform = false;
            shakeTimer = 0f;

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

            float shakeX = Mathf.Sin(Time.time * shakeSpeed) * shakeAmount;
            float shakeY = Mathf.Cos(Time.time * shakeSpeed) * shakeAmount * 0.5f;
            transform.position = startPosition + new Vector3(shakeX, shakeY, 0);

            if (spriteRenderer != null)
            {
                float t = shakeTimer / fallDelay;
                spriteRenderer.color = Color.Lerp(originalColor, warningColor, t);
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

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
            rb.linearVelocity = Vector2.down * fallSpeed;
        }

        if (platformCollider != null)
            platformCollider.enabled = false;

        StartCoroutine(DestroyAfterFall());
    }

    IEnumerator DestroyAfterFall()
    {
        yield return new WaitForSeconds(destroyDelay);

        if (respawnAfterFall)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = false;

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
        transform.position = startPosition;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        if (platformCollider != null)
            platformCollider.enabled = true;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = originalColor;
        }

        isFalling = false;
        stepCount = 0;
        shakeTimer = 0f;
    }

    bool IsDamageTarget(GameObject obj)
    {
        foreach (string tag in damageTags)
        {
            if (obj.CompareTag(tag))
                return true;
        }
        return false;
    }

    void DealDamage(GameObject target)
    {
        var health = target.GetComponent<PlayerMovement>();
        if (health != null)
            health.TakeDamage(damageAmount);

        var enemyHealth = target.GetComponent<EnemyAI>();
        if (enemyHealth != null)
            enemyHealth.TakeDamageMethod(damageAmount);

       /* var bossHealth = target.GetComponent<BossAI>();
        if (bossHealth != null)
            bossHealth.TakeDamage(damageAmount);
       */
    }
}