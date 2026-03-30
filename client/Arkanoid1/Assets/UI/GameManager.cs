using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public int lives = 4;
    public TextMeshProUGUI livesText;
    public GameObject ballObject;
    public Transform ballStart;

    void Start()
    {
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
            ballObject.SetActive(false);
        }
    }

    void UpdateLivesUI()
    {
        if (livesText != null)
            livesText.text = "Lives: " + lives;
        else
            Debug.LogWarning("LivesText is not assigned in GameManager!");
    }

    void ResetBall()
    {
        // Make sure ball is visible/active
        ballObject.SetActive(true);

        // Stop all movement immediately
        Rigidbody2D rb = ballObject.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // Reset position and wait for Space press again
        Ball ball = ballObject.GetComponent<Ball>();
        if (ball != null)
            ball.ResetBall(ballStart.position);
    }
}
