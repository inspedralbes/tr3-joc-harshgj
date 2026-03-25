using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using System.Collections;

public class LoginUI : MonoBehaviour
{
    private TextField usernameField;
    private TextField passwordField;
    private Button loginButton;
    private Label statusLabel;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        usernameField = root.Q<TextField>("usernameField");
        passwordField = root.Q<TextField>("passwordField");
        loginButton = root.Q<Button>("loginButton");
        statusLabel = root.Q<Label>("statusLabel");

        loginButton.clicked += OnLoginClicked;
    }

    private void OnLoginClicked()
    {
        string username = usernameField.value;
        string password = passwordField.value;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            statusLabel.text = "Please enter username and password";
            return;
        }

        StartCoroutine(LoginRequest(username, password));
    }

    private IEnumerator LoginRequest(string username, string password)
    {
        string url = "http://localhost:3000/api/login";

        WWWForm form = new WWWForm();
        form.AddField("username", username);
        form.AddField("password", password);

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            // Important for sessions/cookies
            request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                statusLabel.text = "Error: " + request.error;
            }
            else
            {
                string response = request.downloadHandler.text;
                Debug.Log("Server response: " + response);
                
                // Example: check if login was successful
                if (response.Contains("Login successful"))
                {
                    statusLabel.text = "Login successful!";
                    // TODO: Load next scene or dashboard
                }
                else
                {
                    statusLabel.text = "Invalid username or password";
                }
            }
        }
    }
}