using System.Collections;
using TMPro;
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

    void Start()
    {
        ResolveReferences();

        if (ballObject == null)
        {
            Debug.LogError("BallObject is not assigned in GameManager!");
            return;
        }

        CacheSpawnPositions();
        selectedMode = GameModeState.SelectedMode;
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

        RemoveExistingSupportPaddle();
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

    void EnsureAiSupportPaddle()
    {
        if (!spawnAiPaddle || paddleObject == null || aiPaddleObject != null)
            return;

        aiPaddleObject = Instantiate(paddleObject, paddleObject.transform.parent);
        aiPaddleObject.name = "AIPaddle";

        Vector3 startPosition = hasCachedPaddleStart ? cachedPaddleStartPosition : paddleObject.transform.position;
        startPosition.y += aiPaddleVerticalOffset;
        aiPaddleObject.transform.position = startPosition;

        PaddleMovement supportMovement = aiPaddleObject.GetComponent<PaddleMovement>();
        if (supportMovement != null)
            Destroy(supportMovement);

        AIPaddleController aiController = aiPaddleObject.GetComponent<AIPaddleController>();
        if (aiController == null)
            aiController = aiPaddleObject.AddComponent<AIPaddleController>();

        aiController.ballTarget = ballObject != null ? ballObject.transform : null;
        aiController.speed = aiPaddleSpeed;
        aiController.enabled = true;

        SpriteRenderer aiRenderer = aiPaddleObject.GetComponent<SpriteRenderer>();
        if (aiRenderer != null)
            aiRenderer.color = new Color(1f, 0.85f, 0.2f, 1f);

        cachedAiPaddleStartPosition = aiPaddleObject.transform.position;
        hasCachedAiPaddleStart = true;
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

    void ConfigureAsLocalPaddle(GameObject targetPaddle, bool isLocal)
    {
        PaddleMovement paddleMovement = targetPaddle.GetComponent<PaddleMovement>();
        if (paddleMovement == null)
            paddleMovement = targetPaddle.AddComponent<PaddleMovement>();

        paddleMovement.enabled = isLocal;

        AIPaddleController aiController = targetPaddle.GetComponent<AIPaddleController>();
        if (aiController != null)
            aiController.enabled = false;

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

        hasCachedAiPaddleStart = false;
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

        remainingBlocks = Mathf.Max(0, remainingBlocks - 1);
        if (remainingBlocks == 0)
            CompleteLevel();
    }

    void CompleteLevel()
    {
        isGameOver = true;
        FreezeBall(true);
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
            livesText.text = "Lives: " + lives;
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
        if (livesText == null)
            return;

        Canvas canvas = livesText.canvas;
        if (canvas == null)
            return;

        EnsureCanvasInteractionSupport(canvas);

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        RectTransform livesRect = livesText.rectTransform;
        float hudMarginX = Mathf.Max(24f, canvasRect.rect.width * 0.04f);
        float hudMarginY = Mathf.Max(24f, canvasRect.rect.height * 0.04f);

        livesRect.anchorMin = new Vector2(0f, 1f);
        livesRect.anchorMax = new Vector2(0f, 1f);
        livesRect.pivot = new Vector2(0f, 1f);
        livesRect.anchoredPosition = new Vector2(hudMarginX, -hudMarginY);
        livesRect.sizeDelta = new Vector2(Mathf.Max(220f, canvasRect.rect.width * 0.25f), HudButtonHeight);
        livesText.alignment = TextAlignmentOptions.MidlineLeft;

        if (separatorLine != null)
        {
            RectTransform lineRect = separatorLine.rectTransform;
            lineRect.anchorMin = new Vector2(0f, 1f);
            lineRect.anchorMax = new Vector2(1f, 1f);
            lineRect.pivot = new Vector2(0.5f, 1f);
            lineRect.offsetMin = new Vector2(0f, -hudMarginY - HudButtonHeight - 8f);
            lineRect.offsetMax = new Vector2(0f, -hudMarginY - HudButtonHeight - 5f);
        }

        if (menuButton != null)
        {
            RectTransform menuRect = menuButton.GetComponent<RectTransform>();
            menuRect.anchorMin = new Vector2(1f, 1f);
            menuRect.anchorMax = new Vector2(1f, 1f);
            menuRect.pivot = new Vector2(1f, 1f);
            menuRect.anchoredPosition = new Vector2(-hudMarginX, -hudMarginY);
            menuRect.sizeDelta = new Vector2(150f, HudButtonHeight);
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
        if (livesText == null)
            return;

        Canvas canvas = livesText.canvas;
        if (canvas == null)
            return;

        EnsureCanvasInteractionSupport(canvas);
        Transform existingButton = canvas.transform.Find("MenuButton");
        if (existingButton != null)
        {
            menuButton = existingButton.GetComponent<Button>();
            if (menuButton != null)
            {
                menuButton.onClick.RemoveListener(ReturnToMenu);
                menuButton.onClick.AddListener(ReturnToMenu);
            }
            return;
        }

        EnsureEventSystem();

        GameObject buttonObject = new GameObject("MenuButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(canvas.transform, false);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.08f, 0.08f, 0.08f, 0.92f);

        menuButton = buttonObject.GetComponent<Button>();
        menuButton.targetGraphic = buttonImage;
        menuButton.onClick.AddListener(ReturnToMenu);

        GameObject labelObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10f, 6f);
        labelRect.offsetMax = new Vector2(-10f, -6f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "Menu";
        label.fontSize = 28f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
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

    int CountRemainingBlocks()
    {
        return GameObject.FindGameObjectsWithTag("Block").Length;
    }
}
