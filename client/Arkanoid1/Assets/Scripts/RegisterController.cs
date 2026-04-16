using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class RegisterController : MonoBehaviour
{
    [System.Serializable]
    class RegisterResponse
    {
        public string message;
        public string error;
    }

    private const string LoginSceneName = "SampleScene";

    private TextField usernameField;
    private TextField passwordField;
    private TextField confirmPasswordField;
    private Button registerButton;
    private Button backButton;
    private Label statusLabel;

    void Start()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        usernameField = root.Q<TextField>("usernameField");
        passwordField = root.Q<TextField>("passwordField");
        confirmPasswordField = root.Q<TextField>("confirmPasswordField");
        registerButton = root.Q<Button>("registerButton");
        backButton = root.Q<Button>("backButton");
        statusLabel = root.Q<Label>("statusLabel");

        if (registerButton != null)
            registerButton.clicked += OnRegisterClicked;

        if (backButton != null)
            backButton.clicked += BackToLogin;
    }

    void OnRegisterClicked()
    {
        if (!ValidateRegistration())
            return;

        StartCoroutine(RegisterRequest(usernameField.value.Trim(), passwordField.value));
    }

    bool ValidateRegistration()
    {
        string username = usernameField != null ? usernameField.value.Trim() : string.Empty;
        string password = passwordField != null ? passwordField.value : string.Empty;
        string confirmation = confirmPasswordField != null ? confirmPasswordField.value : string.Empty;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmation))
        {
            statusLabel.text = "All fields are required";
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

        if (password != confirmation)
        {
            statusLabel.text = "Passwords do not match";
            return false;
        }

        statusLabel.text = string.Empty;
        return true;
    }

    IEnumerator RegisterRequest(string username, string password)
    {
        string json = JsonUtility.ToJson(new RegisterData(username, password));

        UnityWebRequest request = new UnityWebRequest(SessionState.RegisterUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        RegisterResponse response = string.IsNullOrWhiteSpace(request.downloadHandler.text)
            ? null
            : JsonUtility.FromJson<RegisterResponse>(request.downloadHandler.text);

        if (request.result != UnityWebRequest.Result.Success)
        {
            statusLabel.text = response != null && !string.IsNullOrWhiteSpace(response.error)
                ? response.error
                : "Registration failed";
            yield break;
        }

        if (response == null || response.message != "User registered")
        {
            statusLabel.text = response != null && !string.IsNullOrWhiteSpace(response.error)
                ? response.error
                : "Registration failed";
            yield break;
        }

        statusLabel.text = "Registration successful";
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(LoginSceneName);
    }

    void BackToLogin()
    {
        SceneManager.LoadScene(LoginSceneName);
    }

    [System.Serializable]
    class RegisterData
    {
        public string username;
        public string password;

        public RegisterData(string user, string pass)
        {
            username = user;
            password = pass;
        }
    }
}
