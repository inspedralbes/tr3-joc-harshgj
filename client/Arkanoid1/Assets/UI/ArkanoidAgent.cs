using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class ArkanoidAgent : Agent
{
    [Header("Scene References")]
    public Transform ball;
    public Rigidbody2D ballRb;
    public GameManager gameManager;

    [Header("Movement")]
    public float moveSpeed = 10f;
    public float edgePadding = 0.2f;
    public float followDeadZone = 0.15f;
    public bool autoFollowBall = false;
    public bool endEpisodeOnResult = true;

    [Header("Rewards")]
    public float surviveRewardPerStep = 0.0005f;
    public float paddleHitReward = 0.75f;
    public float blockBreakReward = 0.3f;
    public float loseBallPenalty = -3f;
    public float clearLevelReward = 3f;
    public float descendAlignmentReward = 0.02f;
    public float descendMisalignmentPenalty = -0.03f;
    public float upwardBallReward = 0.01f;
    public float ballAbovePaddleReward = 0.005f;
    public float ballBelowPaddlePenalty = -0.05f;

    [Header("Training Mode")]
    public bool trainingMode = false;
    public float ballResetHeight = -5f;
    public Transform ballStart;
    public float trainingBallResetDelay = 0.3f;
    public GameObject[] initialBlocks;
    public Transform blocksContainer;

    private Camera mainCamera;
    private Collider2D paddleCollider;
    private float lockedY;
    private bool hasStartedEpisode;
    private bool isRespawning;
    private Rigidbody2D ballRbInternal;
    private float stepsAlive;
    private float maxStepsWithoutHit = 500f;
    private Vector3 initialPaddlePosition;
    private List<GameObject> blockList = new List<GameObject>();
    private List<Vector3> savedBlockPositions = new List<Vector3>();
    private List<Quaternion> savedBlockRotations = new List<Quaternion>();
    private int activeBlockCount;

    public override void Initialize()
    {
        mainCamera = Camera.main;
        paddleCollider = GetComponent<Collider2D>();
        lockedY = transform.position.y;
        initialPaddlePosition = transform.position;

        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        FindBall();

        PaddleMovement paddleMovement = GetComponent<PaddleMovement>();
        if (paddleMovement != null)
            paddleMovement.enabled = false;

        AIPaddleController aiPaddleController = GetComponent<AIPaddleController>();
        if (aiPaddleController != null)
            aiPaddleController.enabled = false;

        if (trainingMode)
        {
            Invoke(nameof(DelayedFindBlocks), 0.5f);
        }

        Debug.Log($"ArkanoidAgent Initialize: trainingMode={trainingMode}, moveSpeed={moveSpeed}");
    }

    public void RefreshBall()
    {
        FindBall();
    }

    void FindBall()
    {
        if (ball == null)
        {
            GameObject ballObject = GameObject.FindGameObjectWithTag("Ball");
            if (ballObject != null)
            {
                ball = ballObject.transform;
                ballRb = ballObject.GetComponent<Rigidbody2D>();
                ballRbInternal = ballRb;
            }
        }

        if (ballRb == null && ball != null)
            ballRb = ball.GetComponent<Rigidbody2D>();
    }

    void DelayedFindBlocks()
    {
        FindAndSaveBlocks();
    }

    void FindAndSaveBlocks()
    {
        blockList.Clear();
        savedBlockPositions.Clear();
        savedBlockRotations.Clear();

        GameObject[] blocks = GameObject.FindGameObjectsWithTag("Block");
        Debug.Log($"TrainingAgent: Found {blocks.Length} blocks");

        for (int i = 0; i < blocks.Length; i++)
        {
            blockList.Add(blocks[i]);
            savedBlockPositions.Add(blocks[i].transform.position);
            savedBlockRotations.Add(blocks[i].transform.rotation);
        }

        activeBlockCount = blockList.Count;
    }

    void ResetAllBlocks()
    {
        if (blockList.Count == 0)
        {
            Debug.LogWarning("ResetAllBlocks: No blocks saved!");
            return;
        }

        Debug.Log("ResetAllBlocks: Resetting all blocks...");
        for (int i = 0; i < blockList.Count; i++)
        {
            if (blockList[i] == null) continue;

            blockList[i].transform.position = savedBlockPositions[i];
            blockList[i].transform.rotation = savedBlockRotations[i];
            blockList[i].SetActive(true);

            Collider2D col = blockList[i].GetComponent<Collider2D>();
            if (col != null)
                col.enabled = true;

            Block blockScript = blockList[i].GetComponent<Block>();
            if (blockScript != null)
                blockScript.ResetBlock();
        }
        Debug.Log($"ResetAllBlocks: Reset {blockList.Count} blocks");
    }

    void Update()
    {
        if (ball == null)
            FindBall();

        if (trainingMode && ball != null)
        {
            if (ball.position.y <= ballResetHeight && !isRespawning)
            {
                RespawnBall();
            }
            else if (ball.position.y < transform.position.y - 1f && ballRb != null && ballRb.linearVelocity.y < 0)
            {
                AddReward(-0.05f);
            }

            if (ball.position.y > transform.position.y)
                AddReward(ballAbovePaddleReward);
            else if (ball.position.y < transform.position.y)
                AddReward(ballBelowPaddlePenalty);

            stepsAlive++;
            if (stepsAlive > maxStepsWithoutHit)
            {
                stepsAlive = 0;
                EndEpisode();
            }
        }
    }

    void RespawnBall()
    {
        isRespawning = true;
        AddReward(loseBallPenalty);

        if (ballRb != null)
        {
            ballRb.linearVelocity = Vector2.zero;
            ballRb.angularVelocity = 0f;
        }

        Invoke(nameof(ResetBallPosition), 0.5f);
    }

    void ResetBallPosition()
    {
        if (ball != null && ballStart != null)
            ball.position = ballStart.position;

        if (ballRb != null)
        {
            ballRb.linearVelocity = Vector2.zero;
            ballRb.angularVelocity = 0f;
        }

        Ball ballScript = ball != null ? ball.GetComponent<Ball>() : null;
        if (ballScript != null)
            ballScript.Launch();

        isRespawning = false;
    }

    public override void OnEpisodeBegin()
    {
        stepsAlive = 0;
        isRespawning = false;

        if (trainingMode)
        {
            transform.position = initialPaddlePosition;
            activeBlockCount = blockList.Count;
            ResetAllBlocks();

            if (ball != null && ballStart != null)
                ball.position = ballStart.position;

            if (ballRb != null)
            {
                ballRb.linearVelocity = Vector2.zero;
                ballRb.angularVelocity = 0f;
            }

            Ball ballScript = ball != null ? ball.GetComponent<Ball>() : null;
            if (ballScript != null)
                ballScript.Launch();
        }

        if (!hasStartedEpisode)
        {
            hasStartedEpisode = true;
            return;
        }

        if (!trainingMode && gameManager != null)
            gameManager.ResetTrainingEpisode();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (ball == null || ballRb == null)
        {
            FindBall();
        }

        if (ball == null || ballRb == null)
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            return;
        }

        float halfWidth = GetCameraHalfWidth();
        float normalizedPaddleX = halfWidth > 0f ? transform.position.x / halfWidth : 0f;
        float normalizedBallX = halfWidth > 0f ? ball.position.x / halfWidth : 0f;
        float normalizedBallY = mainCamera != null && mainCamera.orthographicSize > 0f
            ? ball.position.y / mainCamera.orthographicSize
            : 0f;
        float normalizedVelocityX = Mathf.Clamp(ballRb.linearVelocity.x / 10f, -1f, 1f);
        float normalizedVelocityY = Mathf.Clamp(ballRb.linearVelocity.y / 10f, -1f, 1f);

        sensor.AddObservation(normalizedPaddleX);
        sensor.AddObservation(normalizedBallX);
        sensor.AddObservation(normalizedBallY);
        sensor.AddObservation(normalizedVelocityX);
        sensor.AddObservation(normalizedVelocityY);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int action = actions.DiscreteActions[0];
        float move = action == 1 ? -1f : action == 2 ? 1f : 0f;

        float targetX = transform.position.x + move * moveSpeed * Time.deltaTime;
        float clampedX = Mathf.Clamp(targetX, GetLeftLimit(), GetRightLimit());
        transform.position = new Vector3(clampedX, lockedY, transform.position.z);

        if (!trainingMode)
            AddReward(surviveRewardPerStep);

        if (!trainingMode)
            AddReward(surviveRewardPerStep);
        
        if (trainingMode && ball != null && ballRb != null)
        {
            if (ball.position.y > transform.position.y && ballRb.linearVelocity.y < 0)
            {
                float distanceToBall = Mathf.Abs(ball.position.x - transform.position.x);
                if (distanceToBall < 0.5f)
                {
                    AddReward(descendAlignmentReward);
                }
                else if (distanceToBall < 1.5f)
                {
                    AddReward(descendAlignmentReward * 0.25f);
                }
                else
                {
                    AddReward(descendMisalignmentPenalty);
                }
            }

            if (ballRb.linearVelocity.y > 0f)
                AddReward(upwardBallReward);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if (actionsOut.DiscreteActions.Array == null)
            return;

        if (ball == null)
        {
            actionsOut.DiscreteActions.Array[0] = 0;
            return;
        }

        int action = 0;
        float deltaX = ball.position.x - transform.position.x;

        if (deltaX < -followDeadZone)
            action = 1;
        else if (deltaX > followDeadZone)
            action = 2;

        actionsOut.DiscreteActions.Array[0] = action;
    }

    public void NotifyPaddleHit()
    {
        AddReward(paddleHitReward);
    }

    public void NotifyBlockBroken()
    {
        AddReward(blockBreakReward);
        activeBlockCount--;

        if (trainingMode && activeBlockCount <= 0)
        {
            AddReward(clearLevelReward);
            Invoke(nameof(ResetAllBlocksAndBall), 1f);
        }
    }

    void ResetAllBlocksAndBall()
    {
        activeBlockCount = blockList.Count;
        ResetAllBlocks();

        if (ball != null && ballStart != null)
            ball.position = ballStart.position;

        if (ballRb != null)
        {
            ballRb.linearVelocity = Vector2.zero;
            ballRb.angularVelocity = 0f;
        }

        Ball ballScript = ball != null ? ball.GetComponent<Ball>() : null;
        if (ballScript != null)
            ballScript.Launch();
    }

    public void NotifyBallLost()
    {
        if (trainingMode)
        {
            AddReward(loseBallPenalty);
            EndEpisode();
        }
        else
        {
            AddReward(loseBallPenalty);
            if (endEpisodeOnResult)
                EndEpisode();
        }
    }

    public void NotifyLevelCleared()
    {
        AddReward(clearLevelReward);
        if (endEpisodeOnResult)
            EndEpisode();
    }

    float GetLeftLimit()
    {
        return -GetCameraHalfWidth() + GetPaddleHalfWidth() + edgePadding;
    }

    float GetRightLimit()
    {
        return GetCameraHalfWidth() - GetPaddleHalfWidth() - edgePadding;
    }

    float GetCameraHalfWidth()
    {
        if (mainCamera == null)
            return 8f;

        return mainCamera.orthographicSize * mainCamera.aspect;
    }

    float GetPaddleHalfWidth()
    {
        if (paddleCollider == null)
            return 0.75f;

        return paddleCollider.bounds.extents.x;
    }
}
