using UnityEngine;

public class BouncePlatform : MonoBehaviour
{
    [Header("����� �������������")]
    public BounceMode bounceMode = BounceMode.Vertical;
    public enum BounceMode
    {
        Vertical,        // ������� ������������ ������
        Directional,      // ������������� � ������������ �����������
        LaunchPad,        // ��� �������� ��������� (������� ���������)
        VariableJump      // ������� �� ����, ��� ����� ����� ����� �� ���������
    }

    [Header("�������� ���������")]
    public float bounceForce = 15f;        // ���� �������������
    public bool resetVelocityOnBounce = true; // ���������� �� ������� �������� ������
    public LayerMask playerLayer;           // ���� ������

    [Header("��������� ��� Directional ������")]
    public Vector2 launchDirection = new Vector2(1, 1); // ����������� �������������
    public bool normalizeDirection = true;   // ������������� �� �����������

    [Header("��������� ��� VariableJump ������")]
    public float minBounceForce = 5f;        // ����������� ���� ������
    public float maxBounceForce = 25f;       // ������������ ���� ������
    public float chargeTime = 1.5f;           // ����� ��� ������������� ������

    [Header("���������� � �������� �������")]
    public GameObject bounceEffect;           // Particle effect ��� �������������
    public AudioClip bounceSound;             // ���� �������������
    public Animator animator;                  // �������� ���������
    public string bounceTriggerName = "Bounce"; // ��� �������� ��������

    [Header("�������������� ���������")]
    public bool oneTimeUse = false;           // ����������� �������������
    public float cooldownTime = 0.5f;          // ����� �����������
    public bool destroyOnUse = false;          // ���������� ����� �������������

    // ��������� ����������
    private bool isCharging = false;
    private float chargeStartTime;
    private bool canBounce = true;
    private Rigidbody2D playerRb;
    private PlayerMovement playerController; // ���� ���� ���� ���������� ������
    private Collider2D platformCollider;

    void Start()
    {
        platformCollider = GetComponent<Collider2D>();
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        // ��� ������ ����������� ������
        if (bounceMode == BounceMode.VariableJump && isCharging && playerRb != null)
        {
            // ���������� ��������� ������ (�����������)
            float chargePercent = Mathf.Clamp01((Time.time - chargeStartTime) / chargeTime);
            // ����� ������ ���� ���������, ������ � �.�.
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!canBounce) return;

        // ���������, ��� ��� ����� � �� ����������� ������ �� ���������
        if (collision.gameObject.CompareTag("Player") && IsPlayerLandingOnTop(collision))
        {
            HandleBounce(collision);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // ��� ������ ����������� ������ - �������� �����, ���� ����� ����� �� ���������
        if (bounceMode == BounceMode.VariableJump &&
            collision.gameObject.CompareTag("Player") &&
            IsPlayerLandingOnTop(collision) &&
            !isCharging && canBounce)
        {
            StartCharging(collision);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (bounceMode == BounceMode.VariableJump &&
            collision.gameObject.CompareTag("Player") &&
            isCharging)
        {
            // ����� ������� ��������� - ��������� ������ � ����������� �����
            ExecuteVariableBounce(collision);
            isCharging = false;
        }
    }

    void HandleBounce(Collision2D collision)
    {
        playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
        if (playerRb == null) return;

        // ���������� ������������ �������� ��� ����� ����������� ������
        if (resetVelocityOnBounce)
        {
            playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 0);
        }

        // ��������� ���� � ����������� �� ������
        switch (bounceMode)
        {
            case BounceMode.Vertical:
                playerRb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);
                break;

            case BounceMode.Directional:
                Vector2 direction = launchDirection;
                if (normalizeDirection)
                    direction = launchDirection.normalized;
                playerRb.AddForce(direction * bounceForce, ForceMode2D.Impulse);
                break;

            case BounceMode.LaunchPad:
                // ����� ������� ��������� � ����������� �������������� ��������
                playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x * 1.5f, bounceForce);
                break;
        }

        // �������
        PlayBounceEffects();

        // �������� �� �������������
        if (oneTimeUse)
        {
            canBounce = false;
        }

        // �����������
        if (cooldownTime > 0 && !oneTimeUse)
        {
            StartCoroutine(BounceCooldown());
        }

        // �����������
        if (destroyOnUse)
        {
            Destroy(gameObject);
        }
    }

    void StartCharging(Collision2D collision)
    {
        isCharging = true;
        chargeStartTime = Time.time;
        playerRb = collision.gameObject.GetComponent<Rigidbody2D>();

        // �������� ��������� ������, ���� ����
        playerController = collision.gameObject.GetComponent<PlayerMovement>();

        // ����� ���������� ��������� ������
        Debug.Log("������ ������� ������!");
    }

    [System.Obsolete]
    void ExecuteVariableBounce(Collision2D collision)
    {
        float chargeDuration = Time.time - chargeStartTime;
        float chargePercent = Mathf.Clamp01(chargeDuration / chargeTime);
        float currentBounceForce = Mathf.Lerp(minBounceForce, maxBounceForce, chargePercent);

        // ��������� ����
        if (playerRb != null)
        {
            playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 0);
            playerRb.AddForce(Vector2.up * currentBounceForce, ForceMode2D.Impulse);

            Debug.Log($"���������� ������: {chargePercent * 100}% ���� = {currentBounceForce}");
        }

        PlayBounceEffects();
    }

    bool IsPlayerLandingOnTop(Collision2D collision)
    {
        // ���������, ��� ����� ����������� ������
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y < -0.5f) // ������� ���������� ���� - ������ ����� ������
            {
                return true;
            }
        }
        return false;
    }

    void PlayBounceEffects()
    {
        // Particle effect
        if (bounceEffect != null)
        {
            Instantiate(bounceEffect, transform.position, Quaternion.identity);
        }

        // ����
        if (bounceSound != null && GetComponent<AudioSource>() != null)
        {
            GetComponent<AudioSource>().PlayOneShot(bounceSound);
        }

        // ��������
        if (animator != null && !string.IsNullOrEmpty(bounceTriggerName))
        {
            animator.SetTrigger(bounceTriggerName);
        }
    }

    System.Collections.IEnumerator BounceCooldown()
    {
        canBounce = false;
        yield return new WaitForSeconds(cooldownTime);
        canBounce = true;
    }

    // ������������ ����������� � ���������
    private void OnDrawGizmosSelected()
    {
        if (bounceMode == BounceMode.Directional)
        {
            Gizmos.color = Color.red;
            Vector2 direction = launchDirection.normalized;
            Gizmos.DrawRay(transform.position, direction * 2f);

            // ������ �������
            Vector2 right = Quaternion.Euler(0, 0, 30) * -direction;
            Vector2 left = Quaternion.Euler(0, 0, -30) * -direction;
            Gizmos.DrawRay(transform.position + (Vector3)direction * 2f, right * 0.5f);
            Gizmos.DrawRay(transform.position + (Vector3)direction * 2f, left * 0.5f);
        }
    }
}