using UnityEngine.Networking;

public static class SessionState
{
    public static string HttpBaseUrl { get; private set; } = "http://localhost:3000";
    public static string Username { get; private set; }
    public static string SessionCookie { get; private set; }
    public static string RoomCode { get; set; }
    public static string RoomRole { get; set; }

    public static string LoginUrl => HttpBaseUrl + "/api/login";
    public static string CreateRoomUrl => HttpBaseUrl + "/api/rooms";
    public static string JoinRoomUrl => HttpBaseUrl + "/api/rooms/join";

    public static string WebSocketUrl
    {
        get
        {
            if (HttpBaseUrl.StartsWith("https://"))
                return "wss://" + HttpBaseUrl.Substring("https://".Length);

            if (HttpBaseUrl.StartsWith("http://"))
                return "ws://" + HttpBaseUrl.Substring("http://".Length);

            return HttpBaseUrl;
        }
    }

    public static bool HasAuthenticatedSession =>
        !string.IsNullOrWhiteSpace(Username) &&
        !string.IsNullOrWhiteSpace(SessionCookie);

    public static void StoreLoginSession(string username, string setCookieHeader)
    {
        Username = username;
        SessionCookie = ExtractCookie(setCookieHeader);
    }

    public static void StoreLoginSessionCookie(string username, string cookieValue)
    {
        Username = username;
        SessionCookie = string.IsNullOrWhiteSpace(cookieValue) ? null : cookieValue.Trim();
    }

    public static void ApplySessionCookie(UnityWebRequest request)
    {
        if (!string.IsNullOrWhiteSpace(SessionCookie))
            request.SetRequestHeader("Cookie", SessionCookie);
    }

    public static void ClearRoomState()
    {
        RoomCode = null;
        RoomRole = null;
    }

    static string ExtractCookie(string setCookieHeader)
    {
        if (string.IsNullOrWhiteSpace(setCookieHeader))
            return null;

        int separatorIndex = setCookieHeader.IndexOf(';');
        if (separatorIndex >= 0)
            return setCookieHeader.Substring(0, separatorIndex);

        return setCookieHeader;
    }
}
