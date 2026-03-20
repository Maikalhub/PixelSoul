using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemData item;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Trigger detected");

        if (collision.CompareTag("Player"))
        {
            Debug.Log("Player picked item");

            Inventory.Instance.AddItem(item);
            Destroy(gameObject);
        }
    }
}