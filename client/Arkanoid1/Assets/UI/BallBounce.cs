using UnityEngine;

public class Ball : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 8f;
    public float paddleBounceHeight = 1f;
    public float horizontalBounceStrength = 0.9f;

    private Rigidbody2D rb;
    private CircleCollider2D circleCollider;
    private BoxCollider2D extraBoxCollider;
    private PhysicsMaterial2D bounceMaterial;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        circleCollider = GetComponent<CircleCollider2D>();
        extraBoxCollider = GetComponent<BoxCollider2D>();

        if (rb == null)
        {
            Debug.LogError("Ball needs a Rigidbody2D component.");
            enabled = false;
            return;
        }

        ConfigurePhysics();
    }

    void Start()
    {
        ResetBall(transform.position);
        Launch();
    }

    void ConfigurePhysics()
    {
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        bounceMaterial = new PhysicsMaterial2D("BallBounce")
        {
            bounciness = 1f,
            friction = 0f
        };

        if (circleCollider != null)
            circleCollider.sharedMaterial = bounceMaterial;

        if (extraBoxCollider != null)
            extraBoxCollider.enabled = false;
    }

    void FixedUpdate()
    {
        if (rb.linearVelocity.sqrMagnitude <= 0.001f)
        {
            Launch();
            return;
        }

        rb.linearVelocity = rb.linearVelocity.normalized * speed;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Paddle"))
            BounceFromPaddle(collision);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Paddle") && rb.linearVelocity.y <= 0f)
            BounceFromPaddle(collision);
    }

    void BounceFromPaddle(Collision2D collision)
    {
        ArkanoidAgent agent = collision.gameObject.GetComponent<ArkanoidAgent>();
        if (agent != null)
            agent.NotifyPaddleHit();

        float paddleHalfWidth = collision.collider.bounds.extents.x;
        float hitOffset = transform.position.x - collision.transform.position.x;
        float normalizedOffset = paddleHalfWidth > 0f ? hitOffset / paddleHalfWidth : 0f;
        normalizedOffset = Mathf.Clamp(normalizedOffset, -1f, 1f);

        Vector2 direction = new Vector2(normalizedOffset * horizontalBounceStrength, paddleBounceHeight).normalized;

        rb.position += Vector2.up * 0.08f;
        rb.linearVelocity = direction * speed;
    }

    public void ResetBall(Vector3 startPos)
    {
        transform.position = startPos;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = 0f;
        rb.simulated = true;
    }

    public void Launch()
    {
        float horizontal = Random.Range(-0.6f, 0.6f);
        Vector2 direction = new Vector2(horizontal, 1f).normalized;
        rb.linearVelocity = direction * speed;
    }
}
