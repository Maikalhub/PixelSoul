using UnityEngine;

public class FloatingCoin : MonoBehaviour
{ 
    [Header("Настройки покачивания")]
    [SerializeField] private float floatAmplitude = 0.5f; // Амплитуда (высота) покачивания
    [SerializeField] private float floatFrequency = 1f; // Частота покачивания (скорость)

    private Vector3 startPosition;
    private float randomOffset; // Для разнообразия, если много монет

    void Start()
    {
        // Запоминаем начальную позицию
        startPosition = transform.position;
        // Случайное смещение, чтобы монеты не двигались синхронно
        randomOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {

        // Движение вверх-вниз (используем синусоиду)
        float newY = startPosition.y + Mathf.Sin(Time.time * floatFrequency + randomOffset) * floatAmplitude;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }
}