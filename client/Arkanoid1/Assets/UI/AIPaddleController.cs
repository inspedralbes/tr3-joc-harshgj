using UnityEngine;

public class AIPaddleController : MonoBehaviour
{
    public Transform ballTarget;
    public float speed = 11f;
    public float edgePadding = 0.2f;
    public float reactionDeadZone = 0.15f;

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
        if (ballTarget == null)
            return;

        float currentX = transform.position.x;
        float targetX = ballTarget.position.x;

        if (Mathf.Abs(targetX - currentX) <= reactionDeadZone)
            return;

        float desiredX = Mathf.MoveTowards(currentX, targetX, speed * Time.deltaTime);
        float clampedX = Mathf.Clamp(desiredX, GetLeftLimit(), GetRightLimit());
        transform.position = new Vector3(clampedX, lockedYPosition, transform.position.z);
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
