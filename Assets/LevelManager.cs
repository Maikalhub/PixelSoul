using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    [Header("Player & Camera")]
    public Transform player;
    public CinemachineCamera virtualCamera;
    public CinemachineConfiner2D confiner;

    [Header("Fade Settings")]
    public float fadeDuration = 1f;
    public Image fadeImage;

    [Header("Collectibles")]
    public List<Collider2D> collectibles;

    [Header("Key UI")]
    public List<Image> keyImages;
    public Color collectedColor = Color.gray;
    public Color defaultColor = Color.white;

    [HideInInspector]
    public bool canTransition = false;

    private bool isTransitioning = false;

    void Start()
    {
        if (fadeImage != null)
            fadeImage.gameObject.SetActive(false);

        ResetKeyUI();
        UpdateTransitionStatus();
    }

    // Сбор предмета
    public void CollectItem(Collider2D item)
    {
        int index = collectibles.IndexOf(item);

        if (index >= 0)
        {
            collectibles.RemoveAt(index);
            item.gameObject.SetActive(false);

            // Обновляем UI
            if (index < keyImages.Count)
                keyImages[index].color = collectedColor;

            UpdateTransitionStatus();
        }
    }

    // Проверка, можно ли переходить
    private void UpdateTransitionStatus()
    {
        canTransition = collectibles.Count == 0;
    }

    // ЕДИНСТВЕННЫЙ метод перехода
    public void StartTransition(Transform newPosition, Collider2D newConfiner)
    {
        if (!canTransition)
        {
            Debug.Log("Collect all items first!");
            return;
        }

        if (!isTransitioning)
            StartCoroutine(TransitionLevel(newPosition, newConfiner));
    }

    private IEnumerator TransitionLevel(Transform newPosition, Collider2D newConfiner)
    {
        isTransitioning = true;

        // 🔹 Сбрасываем UI ключей
        ResetKeyUI();

        // 🔵 Fade IN
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            Color c = fadeImage.color;
            float t = 0;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                c.a = Mathf.Lerp(0, 1, t / fadeDuration);
                fadeImage.color = c;
                yield return null;
            }
        }

        // 🚀 Телепорт игрока
        player.position = newPosition.position;

        // 🎥 Смена confiner
        if (confiner != null && newConfiner != null)
        {
            confiner.BoundingShape2D = newConfiner;
            confiner.InvalidateBoundingShapeCache();
        }

        // 🔵 Fade OUT
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            float t = 0;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                c.a = Mathf.Lerp(1, 0, t / fadeDuration);
                fadeImage.color = c;
                yield return null;
            }
            fadeImage.gameObject.SetActive(false);
        }

        isTransitioning = false;
    }

    // Сброс UI ключей
    private void ResetKeyUI()
    {
        foreach (var img in keyImages)
        {
            if (img != null)
                img.color = defaultColor;
        }
    }
}