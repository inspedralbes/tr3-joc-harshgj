using UnityEngine;

public class PaddleMovement : MonoBehaviour
{
    public float speed = 10f;
    public float leftLimit = -7.5f;  // Adjust to match your wall positions
    public float rightLimit = 7.5f;

    void Update()
    {
        float move = 0f;

        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            move = -1f;

        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            move = 1f;

        transform.Translate(Vector2.right * move * speed * Time.deltaTime);

        // Clamp paddle so it can't leave the screen
        float clampedX = Mathf.Clamp(transform.position.x, leftLimit, rightLimit);
        transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
    }
}