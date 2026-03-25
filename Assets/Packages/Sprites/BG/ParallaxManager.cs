using UnityEngine;

[System.Serializable]
public class ParallaxLayer
{
    public GameObject obj;        // Объект
    [Range(0f, 2f)]
    public float parallaxFactor = 0.5f; // Сила параллакса (индивидуально)
}

public class ParallaxManager : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Parallax Layers")]
    public ParallaxLayer[] layers;

    [Header("Settings")]
    public float smoothing = 0.1f;

    private Vector3 previousPlayerPosition;

    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;

        previousPlayerPosition = player.position;
    }

    void LateUpdate()
    {
        Vector3 deltaMovement = player.position - previousPlayerPosition;

        foreach (ParallaxLayer layer in layers)
        {
            if (layer.obj == null) continue;

            float moveX = deltaMovement.x * layer.parallaxFactor;

            Vector3 targetPosition = layer.obj.transform.position + new Vector3(moveX, 0f, 0f);

            layer.obj.transform.position = Vector3.Lerp(
                layer.obj.transform.position,
                targetPosition,
                smoothing
            );
        }

        previousPlayerPosition = player.position;
    }
}