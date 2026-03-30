using UnityEngine;

// Attach this script to an empty GameObject called "BorderManager"
// It will automatically create invisible walls around the screen at runtime

public class BorderManager : MonoBehaviour
{
    public float wallThickness = 0.5f;

    void Start()
    {
        CreateBorders();
    }

    void CreateBorders()
    {
        // Get screen size in world units
        Camera cam = Camera.main;
        float height = 2f * cam.orthographicSize;
        float width = height * cam.aspect;

        float halfW = width / 2f;
        float halfH = height / 2f;

        // LEFT wall
        CreateWall("WallLeft",   new Vector2(-halfW - wallThickness / 2f, 0),
                                 new Vector2(wallThickness, height + wallThickness * 2f));

        // RIGHT wall
        CreateWall("WallRight",  new Vector2(halfW + wallThickness / 2f, 0),
                                 new Vector2(wallThickness, height + wallThickness * 2f));

        // TOP wall
        CreateWall("WallTop",    new Vector2(0, halfH + wallThickness / 2f),
                                 new Vector2(width + wallThickness * 2f, wallThickness));

        // BOTTOM — this is the DeathZone, NOT a solid wall
        // The ball falls through here and triggers life loss
        // So we do NOT create a bottom wall
        // Your DeathZone collider (IsTrigger) should sit here instead
    }

    void CreateWall(string wallName, Vector2 position, Vector2 size)
    {
        GameObject wall = new GameObject(wallName);
        wall.transform.position = position;

        BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
        col.size = size;

        // Give it the "Wall" physics material if you have one,
        // or just leave it — default physics will reflect the ball
        wall.tag = "Wall"; // optional, set up tag in Unity if you want

        // Make it a child of this manager for clean hierarchy
        wall.transform.parent = this.transform;
    }
}
