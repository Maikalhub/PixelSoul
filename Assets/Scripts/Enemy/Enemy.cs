using System.Collections;
using UnityEngine;
using RangeAttribute = UnityEngine.RangeAttribute;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class EnemyAI : MonoBehaviour
{
    // =====================================================
    // ENUMS
    // =====================================================

    public enum AIState { Patrolling, Chasing, Attacking, Evading, Idle, Alert, Retreating, Fleeing }

    public enum MovementMode { GroundOnly, SmartJump, JumpOnly }

    public enum AttackType { Standard, Charge, Ranged }

    // =====================================================
    // SETTINGS
    // =====================================================

    [Header("AI Configuration")]
    public AIState currentState = AIState.Patrolling;

    [Header("Movement Mode")]
    public MovementMode movementMode = MovementMode.SmartJump;

    [Header("Attack Type")]
    public AttackType attackType = AttackType.Standard;

    [Header("Movement Speeds")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 5f;

    [Header("Jump Settings")]
    public float jumpForce = 10f;
    public float jumpForwardForce = 6f;
    public float jumpCooldown = 1f;

    [Header("Attack Settings")]
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    public int attackDamage = 10;

    [Header("Charge Settings")]
    public float chargeSpeed = 12f;
    public float chargeDuration = 0.8f;

    [Header("Ranged Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 10f;

    [Header("Sight Settings")]
    public float sightRange = 10f;
    public float evadeRange = 3f;
    public float aggroRange = 8f;

    [Header("Health")]
    public float health = 100f;
    public float retreatHealthThreshold = 0.3f;

    [Header("Patrol Points")]
    public Transform[] patrolPoints;

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

    [System.Serializable]
    public class DropItem
    {
        public GameObject prefab;
        [Range(0f, 1f)] public float dropChance = 1f;
    }

    // А `[Header]` ставим над полем массива DropItem
    [Header("Drop Settings")]
    public DropItem[] drops;
    public float dropForce = 3f;
    public string dropLayerName = "DropItem";
    // =====================================================
    // STUN SETTINGS
    // =====================================================
    [Header("Stun Settings")]
    public float stunDuration = 1f;
    public Color stunColor = Color.red;
    public float blinkFrequency = 0.2f;

    // =====================================================
    // PRIVATE
    // =====================================================
    private Rigidbody2D rb;
    private Animator animator;
    private Transform player;
    private int currentPatrolIndex;
    private float lastAttackTime;
    private float lastJumpTime;
    private bool isDead;
    private bool isCharging;
    private bool isStunned;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    // =====================================================
    // UNITY METHODS
    // =====================================================

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        player = GameObject.FindWithTag("Player")?.transform;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        currentPatrolIndex = 0;
    }

    private void Update()
    {
        if (isDead || player == null || isStunned) return;

        float distance = Vector2.Distance(transform.position, player.position);

        HandleStateTransitions(distance);

        animator.SetBool("isWalking",
            currentState == AIState.Patrolling ||
            currentState == AIState.Chasing);
    }

    private void FixedUpdate()
    {
        if (isDead || isCharging || isStunned) return;

        switch (currentState)
        {
            case AIState.Patrolling: PatrolBehavior(); break;
            case AIState.Chasing: ChaseBehavior(); break;
            case AIState.Attacking: AttackBehavior(); break;
            case AIState.Evading: EvadeBehavior(); break;
            case AIState.Idle: IdleBehavior(); break;
            case AIState.Alert: IdleBehavior(); break;
            case AIState.Retreating: RetreatBehavior(); break;
            case AIState.Fleeing: FleeBehavior(); break;
        }
    }

    // =====================================================
    // STATE LOGIC
    // =====================================================
    private void HandleStateTransitions(float distance)
    {
        float healthPercent = health / 100f;

        if (healthPercent <= 0.1f && canFlee)
        {
            currentState = AIState.Fleeing;
            return;
        }

        if (healthPercent <= retreatHealthThreshold && canRetreat)
        {
            currentState = AIState.Retreating;
            return;
        }

        if (distance <= attackRange && canAttack)
        {
            currentState = AIState.Attacking;
            return;
        }

        if (distance < evadeRange && canEvade)
        {
            currentState = AIState.Evading;
            return;
        }

        if (distance < sightRange && canChase)
        {
            currentState = AIState.Chasing;
            return;
        }

        if (patrolPoints != null && patrolPoints.Length > 0)
            currentState = AIState.Patrolling;
        else
            currentState = AIState.Idle;
    }

    // =====================================================
    // BEHAVIOURS
    // =====================================================

    private void PatrolBehavior()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        Vector2 target = patrolPoints[currentPatrolIndex].position;
        FaceTarget(target);

        float dir = isFacingRight ? 1 : -1;
        rb.velocity = new Vector2(dir * patrolSpeed, rb.velocity.y);

        if (Vector2.Distance(transform.position, target) < 0.3f)
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

    private void ChaseBehavior()
    {
        if (!canChase) return;

        float xDiff = player.position.x - transform.position.x;
        float dir = Mathf.Sign(xDiff);

        if ((dir > 0 && !isFacingRight) ||
            (dir < 0 && isFacingRight))
            Flip();

        switch (movementMode)
        {
            case MovementMode.GroundOnly:
                rb.velocity = new Vector2(dir * chaseSpeed, rb.velocity.y);
                break;

            case MovementMode.SmartJump:
                if (!IsGroundAhead() && IsGrounded())
                {
                    TryJump(dir);
                    return;
                }
                if (IsWallAhead() && IsGrounded())
                {
                    TryJump(dir);
                    return;
                }
                rb.velocity = new Vector2(dir * chaseSpeed, rb.velocity.y);
                break;

            case MovementMode.JumpOnly:
                if (IsGrounded())
                    TryJump(dir);
                break;
        }
    }

    private void AttackBehavior()
    {
        if (!canAttack) return;
        if (Time.time - lastAttackTime < attackCooldown) return;

        lastAttackTime = Time.time;

        switch (attackType)
        {
            case AttackType.Standard: StandardAttack(); break;
            case AttackType.Charge: StartCoroutine(ChargeAttack()); break;
            case AttackType.Ranged: RangedAttack(); break;
        }
    }

    private void StandardAttack()
    {
        rb.velocity = new Vector2(0, rb.velocity.y);
        animator.SetTrigger("attack");
    }

    private IEnumerator ChargeAttack()
    {
        if (isCharging) yield break;
        isCharging = true;
        animator.SetTrigger("attack");

        float dir = isFacingRight ? 1f : -1f;
        float timer = 0f;

        while (timer < chargeDuration)
        {
            rb.velocity = new Vector2(dir * chargeSpeed, rb.velocity.y);
            timer += Time.deltaTime;
            yield return null;
        }

        rb.velocity = Vector2.zero;
        isCharging = false;
    }

    private void RangedAttack()
    {
        rb.velocity = new Vector2(0, rb.velocity.y);
        animator.SetTrigger("attack");

        if (bulletPrefab == null || firePoint == null) return;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            float dir = isFacingRight ? 1 : -1;
            bulletRb.velocity = new Vector2(dir * bulletSpeed, 0);
        }
    }

    private void EvadeBehavior()
    {
        if (!canEvade) return;
        float dir = player.position.x > transform.position.x ? -1 : 1;
        rb.velocity = new Vector2(dir * chaseSpeed, rb.velocity.y);
    }

    private void RetreatBehavior()
    {
        if (!canRetreat) return;
        float dir = player.position.x > transform.position.x ? -1 : 1;
        rb.velocity = new Vector2(dir * patrolSpeed, rb.velocity.y);
    }

    private void FleeBehavior()
    {
        if (!canFlee) return;
        float dir = player.position.x > transform.position.x ? -1 : 1;
        rb.velocity = new Vector2(dir * chaseSpeed, rb.velocity.y);
    }

    private void IdleBehavior()
    {
        rb.velocity = new Vector2(0, rb.velocity.y);
    }

    // =====================================================
    // JUMP / GROUND / WALL
    // =====================================================

    private void TryJump(float dir)
    {
        if (!IsGrounded() || Time.time - lastJumpTime < jumpCooldown) return;

        lastJumpTime = Time.time;
        animator.SetTrigger("jump");
        rb.velocity = new Vector2(dir * jumpForwardForce, jumpForce);
    }

    private bool IsGrounded()
    {
        if (groundCheck == null) return false;
        RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);
        return hit.collider != null;
    }

    private bool IsGroundAhead()
    {
        if (groundAheadCheck == null) return true;
        RaycastHit2D hit = Physics2D.Raycast(groundAheadCheck.position, Vector2.down, groundCheckDistance, groundLayer);
        return hit.collider != null;
    }

    private bool IsWallAhead()
    {
        if (wallCheck == null) return false;
        Vector2 dir = isFacingRight ? Vector2.right : Vector2.left;
        RaycastHit2D hit = Physics2D.Raycast(wallCheck.position, dir, wallCheckDistance, groundLayer);
        return hit.collider != null;
    }

    // =====================================================
    // DAMAGE + STUN
    // =====================================================

    public void TakeDamageMethod(float damage, bool applyStun = false)
    {
        if (isDead) return;

        health -= damage;
        animator.SetTrigger("Hit");
        Debug.Log($"Enemy took damage: {damage} | HP: {health}");

        if (applyStun && !isStunned)
            StartCoroutine(StunCoroutine(stunDuration));

        if (health <= 0)
            Die();
    }

    private IEnumerator StunCoroutine(float duration)
    {
        isStunned = true;
        rb.velocity = Vector2.zero;
        currentState = AIState.Idle;

        float timer = 0f;
        bool colorToggle = false;

        while (timer < duration)
        {
            timer += blinkFrequency;

            if (spriteRenderer != null)
            {
                colorToggle = !colorToggle;
                spriteRenderer.color = colorToggle ? stunColor : originalColor;
            }

            yield return new WaitForSeconds(blinkFrequency);
        }

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        isStunned = false;
    }

    private void Die()
    {
        isDead = true;
        float pickupDelay = 0.5f;

        foreach (var drop in drops)
        {
            if (drop.prefab == null) continue;
            if (Random.value > drop.dropChance) continue;

            GameObject obj = Instantiate(drop.prefab, transform.position, Quaternion.identity);

            int layer = LayerMask.NameToLayer(dropLayerName);
            if (layer >= 0 && layer <= 31)
                obj.layer = layer;
            else
                Debug.LogWarning($"Drop layer '{dropLayerName}' не существует!");

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
        rb.velocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        GetComponent<Collider2D>().enabled = false;

        Destroy(gameObject, 2f);
    }

    private IEnumerator EnableColliderAfterDelay(Collider2D col, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (col != null) col.enabled = true;
    }

    // =====================================================
    // UTILITY
    // =====================================================

    private void FaceTarget(Vector2 target)
    {
        if (target.x > transform.position.x && !isFacingRight) Flip();
        else if (target.x < transform.position.x && isFacingRight) Flip();
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}