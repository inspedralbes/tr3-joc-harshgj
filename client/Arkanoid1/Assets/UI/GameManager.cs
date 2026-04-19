using System.Collections;
using TMPro;
using Unity.InferenceEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public int startingLives = 3;
    public float respawnDelay = 3f;

    [Header("UI Elements")]
    public TextMeshProUGUI livesText;
    public GameObject gameOverUI;

    [Header("Ball Settings")]
    public GameObject ballObject;
    public Transform ballStart;

    [Header("Paddle Settings")]
    public GameObject paddleObject;
    public Transform paddleStart;
    public bool spawnAiPaddle = true;
    public float aiPaddleVerticalOffset = -0.7f;
    public float aiPaddleSpeed = 13f;

    private int lives;
    private bool isGameOver;
    private bool isRespawning;
    private Image separatorLine;
    private Vector3 cachedBallStartPosition;
    private Vector3 cachedPaddleStartPosition;
    private Vector3 cachedAiPaddleStartPosition;
    private bool hasCachedBallStart;
    private bool hasCachedPaddleStart;
    private bool hasCachedAiPaddleStart;
    private GameObject aiPaddleObject;
    private Collider2D deathZoneCollider;
    private bool ballLossHandled;
    private GameMode selectedMode;
    private int remainingBlocks;
    private TextMeshProUGUI gameOverText;
    private Button menuButton;
private const string MenuSceneName = "GameMenu";
    private const float HudButtonHeight = 52f;
    private const string AiModelResourcePath = "Models/ArkanoidPaddle";
    private ArkanoidAgent trainingAgent;
    private bool hasCreatedMenuButton;

    void Start()
    {
        ResolveReferences();

        if (ballObject == null)
        {
            Debug.LogError("BallObject is not assigned in GameManager!");
            return;
        }

        CacheSpawnPositions();
        selectedMode = IsTrainingScene() ? GameMode.AI : GameModeState.SelectedMode;
        ConfigurePaddlesForMode();
        lives = startingLives;
        UpdateLivesUI();
        EnsureGameOverUI();
        EnsureSeparatorLine();
        EnsureMenuButton();
        remainingBlocks = CountRemainingBlocks();
        SetGameOverVisible(false);
        PositionHud();
        ResetRound();
    }

    void Update()
    {
        if (isGameOver || isRespawning || ballLossHandled || ballObject == null)
            return;

        if (deathZoneCollider == null)
            ResolveDeathZone();

        if (deathZoneCollider != null && ballObject.transform.position.y <= deathZoneCollider.bounds.max.y)
        {
            ballLossHandled = true;
            LoseLife();
        }
    }

    void ResolveReferences()
    {
        if (ballObject == null)
            ballObject = GameObject.FindGameObjectWithTag("Ball");

        if (ballStart == null && ballObject != null)
            ballStart = ballObject.transform;

        if (paddleObject == null)
            paddleObject = GameObject.FindGameObjectWithTag("Paddle");

        if (paddleStart == null && paddleObject != null)
            paddleStart = paddleObject.transform;

        ResolveDeathZone();

        if (livesText == null)
        {
            TextMeshProUGUI[] texts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            foreach (TextMeshProUGUI text in texts)
            {
                if (text.name == "Text (TMP)" || text.name == "LivesText")
                {
                    livesText = text;
                    break;
                }
            }

            if (livesText == null)
                livesText = FindFirstObjectByType<TextMeshProUGUI>();
        }

        if (gameOverUI == null)
        {
            GameObject existingPopup = GameObject.Find("GameOverPopup");
            if (existingPopup != null)
                gameOverUI = existingPopup;
        }

        if (livesText != null && gameOverUI == livesText.gameObject)
            gameOverUI = null;
    }

    void ResolveDeathZone()
    {
        if (deathZoneCollider != null)
            return;

        GameObject deathZoneObject = GameObject.Find("DeathZone");
        if (deathZoneObject != null)
            deathZoneCollider = deathZoneObject.GetComponent<Collider2D>();
    }

    void ConfigurePaddlesForMode()
    {
        if (paddleObject == null)
            return;

        RemoveStraySupportPaddles();
        RemoveExistingSupportPaddle();

        if (IsTrainingScene())
        {
            ConfigureAsTrainingAgent(paddleObject);
            return;
        }

        ConfigurePrimaryPaddle();

        if (selectedMode == GameMode.Partner)
            EnsurePartnerPaddle();

        if (selectedMode == GameMode.AI)
            EnsureAiSupportPaddle();
    }

    void ConfigurePrimaryPaddle()
    {
        if (selectedMode == GameMode.Partner)
        {
            bool localControlsPrimary = SessionState.RoomRole != "guest";
            ConfigureAsLocalPaddle(paddleObject, localControlsPrimary);

            if (!localControlsPrimary)
                ConfigureAsRemotePaddle(paddleObject);

            return;
        }

        ConfigureAsLocalPaddle(paddleObject, true);
}

void ConfigureAsInferenceAgent(GameObject targetPaddle)
    {
        PaddleMovement paddleMovement = targetPaddle.GetComponent<PaddleMovement>();
        if (paddleMovement != null)
            paddleMovement.enabled = false;

        ArkanoidAgent agent = targetPaddle.GetComponent<ArkanoidAgent>();
        if (agent != null)
        {
            agent.enabled = false;
            Destroy(agent);
        }

        BehaviorParameters bp = targetPaddle.GetComponent<BehaviorParameters>();
        if (bp != null) Destroy(bp);

        DecisionRequester dr = targetPaddle.GetComponent<DecisionRequester>();
        if (dr != null) Destroy(dr);

        AIPaddleController aiController = targetPaddle.GetComponent<AIPaddleController>();
        if (aiController == null)
            aiController = targetPaddle.AddComponent<AIPaddleController>();

        aiController.enabled = true;
        aiController.speed = aiPaddleSpeed;
        aiController.reactionDeadZone = 0.15f;

        if (ballObject != null)
            aiController.ballTarget = ballObject.transform;

        Debug.Log("AI paddle configured to follow ball");
    }

    void ConfigureAsTrainingAgent(GameObject targetPaddle)
    {
        PaddleMovement paddleMovement = targetPaddle.GetComponent<PaddleMovement>();
        if (paddleMovement != null)
            paddleMovement.enabled = false;

        AIPaddleController aiController = targetPaddle.GetComponent<AIPaddleController>();
        if (aiController != null)
            aiController.enabled = false;

        RemotePaddleController remoteController = targetPaddle.GetComponent<RemotePaddleController>();
        if (remoteController != null)
            remoteController.enabled = false;

        LocalPaddleNetworkSync networkSync = targetPaddle.GetComponent<LocalPaddleNetworkSync>();
        if (networkSync != null)
            networkSync.enabled = false;

        BehaviorParameters behaviorParameters = targetPaddle.GetComponent<BehaviorParameters>();
        if (behaviorParameters == null)
            behaviorParameters = targetPaddle.AddComponent<BehaviorParameters>();

        DecisionRequester decisionRequester = targetPaddle.GetComponent<DecisionRequester>();
        if (decisionRequester == null)
            decisionRequester = targetPaddle.AddComponent<DecisionRequester>();

        behaviorParameters.BehaviorName = "ArkanoidPaddle";
        behaviorParameters.Model = null;
        behaviorParameters.BehaviorType = BehaviorType.Default;
        behaviorParameters.UseChildSensors = false;
        behaviorParameters.UseChildActuators = false;
        behaviorParameters.BrainParameters.VectorObservationSize = 5;
        behaviorParameters.BrainParameters.NumStackedVectorObservations = 1;
        behaviorParameters.BrainParameters.ActionSpec = ActionSpec.MakeDiscrete(3);
        behaviorParameters.DeterministicInference = false;

        decisionRequester.DecisionPeriod = 1;
        decisionRequester.TakeActionsBetweenDecisions = true;

        trainingAgent = targetPaddle.GetComponent<ArkanoidAgent>();
        if (trainingAgent == null)
            trainingAgent = targetPaddle.AddComponent<ArkanoidAgent>();

        trainingAgent.enabled = true;
        trainingAgent.gameManager = this;
        trainingAgent.trainingMode = true;
        trainingAgent.autoFollowBall = false;
        trainingAgent.moveSpeed = aiPaddleSpeed;
        trainingAgent.endEpisodeOnResult = true;
        trainingAgent.ballStart = ballStart;

        if (ballObject != null)
        {
            trainingAgent.ball = ballObject.transform;
            trainingAgent.ballRb = ballObject.GetComponent<Rigidbody2D>();
        }
    }

    void EnsurePartnerPaddle()
    {
        if (!spawnAiPaddle || paddleObject == null || aiPaddleObject != null)
            return;

        aiPaddleObject = Instantiate(paddleObject, paddleObject.transform.parent);
        aiPaddleObject.name = "PartnerPaddle";

        Vector3 startPosition = hasCachedPaddleStart ? cachedPaddleStartPosition : paddleObject.transform.position;
        startPosition.y += aiPaddleVerticalOffset;
        aiPaddleObject.transform.position = startPosition;

        bool localControlsSupport = SessionState.RoomRole == "guest";
        ConfigureAsLocalPaddle(aiPaddleObject, localControlsSupport);

        if (!localControlsSupport)
            ConfigureAsRemotePaddle(aiPaddleObject);

        SpriteRenderer partnerRenderer = aiPaddleObject.GetComponent<SpriteRenderer>();
        if (partnerRenderer != null)
            partnerRenderer.color = new Color(0.35f, 0.95f, 1f, 1f);

        cachedAiPaddleStartPosition = aiPaddleObject.transform.position;
        hasCachedAiPaddleStart = true;
    }

    void EnsureAiSupportPaddle()
    {
        if (!spawnAiPaddle || paddleObject == null || aiPaddleObject != null)
            return;

        aiPaddleObject = Instantiate(paddleObject, paddleObject.transform.parent);
        aiPaddleObject.name = "AIPaddle";

        Vector3 startPosition = hasCachedPaddleStart ? cachedPaddleStartPosition : paddleObject.transform.position;
        startPosition.y += aiPaddleVerticalOffset;
        aiPaddleObject.transform.position = startPosition;

        ConfigureAsInferenceAgent(aiPaddleObject);

        SpriteRenderer aiRenderer = aiPaddleObject.GetComponent<SpriteRenderer>();
        if (aiRenderer != null)
            aiRenderer.color = new Color(1f, 0.85f, 0.2f, 1f);

        cachedAiPaddleStartPosition = aiPaddleObject.transform.position;
        hasCachedAiPaddleStart = true;
    }

    void ConfigureAsLocalPaddle(GameObject targetPaddle, bool isLocal)
    {
        PaddleMovement paddleMovement = targetPaddle.GetComponent<PaddleMovement>();
        if (paddleMovement == null)
            paddleMovement = targetPaddle.AddComponent<PaddleMovement>();

        paddleMovement.enabled = isLocal;

        AIPaddleController aiController = targetPaddle.GetComponent<AIPaddleController>();
        if (aiController != null)
            aiController.enabled = false;

        ArkanoidAgent agent = targetPaddle.GetComponent<ArkanoidAgent>();
        if (agent != null)
            agent.enabled = false;

        RemotePaddleController remoteController = targetPaddle.GetComponent<RemotePaddleController>();
        if (remoteController != null)
            remoteController.enabled = !isLocal;

        LocalPaddleNetworkSync networkSync = targetPaddle.GetComponent<LocalPaddleNetworkSync>();
        if (isLocal)
        {
            if (networkSync == null)
                networkSync = targetPaddle.AddComponent<LocalPaddleNetworkSync>();

            networkSync.enabled = selectedMode == GameMode.Partner;
        }
        else if (networkSync != null)
        {
            networkSync.enabled = false;
        }
    }

    void ConfigureAsRemotePaddle(GameObject targetPaddle)
    {
        PaddleMovement paddleMovement = targetPaddle.GetComponent<PaddleMovement>();
        if (paddleMovement != null)
            paddleMovement.enabled = false;

        AIPaddleController aiController = targetPaddle.GetComponent<AIPaddleController>();
        if (aiController != null)
            aiController.enabled = false;

        ArkanoidAgent agent = targetPaddle.GetComponent<ArkanoidAgent>();
        if (agent != null)
            agent.enabled = false;

        LocalPaddleNetworkSync networkSync = targetPaddle.GetComponent<LocalPaddleNetworkSync>();
        if (networkSync != null)
            networkSync.enabled = false;

        RemotePaddleController remoteController = targetPaddle.GetComponent<RemotePaddleController>();
        if (remoteController == null)
            remoteController = targetPaddle.AddComponent<RemotePaddleController>();

        remoteController.enabled = true;
    }

    void RemoveExistingSupportPaddle()
    {
        if (aiPaddleObject != null)
        {
            Destroy(aiPaddleObject);
            aiPaddleObject = null;
        }

        trainingAgent = null;
        hasCachedAiPaddleStart = false;
    }

    void RemoveStraySupportPaddles()
    {
        GameObject[] supportPaddles = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject candidate in supportPaddles)
        {
            if (candidate == null || candidate == paddleObject)
                continue;

            if (candidate.name == "AIPaddle" || candidate.name == "PartnerPaddle")
                Destroy(candidate);
        }
    }

    void CacheSpawnPositions()
    {
        if (ballStart != null)
        {
            cachedBallStartPosition = ballStart.position;
            hasCachedBallStart = true;
        }
        else if (ballObject != null)
        {
            cachedBallStartPosition = ballObject.transform.position;
            hasCachedBallStart = true;
        }

        if (paddleStart != null)
        {
            cachedPaddleStartPosition = paddleStart.position;
            hasCachedPaddleStart = true;
        }
        else if (paddleObject != null)
        {
            cachedPaddleStartPosition = paddleObject.transform.position;
            hasCachedPaddleStart = true;
        }

        if (aiPaddleObject != null)
        {
            cachedAiPaddleStartPosition = aiPaddleObject.transform.position;
            hasCachedAiPaddleStart = true;
        }
    }

    public void LoseLife()
    {
        if (isGameOver || isRespawning)
            return;

        lives = Mathf.Max(0, lives - 1);
        ballLossHandled = true;
        UpdateLivesUI();

        if (lives == 0)
        {
            GameOver();
            return;
        }

        StartCoroutine(RespawnAfterDelay());
    }

    IEnumerator RespawnAfterDelay()
    {
        isRespawning = true;
        FreezeBall(true);
        yield return new WaitForSeconds(respawnDelay);
        ResetRound();
        isRespawning = false;
    }

    void GameOver()
    {
        isGameOver = true;
        FreezeBall(true);
        SetGameOverMessage("GAME OVER");
        SetGameOverVisible(true);
    }

    public void NotifyBlockDestroyed()
    {
        if (isGameOver)
            return;

        remainingBlocks = CountRemainingBlocks();
        if (remainingBlocks == 0)
            CompleteLevel();
    }

    void CompleteLevel()
    {
        isGameOver = true;
        FreezeBall(true);
        if (selectedMode == GameMode.AI && trainingAgent != null)
            trainingAgent.NotifyLevelCleared();
        SetGameOverMessage("GAME OVER");
        SetGameOverVisible(true);
    }

    void FreezeBall(bool freeze)
    {
        if (ballObject == null)
            return;

        Rigidbody2D rb = ballObject.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = !freeze;
        }

        if (freeze)
            ballObject.SetActive(false);
    }

    void UpdateLivesUI()
    {
        if (livesText != null)
        {
            livesText.gameObject.SetActive(true);
            livesText.text = "Lives: " + lives;
        }

        if (separatorLine != null)
            separatorLine.gameObject.SetActive(true);
    }

    void ResetRound()
    {
        ResetPaddle();
        ResetBall();
        PositionHud();
        SetGameOverVisible(false);
    }

    void ResetBall()
    {
        if (ballObject == null || !hasCachedBallStart)
            return;

        ballObject.SetActive(true);
        ballLossHandled = false;

        Rigidbody2D rb = ballObject.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        Ball ball = ballObject.GetComponent<Ball>();
        if (ball != null)
        {
            ball.ResetBall(cachedBallStartPosition);
            ball.Launch();
        }
        else
        {
            ballObject.transform.position = cachedBallStartPosition;
        }
    }

    void ResetPaddle()
    {
        if (paddleObject != null && hasCachedPaddleStart)
            paddleObject.transform.position = cachedPaddleStartPosition;

        if (aiPaddleObject != null && hasCachedAiPaddleStart)
            aiPaddleObject.transform.position = cachedAiPaddleStartPosition;
    }

    void PositionHud()
    {
        Canvas canvas = GetHudCanvas();
        if (canvas == null)
            return;

        EnsureCanvasInteractionSupport(canvas);

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;
        float hudMarginX = canvasWidth * 0.03f;
        float hudMarginY = canvasHeight * 0.03f;
        float hudHeight = Mathf.Clamp(canvasHeight * 0.06f, 35f, 50f);
        float hudWidth = Mathf.Clamp(canvasWidth * 0.25f, 150f, 250f);

        if (livesText != null)
        {
            RectTransform livesRect = livesText.rectTransform;
            livesRect.anchorMin = new Vector2(0f, 1f);
            livesRect.anchorMax = new Vector2(0f, 1f);
            livesRect.pivot = new Vector2(0f, 1f);
            livesRect.anchoredPosition = new Vector2(hudMarginX, -hudMarginY);
            livesRect.sizeDelta = new Vector2(hudWidth, hudHeight);
            livesText.fontSize = Mathf.RoundToInt(canvasHeight * 0.04f);
            livesText.alignment = TextAlignmentOptions.MidlineLeft;
        }

        if (separatorLine != null)
        {
            RectTransform lineRect = separatorLine.rectTransform;
            lineRect.anchorMin = new Vector2(0f, 1f);
            lineRect.anchorMax = new Vector2(1f, 1f);
            lineRect.pivot = new Vector2(0.5f, 1f);
            lineRect.offsetMin = new Vector2(0f, -hudMarginY - hudHeight - 3f);
            lineRect.offsetMax = new Vector2(0f, -hudMarginY - hudHeight);
            lineRect.sizeDelta = new Vector2(0f, 2f);
        }

        if (menuButton != null)
        {
            RectTransform menuRect = menuButton.GetComponent<RectTransform>();
            menuRect.anchorMin = new Vector2(1f, 1f);
            menuRect.anchorMax = new Vector2(1f, 1f);
            menuRect.pivot = new Vector2(1f, 1f);
            menuRect.anchoredPosition = new Vector2(-hudMarginX, -hudMarginY);
            menuRect.sizeDelta = new Vector2(hudWidth * 0.5f, hudHeight);
        }
    }

    void EnsureSeparatorLine()
    {
        if (livesText == null)
            return;

        Canvas canvas = livesText.canvas;
        if (canvas == null)
            return;

        Transform existingLine = canvas.transform.Find("LivesSeparator");
        if (existingLine != null)
        {
            separatorLine = existingLine.GetComponent<Image>();
            return;
        }

        GameObject lineObject = new GameObject("LivesSeparator", typeof(RectTransform), typeof(Image));
        lineObject.transform.SetParent(canvas.transform, false);
        separatorLine = lineObject.GetComponent<Image>();
        separatorLine.color = new Color(1f, 1f, 1f, 0.8f);
    }

    void EnsureGameOverUI()
    {
        if (gameOverUI != null)
        {
            CacheGameOverText();
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        GameObject popup = new GameObject("GameOverPopup", typeof(RectTransform), typeof(Image));
        popup.transform.SetParent(canvas.transform, false);

        RectTransform popupRect = popup.GetComponent<RectTransform>();
        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.pivot = new Vector2(0.5f, 0.5f);
        popupRect.sizeDelta = new Vector2(520f, 180f);
        popupRect.anchoredPosition = Vector2.zero;

        Image popupImage = popup.GetComponent<Image>();
        popupImage.color = new Color(0f, 0f, 0f, 0.8f);

        GameObject textObject = new GameObject("GameOverText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(popup.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(24f, 24f);
        textRect.offsetMax = new Vector2(-24f, -24f);

        TextMeshProUGUI gameOverText = textObject.GetComponent<TextMeshProUGUI>();
        gameOverText.text = "GAME OVER";
        gameOverText.fontSize = 48;
        gameOverText.alignment = TextAlignmentOptions.Center;
        gameOverText.color = Color.white;

        gameOverUI = popup;
        this.gameOverText = gameOverText;
    }

    void SetGameOverVisible(bool visible)
    {
        if (gameOverUI != null)
            gameOverUI.SetActive(visible);
    }

    void CacheGameOverText()
    {
        if (gameOverUI == null || gameOverText != null)
            return;

        gameOverText = gameOverUI.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    void SetGameOverMessage(string message)
    {
        CacheGameOverText();
        if (gameOverText != null)
            gameOverText.text = message;
    }

void EnsureMenuButton()
    {
        if (hasCreatedMenuButton)
            return;

        Canvas canvas = GetHudCanvas();
        if (canvas == null)
            return;

        EnsureCanvasInteractionSupport(canvas);
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        
        Transform existingButton = canvas.transform.Find("MenuButton");
        if (existingButton != null)
        {
            menuButton = existingButton.GetComponent<Button>();
            if (menuButton != null)
            {
                menuButton.gameObject.SetActive(true);
                menuButton.onClick.RemoveListener(ReturnToMenu);
                menuButton.onClick.AddListener(ReturnToMenu);
                menuButton.transform.SetAsLastSibling();
PositionMenuButton(canvas);
        hasCreatedMenuButton = true;
    }
            return;
        }

        EnsureEventSystem();

        GameObject buttonObject = new GameObject("MenuButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(canvas.transform, false);
        buttonObject.layer = canvas.gameObject.layer;

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.08f, 0.08f, 0.08f, 0.92f);

        menuButton = buttonObject.GetComponent<Button>();
        menuButton.gameObject.SetActive(true);
        menuButton.targetGraphic = buttonImage;
        menuButton.onClick.AddListener(ReturnToMenu);
        menuButton.transform.SetAsLastSibling();

        GameObject labelObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        labelObject.layer = canvas.gameObject.layer;

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(canvasRect.rect.width * 0.02f, canvasRect.rect.height * 0.01f);
        labelRect.offsetMax = new Vector2(-canvasRect.rect.width * 0.02f, -canvasRect.rect.height * 0.01f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "⬅️ Menu";
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        label.fontSize = Mathf.Clamp(Mathf.RoundToInt(canvasRect.rect.height * 0.035f), 18, 28);

        PositionMenuButton(canvas);
    }

    Canvas GetHudCanvas()
    {
        if (livesText != null && livesText.canvas != null)
            return livesText.canvas;

        return FindFirstObjectByType<Canvas>();
    }

    void PositionMenuButton(Canvas canvas)
    {
        if (menuButton == null || canvas == null)
            return;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        RectTransform menuRect = menuButton.GetComponent<RectTransform>();
        float canvasHeight = canvasRect.rect.height;
        float hudMarginX = canvasRect.rect.width * 0.03f;
        float hudMarginY = canvasRect.rect.height * 0.03f;
        float buttonHeight = Mathf.Clamp(canvasHeight * 0.05f, 32f, 45f);
        float buttonWidth = Mathf.Clamp(canvasRect.rect.width * 0.2f, 100f, 150f);

        menuRect.anchorMin = new Vector2(1f, 1f);
        menuRect.anchorMax = new Vector2(1f, 1f);
        menuRect.pivot = new Vector2(1f, 1f);
        menuRect.anchoredPosition = new Vector2(-hudMarginX, -hudMarginY);
        menuRect.sizeDelta = new Vector2(buttonWidth, buttonHeight);
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
        if (TryAddInputSystemModule(eventSystemObject))
            return;

        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    void EnsureCanvasInteractionSupport(Canvas canvas)
    {
        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();
    }

    bool TryAddInputSystemModule(GameObject eventSystemObject)
    {
        System.Type inputSystemModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemModuleType == null)
            return false;

        eventSystemObject.AddComponent(inputSystemModuleType);
        return true;
    }

    void ReturnToMenu()
    {
        SessionState.ClearRoomState();
        if (NetworkSessionManager.Instance != null)
            _ = NetworkSessionManager.Instance.DisconnectAsync();

        SceneManager.LoadScene(MenuSceneName);
    }

    public void NotifyTrainingBallLost()
    {
        if ((selectedMode == GameMode.AI || IsTrainingScene()) && trainingAgent != null)
            trainingAgent.NotifyBallLost();
    }

    public void NotifyTrainingBlockDestroyed()
    {
        if ((selectedMode == GameMode.AI || IsTrainingScene()) && trainingAgent != null)
            trainingAgent.NotifyBlockBroken();
    }

    public void ResetTrainingEpisode()
    {
        if (selectedMode != GameMode.AI)
            return;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    int CountRemainingBlocks()
    {
        return GameObject.FindGameObjectsWithTag("Block").Length;
    }

    bool IsTrainingScene()
    {
        return SceneManager.GetActiveScene().name == "TrainingScene";
    }
}
