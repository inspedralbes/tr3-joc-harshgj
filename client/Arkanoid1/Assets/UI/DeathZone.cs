using UnityEngine;

public class DeathZone : MonoBehaviour
{
    public GameManager gameManager;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Ball"))
        {
            gameManager.LoseLife();
        }
    }
}
