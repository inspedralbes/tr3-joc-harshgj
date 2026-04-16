using UnityEngine;
using UnityEngine.SceneManagement;

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
        gameManager.NotifyTrainingBallLost();

        if (!IsTrainingScene())
            gameManager.LoseLife();
    }

    bool IsTrainingScene()
    {
        return SceneManager.GetActiveScene().name == "TrainingScene";
    }

    void LateUpdate()
    {
        ballHandled = false;
    }
}
