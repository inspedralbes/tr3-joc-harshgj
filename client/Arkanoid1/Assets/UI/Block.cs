using UnityEngine;

public class Block : MonoBehaviour
{
    private bool isDestroyed;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDestroyed || !collision.gameObject.CompareTag("Ball"))
            return;

        isDestroyed = true;

        GameManager manager = FindFirstObjectByType<GameManager>();
        if (manager != null)
        {
            manager.NotifyBlockDestroyed();
        }

        Destroy(gameObject);
    }
}
