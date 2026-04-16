using UnityEngine;

public class Block : MonoBehaviour
{
    private bool isDestroyed;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDestroyed || !collision.gameObject.CompareTag("Ball"))
            return;

        isDestroyed = true;
        GetComponent<Collider2D>().enabled = false;
        gameObject.SetActive(false);

        GameManager manager = FindFirstObjectByType<GameManager>();
        ArkanoidAgent agent = FindFirstObjectByType<ArkanoidAgent>();

        if (manager != null)
            manager.NotifyBlockDestroyed();

        if (agent != null)
            agent.NotifyBlockBroken();
    }

    public void ResetBlock()
    {
        isDestroyed = false;
    }
}
