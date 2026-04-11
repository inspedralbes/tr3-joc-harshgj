using UnityEngine;

public class LocalPaddleNetworkSync : MonoBehaviour
{
    public float edgePadding = 0.2f;

    private Camera mainCamera;
    private Collider2D paddleCollider;

    void Awake()
    {
        mainCamera = Camera.main;
        paddleCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
        NetworkSessionManager manager = NetworkSessionManager.Instance;
        if (manager == null || !manager.IsConnected)
            return;

        float normalizedX = Mathf.InverseLerp(GetLeftLimit(), GetRightLimit(), transform.position.x);
        manager.SendPaddleMove(normalizedX);
    }

    float GetLeftLimit()
    {
        return -GetCameraHalfWidth() + GetPaddleHalfWidth() + edgePadding;
    }

    float GetRightLimit()
    {
        return GetCameraHalfWidth() - GetPaddleHalfWidth() - edgePadding;
    }

    float GetCameraHalfWidth()
    {
        if (mainCamera == null)
            return 8f;

        return mainCamera.orthographicSize * mainCamera.aspect;
    }

    float GetPaddleHalfWidth()
    {
        if (paddleCollider == null)
            return 0.75f;

        return paddleCollider.bounds.extents.x;
    }
}
