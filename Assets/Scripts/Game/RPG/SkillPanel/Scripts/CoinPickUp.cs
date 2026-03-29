using UnityEngine;

public class CoinPickUp : MonoBehaviour
{
    public CoinSystem coin;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Trigger detected");

        if (collision.CompareTag("Player"))
        {
            Debug.Log("Player picked item");

            coin.AddCoins(1);
            Destroy(gameObject);
        }
    }
}


