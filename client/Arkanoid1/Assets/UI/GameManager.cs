using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public int lives = 4;

    [Header("UI Elements")]
    public TextMeshProUGUI livesText;

    [Header("Ball Settings")]
    public GameObject ballObject;
    public Transform ballStart;

    void Start()
    {
        // Ensure we have a ball assigned
        if (ballObject == null)
        {
            Debug.LogError("BallObject is not assigned in GameManager!");
            return;
        }

        // Ensure we have a TextMeshProUGUI assigned
        if (livesText == null)
            Debug.LogWarning("LivesText is not assigned in GameManager!");

        UpdateLivesUI();
    }

    public void LoseLife()
    {
        lives--;

        UpdateLivesUI();

        if (lives > 0)
        {
            ResetBall();
        }
        else
        {
            Debug.Log("Game Over!");
            if (ballObject != null)
                ballObject.SetActive(false);
        }
    }

    void UpdateLivesUI()
    {
        if (livesText != null)
        {
            livesText.text = "Lives: " + lives;
        }
    }

    void ResetBall()
    {
        if (ballObject == null || ballStart == null)
            return;

        // Reactivate the ball
        ballObject.SetActive(true);

        // Stop all movement
        Rigidbody2D rb = ballObject.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;   // linearVelocity was renamed to velocity
            rb.angularVelocity = 0f;
        }

        // Reset position via Ball script
        Ball ball = ballObject.GetComponent<Ball>();
        if (ball != null)
        {
            ball.ResetBall(ballStart.position);
        }
        else
        {
            // Fallback: directly reset position if Ball script missing
            ballObject.transform.position = ballStart.position;
        }
    }
}