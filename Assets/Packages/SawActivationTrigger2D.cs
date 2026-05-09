using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SawActivationTrigger2D : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    [Header("Saws To Appear")]
    [SerializeField] private SawSpin2D[] sawsToAppear;

    [Header("Saws To Disappear")]
    [SerializeField] private SawSpin2D[] sawsToDisappear;

    [Header("Objects To Appear On Trigger")]
    [SerializeField] private GameObject[] objectsToAppear;

    [Header("Objects To Disappear On Trigger")]
    [SerializeField] private GameObject[] objectsToDisappear;

    [Header("Trigger Behaviour")]
    [SerializeField] private bool activateOnEnter = true;
    [SerializeField] private bool deactivateOnExit = false;

    [Header("Reverse Objects On Exit")]
    [SerializeField] private bool reverseObjectsOnExit = false;

    [Header("Debug")]
    [SerializeField] private bool showDebugMessages = false;

    private bool triggeredOnce = false;

    private void Reset()
    {
        Collider2D triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;
    }

    private void Awake()
    {
        Collider2D triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!activateOnEnter)
            return;

        if (!other.CompareTag(playerTag))
            return;

        TriggerEnterActions();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!deactivateOnExit)
            return;

        if (!other.CompareTag(playerTag))
            return;

        TriggerExitActions();
    }

    private void TriggerEnterActions()
    {
        triggeredOnce = true;

        AppearSaws(sawsToAppear);
        DisappearSaws(sawsToDisappear);

        SetObjectsActive(objectsToAppear, true);
        SetObjectsActive(objectsToDisappear, false);

        if (showDebugMessages)
            Debug.Log("SawActivationTrigger2D: trigger enter actions выполнены.");
    }

    private void TriggerExitActions()
    {
        DisappearSaws(sawsToAppear);
        AppearSaws(sawsToDisappear);

        if (reverseObjectsOnExit)
        {
            SetObjectsActive(objectsToAppear, false);
            SetObjectsActive(objectsToDisappear, true);
        }

        if (showDebugMessages)
            Debug.Log("SawActivationTrigger2D: trigger exit actions выполнены.");
    }

    private void AppearSaws(SawSpin2D[] saws)
    {
        if (saws == null || saws.Length == 0)
            return;

        foreach (SawSpin2D saw in saws)
        {
            if (saw == null)
                continue;

            saw.AppearFromTrigger();
        }
    }

    private void DisappearSaws(SawSpin2D[] saws)
    {
        if (saws == null || saws.Length == 0)
            return;

        foreach (SawSpin2D saw in saws)
        {
            if (saw == null)
                continue;

            saw.DisappearFromTrigger();
        }
    }

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null || objects.Length == 0)
            return;

        foreach (GameObject obj in objects)
        {
            if (obj == null)
                continue;

            obj.SetActive(active);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = triggeredOnce ? Color.green : Color.cyan;

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.offset, box.size);
            return;
        }

        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        if (circle != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireSphere(circle.offset, circle.radius);
            return;
        }

        CapsuleCollider2D capsule = GetComponent<CapsuleCollider2D>();
        if (capsule != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(capsule.offset, capsule.size);
            return;
        }

        Gizmos.DrawWireSphere(transform.position, 0.4f);
    }
}