using UnityEngine;

public class PaddleMovement : MonoBehaviour
{
    [Header("Keyboard Controls")]
    public KeyCode moveLeftKey = KeyCode.LeftArrow;
    public KeyCode moveLeftAltKey = KeyCode.A;
    public KeyCode moveRightKey = KeyCode.RightArrow;
    public KeyCode moveRightAltKey = KeyCode.D;

    public float speed = 10f;
    public float touchFollowSpeed = 25f;
    public float edgePadding = 0.2f;
    public bool allowPointerInput = true;

    private Camera mainCamera;
    private Collider2D paddleCollider;

    void Awake()
    {
        mainCamera = Camera.main;
        paddleCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
        float currentX = transform.position.x;
        float targetX = currentX;

        bool usingTouch = false;

        if (allowPointerInput && Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector3 touchWorld = mainCamera.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, -mainCamera.transform.position.z));
            targetX = touchWorld.x;
            usingTouch = true;
        }
        else if (allowPointerInput && Input.GetMouseButton(0) && !Application.isMobilePlatform)
        {
            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -mainCamera.transform.position.z));
            targetX = mouseWorld.x;
            usingTouch = true;
        }
        else
        {
            float move = 0f;

            if (Input.GetKey(moveLeftKey) || Input.GetKey(moveLeftAltKey))
                move = -1f;

            if (Input.GetKey(moveRightKey) || Input.GetKey(moveRightAltKey))
                move = 1f;

            targetX = currentX + move * speed * Time.deltaTime;
        }

        if (usingTouch)
            targetX = Mathf.MoveTowards(currentX, targetX, touchFollowSpeed * Time.deltaTime);

        float clampedX = Mathf.Clamp(targetX, GetLeftLimit(), GetRightLimit());
        transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
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
