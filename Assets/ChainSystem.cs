using System.Collections.Generic;
using UnityEngine;

public class ChainSystem : MonoBehaviour
{
    [Header("Chain Settings")]
    public GameObject linkPrefab;
    public int chainLength = 10;
    public float distance = 0.5f;

    [Header("Physics")]
    public float swingForce = 50f;

    private List<Rigidbody2D> links = new List<Rigidbody2D>();

    void Start()
    {
        GenerateChain();
        Invoke(nameof(StartSwing), 0.2f);
    }

    // 🔘 КНОПКА В ИНСПЕКТОРЕ
    [ContextMenu("Generate Chain")]
    public void GenerateChain()
    {
        ClearChain();

        Rigidbody2D previousRB = null;
        links = new List<Rigidbody2D>();

        for (int i = 0; i < chainLength; i++)
        {
            GameObject link = Instantiate(
                linkPrefab,
                transform.position + Vector3.down * i * distance,
                Quaternion.identity,
                transform
            );

            Rigidbody2D rb = link.GetComponent<Rigidbody2D>();
            HingeJoint2D joint = link.GetComponent<HingeJoint2D>();

            if (i == 0)
            {
                rb.bodyType = RigidbodyType2D.Static;
            }
            else
            {
                joint.connectedBody = previousRB;
            }

            links.Add(rb);
            previousRB = rb;
        }
    }

    // 🧹 Очистка
    [ContextMenu("Clear Chain")]
    public void ClearChain()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(transform.GetChild(i).gameObject);
            else
                DestroyImmediate(transform.GetChild(i).gameObject);
        }

        links.Clear();
    }

    void StartSwing()
    {
        if (links.Count > 1)
        {
            links[1].AddForce(Vector2.right * swingForce, ForceMode2D.Impulse);
        }
    }
}