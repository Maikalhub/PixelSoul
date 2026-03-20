using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class LinkButton : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public bool randomDirection = true;
    public bool moveRight = true;

    [Header("Lifetime")]
    public float lifeTime = 8f;
    public float fadeDuration = 1.5f;
    public float respawnDelay = 2f;

    [Header("Link")]
    public string url = "https://example.com";

    private Rigidbody2D rb;
    private SpriteRenderer sr;

    private Vector3 startPosition;
    private Color startColor;
    private Vector3 startScale;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        startPosition = transform.position;
        startColor = sr.color;
        startScale = transform.localScale;
    }

    private void OnEnable()
    {
        ResetButton();
        StartCoroutine(LifeCycle());
    }

    private void FixedUpdate()
    {
        float dir = moveRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
    }

    private IEnumerator LifeCycle()
    {
        yield return new WaitForSeconds(lifeTime);
        yield return FadeOut();

        rb.simulated = false;
        yield return new WaitForSeconds(respawnDelay);

        ResetButton();
        StartCoroutine(LifeCycle());
    }

    private IEnumerator FadeOut()
    {
        float t = 0f;
        Color c = sr.color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(startColor.a, 0f, t / fadeDuration);
            sr.color = c;
            yield return null;
        }
    }

    private void ResetButton()
    {
        transform.position = startPosition;
        transform.localScale = startScale;

        sr.color = startColor;
        rb.simulated = true;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        if (randomDirection)
            moveRight = Random.value > 0.5f;
    }

    private void OnMouseDown()
    {
        if (!string.IsNullOrEmpty(url))
            Application.OpenURL(url);
    }
}
