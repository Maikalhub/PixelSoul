using System.Collections;
using UnityEngine;

public class BreakTile : MonoBehaviour
{
    [Header("Tiles Settings")]
    public GameObject[] tiles;             // Массив дочерних объектов
    public float disappearDelay = 0.5f;    // Задержка перед сменой слоя
    public float fadeDuration = 2f;        // Время исчезновения
    public string brokenLayer = "Platform";  // Новый Sorting Layer после удара

    [Header("Collision Settings")]
    public Collider2D attackCollider;      // Коллайдер атаки игрока

    [Header("Sound Settings")]
    public AudioSource audioSource;        // Сюда ставим AudioSource через инспектор
    public AudioClip breakSound;           // Звук разрушения
    public float volume = 1f;              // Громкость

    private bool isBroken = false;         // Чтобы срабатывало только один раз

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isBroken) return;

        if (collision == attackCollider)
        {
            isBroken = true;
            PlayBreakSound();
            Break(collision.gameObject);
        }
    }

    private void PlayBreakSound()
    {
        if (audioSource != null && breakSound != null)
        {
            audioSource.PlayOneShot(breakSound, volume);
        }
        else if (breakSound != null)
        {
            Debug.LogWarning("BreakTile: AudioSource не назначен!");
        }
    }

    public void Break(GameObject attackObject)
    {
        StartCoroutine(BreakTiles(attackObject));
    }

    private IEnumerator BreakTiles(GameObject attackObject)
    {
        foreach (GameObject tile in tiles)
        {
            Rigidbody2D rb = tile.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;

                Vector2 direction = (tile.transform.position - attackObject.transform.position).normalized;
                Vector2 force = direction * Random.Range(1f, 3f) + Vector2.up * Random.Range(1f, 2f);
                rb.AddForce(force, ForceMode2D.Impulse);

                rb.AddTorque(Random.Range(-200f, 200f));
            }

            SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                StartCoroutine(ChangeLayerAndFade(sr));
            }
        }

        // Ждём fadeDuration + disappearDelay и потом удаляем родителя
        yield return new WaitForSeconds(disappearDelay + fadeDuration);
        Destroy(gameObject); // удаляем весь родительский объект
    }

    private IEnumerator ChangeLayerAndFade(SpriteRenderer sr)
    {
        yield return new WaitForSeconds(disappearDelay);

        sr.sortingLayerName = brokenLayer;

        float elapsed = 0f;
        Color originalColor = sr.color;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, Mathf.Lerp(1f, 0f, elapsed / fadeDuration));
            yield return null;
        }

        // НЕ удаляем дочерние объекты по одному
        // Destroy(sr.gameObject); <-- больше не нужно
    }
}