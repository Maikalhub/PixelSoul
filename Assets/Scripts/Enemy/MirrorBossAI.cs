using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Animator))]
public class MirrorBossAI : MonoBehaviour
{
    public enum MirrorState
    {
        Idle,
        Chase,
        Retreat,
        Melee,
        Shoot,
        Dash
    }

    [Header("Main References")]
    [SerializeField] private PlayerMovement player;
    [SerializeField] private EnemyAI healthReceiver;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D bodyCollider;

    [Header("Mirror Boss Mode")]
    [SerializeField] private bool copyPlayerStatsOnStart = true;
    [SerializeField] private bool disableEnemyAIControl = true;
    [SerializeField] private float bossHealthMultiplier = 2.5f;
    [SerializeField] private float bossDamageMultiplier = 1.25f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float keepDistance = 5f;
    [SerializeField] private float retreatDistance = 2.2f;
    [SerializeField] private float verticalTolerance = 2f;

    [Header("Jump")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.08f);
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float jumpPower = 11f;
    [SerializeField] private float jumpCooldown = 0.8f;
    [SerializeField] private float jumpIfPlayerHigherBy = 1.5f;

    [Header("Dash")]
    [SerializeField] private bool canUseDash = true;
    [SerializeField] private float dashSpeed = 25f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float dashCooldown = 1.2f;
    [SerializeField] private float dashMinDistance = 3f;
    [Range(0f, 1f)]
    [SerializeField] private float dashDecisionChance = 0.35f;
    [Range(0f, 1f)]
    [SerializeField] private float dashBehindPlayerChance = 0.55f;
    [SerializeField] private float dashBehindDistance = 2f;
    [SerializeField] private TrailRenderer dashTrail;

    [Header("Melee Attack")]
    [SerializeField] private Transform meleePoint;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private int meleeDamage = 15;
    [SerializeField] private float meleeRange = 1.5f;
    [SerializeField] private float meleeRadius = 0.45f;
    [SerializeField] private float meleeCooldown = 0.9f;
    [SerializeField] private float meleeWindup = 0.14f;
    [SerializeField] private float meleeLockTime = 0.42f;
    [SerializeField] private float meleeKnockbackX = 4f;
    [SerializeField] private float meleeKnockbackY = 4f;

    [Header("Shooting")]
    [SerializeField] private bool canUseShooting = true;
    [SerializeField] private bool copyShootingUnlockFromPlayer = true;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private int projectileDamage = 10;
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private float shootRange = 9f;
    [SerializeField] private float shootCooldown = 0.75f;
    [SerializeField] private float shootWindup = 0.16f;
    [SerializeField] private float shootLockTime = 0.35f;
    [SerializeField] private LayerMask projectileDestroyLayers;

    [Header("Copied Skill Projectile Settings")]
    [SerializeField] private int extraProjectiles = 0;
    [SerializeField] private float projectileSpreadAngle = 8f;
    [SerializeField] private bool projectileSplashEnabled = false;
    [SerializeField] private float projectileSplashDamage = 0f;
    [SerializeField] private float projectileSplashRadius = 0f;

    [Header("AI Timing")]
    [SerializeField] private float decisionInterval = 0.15f;

    [Header("Animator Parameters")]
    [SerializeField] private string walkingBool = "isWalking";
    [SerializeField] private string jumpTrigger = "jump";
    [SerializeField] private string dashTrigger = "Dash";
    [SerializeField] private string attack1Trigger = "Attack1";
    [SerializeField] private string attack2Trigger = "Attack2";
    [SerializeField] private string throwTrigger = "Throw";

    private MirrorState currentState = MirrorState.Idle;

    private float nextDecisionTime;
    private float nextDashTime;
    private float nextMeleeTime;
    private float nextShootTime;
    private float nextJumpTime;

    private float desiredMoveX;
    private bool isFacingRight = true;
    private bool isBusy;
    private bool isDashing;
    private bool isDead;

    private int comboStep = 0;

    private SkillEffectApplier playerSkills;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (bodyCollider == null)
            bodyCollider = GetComponent<Collider2D>();

        if (healthReceiver == null)
            healthReceiver = GetComponent<EnemyAI>();

        if (dashTrail == null)
            dashTrail = GetComponent<TrailRenderer>();
    }

    private void Start()
    {
        ResolvePlayer();

        if (player != null)
        {
            playerSkills = player.skillEffects;

            if (copyPlayerStatsOnStart)
                CopyPlayerStats();
        }

        if (healthReceiver != null && disableEnemyAIControl)
            StartCoroutine(DisableEnemyAIControlAfterFirstFrame());
    }

    private IEnumerator DisableEnemyAIControlAfterFirstFrame()
    {
        yield return null;

        if (healthReceiver != null)
            healthReceiver.enabled = false;
    }

    private void Update()
    {
        if (isDead)
            return;

        ResolvePlayer();

        if (player == null || player.isDead)
        {
            desiredMoveX = 0f;
            SetAnimatorBoolSafe(walkingBool, false);
            return;
        }

        if (healthReceiver != null && healthReceiver.health <= 0f)
        {
            isDead = true;
            desiredMoveX = 0f;
            return;
        }

        if (Time.time >= nextDecisionTime && !isBusy && !isDashing)
        {
            nextDecisionTime = Time.time + decisionInterval;
            DecideAction();
        }

        SetAnimatorBoolSafe(walkingBool, Mathf.Abs(rb.linearVelocity.x) > 0.1f && !isBusy);
    }

    private void FixedUpdate()
    {
        if (isDead || player == null)
            return;

        if (isBusy || isDashing)
            return;

        HandleJumpLogic();
        MoveHorizontally();
    }

    private void ResolvePlayer()
    {
        if (player != null)
            return;

        GameObject foundPlayer = GameObject.FindWithTag("Player");

        if (foundPlayer != null)
        {
            player = foundPlayer.GetComponent<PlayerMovement>();

            if (player != null)
                playerSkills = player.skillEffects;
        }
    }

    private void CopyPlayerStats()
    {
        if (player == null)
            return;

        moveSpeed = player.moveSpeed;
        jumpPower = player.jumpPower;

        dashSpeed = player.dashSpeed;
        dashDuration = player.dashDuration;
        dashCooldown = player.dashCooldown;

        meleeDamage = Mathf.Max(1, Mathf.RoundToInt(player.attackDamage * bossDamageMultiplier));
        projectileDamage = meleeDamage;

        projectileSpeed = player.throwForce;
        shootCooldown = Mathf.Max(0.05f, player.cooldown);

        if (projectilePrefab == null)
            projectilePrefab = player.bulletPrefab;

        if (projectileDestroyLayers.value == 0)
            projectileDestroyLayers = player.destroyLayers;

        if (healthReceiver != null)
        {
            healthReceiver.maxHealth = Mathf.Max(1f, player.maxHealth * bossHealthMultiplier);
            healthReceiver.health = healthReceiver.maxHealth;
        }

        if (playerSkills != null)
        {
            if (copyShootingUnlockFromPlayer)
                canUseShooting = playerSkills.CanShoot;

            extraProjectiles = Mathf.Max(0, playerSkills.ExtraProjectiles);
            projectileSpreadAngle = playerSkills.ProjectileSpreadAngle;

            projectileSplashEnabled = playerSkills.BulletSplashEnabled;
            projectileSplashDamage = playerSkills.BulletSplashDamage;
            projectileSplashRadius = playerSkills.BulletSplashRadius;
        }
    }

    private void DecideAction()
    {
        Vector2 bossPos = transform.position;
        Vector2 playerPos = player.transform.position;

        float xDistance = Mathf.Abs(playerPos.x - bossPos.x);
        float yDistance = Mathf.Abs(playerPos.y - bossPos.y);

        FaceTarget(playerPos);

        if (xDistance <= meleeRange && yDistance <= verticalTolerance && Time.time >= nextMeleeTime)
        {
            StartCoroutine(MeleeRoutine());
            return;
        }

        if (ShouldDash(xDistance))
        {
            StartCoroutine(DashRoutine());
            return;
        }

        if (canUseShooting &&
            projectilePrefab != null &&
            firePoint != null &&
            xDistance <= shootRange &&
            xDistance > meleeRange * 0.9f &&
            Time.time >= nextShootTime)
        {
            StartCoroutine(ShootRoutine());
            return;
        }

        if (xDistance < retreatDistance)
        {
            currentState = MirrorState.Retreat;
            desiredMoveX = playerPos.x > bossPos.x ? -1f : 1f;
            return;
        }

        if (xDistance > keepDistance)
        {
            currentState = MirrorState.Chase;
            desiredMoveX = playerPos.x > bossPos.x ? 1f : -1f;
            return;
        }

        currentState = MirrorState.Idle;
        desiredMoveX = 0f;
    }

    private bool ShouldDash(float xDistance)
    {
        if (!canUseDash)
            return false;

        if (Time.time < nextDashTime)
            return false;

        if (xDistance < dashMinDistance)
            return false;

        return Random.value <= dashDecisionChance;
    }

    private void MoveHorizontally()
    {
        if (Mathf.Abs(desiredMoveX) > 0.01f)
            FaceDirection(desiredMoveX);

        float targetSpeed = desiredMoveX * moveSpeed;
        float speedDiff = targetSpeed - rb.linearVelocity.x;

        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x + speedDiff * acceleration * Time.fixedDeltaTime,
            rb.linearVelocity.y
        );
    }

    private void HandleJumpLogic()
    {
        if (groundCheck == null)
            return;

        if (Time.time < nextJumpTime)
            return;

        if (!IsGrounded())
            return;

        float playerHigherAmount = player.transform.position.y - transform.position.y;

        if (playerHigherAmount >= jumpIfPlayerHigherBy)
        {
            nextJumpTime = Time.time + jumpCooldown;

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpPower);
            SetAnimatorTriggerSafe(jumpTrigger);
        }
    }

    private bool IsGrounded()
    {
        if (groundCheck == null)
            return false;

        Collider2D hit = Physics2D.OverlapBox(
            groundCheck.position,
            groundCheckSize,
            0f,
            groundLayer
        );

        return hit != null;
    }

    private IEnumerator MeleeRoutine()
    {
        isBusy = true;
        currentState = MirrorState.Melee;
        desiredMoveX = 0f;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        comboStep = comboStep == 1 ? 2 : 1;

        if (comboStep == 1)
            SetAnimatorTriggerSafe(attack1Trigger);
        else
            SetAnimatorTriggerSafe(attack2Trigger);

        yield return new WaitForSeconds(meleeWindup);

        DealMeleeDamage();

        nextMeleeTime = Time.time + meleeCooldown;

        float remainingLock = Mathf.Max(0f, meleeLockTime - meleeWindup);
        if (remainingLock > 0f)
            yield return new WaitForSeconds(remainingLock);

        isBusy = false;
    }

    private void DealMeleeDamage()
    {
        Vector2 point = meleePoint != null ? meleePoint.position : transform.position;

        Collider2D[] hits;

        if (playerLayer.value == 0)
            hits = Physics2D.OverlapCircleAll(point, meleeRadius);
        else
            hits = Physics2D.OverlapCircleAll(point, meleeRadius, playerLayer);

        bool damagedPlayer = false;

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            PlayerMovement targetPlayer = hit.GetComponent<PlayerMovement>();

            if (targetPlayer == null)
                targetPlayer = hit.GetComponentInParent<PlayerMovement>();

            if (targetPlayer == null)
                continue;

            if (damagedPlayer)
                continue;

            damagedPlayer = true;

            targetPlayer.TakeDamage(
                meleeDamage,
                transform.position,
                meleeKnockbackX,
                meleeKnockbackY
            );
        }
    }

    private IEnumerator ShootRoutine()
    {
        isBusy = true;
        currentState = MirrorState.Shoot;
        desiredMoveX = 0f;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        FaceTarget(player.transform.position);

        SetAnimatorTriggerSafe(throwTrigger);

        yield return new WaitForSeconds(shootWindup);

        FireProjectiles();

        nextShootTime = Time.time + shootCooldown;

        float remainingLock = Mathf.Max(0f, shootLockTime - shootWindup);
        if (remainingLock > 0f)
            yield return new WaitForSeconds(remainingLock);

        isBusy = false;
    }

    private void FireProjectiles()
    {
        if (projectilePrefab == null || firePoint == null || player == null)
            return;

        Vector2 baseDirection = ((Vector2)player.transform.position - (Vector2)firePoint.position).normalized;

        if (baseDirection.sqrMagnitude <= 0.001f)
            baseDirection = isFacingRight ? Vector2.right : Vector2.left;

        int projectileCount = 1 + Mathf.Max(0, extraProjectiles);

        for (int i = 0; i < projectileCount; i++)
        {
            float angleOffset = 0f;

            if (projectileCount > 1)
            {
                float centerOffset = (projectileCount - 1) * 0.5f;
                angleOffset = (i - centerOffset) * projectileSpreadAngle;
            }

            Vector2 direction = RotateVector(baseDirection, angleOffset);

            GameObject projectile = Instantiate(
                projectilePrefab,
                firePoint.position,
                Quaternion.identity
            );

            Bullet oldPlayerBullet = projectile.GetComponent<Bullet>();
            if (oldPlayerBullet != null)
                oldPlayerBullet.enabled = false;

            Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
            if (projectileRb == null)
                projectileRb = projectile.AddComponent<Rigidbody2D>();

            projectileRb.gravityScale = 0f;
            projectileRb.linearVelocity = direction * projectileSpeed;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            projectile.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            MirrorBossProjectile bossProjectile = projectile.GetComponent<MirrorBossProjectile>();
            if (bossProjectile == null)
                bossProjectile = projectile.AddComponent<MirrorBossProjectile>();

            bossProjectile.Setup(
                owner: transform,
                damage: projectileDamage,
                destroyLayers: projectileDestroyLayers,
                splashEnabled: projectileSplashEnabled,
                splashDamage: projectileSplashDamage,
                splashRadius: projectileSplashRadius
            );
        }
    }

    private IEnumerator DashRoutine()
    {
        isBusy = true;
        isDashing = true;
        currentState = MirrorState.Dash;
        desiredMoveX = 0f;

        nextDashTime = Time.time + dashCooldown;

        SetAnimatorTriggerSafe(dashTrigger);

        if (dashTrail != null)
            dashTrail.emitting = true;

        float dashDir = GetDashDirection();

        FaceDirection(dashDir);

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        float timer = 0f;

        while (timer < dashDuration)
        {
            rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.gravityScale = originalGravity;
        rb.linearVelocity = Vector2.zero;

        if (dashTrail != null)
            dashTrail.emitting = false;

        isDashing = false;
        isBusy = false;
    }

    private float GetDashDirection()
    {
        if (player == null)
            return isFacingRight ? 1f : -1f;

        if (Random.value <= dashBehindPlayerChance)
        {
            float playerFacing = player.transform.localScale.x >= 0f ? 1f : -1f;
            float targetX = player.transform.position.x - playerFacing * dashBehindDistance;
            return targetX > transform.position.x ? 1f : -1f;
        }

        return player.transform.position.x > transform.position.x ? 1f : -1f;
    }

    private Vector2 RotateVector(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;

        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);

        float x = vector.x * cos - vector.y * sin;
        float y = vector.x * sin + vector.y * cos;

        return new Vector2(x, y).normalized;
    }

    private void FaceTarget(Vector2 target)
    {
        float dir = target.x - transform.position.x;

        if (Mathf.Abs(dir) > 0.05f)
            FaceDirection(dir);
    }

    private void FaceDirection(float dir)
    {
        if (dir > 0f && !isFacingRight)
            Flip();
        else if (dir < 0f && isFacingRight)
            Flip();
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }

    private void SetAnimatorTriggerSafe(string parameterName)
    {
        if (animator == null || string.IsNullOrEmpty(parameterName))
            return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName &&
                parameter.type == AnimatorControllerParameterType.Trigger)
            {
                animator.ResetTrigger(parameterName);
                animator.SetTrigger(parameterName);
                return;
            }
        }
    }

    private void SetAnimatorBoolSafe(string parameterName, bool value)
    {
        if (animator == null || string.IsNullOrEmpty(parameterName))
            return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName &&
                parameter.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(parameterName, value);
                return;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (meleePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(meleePoint.position, meleeRadius);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, shootRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, keepDistance);

        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }
}

public class MirrorBossProjectile : MonoBehaviour
{
    private Transform owner;
    private int damage;
    private LayerMask destroyLayers;

    private bool splashEnabled;
    private float splashDamage;
    private float splashRadius;

    private Rigidbody2D rb;
    private Collider2D col;
    private Animator animator;

    private bool isDead;

    public void Setup(
        Transform owner,
        int damage,
        LayerMask destroyLayers,
        bool splashEnabled,
        float splashDamage,
        float splashRadius)
    {
        this.owner = owner;
        this.damage = damage;
        this.destroyLayers = destroyLayers;
        this.splashEnabled = splashEnabled;
        this.splashDamage = splashDamage;
        this.splashRadius = splashRadius;

        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();

        IgnoreOwnerCollisions();

        Destroy(gameObject, 8f);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
    }

    private void IgnoreOwnerCollisions()
    {
        if (owner == null || col == null)
            return;

        Collider2D[] ownerColliders = owner.GetComponentsInChildren<Collider2D>();

        foreach (Collider2D ownerCollider in ownerColliders)
        {
            if (ownerCollider != null)
                Physics2D.IgnoreCollision(col, ownerCollider, true);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead || collision == null)
            return;

        if (owner != null)
        {
            if (collision.transform == owner || collision.transform.IsChildOf(owner))
                return;
        }

        PlayerMovement player = collision.GetComponent<PlayerMovement>();

        if (player == null)
            player = collision.GetComponentInParent<PlayerMovement>();

        if (player != null)
        {
            player.TakeDamage(damage, transform.position);
            ApplySplashDamage(player);
            Die();
            return;
        }

        if (((1 << collision.gameObject.layer) & destroyLayers) != 0)
        {
            ApplySplashDamage(null);
            Die();
        }
    }

    private void ApplySplashDamage(PlayerMovement directHitPlayer)
    {
        if (!splashEnabled || splashRadius <= 0f || splashDamage <= 0f)
            return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, splashRadius);

        bool damagedPlayer = directHitPlayer != null;

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            PlayerMovement player = hit.GetComponent<PlayerMovement>();

            if (player == null)
                player = hit.GetComponentInParent<PlayerMovement>();

            if (player == null)
                continue;

            if (damagedPlayer)
                continue;

            damagedPlayer = true;
            player.TakeDamage(Mathf.RoundToInt(splashDamage), transform.position);
        }
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (col != null)
            col.enabled = false;

        if (animator != null)
        {
            animator.SetTrigger("Death");
            Destroy(gameObject, 0.5f);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!splashEnabled || splashRadius <= 0f)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, splashRadius);
    }
}