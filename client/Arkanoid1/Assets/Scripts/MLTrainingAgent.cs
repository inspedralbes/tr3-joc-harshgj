using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

[RequireComponent(typeof(UnityEngine.Rigidbody2D))]
public class MLTrainingAgent : Agent
{
    [Header("Training")]
    public int maxTrainingEpisodes = 100;
    private int episodesCompleted = 0;

    [Header("Settings")]
    public float moveSpeed = 10f;
    public float edgePadding = 0.2f;

    [Header("References")]
    public Transform ball;
    public Rigidbody2D ballRb;
    public Transform ballStart;

    [Header("Rewards")]
    public float surviveReward = 0.001f;
    public float hitReward = 0.5f;
    public float fallPenalty = -1f;

    private Camera mainCamera;
    private Collider2D paddleCollider;
    private float lockedY;
    private float stepsAlive;
    private float maxStepsWithoutHit = 500f;
    private bool isRespawning;
    private float ballResetHeight = -5f;

    public override void Initialize()
    {
        mainCamera = Camera.main;
        paddleCollider = GetComponent<Collider2D>();
        lockedY = transform.position.y;

        // Disable manual movement scripts — the Agent controls movement
        var pm = GetComponent<PaddleMovement>();
        if (pm != null) pm.enabled = false;

        var ai = GetComponent<AIPaddleController>();
        if (ai != null) ai.enabled = false;

        if (ball == null)
        {
            var ballObj = GameObject.FindGameObjectWithTag("Ball");
            if (ballObj != null)
            {
                ball = ballObj.transform;
                ballRb = ballObj.GetComponent<Rigidbody2D>();
            }
        }
    }

    void Update()
    {
        if (ball != null && ball.position.y <= ballResetHeight && !isRespawning)
        {
            RespawnBall();
        }

        stepsAlive++;
        if (stepsAlive > maxStepsWithoutHit)
        {
            stepsAlive = 0;
            EndEpisodeInternal();
        }
    }

    void RespawnBall()
    {
        isRespawning = true;
        AddReward(fallPenalty);

        if (ballRb != null)
        {
            ballRb.linearVelocity = Vector2.zero;
            ballRb.angularVelocity = 0f;
        }

        Invoke(nameof(ResetBall), 0.3f);
    }

    void ResetBall()
    {
        if (ball != null && ballStart != null)
            ball.position = ballStart.position;

        if (ballRb != null)
        {
            ballRb.linearVelocity = Vector2.zero;
            ballRb.angularVelocity = 0f;
        }

        var ballScript = ball?.GetComponent<Ball>();
        ballScript?.Launch();

        isRespawning = false;
    }

    public override void OnEpisodeBegin()
    {
        stepsAlive = 0;
        isRespawning = false;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (ball == null || ballRb == null)
        {
            for (int i = 0; i < 5; i++) sensor.AddObservation(0f);
            return;
        }

        float halfWidth = GetCameraHalfWidth();
        float normPaddleX = halfWidth > 0f ? transform.position.x / halfWidth : 0f;
        float normBallX   = halfWidth > 0f ? ball.position.x / halfWidth : 0f;
        float normBallY   = mainCamera != null ? ball.position.y / mainCamera.orthographicSize : 0f;
        float normVelX    = Mathf.Clamp(ballRb.linearVelocity.x / 10f, -1f, 1f);
        float normVelY    = Mathf.Clamp(ballRb.linearVelocity.y / 10f, -1f, 1f);

        sensor.AddObservation(normPaddleX); // paddle X (normalised)
        sensor.AddObservation(normBallX);   // ball X   (normalised)
        sensor.AddObservation(normBallY);   // ball Y   (normalised)
        sensor.AddObservation(normVelX);    // ball velocity X
        sensor.AddObservation(normVelY);    // ball velocity Y
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // 0 = stay, 1 = left, 2 = right
        int action = actions.DiscreteActions[0];
        float move = action == 1 ? -1f : action == 2 ? 1f : 0f;

        float targetX  = transform.position.x + move * moveSpeed * Time.deltaTime;
        float clampedX = Mathf.Clamp(targetX, GetLeftLimit(), GetRightLimit());
        transform.position = new Vector3(clampedX, lockedY, transform.position.z);

        AddReward(surviveReward);

        // Proximity bonus: reward being close to the ball when it's falling
        if (ball != null && ballRb != null &&
            ball.position.y < transform.position.y &&
            ballRb.linearVelocity.y < 0)
        {
            float dist = Mathf.Abs(ball.position.x - transform.position.x);
            AddReward(dist < 1.5f ? 0.01f : -0.02f);
        }
    }

    /// <summary>
    /// Called when Behavior Type is set to "Heuristic Only".
    /// Arrow keys / A-D let you play manually to test observations.
    /// </summary>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discrete = actionsOut.DiscreteActions;
        discrete[0] = 0; // default: stay

        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            discrete[0] = 1; // left
        else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            discrete[0] = 2; // right
    }

    public void NotifyHit() => AddReward(hitReward);

    private void EndEpisodeInternal()
    {
        base.EndEpisode();
        episodesCompleted++;

        if (episodesCompleted >= maxTrainingEpisodes)
            Debug.Log("Training complete! Assign the exported .onnx in Behavior Parameters → Model.");
    }

    float GetLeftLimit()        => -GetCameraHalfWidth() + GetPaddleHalfWidth() + edgePadding;
    float GetRightLimit()       =>  GetCameraHalfWidth() - GetPaddleHalfWidth() - edgePadding;
    float GetCameraHalfWidth()  => mainCamera != null ? mainCamera.orthographicSize * mainCamera.aspect : 8f;
    float GetPaddleHalfWidth()  => paddleCollider != null ? paddleCollider.bounds.extents.x : 0.75f;
}