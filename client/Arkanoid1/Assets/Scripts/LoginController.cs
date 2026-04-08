using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using UnityEngine.Networking;
using System.Text;
using UnityEngine.SceneManagement;

public class LoginController : MonoBehaviour
{
    private TextField usernameField;
    private TextField passwordField;
    private Button loginButton;
    private Label statusLabel;

    private string serverUrl = "http://localhost:3000/api/login";

    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        usernameField = root.Q<TextField>("usernameField");
        passwordField = root.Q<TextField>("passwordField");
        loginButton = root.Q<Button>("loginButton");
        statusLabel = root.Q<Label>("statusLabel");

        loginButton.clicked += OnLoginClicked;
    }

    void OnLoginClicked()
    {
        StartCoroutine(LoginRequest(
            usernameField.value,
            passwordField.value
        ));
    }

    IEnumerator LoginRequest(string username, string password)
    {
        var json = JsonUtility.ToJson(new LoginData(username, password));

        UnityWebRequest req = new UnityWebRequest(serverUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var response = req.downloadHandler.text;

            if (response.Contains("Login successful"))
            {
                statusLabel.text = "✅ Login success!";
                yield return new WaitForSeconds(1f);
                SceneManager.LoadScene("0");
            }
            else
            {
                statusLabel.text = "❌ " + response;
            }
        }
    }

    [System.Serializable]
    public class LoginData
    {
        public string username;
        public string password;

        public LoginData(string u, string p)
        {
            username = u;
            password = p;
        }
    }
}