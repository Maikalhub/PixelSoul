using UnityEngine;

public class KeyPickUp : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (LevelManager.Instance == null) return;

        LevelManager.Instance.CollectKey(gameObject);
    }
}