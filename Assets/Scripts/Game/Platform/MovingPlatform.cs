using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [System.Serializable]
    public class MovementPattern
    {
        public enum PatternType { Horizontal, Vertical, Diagonal, Circular, Waypoints }

        public PatternType patternType;
        public float range = 5f;
        public float speed = 2f;
        public float angle = 45f; // Для диагонального движения
    }

    [Header("Основные настройки")]
    public MovementPattern movementPattern;
    public bool usePhysics = false; // Использовать физику для плавного движения
    public float smoothTime = 0.3f;

    [Header("Настройки точек (для Waypoints)")]
    public Transform[] waypoints;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Vector3 velocity = Vector3.zero;
    private int currentWaypoint = 0;
    private int direction = 1;
    private float angle;
    private float timer;

    void Start()
    {
        startPosition = transform.position;
        angle = movementPattern.angle * Mathf.Deg2Rad;
    }

    void FixedUpdate()
    {
        if (usePhysics)
        {
            SmoothMove();
        }
        else
        {
            MovePlatform();
        }
    }

    void MovePlatform()
    {
        switch (movementPattern.patternType)
        {
            case MovementPattern.PatternType.Horizontal:
                HorizontalMovement();
                break;
            case MovementPattern.PatternType.Vertical:
                VerticalMovement();
                break;
            case MovementPattern.PatternType.Diagonal:
                DiagonalMovement();
                break;
            case MovementPattern.PatternType.Circular:
                CircularMovement();
                break;
            case MovementPattern.PatternType.Waypoints:
                WaypointMovement();
                break;
        }
    }

    void HorizontalMovement()
    {
        float x = startPosition.x + Mathf.PingPong(Time.time * movementPattern.speed, movementPattern.range * 2) - movementPattern.range;
        transform.position = new Vector3(x, transform.position.y, transform.position.z);
    }

    void VerticalMovement()
    {
        float y = startPosition.y + Mathf.PingPong(Time.time * movementPattern.speed, movementPattern.range * 2) - movementPattern.range;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }

    void DiagonalMovement()
    {
        float t = Mathf.PingPong(Time.time * movementPattern.speed, movementPattern.range * 2) - movementPattern.range;

        float x = startPosition.x + t * Mathf.Cos(angle);
        float y = startPosition.y + t * Mathf.Sin(angle);

        transform.position = new Vector3(x, y, transform.position.z);
    }

    void CircularMovement()
    {
        timer += Time.deltaTime * movementPattern.speed;

        float x = startPosition.x + Mathf.Cos(timer) * movementPattern.range;
        float y = startPosition.y + Mathf.Sin(timer) * movementPattern.range;

        transform.position = new Vector3(x, y, transform.position.z);
    }

    void WaypointMovement()
    {
        if (waypoints.Length == 0) return;

        Transform target = waypoints[currentWaypoint];
        transform.position = Vector3.MoveTowards(transform.position, target.position, movementPattern.speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            currentWaypoint += direction;

            if (currentWaypoint >= waypoints.Length || currentWaypoint < 0)
            {
                direction *= -1;
                currentWaypoint += direction;
            }
        }
    }

    void SmoothMove()
    {
        Vector3 targetPos = CalculateTargetPosition();
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
    }

    Vector3 CalculateTargetPosition()
    {
        // Аналогично MovePlatform, но возвращает позицию вместо установки
        switch (movementPattern.patternType)
        {
            case MovementPattern.PatternType.Horizontal:
                float x = startPosition.x + Mathf.PingPong(Time.time * movementPattern.speed, movementPattern.range * 2) - movementPattern.range;
                return new Vector3(x, transform.position.y, transform.position.z);
            // ... аналогично для других типов
            default:
                return transform.position;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.transform.SetParent(transform);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.transform.SetParent(null);
        }
    }
}