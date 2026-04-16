using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class LoginController : MonoBehaviour
{
    [System.Serializable]
    class LoginResponse
    {
        public string message;
        public string username;
        public string sessionCookie;
        public string error;
    }

    private const string PostLoginSceneName = "GameMenu";
    private const string RegisterSceneName = "Register";

    private TextField usernameField;
    private TextField passwordField;
    private Button loginButton;
    private Button registerButton;
    private Label statusLabel;

    void Start()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        usernameField = root.Q<TextField>("usernameField");
        passwordField = root.Q<TextField>("passwordField");
        loginButton = root.Q<Button>("loginButton");
        registerButton = root.Q<Button>("registerButton");
        statusLabel = root.Q<Label>("statusLabel");

        if (loginButton != null)
            loginButton.clicked += OnLoginClicked;

        if (registerButton != null)
            registerButton.clicked += OnRegisterClicked;
    }

    void OnLoginClicked()
    {
        if (!ValidateCredentials())
            return;

        StartCoroutine(LoginRequest(usernameField.value, passwordField.value));
    }

    void OnRegisterClicked()
    {
        SceneManager.LoadScene(RegisterSceneName);
    }

    bool ValidateCredentials()
    {
        string username = usernameField != null ? usernameField.value.Trim() : string.Empty;
        string password = passwordField != null ? passwordField.value : string.Empty;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            statusLabel.text = "Username and password are required";
            return false;
        }

        if (username.Length < 3)
        {
            statusLabel.text = "Username must have at least 3 characters";
            return false;
        }

        if (password.Length < 6)
        {
            statusLabel.text = "Password must have at least 6 characters";
            return false;
        }

        statusLabel.text = string.Empty;
        return true;
    }

    IEnumerator LoginRequest(string username, string password)
    {
        string json = JsonUtility.ToJson(new LoginData(username, password));

        UnityWebRequest request = new UnityWebRequest(SessionState.LoginUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            statusLabel.text = "Login failed";
            yield break;
        }

        LoginResponse response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
        if (response == null || response.message != "Login successful")
        {
            statusLabel.text = response != null && !string.IsNullOrWhiteSpace(response.error)
                ? response.error
                : "Login failed";
            yield break;
        }

        string sessionCookie = !string.IsNullOrWhiteSpace(response.sessionCookie)
            ? response.sessionCookie
            : request.GetResponseHeader("Set-Cookie");

        SessionState.StoreLoginSession(response.username, sessionCookie);
        statusLabel.text = "Login success";
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(PostLoginSceneName);
    }

    [System.Serializable]
    public class LoginData
    {
        public string username;
        public string password;

        public LoginData(string user, string pass)
        {
            username = user;
            password = pass;
        }
    }
}
