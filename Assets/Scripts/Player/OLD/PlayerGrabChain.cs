using UnityEngine;

public class PlayerGrabChain : MonoBehaviour
{
    public float swingForce = 10f;
    public float grabRadius = 1f; // радиус поиска ближайшего звена цепи

    private Rigidbody2D rb;
    private HingeJoint2D grabJoint;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        grabJoint = gameObject.AddComponent<HingeJoint2D>();
        grabJoint.enabled = false;

        Debug.Log("PlayerGrabChain: скрипт запущен, joint создан");
    }

    void Update()
    {
        // Нажатие кнопки "хвататься"
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryGrab();
        }

        // Отпустить
        if (Input.GetKeyUp(KeyCode.E))
        {
            Release();
        }

        // Движение по цепи
        if (grabJoint.enabled)
        {
            float h = Input.GetAxis("Horizontal");
            rb.AddForce(Vector2.right * h * swingForce);
            if (Mathf.Abs(h) > 0.01f)
                Debug.Log($"PlayerGrabChain: раскачивание, сила {h * swingForce}");
        }
    }

    void TryGrab()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, grabRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("ChainLink"))
            {
                grabJoint.connectedBody = hit.attachedRigidbody;
                grabJoint.autoConfigureConnectedAnchor = false;

                // Верх цепного звена (half-height коллайдера)
                float linkHeight = hit.bounds.size.y;
                grabJoint.connectedAnchor = new Vector2(0, linkHeight / 2f);

                grabJoint.enabled = true;

                Debug.Log($"PlayerGrabChain: зацеплено за {hit.name} на высоте {linkHeight / 2f}");
                return;
            }
        }

        Debug.Log("PlayerGrabChain: рядом нет цепного звена для хвата");
    }

    void Release()
    {
        if (grabJoint.enabled)
        {
            grabJoint.enabled = false;
            Debug.Log("PlayerGrabChain: отпустил цепь");
        }
    }

    // Для визуализации радиуса хвата в Scene
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, grabRadius);
    }
}