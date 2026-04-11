using UnityEngine;

public class DeathZone : MonoBehaviour
{
    public GameManager gameManager;
    private bool ballHandled;

    void Awake()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        TryHandleBall(collision);
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        TryHandleBall(collision);
    }

    void TryHandleBall(Collider2D collision)
    {
        if (ballHandled || !collision.CompareTag("Ball") || gameManager == null)
            return;

        ballHandled = true;
        gameManager.LoseLife();
    }

    void LateUpdate()
    {
        ballHandled = false;
    }
}
