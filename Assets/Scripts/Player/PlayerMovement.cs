using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    [Header("Projectile Settings")]
    public GameObject bulletPrefab;   // префаб пули
    public Transform firePoint;       // точка, откуда бросаем
    public float throwForce = 15f;
    public float cooldown = 0.5f;
    [Header("Collision Settings")]
    public LayerMask destroyLayers;   // слои, при столкновении с которыми пуля умирает
    private bool canThrow = true;
    //
    [Header("References")]
    public Rigidbody2D rb;
    public Animator animator;
    bool isFacingRight = true;
    bool wasGrounded;
    public ParticleSystem smokeFX;
    public Collider2D playerCollider; // Добавьте эту ссылку
    //
    [Header("Gravity")]
    public float baseGravity = 2f;
    public float maxFallSpeed = 18f;
    public float fallGravityMult = 2f;
    //
    [Header("Movement")]
    public float moveSpeed = 5f;
    float horizontalMovement;
    //
    [Header("Dashing")]
    public float dashSpeed = 25f;
    float dashDuration = 0.2f;
    public float dashCooldown = 0.5f;
    bool isDashing;
    bool canDash = true;
    TrailRenderer trailRenderer;
    //
    [Header("Jumping")]
    public float jumpPower = 10f;
    public int maxJumps = 2;
    private int jumpsRemaining;
    //
    [Header("Ground Check")]
    public Transform groundCheckPos;
    public Vector2 groundCheckSize = new Vector2(0.49f, 0.03f);
    public LayerMask groundLayer;
    bool isGrounded;
    //
    [Header("Wall Check")]
    public Transform wallCheckPos;
    public Vector2 wallCheckSize = new Vector2(0.49f, 0.03f);
    public LayerMask wallLayer;
    //
    [Header("Wall Movement")]
    public float wallSlideSpeed = 2f;
    bool isWallSliding;
    //
    [Header("Wall Jump")]
    bool isWallJumping;
    float wallJumpDirection;
    public float wallJumpTime = 0.2f;
    float wallJumpTimer;
    public Vector2 wallJumpPower = new Vector2(5f, 10f);
    //
    [Header("Platform Drop")]
    public float dropSpeed = 8f; // Скорость спуска
    public float dropDisableTime = 0.3f; // Время игнорирования платформы
    bool isDroppingFromPlatform = false;
    //
    public CinemachineImpulseSource impulseSource;
    //
    [Header("Ripple Effect")]
    public RippleEffect rippleEffect;
    public float rippleStrength = 1.0f; // Сила эффекта ripple при рывке
    //
    [Header("Ripple Material")]
    public Material rippleMaterial;
    //
    [Header("Ghost Trail")]
    [SerializeField] private GhostTrail ghostTrail;
    //
    [Header("Attack Check")]
    public int attackDamage = 10;
    //
    [Header("Combat")]
    public float comboWindow = 0.45f;
    public float attack1Lock = 0.45f;
    public float attack2Lock = 0.55f;
    int comboStep;
    float comboTimer;
    bool isAttacking;

    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool isDead;

    [Header("Stamina")]
    public float maxStamina = 100;
    public float currentStamina;
    public float staminaRegenRate = 15f; // Скорость восстановления стамины в секунду
    public float staminaRegenDelay = 1f; // Задержка перед восстановлением
    private float staminaRegenTimer; // Таймер для задержки восстановления

    [Header("Stamina Costs")]
    public int dashStaminaCost = 20;
    public int jumpStaminaCost = 10;
    public int wallJumpStaminaCost = 15;
    public int attackStaminaCost = 15;
    public int throwStaminaCost = 25;
    public int sprintStaminaCost = 5; // Стоимость спринта в секунду (если будет спринт)

    private void Start()
    {
        TriggerRipple();

        currentHealth = maxHealth;
        currentStamina = maxStamina; // Инициализация стамины
        trailRenderer = GetComponent<TrailRenderer>();
        if (trailRenderer == null)
        {
            Debug.LogError("TrailRenderer не найден на объекте!");
        }

        // Если не назначен коллайдер в инспекторе, пытаемся найти его
        if (playerCollider == null)
        {
            playerCollider = GetComponent<Collider2D>();
        }

        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    void Update()
    {
        animator.SetFloat("yVelocity", rb.linearVelocity.y);
        animator.SetFloat("magnitude", rb.linearVelocity.magnitude);
        animator.SetBool("isWallSliding", isWallSliding);

        // combo timer
        if (comboTimer > 0)
            comboTimer -= Time.deltaTime;
        else
            comboStep = 0;

        if (isDead)
            return;

        // Восстановление стамины
        HandleStaminaRegeneration();

        if (isDashing)
        {
            return;
        }

        GroundCheck();
        ProcessGravity();
        ProcessWallSlide();
        ProcessWallJump();

        if (!isWallJumping)
        {
            rb.linearVelocity = new Vector2(horizontalMovement * moveSpeed, rb.linearVelocity.y);
            Flip();
        }
    }

    // Метод для восстановления стамины
    private void HandleStaminaRegeneration()
    {
        // Если таймер восстановления активен
        if (staminaRegenTimer > 0)
        {
            staminaRegenTimer -= Time.deltaTime;
        }
        // Восстанавливаем стамину только если таймер <= 0 и стамина не полная
        else if (currentStamina < maxStamina)
        {
            currentStamina += Mathf.RoundToInt(staminaRegenRate * Time.deltaTime);
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }
    }

    // Метод для проверки и траты стамины
    private bool TrySpendStamina(int cost)
    {
        if (currentStamina >= cost)
        {
            currentStamina -= cost;
            // Сбрасываем таймер восстановления при трате стамины
            staminaRegenTimer = staminaRegenDelay;
            return true;
        }
        return false;
    }

    // Метод для спуска с платформы
    public void DropFromPlatform(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded && !isDashing && !isWallJumping)
        {
            StartCoroutine(DropFromPlatformCoroutine());
        }
    }

    private IEnumerator DropFromPlatformCoroutine()
    {
        if (isDroppingFromPlatform) yield break;

        isDroppingFromPlatform = true;

        // Ищем платформы под ногами
        Collider2D[] platforms = Physics2D.OverlapBoxAll(
            groundCheckPos.position,
            groundCheckSize,
            0,
            LayerMask.GetMask("Platform") // Убедитесь, что слой правильный
        );

        if (platforms.Length > 0 && playerCollider != null)
        {
            // Временно отключаем столкновение со всеми найденными платформами
            foreach (Collider2D platform in platforms)
            {
                if (platform != null && platform.GetComponent<PlatformEffector2D>() != null)
                {
                    Physics2D.IgnoreCollision(playerCollider, platform, true);
                }
            }

            // Применяем скорость вниз для спуска
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -dropSpeed);

            // Ждём
            yield return new WaitForSeconds(dropDisableTime);

            // Восстанавливаем столкновение
            foreach (Collider2D platform in platforms)
            {
                if (platform != null && platform.GetComponent<PlatformEffector2D>() != null)
                {
                    Physics2D.IgnoreCollision(playerCollider, platform, false);
                }
            }
        }

        isDroppingFromPlatform = false;
    }

    public void Move(InputAction.CallbackContext context)
    {
        horizontalMovement = context.ReadValue<Vector2>().x;
    }

    public void Attack(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (isAttacking && comboTimer <= 0) return;

        // Проверяем наличие стамины для атаки
        if (!TrySpendStamina(attackStaminaCost))
        {
            Debug.Log("Недостаточно стамины для атаки!");
            return;
        }

        comboTimer = comboWindow;
        comboStep++;

        if (comboStep == 1)
            StartCoroutine(AttackRoutine("Attack1", attack1Lock));
        else if (comboStep == 2)
        {
            StartCoroutine(AttackRoutine("Attack2", attack2Lock));
            comboStep = 0;
        }

        Debug.Log("Атака выполнена");
    }

    public void Dash(InputAction.CallbackContext context)
    {
        Debug.Log($"Dash input: {context.performed}, CanDash: {canDash}, IsDashing: {isDashing}");

        // Проверяем наличие стамины для рывка
        if (context.performed && canDash && !isDashing)
        {
            if (!TrySpendStamina(dashStaminaCost))
            {
                Debug.Log("Недостаточно стамины для рывка!");
                return;
            }

            CameraShakeManager.Instance.Shake(impulseSource);

            // Добавляем эффект ripple при рывке
            if (rippleEffect != null)
            {
                TriggerRipple();

                // Вычисляем позицию для ripple (центр экрана или позиция игрока на экране)
                rippleEffect.Emit(new Vector2(0.5f, 0.5f));

                // Можно регулировать параметры ripple для рывка
                StartCoroutine(AdjustRippleForDash());
            }

            StartCoroutine(DashCoroutine());
        }
    }

    public void Throw(InputAction.CallbackContext context)
    {
        if (context.performed && !isDead)
        {
            // Проверяем наличие стамины для броска
            if (!TrySpendStamina(throwStaminaCost))
            {
                Debug.Log("Недостаточно стамины для броска!");
                return;
            }

            animator.SetTrigger("Throw"); // через Any State
            Throw();
        }
    }

    IEnumerator AttackRoutine(string trigger, float lockTime)
    {
        isAttacking = true;
        animator.SetTrigger(trigger);

        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        yield return new WaitForSeconds(lockTime);

        isAttacking = false;
    }

    private IEnumerator AdjustRippleForDash()
    {
        if (rippleEffect == null) yield break;

        // Сохраняем оригинальные значения
        float originalRefraction = rippleEffect.refractionStrength;
        float originalReflection = rippleEffect.reflectionStrength;
        float originalWaveSpeed = rippleEffect.waveSpeed;

        // Увеличиваем эффект для рывка
        rippleEffect.refractionStrength = Mathf.Min(originalRefraction * 1.5f, 1.0f);
        rippleEffect.reflectionStrength = Mathf.Min(originalReflection * 1.3f, 1.0f);
        rippleEffect.waveSpeed = originalWaveSpeed * 0.8f; // Немного замедляем волны

        // Ждем короткое время
        yield return new WaitForSeconds(0.3f);

        // Возвращаем оригинальные значения
        rippleEffect.refractionStrength = originalRefraction;
        rippleEffect.reflectionStrength = originalReflection;
        rippleEffect.waveSpeed = originalWaveSpeed;
    }

    private IEnumerator DashCoroutine()
    {
        if (isDashing) yield break;

        Debug.Log("DASH STARTED!");

        ghostTrail?.PlayTrail();

        canDash = false;
        isDashing = true;

        if (trailRenderer != null)
        {
            trailRenderer.emitting = true;
        }

        float dashDirection;
        if (Mathf.Abs(horizontalMovement) > 0.1f)
        {
            dashDirection = Mathf.Sign(horizontalMovement);
        }
        else
        {
            dashDirection = isFacingRight ? 1f : -1f;
        }

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        float originalYVelocity = rb.linearVelocity.y;

        rb.linearVelocity = new Vector2(
            dashDirection * dashSpeed,
            originalYVelocity
        );

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = originalGravity;

        if (Mathf.Abs(horizontalMovement) > 0.1f)
        {
            rb.linearVelocity = new Vector2(
                horizontalMovement * moveSpeed,
                rb.linearVelocity.y
            );
        }
        else
        {
            rb.linearVelocity = new Vector2(
                0f,
                rb.linearVelocity.y
            );
        }

        if (trailRenderer != null)
        {
            trailRenderer.emitting = false;
        }

        isDashing = false;
        Debug.Log("DASH ENDED!");

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
        Debug.Log("DASH READY AGAIN!");
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed && wallJumpTimer > 0f)
        {
            // Проверяем наличие стамины для прыжка от стены
            if (!TrySpendStamina(wallJumpStaminaCost))
            {
                Debug.Log("Недостаточно стамины для прыжка от стены!");
                return;
            }

            isWallJumping = true;

            rb.linearVelocity = new Vector2(
                wallJumpDirection * wallJumpPower.x,
                wallJumpPower.y
            );

            wallJumpTimer = 0f;
            JumpFX();

            Invoke(nameof(CancelWallJump), wallJumpTime);
            return;
        }

        if (context.performed && jumpsRemaining > 0)
        {
            // Проверяем наличие стамины для прыжка
            if (!TrySpendStamina(jumpStaminaCost))
            {
                Debug.Log("Недостаточно стамины для прыжка!");
                return;
            }

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpPower);
            jumpsRemaining--;
            JumpFX();
        }

        if (context.canceled && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }
    }

    private void JumpFX()
    {
        animator.SetTrigger("jump");
        if (smokeFX != null)
        {
            smokeFX.Play();
        }
    }

    private void GroundCheck()
    {
        bool groundedNow = Physics2D.OverlapBox(
            groundCheckPos.position,
            groundCheckSize,
            0,
            groundLayer
        );

        // ПРИЗЕМЛЕНИЕ
        if (!wasGrounded && groundedNow)
        {
            OnLand();
        }

        isGrounded = groundedNow;
        wasGrounded = groundedNow;

        if (isGrounded)
        {
            jumpsRemaining = maxJumps;
            canDash = true;
        }
    }

    private void OnLand()
    {
        animator.SetTrigger("land");

        if (smokeFX != null)
        {
            smokeFX.Play();
        }
    }

    private bool WallCheck()
    {
        return Physics2D.OverlapBox(wallCheckPos.position, wallCheckSize, 0, wallLayer);
    }

    private void ProcessGravity()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = baseGravity * fallGravityMult;
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                Mathf.Max(rb.linearVelocity.y, -maxFallSpeed)
            );
        }
        else
        {
            rb.gravityScale = baseGravity;
        }
    }

    private void ProcessWallSlide()
    {
        if (!isGrounded && WallCheck() && horizontalMovement != 0 && Mathf.Sign(horizontalMovement) == Mathf.Sign(transform.localScale.x))
        {
            isWallSliding = true;

            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                Mathf.Max(rb.linearVelocity.y, -wallSlideSpeed)
            );
        }
        else
        {
            isWallSliding = false;
        }
    }

    private void ProcessWallJump()
    {
        if (isWallSliding)
        {
            isWallJumping = false;
            wallJumpDirection = -transform.localScale.x;
            wallJumpTimer = wallJumpTime;

            CancelInvoke(nameof(CancelWallJump));
        }
        else if (wallJumpTimer > 0f)
        {
            wallJumpTimer -= Time.deltaTime;
        }
    }

    private void CancelWallJump()
    {
        isWallJumping = false;
    }

    private void Flip()
    {
        if (horizontalMovement == 0) return;

        if (isFacingRight && horizontalMovement < 0 ||
            !isFacingRight && horizontalMovement > 0)
        {
            isFacingRight = !isFacingRight;

            Vector3 scale = transform.localScale;
            scale.x *= -1f;
            transform.localScale = scale;

            if (rb.linearVelocity.y == 0 && smokeFX != null)
            {
                smokeFX.Play();
            }
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        animator.SetTrigger("IsDead");

        // Отключаем управление
        enabled = false;

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        // Коллайдер выключаем
        if (playerCollider != null)
            playerCollider.enabled = false;

        StartCoroutine(Death());
        SceneManager.LoadScene("Draft");
    }

    IEnumerator Death()
    {
        yield return new WaitForSeconds(10f); // немного подождать
    }




    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        // Анимация удара
        animator.SetTrigger("Hit");

        // небольшой отскок (по желанию)
        rb.linearVelocity = new Vector2(
            -transform.localScale.x * 3f,
            5f
        );

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void TriggerRipple()
    {
        if (rippleMaterial == null) return;

        Vector3 vp = Camera.main.WorldToViewportPoint(transform.position);

        rippleMaterial.SetVector("_RippleCenter", new Vector4(vp.x, vp.y, 0, 0));
    }

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

    // Публичные методы для доступа к состоянию стамины (для UI)
    public float GetStaminaPercentage()
    {
        return (float)currentStamina / maxStamina;
    }

    public bool HasEnoughStamina(int cost)
    {
        return currentStamina >= cost;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheckPos != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(groundCheckPos.position, groundCheckSize);
        }

        if (wallCheckPos != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(wallCheckPos.position, wallCheckSize);
        }
    }
}