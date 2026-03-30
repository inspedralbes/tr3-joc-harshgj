using UnityEngine;

public class Ball : MonoBehaviour
{
    public float speed = 8f;
    private Rigidbody2D rb;
    private bool launched = false;

    // Store screen bounds for manual clamping backup
    private float minX, maxX, maxY;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Calculate screen bounds
        Camera cam = Camera.main;
        float height = cam.orthographicSize;
        float width = height * cam.aspect;
        minX = -width;
        maxX = width;
        maxY = height;
    }

    void Update()
    {
        // Press Space to launch
        if (!launched && Input.GetKeyDown(KeyCode.Space))
        {
            launched = true;
            float randomX = Random.Range(-0.4f, 0.4f);
            rb.linearVelocity = new Vector2(randomX, 1f).normalized * speed;
        }

        // Keep constant speed
        if (launched && rb.linearVelocity.magnitude < speed * 0.95f)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * speed;
        }

        // Safety clamp — in case ball somehow clips through a wall
        Vector3 pos = transform.position;
        float r = transform.localScale.x / 2f;

        if (pos.x - r < minX)
        {
            transform.position = new Vector3(minX + r, pos.y, 0);
            rb.linearVelocity = new Vector2(Mathf.Abs(rb.linearVelocity.x), rb.linearVelocity.y);
        }
        else if (pos.x + r > maxX)
        {
            transform.position = new Vector3(maxX - r, pos.y, 0);
            rb.linearVelocity = new Vector2(-Mathf.Abs(rb.linearVelocity.x), rb.linearVelocity.y);
        }

        if (pos.y + r > maxY)
        {
            transform.position = new Vector3(pos.x, maxY - r, 0);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -Mathf.Abs(rb.linearVelocity.y));
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Paddle"))
        {
            // Left side hit = go left, right side hit = go right
            float hitFactor = (transform.position.x - collision.transform.position.x)
                              / (collision.collider.bounds.size.x / 2f);

            hitFactor = Mathf.Clamp(hitFactor, -0.85f, 0.85f);

            Vector2 dir = new Vector2(hitFactor, 1f).normalized;
            rb.linearVelocity = dir * speed;
        }
    }

    public void ResetBall(Vector3 startPos)
    {
        launched = false;
        transform.position = startPos;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }
}
