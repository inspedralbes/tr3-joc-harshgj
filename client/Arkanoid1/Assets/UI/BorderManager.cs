using System.Collections.Generic;
using UnityEngine;

public class BorderManager : MonoBehaviour
{
    public float wallThickness = 0.5f;
    public float horizontalMargin = 0.25f;
    public float blockGap = 0.12f;
    public float blockHeight = 0.45f;
    public float topRowY = 3.5f;
    public float rowSpacing = 0.6f;
    public int columns = 5;
    public int rows = 4;
    public float paddleWidthInBlocks = 1f;
    public float paddleHeight = 0.3f;

    private Camera mainCamera;

    void Awake()
    {
        mainCamera = Camera.main;
        CenterCamera();
        float blockWidth = LayoutBlocksForScreen();
        ResizePaddle(blockWidth);
        ResizeDeathZone();
    }

    void Start()
    {
        CreateBorders();
    }

    void CenterCamera()
    {
        if (mainCamera == null)
            return;

        Vector3 cameraPosition = mainCamera.transform.position;
        mainCamera.transform.position = new Vector3(0f, cameraPosition.y, cameraPosition.z);
    }

    float LayoutBlocksForScreen()
    {
        if (mainCamera == null)
            return 1.6f;

        GameObject[] blockObjects = GameObject.FindGameObjectsWithTag("Block");
        if (blockObjects.Length == 0)
            return 1.6f;

        List<Transform> blocks = new List<Transform>();
        foreach (GameObject block in blockObjects)
            blocks.Add(block.transform);

        blocks.Sort((a, b) =>
        {
            int rowCompare = b.position.y.CompareTo(a.position.y);
            if (rowCompare != 0)
                return rowCompare;

            return a.position.x.CompareTo(b.position.x);
        });

        float halfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        float usableWidth = Mathf.Max(1f, (halfWidth * 2f) - (horizontalMargin * 2f));
        float blockWidth = Mathf.Max(0.55f, (usableWidth - (blockGap * (columns - 1))) / columns);
        float totalWidth = (blockWidth * columns) + (blockGap * (columns - 1));
        float startX = -totalWidth * 0.5f + blockWidth * 0.5f;

        int maxBlocks = Mathf.Min(blocks.Count, rows * columns);
        for (int i = 0; i < maxBlocks; i++)
        {
            int row = i / columns;
            int column = i % columns;

            float x = startX + column * (blockWidth + blockGap);
            float y = topRowY - row * rowSpacing;

            Transform block = blocks[i];
            block.position = new Vector3(x, y, block.position.z);
            block.localScale = new Vector3(blockWidth, blockHeight, block.localScale.z);
        }

        return blockWidth;
    }

    void ResizePaddle(float blockWidth)
    {
        GameObject paddle = GameObject.FindGameObjectWithTag("Paddle");
        if (paddle == null)
            return;

        Vector3 scale = paddle.transform.localScale;
        paddle.transform.localScale = new Vector3(blockWidth * paddleWidthInBlocks, paddleHeight, scale.z);
    }

    void ResizeDeathZone()
    {
        if (mainCamera == null)
            return;

        GameObject deathZone = GameObject.Find("DeathZone");
        if (deathZone == null)
            return;

        BoxCollider2D deathZoneCollider = deathZone.GetComponent<BoxCollider2D>();
        if (deathZoneCollider == null)
            return;

        float width = (mainCamera.orthographicSize * mainCamera.aspect * 2f) + wallThickness * 2f;
        deathZone.transform.position = new Vector3(0f, deathZone.transform.position.y, deathZone.transform.position.z);
        deathZoneCollider.size = new Vector2(width, deathZoneCollider.size.y);
    }

    void CreateBorders()
    {
        if (mainCamera == null)
            return;

        float height = 2f * mainCamera.orthographicSize;
        float width = height * mainCamera.aspect;

        float halfW = width / 2f;
        float halfH = height / 2f;

        CreateWall("WallLeft", new Vector2(-halfW - wallThickness / 2f, 0f),
            new Vector2(wallThickness, height + wallThickness * 2f));

        CreateWall("WallRight", new Vector2(halfW + wallThickness / 2f, 0f),
            new Vector2(wallThickness, height + wallThickness * 2f));

        CreateWall("WallTop", new Vector2(0f, halfH + wallThickness / 2f),
            new Vector2(width + wallThickness * 2f, wallThickness));
    }

    void CreateWall(string wallName, Vector2 position, Vector2 size)
    {
        GameObject wall = new GameObject(wallName);
        wall.transform.position = position;

        BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
        col.size = size;

        wall.tag = "Wall";
        wall.transform.parent = transform;
    }
}
