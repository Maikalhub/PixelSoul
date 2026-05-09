using System.Collections.Generic;
using UnityEngine;

public class SawSpin2D : MonoBehaviour
{
    public enum SpinSpeedMode
    {
        Constant,
        PingPong,
        RandomSmooth
    }

    public enum PathMovementMode
    {
        None,
        Loop,
        PingPong,
        Once
    }

    [Header("Spin")]
    [SerializeField] private bool clockwise = true;
    [SerializeField] private SpinSpeedMode spinSpeedMode = SpinSpeedMode.PingPong;
    [SerializeField] private float constantSpinSpeed = 1080f;
    [SerializeField] private float minSpinSpeed = 720f;
    [SerializeField] private float maxSpinSpeed = 1440f;
    [SerializeField] private float spinChangeSpeed = 2f;
    [SerializeField] private float randomTargetChangeInterval = 1.5f;

    [Header("Movement")]
    [SerializeField] private PathMovementMode movementMode = PathMovementMode.None;
    [SerializeField] private Transform[] pathPoints;
    [SerializeField] private int startPointIndex = 0;
    [SerializeField] private bool snapToStartPoint = false;

    [SerializeField] private float maxMoveSpeed = 3f;
    [SerializeField] private float moveAcceleration = 8f;
    [SerializeField] private float slowDownDistance = 1f;
    [SerializeField] private float stopDistance = 0.05f;
    [SerializeField] private float waitAtPoint = 0f;

    [Header("Damage")]
    [SerializeField] private bool damageEnabled = true;
    [SerializeField] private int damage = 10;
    [SerializeField] private float knockbackX = 6f;
    [SerializeField] private float knockbackY = 5f;
    [SerializeField] private float hitCooldown = 0.4f;
    [SerializeField] private bool useTrigger = true;
    [SerializeField] private string playerTag = "Player";

    [Header("Appearance By Trigger")]
    [SerializeField] private bool waitForTriggerToAppear = false;

    [Tooltip("Если оставить пустым, скрипт будет скрывать/показывать Renderer и Collider2D на этом объекте и его детях.")]
    [SerializeField] private GameObject sawBody;

    [SerializeField] private float appearDelay = 0f;
    [SerializeField] private bool disappearAfterTriggerExit = false;
    [SerializeField] private float disappearDelay = 0f;
    [SerializeField] private bool resetToStartPointOnAppear = false;

    [Header("Debug")]
    [SerializeField] private bool showDebugMessages = false;

    private float currentSpinSpeed;
    private float targetSpinSpeed;
    private float randomTimer;

    private int currentPointIndex;
    private int pathDirection = 1;
    private float waitTimer;
    private float currentMoveSpeed;

    private bool sawActive = true;

    private readonly Dictionary<PlayerMovement, float> nextHitTime = new Dictionary<PlayerMovement, float>();

    private void Start()
    {
        minSpinSpeed = Mathf.Max(0f, minSpinSpeed);
        maxSpinSpeed = Mathf.Max(minSpinSpeed, maxSpinSpeed);
        constantSpinSpeed = Mathf.Max(0f, constantSpinSpeed);
        spinChangeSpeed = Mathf.Max(0f, spinChangeSpeed);
        randomTargetChangeInterval = Mathf.Max(0.05f, randomTargetChangeInterval);

        maxMoveSpeed = Mathf.Max(0f, maxMoveSpeed);
        moveAcceleration = Mathf.Max(0.01f, moveAcceleration);
        stopDistance = Mathf.Max(0.001f, stopDistance);
        slowDownDistance = Mathf.Max(stopDistance + 0.01f, slowDownDistance);
        waitAtPoint = Mathf.Max(0f, waitAtPoint);

        damage = Mathf.Max(0, damage);
        knockbackX = Mathf.Max(0f, knockbackX);
        knockbackY = Mathf.Max(0f, knockbackY);
        hitCooldown = Mathf.Max(0f, hitCooldown);

        appearDelay = Mathf.Max(0f, appearDelay);
        disappearDelay = Mathf.Max(0f, disappearDelay);

        currentSpinSpeed = GetInitialSpinSpeed();
        targetSpinSpeed = currentSpinSpeed;

        if (pathPoints != null && pathPoints.Length > 0)
        {
            currentPointIndex = Mathf.Clamp(startPointIndex, 0, pathPoints.Length - 1);

            if (snapToStartPoint && pathPoints[currentPointIndex] != null)
            {
                transform.position = pathPoints[currentPointIndex].position;
            }
        }

        if (waitForTriggerToAppear)
        {
            SetSawActive(false);
        }
        else
        {
            SetSawActive(true);
        }
    }

    private void Update()
    {
        if (!sawActive)
            return;

        UpdateSpin();
        UpdateMovement();
    }

    private void UpdateSpin()
    {
        switch (spinSpeedMode)
        {
            case SpinSpeedMode.Constant:
                currentSpinSpeed = constantSpinSpeed;
                break;

            case SpinSpeedMode.PingPong:
                if (Mathf.Approximately(minSpinSpeed, maxSpinSpeed))
                {
                    currentSpinSpeed = minSpinSpeed;
                }
                else
                {
                    float t = (Mathf.Sin(Time.time * spinChangeSpeed) + 1f) * 0.5f;
                    currentSpinSpeed = Mathf.Lerp(minSpinSpeed, maxSpinSpeed, t);
                }
                break;

            case SpinSpeedMode.RandomSmooth:
                randomTimer -= Time.deltaTime;

                if (randomTimer <= 0f)
                {
                    randomTimer = randomTargetChangeInterval;
                    targetSpinSpeed = Random.Range(minSpinSpeed, maxSpinSpeed);
                }

                float spinStep = spinChangeSpeed * Mathf.Abs(maxSpinSpeed - minSpinSpeed) * Time.deltaTime;
                currentSpinSpeed = Mathf.MoveTowards(currentSpinSpeed, targetSpinSpeed, spinStep);
                break;
        }

        float direction = clockwise ? -1f : 1f;
        transform.Rotate(0f, 0f, direction * currentSpinSpeed * Time.deltaTime);
    }

    private void UpdateMovement()
    {
        if (movementMode == PathMovementMode.None || pathPoints == null || pathPoints.Length == 0)
            return;

        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            currentMoveSpeed = Mathf.MoveTowards(currentMoveSpeed, 0f, moveAcceleration * Time.deltaTime);
            return;
        }

        Transform targetPoint = pathPoints[currentPointIndex];

        if (targetPoint == null)
        {
            SetNextPoint();
            return;
        }

        Vector3 currentPosition = transform.position;
        Vector3 targetPosition = targetPoint.position;

        float distanceToTarget = Vector3.Distance(currentPosition, targetPosition);

        if (distanceToTarget <= stopDistance)
        {
            transform.position = targetPosition;
            currentMoveSpeed = 0f;
            waitTimer = waitAtPoint;
            SetNextPoint();
            return;
        }

        float desiredSpeed = maxMoveSpeed;

        if (distanceToTarget < slowDownDistance)
        {
            float t = distanceToTarget / slowDownDistance;
            desiredSpeed = Mathf.Lerp(0f, maxMoveSpeed, t);
            desiredSpeed = Mathf.Max(desiredSpeed, 0.15f);
        }

        currentMoveSpeed = Mathf.MoveTowards(
            currentMoveSpeed,
            desiredSpeed,
            moveAcceleration * Time.deltaTime
        );

        Vector3 direction = (targetPosition - currentPosition).normalized;
        transform.position += direction * currentMoveSpeed * Time.deltaTime;

        if (Vector3.Distance(transform.position, targetPosition) <= stopDistance)
        {
            transform.position = targetPosition;
            currentMoveSpeed = 0f;
            waitTimer = waitAtPoint;
            SetNextPoint();
        }
    }

    private void SetNextPoint()
    {
        if (pathPoints == null || pathPoints.Length <= 1)
            return;

        switch (movementMode)
        {
            case PathMovementMode.Loop:
                currentPointIndex = (currentPointIndex + 1) % pathPoints.Length;
                break;

            case PathMovementMode.PingPong:
                if (currentPointIndex >= pathPoints.Length - 1)
                    pathDirection = -1;
                else if (currentPointIndex <= 0)
                    pathDirection = 1;

                currentPointIndex += pathDirection;
                currentPointIndex = Mathf.Clamp(currentPointIndex, 0, pathPoints.Length - 1);
                break;

            case PathMovementMode.Once:
                if (currentPointIndex < pathPoints.Length - 1)
                    currentPointIndex++;
                break;
        }
    }

    private float GetInitialSpinSpeed()
    {
        switch (spinSpeedMode)
        {
            case SpinSpeedMode.Constant:
                return constantSpinSpeed;

            case SpinSpeedMode.RandomSmooth:
                return Random.Range(minSpinSpeed, maxSpinSpeed);

            default:
                return minSpinSpeed;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!damageEnabled || !useTrigger)
            return;

        TryHit(other.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!damageEnabled || useTrigger)
            return;

        TryHit(collision.gameObject);
    }

    private void TryHit(GameObject target)
    {
        if (!sawActive)
            return;

        if (!target.CompareTag(playerTag))
            return;

        PlayerMovement player = target.GetComponent<PlayerMovement>();
        if (player == null)
            return;

        if (nextHitTime.TryGetValue(player, out float nextTime) && Time.time < nextTime)
            return;

        player.TakeDamage(damage, transform.position, knockbackX, knockbackY);
        nextHitTime[player] = Time.time + hitCooldown;
    }

    public void AppearFromTrigger()
    {
        CancelInvoke(nameof(DisappearNow));

        if (appearDelay <= 0f)
        {
            AppearNow();
        }
        else
        {
            Invoke(nameof(AppearNow), appearDelay);
        }
    }

    public void DisappearFromTrigger()
    {
        if (!disappearAfterTriggerExit)
            return;

        CancelInvoke(nameof(AppearNow));

        if (disappearDelay <= 0f)
        {
            DisappearNow();
        }
        else
        {
            Invoke(nameof(DisappearNow), disappearDelay);
        }
    }

    private void AppearNow()
    {
        if (resetToStartPointOnAppear)
            ResetToStartPoint();

        SetSawActive(true);

        if (showDebugMessages)
            Debug.Log("SawSpin2D: пила появилась.");
    }

    private void DisappearNow()
    {
        SetSawActive(false);

        if (showDebugMessages)
            Debug.Log("SawSpin2D: пила исчезла.");
    }

    private void SetSawActive(bool active)
    {
        sawActive = active;

        Transform root = sawBody != null ? sawBody.transform : transform;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = active;
        }

        Collider2D[] colliders = root.GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = active;
        }
    }

    private void ResetToStartPoint()
    {
        if (pathPoints == null || pathPoints.Length == 0)
            return;

        currentPointIndex = Mathf.Clamp(startPointIndex, 0, pathPoints.Length - 1);
        pathDirection = 1;
        waitTimer = 0f;
        currentMoveSpeed = 0f;

        if (pathPoints[currentPointIndex] != null)
            transform.position = pathPoints[currentPointIndex].position;
    }

    private void OnDrawGizmos()
    {
        if (pathPoints == null || pathPoints.Length == 0)
            return;

        Gizmos.color = Color.yellow;

        for (int i = 0; i < pathPoints.Length; i++)
        {
            if (pathPoints[i] == null)
                continue;

            Gizmos.DrawSphere(pathPoints[i].position, 0.12f);

            if (i < pathPoints.Length - 1 && pathPoints[i + 1] != null)
            {
                Gizmos.DrawLine(pathPoints[i].position, pathPoints[i + 1].position);
            }
        }

        if (movementMode == PathMovementMode.Loop &&
            pathPoints.Length > 1 &&
            pathPoints[0] != null &&
            pathPoints[pathPoints.Length - 1] != null)
        {
            Gizmos.DrawLine(pathPoints[pathPoints.Length - 1].position, pathPoints[0].position);
        }
    }
}