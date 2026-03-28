using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Projectile Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float throwForce = 15f;
    public float cooldown = 0.5f;

    [Header("Collision Settings")]
    public LayerMask destroyLayers;
    private bool canThrow = true;

    [Header("References")]
    public Rigidbody2D rb;
    public Animator animator;
    public ParticleSystem smokeFX;
    public Collider2D playerCollider;

    private bool isFacingRight = true;
    private bool wasGrounded;

    [Header("Gravity")]
    public float baseGravity = 2f;
    public float maxFallSpeed = 18f;
    public float fallGravityMult = 2f;

    [Header("Movement")]
    public float moveSpeed = 5f;
    private float horizontalMovement;

    [Header("Acceleration")]
    public float acceleration = 15f;
    public float deceleration = 20f;

    [Header("Sprint")]
    public float sprintMultiplier = 1.8f;
    private bool isSprinting;

    [Header("Dashing")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 0.5f;
    private bool isDashing;
    private bool canDash = true;
    private bool hasAirDash = true;
    private TrailRenderer trailRenderer;

    [Header("Jumping")]
    public float jumpPower = 10f;
    public int maxJumps = 2;
    private int jumpsRemaining;

    [Header("Ground Check")]
    public Transform groundCheckPos;
    public Vector2 groundCheckSize = new Vector2(0.49f, 0.03f);
    public LayerMask groundLayer;
    private bool isGrounded;

    [Header("Wall Check")]
    public Transform wallCheckPos;
    public Vector2 wallCheckSize = new Vector2(0.49f, 0.03f);
    public LayerMask wallLayer;

    [Header("Wall Movement")]
    public float wallSlideSpeed = 2f;
    private bool isWallSliding;

    [Header("Wall Jump")]
    private bool isWallJumping;
    private float wallJumpDirection;
    public float wallJumpTime = 0.2f;
    private float wallJumpTimer;
    public Vector2 wallJumpPower = new Vector2(5f, 10f);

    [Header("Platform Drop")]
    public float dropSpeed = 8f;
    public float dropDisableTime = 0.3f;
    private bool isDroppingFromPlatform = false;

    public CinemachineImpulseSource impulseSource;

    [Header("Ripple Effect")]
    public RippleEffect rippleEffect;
    public float rippleStrength = 1.0f;

    [Header("Ripple Material")]
    public Material rippleMaterial;

    [Header("Ghost Trail")]
    [SerializeField] private GhostTrail ghostTrail;

    [Header("Attack Check")]
    public int attackDamage = 10;

    [Header("Combat")]
    public float comboWindow = 0.45f;
    public float attack1Lock = 0.45f;
    public float attack2Lock = 0.55f;
    private int comboStep;
    private float comboTimer;
    private bool isAttacking;

    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool isDead;

    [Header("Derived Stats")]
    public int defenseStat = 0;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaRegenRate = 15f;
    public float staminaRegenDelay = 1f;
    private float staminaRegenTimer;

    [Header("Stamina Costs")]
    public float dashStaminaCost = 20f;
    public float jumpStaminaCost = 10f;
    public float wallJumpStaminaCost = 15f;
    public float attackStaminaCost = 15f;
    public float throwStaminaCost = 25f;
    public float sprintStaminaCost = 5f;

    [Header("Jump Buffer")]
    public float jumpBufferTime = 0.15f;
    private float jumpBufferCounter;

    [Header("Death Screen")]
    public DeathScreenController deathScreen;

    [Header("Damage Feedback")]
    public float hitKnockbackX = 3f;
    public float hitKnockbackY = 5f;
    public float damageInvulnerabilityTime = 0.25f;

    private Coroutine attackCoroutine;
    private Coroutine dashCoroutine;
    private Coroutine dashCooldownCoroutine;
    private bool isInvulnerable;

    private void Start()
    {
        TriggerRipple();

        currentHealth = maxHealth;
        currentStamina = maxStamina;
        jumpsRemaining = maxJumps;

        trailRenderer = GetComponent<TrailRenderer>();
        if (trailRenderer == null)
        {
            Debug.LogError("TrailRenderer не найден на объекте!");
        }

        if (playerCollider == null)
        {
            playerCollider = GetComponent<Collider2D>();
        }

        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    private void Update()
    {
        animator.SetFloat("yVelocity", rb.linearVelocity.y);
        animator.SetFloat("magnitude", rb.linearVelocity.magnitude);
        animator.SetBool("isWallSliding", isWallSliding);

        if (jumpBufferCounter > 0f)
            jumpBufferCounter -= Time.deltaTime;

        if (comboTimer > 0f)
            comboTimer -= Time.deltaTime;
        else
            comboStep = 0;

        if (isDead)
            return;

        GroundCheck();
        HandleSprintStamina();
        HandleStaminaRegeneration();
        HandleJumpBuffer();
    }

    private void FixedUpdate()
    {
        if (isDead || isDashing)
            return;

        if (!isWallJumping)
        {
            float targetSpeed = horizontalMovement * moveSpeed;

            if (isSprinting)
                targetSpeed *= sprintMultiplier;

            float speedDiff = targetSpeed - rb.linearVelocity.x;
            float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;

            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x + speedDiff * accelRate * Time.fixedDeltaTime,
                rb.linearVelocity.y
            );

            if (horizontalMovement != 0f)
                Flip();
        }

        ProcessGravity();
        ProcessWallSlide();
        ProcessWallJump();
    }

    private void HandleStaminaRegeneration()
    {
        if (staminaRegenTimer > 0f)
        {
            staminaRegenTimer -= Time.deltaTime;
        }
        else if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }
    }

    private void HandleSprintStamina()
    {
        if (!isSprinting || isDashing)
            return;

        if (Mathf.Abs(horizontalMovement) < 0.01f)
            return;

        if (currentStamina <= 0f)
        {
            currentStamina = 0f;
            isSprinting = false;
            return;
        }

        currentStamina -= sprintStaminaCost * Time.deltaTime;
        currentStamina = Mathf.Max(currentStamina, 0f);
        staminaRegenTimer = staminaRegenDelay;

        if (currentStamina <= 0f)
        {
            currentStamina = 0f;
            isSprinting = false;
        }
    }

    private bool TrySpendStamina(float cost)
    {
        if (currentStamina >= cost)
        {
            currentStamina -= cost;
            staminaRegenTimer = staminaRegenDelay;
            return true;
        }

        return false;
    }

    public void Move(InputAction.CallbackContext context)
    {
        horizontalMovement = context.ReadValue<Vector2>().x;
    }

    public void Sprint(InputAction.CallbackContext context)
    {
        if (context.performed)
            isSprinting = true;

        if (context.canceled)
            isSprinting = false;
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            jumpBufferCounter = jumpBufferTime;
        }

        if (context.canceled && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }
    }

    public void Dash(InputAction.CallbackContext context)
    {
        Debug.Log($"Dash input: {context.performed}, CanDash: {canDash}, HasAirDash: {hasAirDash}, IsDashing: {isDashing}");

        if (!context.performed || !canDash || isDashing || isDead)
            return;

        if (!isGrounded && !hasAirDash)
            return;

        if (!TrySpendStamina(dashStaminaCost))
        {
            Debug.Log("Недостаточно стамины для рывка!");
            return;
        }

        if (!isGrounded)
            hasAirDash = false;

        if (CameraShakeManager.Instance != null && impulseSource != null)
            CameraShakeManager.Instance.Shake(impulseSource);

        if (rippleEffect != null)
        {
            TriggerRipple();
            rippleEffect.Emit(new Vector2(0.5f, 0.5f));
            StartCoroutine(AdjustRippleForDash());
        }

        if (dashCoroutine != null)
            StopCoroutine(dashCoroutine);

        dashCoroutine = StartCoroutine(DashCoroutine());
    }

    public void Attack(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (isDead || isDashing || isWallJumping) return;
        if (isAttacking && comboTimer <= 0f) return;

        if (!TrySpendStamina(attackStaminaCost))
        {
            Debug.Log("Недостаточно стамины для атаки!");
            return;
        }

        comboTimer = comboWindow;
        comboStep = Mathf.Clamp(comboStep + 1, 1, 2);

        string trigger = comboStep == 1 ? "Attack1" : "Attack2";
        float lockTime = comboStep == 1 ? attack1Lock : attack2Lock;

        if (comboStep == 2)
            comboStep = 0;

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        attackCoroutine = StartCoroutine(AttackRoutine(trigger, lockTime));
        Debug.Log("Атака выполнена");
    }

    public void Throw(InputAction.CallbackContext context)
    {
        if (context.performed && !isDead)
        {
            if (!TrySpendStamina(throwStaminaCost))
            {
                Debug.Log("Недостаточно стамины для броска!");
                return;
            }

            animator.SetTrigger("Throw");
            Throw();
        }
    }

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

        Collider2D[] platforms = Physics2D.OverlapBoxAll(
            groundCheckPos.position,
            groundCheckSize,
            0f,
            LayerMask.GetMask("Platform")
        );

        if (platforms.Length > 0 && playerCollider != null)
        {
            foreach (Collider2D platform in platforms)
            {
                if (platform != null && platform.GetComponent<PlatformEffector2D>() != null)
                {
                    Physics2D.IgnoreCollision(playerCollider, platform, true);
                }
            }

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -dropSpeed);

            yield return new WaitForSeconds(dropDisableTime);

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

    private IEnumerator AttackRoutine(string trigger, float lockTime)
    {
        isAttacking = true;

        animator.ResetTrigger("Attack1");
        animator.ResetTrigger("Attack2");
        animator.SetTrigger(trigger);

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        yield return new WaitForSeconds(lockTime);

        isAttacking = false;
        attackCoroutine = null;
    }

    private IEnumerator DashCoroutine()
    {
        if (isDashing) yield break;

        Debug.Log("DASH STARTED!");

        ghostTrail?.PlayTrail();

        canDash = false;
        isDashing = true;

        if (trailRenderer != null)
            trailRenderer.emitting = true;

        float dashDirection = Mathf.Abs(horizontalMovement) > 0.1f
            ? Mathf.Sign(horizontalMovement)
            : (isFacingRight ? 1f : -1f);

        float originalGravity = rb.gravityScale;
        float originalYVelocity = rb.linearVelocity.y;

        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(dashDirection * dashSpeed, originalYVelocity);

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = originalGravity;

        if (Mathf.Abs(horizontalMovement) > 0.1f)
        {
            rb.linearVelocity = new Vector2(horizontalMovement * moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        if (trailRenderer != null)
            trailRenderer.emitting = false;

        isDashing = false;
        dashCoroutine = null;

        Debug.Log("DASH ENDED!");

        if (dashCooldownCoroutine != null)
            StopCoroutine(dashCooldownCoroutine);

        dashCooldownCoroutine = StartCoroutine(DashCooldownRoutine());
    }

    private IEnumerator DashCooldownRoutine()
    {
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
        dashCooldownCoroutine = null;
        Debug.Log("DASH READY AGAIN!");
    }

    private IEnumerator AdjustRippleForDash()
    {
        if (rippleEffect == null) yield break;

        float originalRefraction = rippleEffect.refractionStrength;
        float originalReflection = rippleEffect.reflectionStrength;
        float originalWaveSpeed = rippleEffect.waveSpeed;

        rippleEffect.refractionStrength = Mathf.Min(originalRefraction * 1.5f, 1.0f);
        rippleEffect.reflectionStrength = Mathf.Min(originalReflection * 1.3f, 1.0f);
        rippleEffect.waveSpeed = originalWaveSpeed * 0.8f;

        yield return new WaitForSeconds(0.3f);

        rippleEffect.refractionStrength = originalRefraction;
        rippleEffect.reflectionStrength = originalReflection;
        rippleEffect.waveSpeed = originalWaveSpeed;
    }

    private void HandleJumpBuffer()
    {
        if (isDashing || isAttacking) return;

        if (jumpBufferCounter > 0f && wallJumpTimer > 0f)
        {
            if (!TrySpendStamina(wallJumpStaminaCost))
                return;

            isWallJumping = true;

            rb.linearVelocity = new Vector2(
                wallJumpDirection * wallJumpPower.x,
                wallJumpPower.y
            );

            jumpBufferCounter = 0f;
            wallJumpTimer = 0f;

            JumpFX();
            Invoke(nameof(CancelWallJump), wallJumpTime);
            return;
        }

        if (jumpBufferCounter > 0f && jumpsRemaining > 0)
        {
            if (!TrySpendStamina(jumpStaminaCost))
                return;

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpPower);
            jumpsRemaining--;

            jumpBufferCounter = 0f;

            JumpFX();
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
        Collider2D groundHit = Physics2D.OverlapBox(
            groundCheckPos.position,
            groundCheckSize,
            0f,
            groundLayer
        );

        bool groundedNow = groundHit != null;

        if (!wasGrounded && groundedNow)
        {
            OnLand();
            hasAirDash = true;
        }

        isGrounded = groundedNow;
        wasGrounded = groundedNow;

        if (isGrounded)
        {
            jumpsRemaining = maxJumps;
        }
    }

    private void OnLand()
    {
        isWallJumping = false;
        CancelInvoke(nameof(CancelWallJump));

        animator.SetTrigger("land");

        if (smokeFX != null)
        {
            smokeFX.Play();
        }
    }

    private bool WallCheck()
    {
        Collider2D wallHit = Physics2D.OverlapBox(
            wallCheckPos.position,
            wallCheckSize,
            0f,
            wallLayer
        );

        return wallHit != null;
    }

    private void ProcessGravity()
    {
        if (rb.linearVelocity.y < 0f)
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
        if (isWallJumping)
        {
            isWallSliding = false;
            return;
        }

        bool pushingIntoWall =
            (horizontalMovement > 0f && isFacingRight) ||
            (horizontalMovement < 0f && !isFacingRight);

        if (!isGrounded && WallCheck() && horizontalMovement != 0f && pushingIntoWall)
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
        if (isWallJumping)
            return;

        if (isWallSliding)
        {
            wallJumpDirection = isFacingRight ? -1f : 1f;
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
        if (horizontalMovement == 0f) return;

        if ((isFacingRight && horizontalMovement < 0f) ||
            (!isFacingRight && horizontalMovement > 0f))
        {
            isFacingRight = !isFacingRight;

            Vector3 scale = transform.localScale;
            scale.x *= -1f;
            transform.localScale = scale;

            if (Mathf.Abs(rb.linearVelocity.y) < 0.01f && smokeFX != null)
            {
                smokeFX.Play();
            }
        }
    }

    private void InterruptAttack()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        isAttacking = false;
        comboStep = 0;
        comboTimer = 0f;
    }

    private void InterruptDash(bool startCooldown)
    {
        if (dashCoroutine != null)
        {
            StopCoroutine(dashCoroutine);
            dashCoroutine = null;
        }

        isDashing = false;

        if (trailRenderer != null)
            trailRenderer.emitting = false;

        rb.gravityScale = baseGravity;

        if (startCooldown)
        {
            if (dashCooldownCoroutine != null)
                StopCoroutine(dashCooldownCoroutine);

            dashCooldownCoroutine = StartCoroutine(DashCooldownRoutine());
        }
    }

    private IEnumerator DamageInvulnerabilityRoutine()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(damageInvulnerabilityTime);
        isInvulnerable = false;
    }

    public void TakeDamage(int damage)
    {
        if (isDead || isInvulnerable) return;

        int finalDamage = Mathf.Max(damage - defenseStat, 1);
        currentHealth -= finalDamage;
        currentHealth = Mathf.Max(currentHealth, 0);

        bool lethal = currentHealth <= 0;

        InterruptAttack();
        InterruptDash(!lethal);

        isSprinting = false;
        isWallJumping = false;
        jumpBufferCounter = 0f;

        animator.SetTrigger("Hit");

        float hitDirection = isFacingRight ? -1f : 1f;
        rb.linearVelocity = new Vector2(hitDirection * hitKnockbackX, hitKnockbackY);

        if (lethal)
        {
            Die();
            return;
        }

        StartCoroutine(DamageInvulnerabilityRoutine());
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        InterruptAttack();
        InterruptDash(false);

        isSprinting = false;
        isWallJumping = false;

        animator.SetTrigger("IsDead");

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        if (playerCollider != null)
            playerCollider.enabled = false;

        if (deathScreen != null)
            deathScreen.ShowDeathScreen();
        else
            Debug.LogWarning("DeathScreenController не назначен в PlayerMovement.");
    }

    private void TriggerRipple()
    {
        if (rippleMaterial == null || Camera.main == null) return;

        Vector3 vp = Camera.main.WorldToViewportPoint(transform.position);
        rippleMaterial.SetVector("_RippleCenter", new Vector4(vp.x, vp.y, 0f, 0f));
    }

    public void Throw()
    {
        if (!canThrow || bulletPrefab == null || firePoint == null)
            return;

        if (Camera.main == null)
            return;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        Vector2 direction = (mouseWorldPos - firePoint.position).normalized;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
            bulletRb.linearVelocity = direction * throwForce;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
            bulletScript.destroyOnLayers = destroyLayers;

        canThrow = false;
        StartCoroutine(ResetThrowCooldown());
    }

    private IEnumerator ResetThrowCooldown()
    {
        yield return new WaitForSeconds(cooldown);
        canThrow = true;
    }

    public bool TryUseInventoryItem(ItemData item)
    {
        if (item == null || !item.canUse || isDead)
            return false;

        ApplyItemEffect(item);
        return true;
    }

    private void ApplyItemEffect(ItemData item)
    {
        bool isInstantEffect =
            item.useEffectType == ItemUseEffectType.RestoreHealth ||
            item.useEffectType == ItemUseEffectType.RestoreStamina;

        ApplyEffectValue(item.useEffectType, item.useValue);

        if (item.isTemporaryBuff && !isInstantEffect && item.buffDuration > 0f)
        {
            StartCoroutine(RemoveTemporaryBuffAfterTime(
                item.useEffectType,
                item.useValue,
                item.buffDuration));
        }
    }

    private void ApplyEffectValue(ItemUseEffectType effectType, float value)
    {
        int intValue = Mathf.RoundToInt(value);

        switch (effectType)
        {
            case ItemUseEffectType.Damage:
                attackDamage += intValue;
                break;

            case ItemUseEffectType.Defense:
                defenseStat += intValue;
                break;

            case ItemUseEffectType.MaxHealth:
                maxHealth += intValue;
                currentHealth = Mathf.Clamp(currentHealth + intValue, 0, maxHealth);
                break;

            case ItemUseEffectType.RestoreHealth:
                currentHealth = Mathf.Clamp(currentHealth + intValue, 0, maxHealth);
                break;

            case ItemUseEffectType.MaxStamina:
                maxStamina += value;
                currentStamina = Mathf.Clamp(currentStamina + value, 0f, maxStamina);
                break;

            case ItemUseEffectType.RestoreStamina:
                currentStamina = Mathf.Clamp(currentStamina + value, 0f, maxStamina);
                break;

            case ItemUseEffectType.MoveSpeed:
                moveSpeed += value;
                break;

            case ItemUseEffectType.JumpPower:
                jumpPower += value;
                break;

            case ItemUseEffectType.SprintMultiplier:
                sprintMultiplier += value;
                break;

            case ItemUseEffectType.DashSpeed:
                dashSpeed += value;
                break;
        }
    }

    private IEnumerator RemoveTemporaryBuffAfterTime(ItemUseEffectType effectType, float value, float duration)
    {
        yield return new WaitForSeconds(duration);
        RemoveEffectValue(effectType, value);
        Debug.Log($"{effectType} buff ended");
    }

    private void RemoveEffectValue(ItemUseEffectType effectType, float value)
    {
        int intValue = Mathf.RoundToInt(value);

        switch (effectType)
        {
            case ItemUseEffectType.Damage:
                attackDamage -= intValue;
                break;

            case ItemUseEffectType.Defense:
                defenseStat = Mathf.Max(0, defenseStat - intValue);
                break;

            case ItemUseEffectType.MaxHealth:
                maxHealth = Mathf.Max(1, maxHealth - intValue);
                currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
                break;

            case ItemUseEffectType.MaxStamina:
                maxStamina = Mathf.Max(1f, maxStamina - value);
                currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
                break;

            case ItemUseEffectType.MoveSpeed:
                moveSpeed = Mathf.Max(0.1f, moveSpeed - value);
                break;

            case ItemUseEffectType.JumpPower:
                jumpPower = Mathf.Max(0.1f, jumpPower - value);
                break;

            case ItemUseEffectType.SprintMultiplier:
                sprintMultiplier = Mathf.Max(1f, sprintMultiplier - value);
                break;

            case ItemUseEffectType.DashSpeed:
                dashSpeed = Mathf.Max(0.1f, dashSpeed - value);
                break;
        }
    }

    public float GetStaminaPercentage()
    {
        return currentStamina / maxStamina;
    }

    public bool HasEnoughStamina(float cost)
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