using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class GameMenuController : MonoBehaviour
{
    [System.Serializable]
    class CreateRoomResponse
    {
        public string roomCode;
        public string error;
    }

    private const string GameSceneName = "Final producte";
    private const string LoginSceneName = "SampleScene";

    private VisualElement mainMenuPanel;
    private VisualElement partnerLobbyPanel;
    private TextField roomCodeField;
    private Label roomCodeLabel;
    private Label partnerStatusLabel;

    void Start()
    {
        UIDocument uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
            return;

        NetworkSessionManager manager = NetworkSessionManager.EnsureExists();
        manager.RoomStateChanged += OnRoomStateChanged;
        manager.MatchStarted += OnMatchStarted;
        manager.ErrorReceived += OnNetworkError;

        VisualElement root = uiDocument.rootVisualElement;
        mainMenuPanel = root.Q<VisualElement>("mainMenuPanel");
        partnerLobbyPanel = root.Q<VisualElement>("partnerLobbyPanel");
        roomCodeField = root.Q<TextField>("roomCodeField");
        roomCodeLabel = root.Q<Label>("roomCodeLabel");
        partnerStatusLabel = root.Q<Label>("partnerStatusLabel");

        Button soloPlayButton = root.Q<Button>("soloPlayButton");
        Button partnerPlayButton = root.Q<Button>("partnerPlayButton");
        Button aiPlayButton = root.Q<Button>("aiPlayButton");
        Button backButton = root.Q<Button>("backButton");
        Button createRoomButton = root.Q<Button>("createRoomButton");
        Button joinRoomButton = root.Q<Button>("joinRoomButton");
        Button partnerBackButton = root.Q<Button>("partnerBackButton");

        if (soloPlayButton != null)
            soloPlayButton.clicked += StartSoloGame;

        if (partnerPlayButton != null)
            partnerPlayButton.clicked += ShowPartnerLobby;

        if (aiPlayButton != null)
            aiPlayButton.clicked += StartAiGame;

        if (backButton != null)
            backButton.clicked += BackToLogin;

        if (createRoomButton != null)
            createRoomButton.clicked += () => StartCoroutine(CreateRoomRequest());

        if (joinRoomButton != null)
            joinRoomButton.clicked += () => StartCoroutine(JoinRoomRequest(roomCodeField.value));

        if (partnerBackButton != null)
            partnerBackButton.clicked += HidePartnerLobby;

        SetPartnerLobbyVisible(false);
    }

    void OnDestroy()
    {
        if (NetworkSessionManager.Instance == null)
            return;

        NetworkSessionManager.Instance.RoomStateChanged -= OnRoomStateChanged;
        NetworkSessionManager.Instance.MatchStarted -= OnMatchStarted;
        NetworkSessionManager.Instance.ErrorReceived -= OnNetworkError;
    }

    void StartSoloGame()
    {
        GameModeState.SelectedMode = GameMode.Solo;
        SessionState.ClearRoomState();
        _ = NetworkSessionManager.EnsureExists().DisconnectAsync();
        SceneManager.LoadScene(GameSceneName);
    }

    void StartAiGame()
    {
        GameModeState.SelectedMode = GameMode.AI;
        SessionState.ClearRoomState();
        _ = NetworkSessionManager.EnsureExists().DisconnectAsync();
        SceneManager.LoadScene(GameSceneName);
    }

    void ShowPartnerLobby()
    {
        GameModeState.SelectedMode = GameMode.Partner;
        roomCodeField.value = SessionState.RoomCode ?? string.Empty;
        roomCodeLabel.text = "Room code: " + (string.IsNullOrWhiteSpace(SessionState.RoomCode) ? "-" : SessionState.RoomCode);
        partnerStatusLabel.text = "Create a room or enter a room code.";
        SetPartnerLobbyVisible(true);
    }

    void HidePartnerLobby()
    {
        partnerStatusLabel.text = "Waiting for room action.";
        SetPartnerLobbyVisible(false);
    }

    void BackToLogin()
    {
        SessionState.ClearRoomState();
        _ = NetworkSessionManager.EnsureExists().DisconnectAsync();
        SceneManager.LoadScene(LoginSceneName);
    }

    void SetPartnerLobbyVisible(bool visible)
    {
        if (mainMenuPanel != null)
            mainMenuPanel.style.display = visible ? DisplayStyle.None : DisplayStyle.Flex;

        if (partnerLobbyPanel != null)
            partnerLobbyPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    IEnumerator CreateRoomRequest()
    {
        partnerStatusLabel.text = "Creating room...";

        UnityWebRequest request = new UnityWebRequest(SessionState.CreateRoomUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}"));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        SessionState.ApplySessionCookie(request);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            partnerStatusLabel.text = GetRequestError(request, "Room creation failed.");
            yield break;
        }

        CreateRoomResponse response = JsonUtility.FromJson<CreateRoomResponse>(request.downloadHandler.text);
        if (response == null || string.IsNullOrWhiteSpace(response.roomCode))
        {
            partnerStatusLabel.text = "Server returned an invalid room code.";
            yield break;
        }

        SessionState.RoomCode = response.roomCode;
        roomCodeLabel.text = "Room code: " + response.roomCode;
        partnerStatusLabel.text = "Room created. Waiting for the other player...";
        NetworkSessionManager.EnsureExists().ConnectToRoom(response.roomCode);
    }

    IEnumerator JoinRoomRequest(string roomCode)
    {
        string trimmedCode = roomCode != null ? roomCode.Trim().ToUpperInvariant() : string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedCode))
        {
            partnerStatusLabel.text = "Enter a room code.";
            yield break;
        }

        partnerStatusLabel.text = "Joining room...";

        string json = JsonUtility.ToJson(new JoinRoomRequestBody(trimmedCode));
        UnityWebRequest request = new UnityWebRequest(SessionState.JoinRoomUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        SessionState.ApplySessionCookie(request);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            partnerStatusLabel.text = GetRequestError(request, "Unable to join room.");
            yield break;
        }

        SessionState.RoomCode = trimmedCode;
        roomCodeLabel.text = "Room code: " + trimmedCode;
        partnerStatusLabel.text = "Joined room. Waiting for match sync...";
        NetworkSessionManager.EnsureExists().ConnectToRoom(trimmedCode);
    }

    void OnRoomStateChanged(NetworkSessionManager.RoomSnapshot snapshot)
    {
        roomCodeLabel.text = "Room code: " + snapshot.RoomCode;

        string waitingText = snapshot.GuestConnected
            ? "Both players connected. Starting match..."
            : "Room created. Waiting for the other player...";

        if (snapshot.LocalRole == "guest")
            waitingText = "Connected to room. Waiting for match start...";

        partnerStatusLabel.text = waitingText;
    }

    void OnMatchStarted()
    {
        GameModeState.SelectedMode = GameMode.Partner;
        SceneManager.LoadScene(GameSceneName);
    }

    void OnNetworkError(string errorMessage)
    {
        if (partnerStatusLabel != null)
            partnerStatusLabel.text = errorMessage;
    }

    string GetRequestError(UnityWebRequest request, string fallbackMessage)
    {
        if (!string.IsNullOrWhiteSpace(request.downloadHandler?.text))
            return request.downloadHandler.text;

        if (!string.IsNullOrWhiteSpace(request.error))
            return request.error;

        return fallbackMessage;
    }

    [System.Serializable]
    class JoinRoomRequestBody
    {
        public string roomCode;

        public JoinRoomRequestBody(string roomCodeValue)
        {
            roomCode = roomCodeValue;
        }
    }
}
