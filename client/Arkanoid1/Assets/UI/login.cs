using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;

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
        string username = usernameField.value.Trim();
        string password = passwordField.value.Trim();

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
            request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                statusLabel.text = "Network Error: " + request.error;
            }
            else
            {
                string response = request.downloadHandler.text;
                Debug.Log("Server response: " + response);

                try
                {
                    // Parse JSON response
                    var json = JsonUtility.FromJson<LoginResponse>(response);

                    if (!string.IsNullOrEmpty(json.message) && json.message.ToLower().Contains("login successful"))
                    {
                        statusLabel.text = "Login successful!";

                        // Save username to PlayerPrefs for later (e.g., WebSocket connection)
                        PlayerPrefs.SetString("username", username);

                        // TODO: Load next scene or dashboard
                        // UnityEngine.SceneManagement.SceneManager.LoadScene("Dashboard");
                    }
                    else if (!string.IsNullOrEmpty(json.error))
                    {
                        statusLabel.text = json.error;
                    }
                    else
                    {
                        statusLabel.text = "Unknown server response";
                    }
                }
                catch (Exception ex)
                {
                    statusLabel.text = "JSON parse error: " + ex.Message;
                }
            }
        }
    }

    [Serializable]
    private class LoginResponse
    {
        public string message;
        public string error;
        public string username;
    }
}