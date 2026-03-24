using UnityEngine;

public class Collectible : MonoBehaviour
{
    public LevelManager levelManager;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            levelManager.CollectItem(GetComponent<Collider2D>());
            Debug.Log("Collectible triggered by Player: " + gameObject.name);
        }
    }
}