using System.Collections;
using UnityEngine;

public class GhostTrail : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement player;
    [SerializeField] private SpriteRenderer playerSprite;

    [Header("Ghost Settings")]
    [SerializeField] private Transform ghostsParent;
    [SerializeField] private float ghostInterval = 0.05f;
    [SerializeField] private float fadeTime = 0.25f;

    [Header("Colors")]
    [SerializeField] private Color trailColor = new Color(1, 1, 1, 0.6f);
    [SerializeField] private Color fadeColor = new Color(1, 1, 1, 0f);

    private SpriteRenderer[] ghostSprites;
    private bool isRunning;

    private void Awake()
    {
        ghostSprites = new SpriteRenderer[ghostsParent.childCount];

        for (int i = 0; i < ghostsParent.childCount; i++)
        {
            ghostSprites[i] = ghostsParent.GetChild(i).GetComponent<SpriteRenderer>();
            ghostSprites[i].color = fadeColor;
        }
    }

    public void PlayTrail()
    {
        if (isRunning) return;
        StartCoroutine(TrailCoroutine());
    }

    private IEnumerator TrailCoroutine()
    {
        isRunning = true;

        for (int i = 0; i < ghostSprites.Length; i++)
        {
            SpriteRenderer ghost = ghostSprites[i];

            ghost.transform.position = player.transform.position;
            ghost.sprite = playerSprite.sprite;
            ghost.flipX = playerSprite.flipX;
            ghost.color = trailColor;

            StartCoroutine(FadeGhost(ghost));

            yield return new WaitForSeconds(ghostInterval);
        }

        isRunning = false;
    }

    private IEnumerator FadeGhost(SpriteRenderer ghost)
    {
        float t = 0f;
        Color startColor = trailColor;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            ghost.color = Color.Lerp(startColor, fadeColor, t / fadeTime);
            yield return null;
        }

        ghost.color = fadeColor;
    }
}
