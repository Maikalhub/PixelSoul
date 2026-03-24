using UnityEngine;

public class LevelTransitionTrigger : MonoBehaviour
{
    public LevelManager levelManager;

    [Header("Side A")]
    public Transform pointA;
    public Collider2D confinerA;

    [Header("Side B")]
    public Transform pointB;
    public Collider2D confinerB;

    [Header("Settings")]
    public float cooldown = 0.5f;

    private bool canTrigger = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || !canTrigger)
            return;

        canTrigger = false;

        float distToA = Vector2.Distance(other.transform.position, pointA.position);
        float distToB = Vector2.Distance(other.transform.position, pointB.position);

        // 🔁 Определяем направление
        if (distToA < distToB)
        {
            levelManager.StartTransition(pointB, confinerB);
        }
        else
        {
            levelManager.StartTransition(pointA, confinerA);
        }

        Invoke(nameof(ResetTrigger), cooldown);
    }

    private void ResetTrigger()
    {
        canTrigger = true;
    }
}