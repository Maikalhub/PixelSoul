using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class EnemyAI : MonoBehaviour
{
    public enum AIState { Patrolling, Chasing, Attacking, Evading, Idle, Alert, Retreating, Fleeing }
    public enum MovementMode { GroundOnly, SmartJump, JumpOnly }
    public enum AttackType { Standard, Charge, Ranged, StationaryRanged, DisappearRanged }
    public enum PatrolRouteMode { Loop, PingPong }

    public enum BossType { None, DisappearAmbusher, SelfSummoner }
    public enum BossAmbushMode { BehindPlayer, RandomSide }

    [Header("AI Configuration")]
    public AIState currentState = AIState.Patrolling;
    public MovementMode movementMode = MovementMode.SmartJump;
    public AttackType attackType = AttackType.Standard;

    [Header("References")]
    public Transform player;
    public Transform sightOrigin;
    public EnemyAttack meleeHitbox;

    [Header("Movement Speeds")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 5f;

    [Header("Jump Settings")]
    public float jumpForce = 10f;
    public float jumpForwardForce = 6f;
    public float jumpCooldown = 1f;

    [Header("Attack Settings")]
    public float attackRange = 1.5f;
    public float attackExitBuffer = 0.35f;
    public float attackCooldown = 1f;
    public int attackDamage = 10;
    public float attackWindup = 0.12f;
    public float meleeHitboxDuration = 0.12f;
    public float attackLockDuration = 0.4f;
    public float verticalAttackTolerance = 1.25f;

    [Header("Charge Settings")]
    public float chargeSpeed = 12f;
    public float chargeDuration = 0.8f;
    public float chargeWindup = 0.1f;

    [Header("Ranged Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 10f;
    public float rangedShootDelay = 0.12f;

    [Header("Disappear Ranged Settings")]
    public float disappearDuration = 1.5f;
    public bool disableColliderOnDisappear = true;
    public bool stopPhysicsOnDisappear = true;

    [Header("Sight Settings")]
    public float sightRange = 10f;
    public float aggroRange = 12f;
    public float evadeRange = 3f;
    public float evadeExitBuffer = 0.5f;
    public float alertDuration = 1.2f;
    public LayerMask visionMask;

    [Header("Health")]
    public float maxHealth = 100f;
    public float health = 100f;
    [Range(0f, 1f)] public float retreatHealthThreshold = 0.3f;
    [Range(0f, 1f)] public float fleeHealthThreshold = 0.1f;

    [Header("Patrol Points")]
    public Transform[] patrolPoints;

    [Header("Patrol Tuning")]
    public PatrolRouteMode patrolRouteMode = PatrolRouteMode.Loop;
    public float patrolStopDistance = 0.2f;
    public float patrolSlowdownDistance = 1.25f;
    [Range(0.05f, 1f)] public float patrolMinSpeedPercent = 0.25f;
    public bool patrolWaitAtPoint = true;
    public float patrolWaitTime = 1f;
    public bool patrolFlipToNextPointWhileWaiting = true;

    [Header("Platform AI")]
    public LayerMask groundLayer;
    public Transform groundCheck;
    public Transform groundAheadCheck;
    public Transform wallCheck;
    public float groundCheckDistance = 0.4f;
    public float wallCheckDistance = 0.4f;

    [Header("Direction")]
    public bool isFacingRight = true;

    [Header("Behavior Flags")]
    public bool canFlee = true;
    public bool canRetreat = true;
    public bool canEvade = true;
    public bool canChase = true;
    public bool canAttack = true;

    [Header("Boss Settings")]
    public bool isBoss = false;
    public BossType bossType = BossType.None;

    [Header("Boss Visibility")]
    public GameObject healthBarObject;
    public bool hideAllChildSpriteRenderers = true;
    public bool disableColliderWhenBossHidden = true;
    public bool stopPhysicsWhenBossHidden = true;

    [Header("Boss Special Cooldown")]
    public float bossSpecialCooldown = 4f;
    public float bossSpecialTriggerRange = 8f;

    [Header("Boss Ambusher")]
    public BossAmbushMode bossAmbushMode = BossAmbushMode.BehindPlayer;
    public float bossDisappearTime = 1f;
    public float bossReappearDistance = 2f;
    public float bossReappearYOffset = 0f;
    public float bossStrikeDelay = 0.2f;
    public float bossStrikeSpeed = 10f;
    public float bossStrikeDuration = 0.35f;

    [Header("Boss Summoner")]
    public GameObject[] summonPrefabs;
    public Transform[] summonPoints;
    public int summonCount = 2;
    public float summonDelayBetween = 0.2f;
    public float summonSpawnRadius = 1.5f;
    public bool summonedCopiesCanSummon = true;
    public int maxSummonGeneration = 2;

    [HideInInspector] public int summonGeneration = 0;

    [Header("Light Visibility")]
    public bool startHidden = true;
    [Range(0f, 1f)] public float hiddenAlpha = 0f;
    public float fadeSpeed = 4f;

    [System.Serializable]
    public class DropItem
    {
        public GameObject prefab;
        [Range(0f, 1f)] public float dropChance = 1f;
    }

    [Header("Drop Settings")]
    public DropItem[] drops;
    public float dropForce = 3f;
    public string dropLayerName = "DropItem";

    [Header("Stun Settings")]
    public float stunDuration = 1f;
    public Color stunColor = Color.red;
    public float blinkFrequency = 0.2f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private float currentAlpha;
    private float targetAlpha;
    private Color currentVisualColor;

    private int currentPatrolIndex;
    private int patrolStepDirection = 1;
    private bool isWaitingOnPatrolPoint;
    private float patrolWaitTimer;

    private float lastAttackTime = -999f;
    private float lastJumpTime = -999f;
    private float lastSeenTime = -999f;
    private float nextPlayerSearchTime;

    private bool isDead;
    private bool isCharging;
    private bool isStunned;
    private bool isPerformingAttack;
    private bool hasAggro;

    private float currentDistanceToPlayer = Mathf.Infinity;
    private bool hasLineOfSight;
    private Vector2 lastKnownPlayerPosition;

    private float lastBossSpecialTime = -999f;
    private bool isBossSpecialRunning;
    private SpriteRenderer[] bossSpriteRenderers;
    private Collider2D bossCollider;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bossCollider = GetComponent<Collider2D>();

        bossSpriteRenderers = hideAllChildSpriteRenderers
            ? GetComponentsInChildren<SpriteRenderer>(true)
            : new SpriteRenderer[] { spriteRenderer };

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            currentVisualColor = originalColor;
            currentAlpha = startHidden ? hiddenAlpha : 1f;
            targetAlpha = currentAlpha;
            ApplyVisual();
        }
    }

    private void Start()
    {
        TryResolvePlayer(force: true);

        maxHealth = Mathf.Max(1f, maxHealth);
        health = health <= 0f ? maxHealth : Mathf.Clamp(health, 0f, maxHealth);

        if (meleeHitbox == null)
            meleeHitbox = GetComponentInChildren<EnemyAttack>();

        currentPatrolIndex = 0;
        patrolStepDirection = 1;
        isWaitingOnPatrolPoint = false;
        patrolWaitTimer = 0f;
    }

    private void Update()
    {
        UpdateVisibilityFade();

        if (isDead || isStunned)
            return;

        TryResolvePlayer();
        UpdateTargetInfo();
        HandleStateTransitions();
        TryStartBossSpecial();
        UpdateAnimatorParameters();
    }

    private void FixedUpdate()
    {
        if (isDead || isStunned || isCharging || isBossSpecialRunning)
            return;

        switch (currentState)
        {
            case AIState.Patrolling:
                PatrolBehavior();
                break;

            case AIState.Chasing:
                ChaseBehavior();
                break;

            case AIState.Attacking:
                AttackBehavior();
                break;

            case AIState.Evading:
                EvadeBehavior();
                break;

            case AIState.Idle:
                IdleBehavior();
                break;

            case AIState.Alert:
                AlertBehavior();
                break;

            case AIState.Retreating:
                RetreatBehavior();
                break;

            case AIState.Fleeing:
                FleeBehavior();
                break;
        }
    }

    private void TryResolvePlayer(bool force = false)
    {
        if (!force && player != null)
            return;

        if (!force && Time.time < nextPlayerSearchTime)
            return;

        nextPlayerSearchTime = Time.time + 0.5f;

        GameObject foundPlayer = GameObject.FindWithTag("Player");
        if (foundPlayer != null)
            player = foundPlayer.transform;
    }

    private void UpdateTargetInfo()
    {
        if (player == null)
        {
            currentDistanceToPlayer = Mathf.Infinity;
            hasLineOfSight = false;
            return;
        }

        currentDistanceToPlayer = Vector2.Distance(transform.position, player.position);
        hasLineOfSight = CanSeePlayer();

        if (hasLineOfSight)
        {
            hasAggro = true;
            lastSeenTime = Time.time;
            lastKnownPlayerPosition = player.position;
        }
        else
        {
            float effectiveAggroRange = Mathf.Max(aggroRange, sightRange);

            if (currentDistanceToPlayer > effectiveAggroRange &&
                Time.time - lastSeenTime > alertDuration)
            {
                hasAggro = false;
            }
        }
    }

    private void HandleStateTransitions()
    {
        if (player == null)
        {
            currentState = HasPatrolRoute() ? AIState.Patrolling : AIState.Idle;
            return;
        }

        if (isPerformingAttack || isCharging || isBossSpecialRunning)
        {
            currentState = AIState.Attacking;
            return;
        }

        float healthPercent = health / maxHealth;
        float effectiveAggroRange = Mathf.Max(aggroRange, sightRange);

        if (healthPercent <= fleeHealthThreshold && canFlee && hasAggro)
        {
            currentState = AIState.Fleeing;
            return;
        }

        if (healthPercent <= retreatHealthThreshold && canRetreat && hasAggro)
        {
            currentState = AIState.Retreating;
            return;
        }

        if (ShouldAttack())
        {
            currentState = AIState.Attacking;
            return;
        }

        if (ShouldEvade())
        {
            currentState = AIState.Evading;
            return;
        }

        if ((hasLineOfSight || (hasAggro && currentDistanceToPlayer <= effectiveAggroRange)) && canChase)
        {
            currentState = AIState.Chasing;
            return;
        }

        if (hasAggro && Time.time - lastSeenTime <= alertDuration)
        {
            currentState = AIState.Alert;
            return;
        }

        currentState = HasPatrolRoute() ? AIState.Patrolling : AIState.Idle;
    }

    private bool ShouldAttack()
    {
        if (!canAttack || player == null)
            return false;

        float allowedDistance = currentState == AIState.Attacking
            ? attackRange + attackExitBuffer
            : attackRange;

        if (currentDistanceToPlayer > allowedDistance)
            return false;

        if (Mathf.Abs(player.position.y - transform.position.y) > verticalAttackTolerance)
            return false;

        return hasLineOfSight;
    }

    private bool ShouldEvade()
    {
        if (!canEvade || player == null)
            return false;

        if (ShouldAttack())
            return false;

        float allowedDistance = currentState == AIState.Evading
            ? evadeRange + evadeExitBuffer
            : evadeRange;

        if (currentDistanceToPlayer > allowedDistance)
            return false;

        return Mathf.Abs(player.position.y - transform.position.y) <= verticalAttackTolerance * 1.5f;
    }

    private void PatrolBehavior()
    {
        if (!HasPatrolRoute())
        {
            IdleBehavior();
            return;
        }

        if (HandlePatrolWaiting())
            return;

        Transform targetPoint = patrolPoints[currentPatrolIndex];
        if (targetPoint == null)
        {
            AdvancePatrolIndex();
            IdleBehavior();
            return;
        }

        float xDiff = targetPoint.position.x - transform.position.x;
        float distanceToPoint = Mathf.Abs(xDiff);

        if (distanceToPoint <= patrolStopDistance)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            if (patrolWaitAtPoint && patrolWaitTime > 0f)
            {
                if (patrolFlipToNextPointWhileWaiting)
                    FaceNextPatrolPoint();

                AdvancePatrolIndex();
                StartPatrolWait();
                return;
            }

            AdvancePatrolIndex();
            return;
        }

        float currentPatrolMoveSpeed = GetPatrolMoveSpeed(distanceToPoint);
        MoveInDirection(xDiff, currentPatrolMoveSpeed);
    }

    private bool HandlePatrolWaiting()
    {
        if (!isWaitingOnPatrolPoint)
            return false;

        patrolWaitTimer -= Time.fixedDeltaTime;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (patrolWaitTimer <= 0f)
        {
            patrolWaitTimer = 0f;
            isWaitingOnPatrolPoint = false;
        }

        return true;
    }

    private void StartPatrolWait()
    {
        isWaitingOnPatrolPoint = true;
        patrolWaitTimer = patrolWaitTime;
    }

    private float GetPatrolMoveSpeed(float distanceToPoint)
    {
        if (patrolSlowdownDistance <= patrolStopDistance)
            return patrolSpeed;

        if (distanceToPoint >= patrolSlowdownDistance)
            return patrolSpeed;

        float t = Mathf.InverseLerp(patrolStopDistance, patrolSlowdownDistance, distanceToPoint);
        float speedPercent = Mathf.Lerp(patrolMinSpeedPercent, 1f, t);
        return patrolSpeed * speedPercent;
    }

    private void AdvancePatrolIndex()
    {
        if (patrolPoints == null || patrolPoints.Length <= 1)
            return;

        if (patrolRouteMode == PatrolRouteMode.Loop)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            return;
        }

        currentPatrolIndex += patrolStepDirection;

        if (currentPatrolIndex >= patrolPoints.Length)
        {
            patrolStepDirection = -1;
            currentPatrolIndex = patrolPoints.Length - 2;
        }
        else if (currentPatrolIndex < 0)
        {
            patrolStepDirection = 1;
            currentPatrolIndex = 1;
        }

        currentPatrolIndex = Mathf.Clamp(currentPatrolIndex, 0, patrolPoints.Length - 1);
    }

    private int GetNextPatrolIndexPreview()
    {
        if (patrolPoints == null || patrolPoints.Length <= 1)
            return currentPatrolIndex;

        if (patrolRouteMode == PatrolRouteMode.Loop)
            return (currentPatrolIndex + 1) % patrolPoints.Length;

        int nextIndex = currentPatrolIndex + patrolStepDirection;

        if (nextIndex >= patrolPoints.Length)
            nextIndex = patrolPoints.Length - 2;
        else if (nextIndex < 0)
            nextIndex = 1;

        return Mathf.Clamp(nextIndex, 0, patrolPoints.Length - 1);
    }

    private void FaceNextPatrolPoint()
    {
        int nextIndex = GetNextPatrolIndexPreview();

        if (patrolPoints == null || nextIndex < 0 || nextIndex >= patrolPoints.Length)
            return;

        Transform nextPoint = patrolPoints[nextIndex];
        if (nextPoint == null)
            return;

        float dir = nextPoint.position.x - transform.position.x;
        if (Mathf.Abs(dir) > 0.01f)
            FaceDirection(dir);
    }

    private void ChaseBehavior()
    {
        if (!canChase || player == null)
        {
            IdleBehavior();
            return;
        }

        Vector2 target = hasLineOfSight ? (Vector2)player.position : lastKnownPlayerPosition;
        float xDiff = target.x - transform.position.x;

        if (Mathf.Abs(xDiff) <= 0.1f)
        {
            IdleBehavior();
            return;
        }

        MoveInDirection(xDiff, chaseSpeed);
    }

    private void AttackBehavior()
    {
        if (!canAttack || player == null)
        {
            currentState = AIState.Alert;
            return;
        }

        if (isCharging || isPerformingAttack || isBossSpecialRunning)
            return;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        FaceTarget(player.position);

        if (Time.time - lastAttackTime < attackCooldown)
            return;

        lastAttackTime = Time.time;

        switch (attackType)
        {
            case AttackType.Standard:
                StartCoroutine(StandardAttackRoutine());
                break;

            case AttackType.Charge:
                StartCoroutine(ChargeAttackRoutine());
                break;

            case AttackType.Ranged:
                StartCoroutine(RangedAttackRoutine());
                break;

            case AttackType.StationaryRanged:
                StartCoroutine(StationaryRangedRoutine());
                break;

            case AttackType.DisappearRanged:
                StartCoroutine(DisappearRangedRoutine());
                break;
        }
    }

    private IEnumerator StandardAttackRoutine()
    {
        if (isPerformingAttack)
            yield break;

        isPerformingAttack = true;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        animator.ResetTrigger("attack");
        animator.SetTrigger("attack");

        yield return new WaitForSeconds(attackWindup);

        AnimationEvent_EnableMeleeHitbox();
        yield return new WaitForSeconds(meleeHitboxDuration);
        AnimationEvent_DisableMeleeHitbox();

        float remain = Mathf.Max(0f, attackLockDuration - attackWindup - meleeHitboxDuration);
        if (remain > 0f)
            yield return new WaitForSeconds(remain);

        isPerformingAttack = false;
    }

    private IEnumerator ChargeAttackRoutine()
    {
        if (isCharging)
            yield break;

        isCharging = true;
        isPerformingAttack = true;

        rb.linearVelocity = Vector2.zero;
        FaceTarget(player != null ? player.position : transform.position);

        animator.ResetTrigger("attack");
        animator.SetTrigger("attack");

        yield return new WaitForSeconds(chargeWindup);

        AnimationEvent_EnableMeleeHitbox();

        float dir = isFacingRight ? 1f : -1f;
        float timer = 0f;

        while (timer < chargeDuration)
        {
            rb.linearVelocity = new Vector2(dir * chargeSpeed, rb.linearVelocity.y);
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        AnimationEvent_DisableMeleeHitbox();

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        isCharging = false;
        isPerformingAttack = false;
    }

    private IEnumerator RangedAttackRoutine()
    {
        if (isPerformingAttack)
            yield break;

        isPerformingAttack = true;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        FaceTarget(player != null ? player.position : transform.position);

        animator.ResetTrigger("attack");
        animator.SetTrigger("attack");

        yield return new WaitForSeconds(rangedShootDelay);

        if (player != null && bulletPrefab != null && firePoint != null)
        {
            Vector2 direction = ((Vector2)player.position - (Vector2)firePoint.position).normalized;
            if (direction.sqrMagnitude <= 0.001f)
                direction = isFacingRight ? Vector2.right : Vector2.left;

            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

            Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
                bulletRb.linearVelocity = direction * bulletSpeed;

            bullet.transform.right = direction;

            EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();
            if (bulletScript != null)
                bulletScript.damage = attackDamage;
        }

        float remain = Mathf.Max(0f, attackLockDuration - rangedShootDelay);
        if (remain > 0f)
            yield return new WaitForSeconds(remain);

        isPerformingAttack = false;
    }

    private IEnumerator StationaryRangedRoutine()
    {
        if (isPerformingAttack)
            yield break;

        isPerformingAttack = true;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        FaceTarget(player != null ? player.position : transform.position);

        animator.ResetTrigger("attack");
        animator.SetTrigger("attack");

        yield return new WaitForSeconds(rangedShootDelay);

        if (player != null && bulletPrefab != null && firePoint != null)
        {
            Vector2 direction = ((Vector2)player.position - (Vector2)firePoint.position).normalized;
            if (direction.sqrMagnitude <= 0.001f)
                direction = isFacingRight ? Vector2.right : Vector2.left;

            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

            Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
                bulletRb.linearVelocity = direction * bulletSpeed;

            bullet.transform.right = direction;

            EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();
            if (bulletScript != null)
                bulletScript.damage = attackDamage;
        }

        float remain = Mathf.Max(0f, attackLockDuration - rangedShootDelay);
        if (remain > 0f)
            yield return new WaitForSeconds(remain);

        isPerformingAttack = false;
    }

    private IEnumerator DisappearRangedRoutine()
    {
        if (isPerformingAttack)
            yield break;

        isPerformingAttack = true;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        FaceTarget(player != null ? player.position : transform.position);

        animator.ResetTrigger("attack");
        animator.SetTrigger("attack");

        yield return new WaitForSeconds(rangedShootDelay);

        if (player != null && bulletPrefab != null && firePoint != null)
        {
            Vector2 direction = ((Vector2)player.position - (Vector2)firePoint.position).normalized;
            if (direction.sqrMagnitude <= 0.001f)
                direction = isFacingRight ? Vector2.right : Vector2.left;

            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

            Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
                bulletRb.linearVelocity = direction * bulletSpeed;

            bullet.transform.right = direction;

            EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();
            if (bulletScript != null)
                bulletScript.damage = attackDamage;
        }

        if (disableColliderOnDisappear)
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
                col.enabled = false;
        }

        if (stopPhysicsOnDisappear)
        {
            rb.simulated = false;
        }

        SetLightVisible(false);
        AnimationEvent_DisableMeleeHitbox();

        yield return new WaitForSeconds(disappearDuration);

        SetLightVisible(true);

        if (disableColliderOnDisappear)
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
                col.enabled = true;
        }

        if (stopPhysicsOnDisappear)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
        }

        float remain = Mathf.Max(0f, attackLockDuration - rangedShootDelay);
        if (remain > 0f)
            yield return new WaitForSeconds(remain);

        isPerformingAttack = false;
    }

    private void TryStartBossSpecial()
    {
        if (!isBoss)
            return;

        if (bossType == BossType.None)
            return;

        if (isDead || isStunned || isBossSpecialRunning || isPerformingAttack || isCharging)
            return;

        if (player == null)
            return;

        if (Time.time - lastBossSpecialTime < bossSpecialCooldown)
            return;

        if (currentDistanceToPlayer > bossSpecialTriggerRange)
            return;

        if (!hasAggro && !hasLineOfSight)
            return;

        lastBossSpecialTime = Time.time;

        switch (bossType)
        {
            case BossType.DisappearAmbusher:
                StartCoroutine(BossDisappearAmbushRoutine());
                break;

            case BossType.SelfSummoner:
                StartCoroutine(BossSummonRoutine());
                break;
        }
    }

    private IEnumerator BossDisappearAmbushRoutine()
    {
        isBossSpecialRunning = true;
        isPerformingAttack = true;

        rb.linearVelocity = Vector2.zero;
        AnimationEvent_DisableMeleeHitbox();

        SetBossHidden(true);

        yield return new WaitForSeconds(bossDisappearTime);

        if (player != null)
        {
            Vector3 newPosition = GetBossAmbushPosition();
            transform.position = newPosition;
            FaceTarget(player.position);
        }

        SetBossHidden(false);

        yield return new WaitForSeconds(bossStrikeDelay);

        if (player != null)
        {
            FaceTarget(player.position);

            animator.ResetTrigger("attack");
            animator.SetTrigger("attack");

            yield return new WaitForSeconds(attackWindup);

            AnimationEvent_EnableMeleeHitbox();

            float dir = player.position.x > transform.position.x ? 1f : -1f;
            float timer = 0f;

            while (timer < bossStrikeDuration)
            {
                rb.linearVelocity = new Vector2(dir * bossStrikeSpeed, rb.linearVelocity.y);
                timer += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            AnimationEvent_DisableMeleeHitbox();
        }

        isPerformingAttack = false;
        isBossSpecialRunning = false;
    }

    private Vector3 GetBossAmbushPosition()
    {
        if (player == null)
            return transform.position;

        float side;

        if (bossAmbushMode == BossAmbushMode.RandomSide)
        {
            side = Random.value < 0.5f ? -1f : 1f;
        }
        else
        {
            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();

            if (playerRb != null && Mathf.Abs(playerRb.linearVelocity.x) > 0.1f)
            {
                side = playerRb.linearVelocity.x > 0f ? -1f : 1f;
            }
            else
            {
                side = player.localScale.x >= 0f ? -1f : 1f;
            }
        }

        Vector3 position = player.position + new Vector3(
            side * bossReappearDistance,
            bossReappearYOffset,
            0f
        );

        return position;
    }

    private IEnumerator BossSummonRoutine()
    {
        isBossSpecialRunning = true;
        isPerformingAttack = true;

        rb.linearVelocity = Vector2.zero;
        FaceTarget(player != null ? player.position : transform.position);

        animator.ResetTrigger("attack");
        animator.SetTrigger("attack");

        yield return new WaitForSeconds(attackWindup);

        for (int i = 0; i < summonCount; i++)
        {
            SpawnSummonedEnemy(i);
            yield return new WaitForSeconds(summonDelayBetween);
        }

        yield return new WaitForSeconds(attackLockDuration);

        isPerformingAttack = false;
        isBossSpecialRunning = false;
    }

    private void SpawnSummonedEnemy(int index)
    {
        GameObject prefab = GetRandomSummonPrefab();

        if (prefab == null)
            return;

        Vector3 spawnPosition = GetSummonPosition(index);

        GameObject newEnemy = Instantiate(prefab, spawnPosition, Quaternion.identity);

        EnemyAI newEnemyAI = newEnemy.GetComponent<EnemyAI>();

        if (newEnemyAI != null)
        {
            newEnemyAI.player = player;
            newEnemyAI.summonGeneration = summonGeneration + 1;

            if (!summonedCopiesCanSummon || newEnemyAI.summonGeneration >= maxSummonGeneration)
            {
                newEnemyAI.isBoss = false;
                newEnemyAI.bossType = BossType.None;
            }
            else
            {
                newEnemyAI.isBoss = true;
                newEnemyAI.bossType = BossType.SelfSummoner;
                newEnemyAI.summonedCopiesCanSummon = summonedCopiesCanSummon;
                newEnemyAI.maxSummonGeneration = maxSummonGeneration;
            }
        }
    }

    private GameObject GetRandomSummonPrefab()
    {
        if (summonPrefabs == null || summonPrefabs.Length == 0)
            return null;

        return summonPrefabs[Random.Range(0, summonPrefabs.Length)];
    }

    private Vector3 GetSummonPosition(int index)
    {
        if (summonPoints != null && summonPoints.Length > 0)
        {
            Transform point = summonPoints[index % summonPoints.Length];

            if (point != null)
                return point.position;
        }

        Vector2 randomOffset = Random.insideUnitCircle * summonSpawnRadius;

        return transform.position + new Vector3(
            randomOffset.x,
            randomOffset.y,
            0f
        );
    }

    private void SetBossHidden(bool hidden)
    {
        if (healthBarObject != null)
            healthBarObject.SetActive(!hidden);

        if (bossSpriteRenderers != null)
        {
            for (int i = 0; i < bossSpriteRenderers.Length; i++)
            {
                if (bossSpriteRenderers[i] != null)
                    bossSpriteRenderers[i].enabled = !hidden;
            }
        }

        if (disableColliderWhenBossHidden && bossCollider != null)
            bossCollider.enabled = !hidden;

        if (stopPhysicsWhenBossHidden && rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = !hidden;
        }
    }

    private void EvadeBehavior()
    {
        if (player == null)
        {
            IdleBehavior();
            return;
        }

        float dir = player.position.x > transform.position.x ? -1f : 1f;
        MoveInDirection(dir, chaseSpeed);
    }

    private void RetreatBehavior()
    {
        if (player == null)
        {
            IdleBehavior();
            return;
        }

        float dir = player.position.x > transform.position.x ? -1f : 1f;
        MoveInDirection(dir, patrolSpeed);
    }

    private void FleeBehavior()
    {
        if (player == null)
        {
            IdleBehavior();
            return;
        }

        float dir = player.position.x > transform.position.x ? -1f : 1f;
        MoveInDirection(dir, chaseSpeed);
    }

    private void AlertBehavior()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (lastKnownPlayerPosition.x > transform.position.x && !isFacingRight)
            Flip();
        else if (lastKnownPlayerPosition.x < transform.position.x && isFacingRight)
            Flip();
    }

    private void IdleBehavior()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private void MoveInDirection(float dir, float speed)
    {
        if (Mathf.Abs(dir) <= 0.01f)
        {
            IdleBehavior();
            return;
        }

        dir = Mathf.Sign(dir);
        FaceDirection(dir);

        bool grounded = IsGrounded();
        bool groundAhead = IsGroundAhead();
        bool wallAhead = IsWallAhead();

        switch (movementMode)
        {
            case MovementMode.GroundOnly:
                if (grounded && (!groundAhead || wallAhead))
                {
                    if (currentState == AIState.Patrolling && HasPatrolRoute())
                        AdvancePatrolIndex();

                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                    return;
                }

                rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
                break;

            case MovementMode.SmartJump:
                if (grounded && (!groundAhead || wallAhead))
                {
                    TryJump(dir);
                    return;
                }

                rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
                break;

            case MovementMode.JumpOnly:
                if (grounded)
                    TryJump(dir);
                break;
        }
    }

    private void TryJump(float dir)
    {
        if (!IsGrounded() || Time.time - lastJumpTime < jumpCooldown)
            return;

        lastJumpTime = Time.time;
        animator.SetTrigger("jump");
        rb.linearVelocity = new Vector2(dir * jumpForwardForce, jumpForce);
    }

    private bool IsGrounded()
    {
        if (groundCheck == null)
            return false;

        RaycastHit2D hit = Physics2D.Raycast(
            groundCheck.position,
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );

        return hit.collider != null;
    }

    private bool IsGroundAhead()
    {
        if (groundAheadCheck == null)
            return true;

        RaycastHit2D hit = Physics2D.Raycast(
            groundAheadCheck.position,
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );

        return hit.collider != null;
    }

    private bool IsWallAhead()
    {
        if (wallCheck == null)
            return false;

        Vector2 dir = isFacingRight ? Vector2.right : Vector2.left;

        RaycastHit2D hit = Physics2D.Raycast(
            wallCheck.position,
            dir,
            wallCheckDistance,
            groundLayer
        );

        return hit.collider != null;
    }

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;

        Vector2 origin = sightOrigin != null
            ? (Vector2)sightOrigin.position
            : (Vector2)transform.position + Vector2.up * 0.2f;

        Vector2 target = player.position;
        Vector2 direction = (target - origin).normalized;
        float distance = Vector2.Distance(origin, target);

        if (distance > sightRange)
            return false;

        origin += direction * 0.05f;

        int mask = visionMask.value == 0 ? Physics2D.DefaultRaycastLayers : visionMask.value;
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, mask);

        if (hit.collider == null)
            return false;

        return hit.collider.transform == player || hit.collider.transform.IsChildOf(player);
    }

    public void SetLightVisible(bool visible)
    {
        targetAlpha = visible ? 1f : hiddenAlpha;
    }

    private void UpdateVisibilityFade()
    {
        if (spriteRenderer == null)
            return;

        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.color = new Color(
            currentVisualColor.r,
            currentVisualColor.g,
            currentVisualColor.b,
            currentAlpha
        );
    }

    public void TakeDamageMethod(float damage, bool applyStun = false)
    {
        if (isDead)
            return;

        health = Mathf.Max(0f, health - damage);
        animator.SetTrigger("Hit");

        hasAggro = true;
        if (player != null)
        {
            lastSeenTime = Time.time;
            lastKnownPlayerPosition = player.position;
        }

        if (applyStun && !isStunned)
            StartCoroutine(StunCoroutine(stunDuration));

        if (health <= 0f)
            Die();
    }

    private IEnumerator StunCoroutine(float duration)
    {
        isStunned = true;
        isCharging = false;
        isPerformingAttack = false;
        isBossSpecialRunning = false;

        rb.linearVelocity = Vector2.zero;
        currentState = AIState.Idle;
        AnimationEvent_DisableMeleeHitbox();
        SetBossHidden(false);

        float timer = 0f;
        bool colorToggle = false;

        while (timer < duration)
        {
            timer += blinkFrequency;

            if (spriteRenderer != null)
            {
                colorToggle = !colorToggle;
                currentVisualColor = colorToggle ? stunColor : originalColor;
                ApplyVisual();
            }

            yield return new WaitForSeconds(blinkFrequency);
        }

        if (spriteRenderer != null)
        {
            currentVisualColor = originalColor;
            ApplyVisual();
        }

        isStunned = false;
    }

    private void Die()
    {
        if (isDead)
            return;

        SetBossHidden(false);

        SetLightVisible(true);
        if (disableColliderOnDisappear)
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
                col.enabled = true;
        }

        if (stopPhysicsOnDisappear)
        {
            rb.simulated = true;
        }

        isDead = true;
        isCharging = false;
        isPerformingAttack = false;
        isBossSpecialRunning = false;

        AnimationEvent_DisableMeleeHitbox();

        float pickupDelay = 0.5f;

        foreach (var drop in drops)
        {
            if (drop.prefab == null)
                continue;

            if (Random.value > drop.dropChance)
                continue;

            GameObject obj = Instantiate(drop.prefab, transform.position, Quaternion.identity);

            int layer = LayerMask.NameToLayer(dropLayerName);
            if (layer >= 0 && layer <= 31)
                obj.layer = layer;

            Rigidbody2D rbDrop = obj.GetComponent<Rigidbody2D>();
            Collider2D colDrop = obj.GetComponent<Collider2D>();

            if (rbDrop != null)
            {
                float angle = Random.Range(0f, 180f) * Mathf.Deg2Rad;
                float forceMagnitude = Random.Range(dropForce * 0.5f, dropForce);
                Vector2 force = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * forceMagnitude;
                rbDrop.AddForce(force, ForceMode2D.Impulse);
            }

            if (colDrop != null)
            {
                colDrop.enabled = false;
                StartCoroutine(EnableColliderAfterDelay(colDrop, pickupDelay));
            }
        }

        animator.SetBool("IsDead", true);
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        Collider2D bodyCollider = GetComponent<Collider2D>();
        if (bodyCollider != null)
            bodyCollider.enabled = false;

        Destroy(gameObject, 2f);
    }

    private IEnumerator EnableColliderAfterDelay(Collider2D col, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (col != null)
            col.enabled = true;
    }

    public void AnimationEvent_EnableMeleeHitbox()
    {
        if (meleeHitbox != null)
            meleeHitbox.BeginAttackWindow();
    }

    public void AnimationEvent_DisableMeleeHitbox()
    {
        if (meleeHitbox != null)
            meleeHitbox.EndAttackWindow();
    }

    private void UpdateAnimatorParameters()
    {
        bool moving =
            currentState == AIState.Patrolling ||
            currentState == AIState.Chasing ||
            currentState == AIState.Evading ||
            currentState == AIState.Retreating ||
            currentState == AIState.Fleeing;

        animator.SetBool("isWalking", moving && Mathf.Abs(rb.linearVelocity.x) > 0.05f);
    }

    private bool HasPatrolRoute()
    {
        return patrolPoints != null && patrolPoints.Length > 0;
    }

    private void FaceTarget(Vector2 target)
    {
        if (target.x > transform.position.x && !isFacingRight)
            Flip();
        else if (target.x < transform.position.x && isFacingRight)
            Flip();
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

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = sightOrigin != null ? sightOrigin.position : transform.position + Vector3.up * 0.2f;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, sightRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, evadeRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, bossSpecialTriggerRange);

        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * groundCheckDistance);
        }

        if (groundAheadCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(groundAheadCheck.position, groundAheadCheck.position + Vector3.down * groundCheckDistance);
        }

        if (wallCheck != null)
        {
            Gizmos.color = Color.magenta;
            Vector3 dir = isFacingRight ? Vector3.right : Vector3.left;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + dir * wallCheckDistance);
        }
    }
}