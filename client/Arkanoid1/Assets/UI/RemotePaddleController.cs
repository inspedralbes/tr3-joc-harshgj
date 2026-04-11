using UnityEngine;

public class RemotePaddleController : MonoBehaviour
{
    public float edgePadding = 0.2f;
    public float followSpeed = 20f;

    private Camera mainCamera;
    private Collider2D paddleCollider;
    private float lockedYPosition;

    void Awake()
    {
        mainCamera = Camera.main;
        paddleCollider = GetComponent<Collider2D>();
        lockedYPosition = transform.position.y;
    }

    void Update()
    {
        NetworkSessionManager manager = NetworkSessionManager.Instance;
        if (manager == null)
            return;

        float targetX = Mathf.Lerp(GetLeftLimit(), GetRightLimit(), manager.RemoteNormalizedX);
        float currentX = transform.position.x;
        float nextX = Mathf.MoveTowards(currentX, targetX, followSpeed * Time.deltaTime);
        transform.position = new Vector3(nextX, lockedYPosition, transform.position.z);
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
