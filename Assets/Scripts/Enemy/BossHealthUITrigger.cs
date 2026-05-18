using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BossHealthUITrigger : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    [Header("Boss Health UI")]
    [SerializeField] private EnemyHealthUI bossHealthUI;

    [Header("Behaviour")]
    [SerializeField] private bool activateOnce = true;
    [SerializeField] private bool disableTriggerAfterActivation = true;

    private bool wasActivated;

    private void Awake()
    {
        Collider2D triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (wasActivated && activateOnce)
            return;

        if (!other.CompareTag(playerTag))
            return;

        if (bossHealthUI == null)
        {
            Debug.LogWarning("BossHealthUITrigger: Boss Health UI is not assigned.");
            return;
        }

        wasActivated = true;

        bossHealthUI.ActivateCanvasDisplay();

        if (disableTriggerAfterActivation)
            gameObject.SetActive(false);
    }
}
