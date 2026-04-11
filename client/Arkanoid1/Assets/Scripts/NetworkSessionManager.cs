using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class NetworkSessionManager : MonoBehaviour
{
    [Serializable]
    class SocketMessage
    {
        public string type;
        public string roomCode;
        public string role;
        public string hostUsername;
        public string guestUsername;
        public bool hostConnected;
        public bool guestConnected;
        public bool started;
        public float normalizedX;
        public string error;
    }

    [Serializable]
    class JoinRoomMessage
    {
        public string type = "join_room";
        public string roomCode;
    }

    [Serializable]
    class PaddleMoveMessage
    {
        public string type = "paddle_move";
        public float normalizedX;
    }

    public struct RoomSnapshot
    {
        public string RoomCode;
        public string LocalRole;
        public string HostUsername;
        public string GuestUsername;
        public bool HostConnected;
        public bool GuestConnected;
        public bool Started;
    }

    public static NetworkSessionManager Instance { get; private set; }

    public event Action<RoomSnapshot> RoomStateChanged;
    public event Action<string> ErrorReceived;
    public event Action MatchStarted;

    public string LocalRole { get; private set; }
    public string CurrentRoomCode { get; private set; }
    public bool IsConnected => socket != null && socket.State == WebSocketState.Open;
    public float RemoteNormalizedX { get; private set; } = 0.5f;

    private ClientWebSocket socket;
    private CancellationTokenSource cancellationTokenSource;
    private readonly ConcurrentQueue<Action> mainThreadQueue = new ConcurrentQueue<Action>();
    private bool matchStartedNotified;
    private float lastSentNormalizedX = -1f;

    public static NetworkSessionManager EnsureExists()
    {
        if (Instance != null)
            return Instance;

        GameObject managerObject = new GameObject("NetworkSessionManager");
        DontDestroyOnLoad(managerObject);
        Instance = managerObject.AddComponent<NetworkSessionManager>();
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        while (mainThreadQueue.TryDequeue(out Action action))
            action?.Invoke();
    }

    void OnDestroy()
    {
        _ = DisconnectAsync();
    }

    public async void ConnectToRoom(string roomCode)
    {
        await ConnectToRoomAsync(roomCode);
    }

    public async Task DisconnectAsync()
    {
        matchStartedNotified = false;
        lastSentNormalizedX = -1f;
        RemoteNormalizedX = 0.5f;
        CurrentRoomCode = null;
        LocalRole = null;

        if (cancellationTokenSource != null)
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
        }

        if (socket == null)
            return;

        try
        {
            if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }
        catch
        {
        }
        finally
        {
            socket.Dispose();
            socket = null;
        }
    }

    public void SendPaddleMove(float normalizedX)
    {
        if (!IsConnected || string.IsNullOrWhiteSpace(CurrentRoomCode))
            return;

        if (Mathf.Abs(lastSentNormalizedX - normalizedX) < 0.0025f)
            return;

        lastSentNormalizedX = normalizedX;
        PaddleMoveMessage message = new PaddleMoveMessage
        {
            normalizedX = Mathf.Clamp01(normalizedX)
        };

        _ = SendJsonAsync(JsonUtility.ToJson(message));
    }

    async Task ConnectToRoomAsync(string roomCode)
    {
        await DisconnectAsync();

        if (!SessionState.HasAuthenticatedSession)
        {
            EmitError("You must log in before joining a room.");
            return;
        }

        cancellationTokenSource = new CancellationTokenSource();
        socket = new ClientWebSocket();
        socket.Options.SetRequestHeader("Cookie", SessionState.SessionCookie);

        string socketUrl = SessionState.WebSocketUrl;

        try
        {
            await socket.ConnectAsync(new Uri(socketUrl), cancellationTokenSource.Token);

            CurrentRoomCode = roomCode;
            await SendJsonAsync(JsonUtility.ToJson(new JoinRoomMessage { roomCode = roomCode }));
            _ = ReceiveLoopAsync(socket, cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            EmitError("Room connection failed: " + ex.Message);
        }
    }

    async Task ReceiveLoopAsync(ClientWebSocket webSocket, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[2048];

        try
        {
            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                string payload = Encoding.UTF8.GetString(buffer, 0, result.Count);
                HandleMessage(payload);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            EmitError("WebSocket error: " + ex.Message);
        }
    }

    void HandleMessage(string payload)
    {
        SocketMessage message = JsonUtility.FromJson<SocketMessage>(payload);
        if (message == null || string.IsNullOrWhiteSpace(message.type))
            return;

        if (message.type == "room_state")
        {
            mainThreadQueue.Enqueue(() =>
            {
                CurrentRoomCode = message.roomCode;
                LocalRole = message.role;
                SessionState.RoomCode = message.roomCode;
                SessionState.RoomRole = message.role;

                RoomSnapshot snapshot = new RoomSnapshot
                {
                    RoomCode = message.roomCode,
                    LocalRole = message.role,
                    HostUsername = message.hostUsername,
                    GuestUsername = message.guestUsername,
                    HostConnected = message.hostConnected,
                    GuestConnected = message.guestConnected,
                    Started = message.started
                };

                RoomStateChanged?.Invoke(snapshot);

                if (message.started && !matchStartedNotified)
                {
                    matchStartedNotified = true;
                    MatchStarted?.Invoke();
                }
            });

            return;
        }

        if (message.type == "paddle_move")
        {
            mainThreadQueue.Enqueue(() => RemoteNormalizedX = Mathf.Clamp01(message.normalizedX));
            return;
        }

        if (message.type == "error")
            EmitError(message.error ?? "Unknown room error.");
    }

    void EmitError(string errorMessage)
    {
        mainThreadQueue.Enqueue(() => ErrorReceived?.Invoke(errorMessage));
    }

    async Task SendJsonAsync(string json)
    {
        if (!IsConnected)
            return;

        byte[] messageBuffer = Encoding.UTF8.GetBytes(json);
        await socket.SendAsync(
            new ArraySegment<byte>(messageBuffer),
            WebSocketMessageType.Text,
            true,
            cancellationTokenSource != null ? cancellationTokenSource.Token : CancellationToken.None
        );
    }
}
