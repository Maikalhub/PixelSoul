using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class BossAI : MonoBehaviour
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
    [Header("AI Settings")]
    public AIState currentState = AIState.Idle;
    public MovementMode movementMode = MovementMode.SmartJump;
    public AttackType attackType = AttackType.Standard;

    [Header("Movement")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 5f;
    public float jumpForce = 10f;
    public float jumpForwardForce = 6f;
    public float jumpCooldown = 1f;

    [Header("Attack")]
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    public int attackDamage = 10;

    [Header("Charge Attack")]
    public float chargeSpeed = 12f;
    public float chargeDuration = 0.8f;

    [Header("Ranged Attack")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 10f;

    [Header("Sight / Aggro")]
    public float sightRange = 10f;
    public float evadeRange = 3f;
    public float aggroRange = 8f;

    [Header("Health")]
    public float health = 100f;
    public float retreatHealthThreshold = 0.3f;

    [Header("Patrol")]
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

    // =====================================================
    // PHASE SETTINGS
    // =====================================================
    private int phase = 1; // фаза босса
    public float phase2Threshold = 0.6f;
    public float phase3Threshold = 0.3f;

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
    private float directionTolerance = 0.2f;

    // =====================================================
    // UNITY METHODS
    // =====================================================
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        player = GameObject.FindWithTag("Player")?.transform;
        currentPatrolIndex = 0;
    }

    private void Update()
    {
        if (isDead || player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        HandlePhase();
        HandleStateTransitions(distance);

        animator.SetBool("isWalking",
            currentState == AIState.Patrolling ||
            currentState == AIState.Chasing);
    }

    private void FixedUpdate()
    {
        if (isDead || isCharging) return;

        switch (currentState)
        {
            case AIState.Patrolling: PatrolBehavior(); break;
            case AIState.Chasing: ChaseBehavior(); break;
            case AIState.Attacking: AttackBehavior(); break;
            case AIState.Evading: EvadeBehavior(); break;
            case AIState.Retreating: RetreatBehavior(); break;
            case AIState.Fleeing: FleeBehavior(); break;
            case AIState.Idle:
            case AIState.Alert: IdleBehavior(); break;
        }
    }

    // =====================================================
    // PHASE LOGIC
    // =====================================================
    private void HandlePhase()
    {
        float hpPercent = health / 100f;

        if (phase == 1 && hpPercent <= phase2Threshold)
        {
            phase = 2;
            chaseSpeed += 2f;
            attackCooldown *= 0.8f;
            Debug.Log("Boss Phase 2!");
        }

        if (phase == 2 && hpPercent <= phase3Threshold)
        {
            phase = 3;
            chaseSpeed += 2f;
            attackType = AttackType.Charge;
            jumpForce += 2f;
            Debug.Log("Boss Phase 3!");
        }
    }

    // =====================================================
    // STATE TRANSITIONS
    // =====================================================
    private void HandleStateTransitions(float distance)
    {
        float healthPercent = health / 100f;

        if (healthPercent <= 0.1f)
        {
            currentState = AIState.Fleeing;
            return;
        }

        if (healthPercent <= retreatHealthThreshold)
        {
            currentState = AIState.Retreating;
            return;
        }

        if (distance <= attackRange)
        {
            currentState = AIState.Attacking;
            return;
        }

        if (distance < evadeRange)
        {
            currentState = AIState.Evading;
            return;
        }

        if (distance < sightRange)
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
    // BEHAVIORS
    // =====================================================
    private void PatrolBehavior()
    {
        if (patrolPoints.Length == 0) return;

        Vector2 target = patrolPoints[currentPatrolIndex].position;
        FaceTarget(target);

        float dir = isFacingRight ? 1 : -1;
        rb.linearVelocity = new Vector2(dir * patrolSpeed, rb.linearVelocity.y);

        if (Vector2.Distance(transform.position, target) < 0.3f)
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

    private void ChaseBehavior()
    {
        float xDiff = player.position.x - transform.position.x;
        float dir = Mathf.Sign(xDiff);

        if ((dir > 0 && !isFacingRight) || (dir < 0 && isFacingRight))
            Flip();

        switch (movementMode)
        {
            case MovementMode.GroundOnly:
                rb.linearVelocity = new Vector2(dir * chaseSpeed, rb.linearVelocity.y);
                break;

            case MovementMode.SmartJump:
                if ((!IsGroundAhead() || IsWallAhead()) && IsGrounded())
                {
                    TryJump(dir);
                    return;
                }
                rb.linearVelocity = new Vector2(dir * chaseSpeed, rb.linearVelocity.y);
                break;

            case MovementMode.JumpOnly:
                if (IsGrounded()) TryJump(dir);
                break;
        }
    }

    private void AttackBehavior()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;

        lastAttackTime = Time.time;

        switch (attackType)
        {
            case AttackType.Standard: StandardAttack(); break;
            case AttackType.Charge: StartCoroutine(ChargeAttack()); break;
            case AttackType.Ranged: StartCoroutine(RangedBarrage(3, 0.3f)); break; // 3 залпа
        }
    }

    private void StandardAttack()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
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
            rb.linearVelocity = new Vector2(dir * chargeSpeed, rb.linearVelocity.y);
            timer += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        isCharging = false;
    }

    private void RangedAttack()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        animator.SetTrigger("attack");

        if (bulletPrefab == null || firePoint == null) return;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            float dir = isFacingRight ? 1 : -1;
            bulletRb.linearVelocity = new Vector2(dir * bulletSpeed, 0);
        }
    }

    private IEnumerator RangedBarrage(int shots, float delay)
    {
        for (int i = 0; i < shots; i++)
        {
            RangedAttack();
            yield return new WaitForSeconds(delay);
        }
    }

    private void EvadeBehavior()
    {
        float dir = player.position.x > transform.position.x ? -1 : 1;
        rb.linearVelocity = new Vector2(dir * chaseSpeed, rb.linearVelocity.y);
    }

    private void RetreatBehavior()
    {
        float dir = player.position.x > transform.position.x ? -1 : 1;
        rb.linearVelocity = new Vector2(dir * patrolSpeed, rb.linearVelocity.y);
    }

    private void FleeBehavior()
    {
        float dir = player.position.x > transform.position.x ? -1 : 1;
        rb.linearVelocity = new Vector2(dir * chaseSpeed, rb.linearVelocity.y);
    }

    private void IdleBehavior()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    // =====================================================
    // JUMP / GROUND / WALL
    // =====================================================
    private void TryJump(float dir)
    {
        if (!IsGrounded() || Time.time - lastJumpTime < jumpCooldown) return;

        lastJumpTime = Time.time;
        animator.SetTrigger("jump");

        rb.linearVelocity = new Vector2(dir * jumpForwardForce, jumpForce);
    }

    private bool IsGrounded()
    {
        return groundCheck != null && Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);
    }

    private bool IsGroundAhead()
    {
        return groundAheadCheck == null || Physics2D.Raycast(groundAheadCheck.position, Vector2.down, groundCheckDistance, groundLayer);
    }

    private bool IsWallAhead()
    {
        if (wallCheck == null) return false;
        Vector2 dir = isFacingRight ? Vector2.right : Vector2.left;
        return Physics2D.Raycast(wallCheck.position, dir, wallCheckDistance, groundLayer);
    }

    // =====================================================
    // DAMAGE
    // =====================================================
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        health -= damage;
        animator.SetTrigger("Hit");

        if (health <= 0) Die();
    }

    private void Die()
    {
        isDead = true;
        animator.SetBool("IsDead", true);
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        GetComponent<Collider2D>().enabled = false;
        Destroy(gameObject, 2f);
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