using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int rows = 4;
    [SerializeField] private int cols = 4;

    // The core data model: 0 = empty, >0 = tile value
    private int[,] grid;

    // Input tracking
    private Vector2 touchStartPos;
    private Vector2 touchEndPos;
    private float minSwipeDistance = 30f;

    // --- Step 1: Grid Initialization ---
    void Start()
    {
        InitializeGrid();

        // --- Step 3: Spawn starting numbers (will be added later) ---
         SpawnTile(); //Calling this twice since we need to spawn two tiles with numbers on start
         SpawnTile();
         PrintGrid();
    }

    private void InitializeGrid()
    {
        grid = new int[rows, cols];
        Debug.Log($"Grid initialized: {rows}x{cols}");

       
    }
    private void SpawnTile()
    {
        // Find all empty positions
        int emptyCount = 0;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                if (grid[r, c] == 0) emptyCount++;

        if (emptyCount == 0)
        {
            Debug.LogWarning("No empty cells to spawn!");
            return;
        }

        // Pick a random empty cell
        int targetIndex = Random.Range(0, emptyCount);
        int currentIndex = 0;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (grid[r, c] == 0)
                {
                    if (currentIndex == targetIndex)
                    {
                        // 90% chance of 2, 10% chance of 4
                        int value = Random.Range(0, 10) < 9 ? 2 : 4;
                        grid[r, c] = value;
                        Debug.Log($"Spawned {value} at ({r}, {c})");
                        return;
                    }
                    currentIndex++;
                }
            }
        }
    }
    [ContextMenu("Print Grid")]
    private void PrintGrid()
    {
        string gridStr = "";
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                gridStr += grid[r, c] + "\t";
            }
            gridStr += "\n";
        }
        Debug.Log(gridStr);
    }

    // --- Step 2: Swipe Input Detection ---
    void Update()
    {
        DetectSwipe();
    }

    private void DetectSwipe()
    {
        // Mouse input (for testing in Editor)
        if (Input.GetMouseButtonDown(0))
        {
            touchStartPos = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            touchEndPos = Input.mousePosition;
            ProcessSwipe();
        }

        // Touch input (for mobile)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                touchStartPos = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                touchEndPos = touch.position;
                ProcessSwipe();
            }
        }
    }

    private void ProcessSwipe()
    {
        Vector2 swipeDelta = touchEndPos - touchStartPos;

        // Ignore tiny swipes
        if (swipeDelta.magnitude < minSwipeDistance)
            return;

        // Determine direction
        if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
        {
            // Horizontal
            if (swipeDelta.x > 0)
                HandleSwipe(Direction.Right);
            else
                HandleSwipe(Direction.Left);
        }
        else
        {
            // Vertical
            if (swipeDelta.y > 0)
                HandleSwipe(Direction.Up);
            else
                HandleSwipe(Direction.Down);
        }
    }

    private enum Direction { Up, Down, Left, Right }

    private void HandleSwipe(Direction dir)
    {
        // --- Step 2: Debug log the direction ---
        Debug.Log($"Swipe Detected: {dir}");

        // --- Steps 4 & 5: We'll call the move logic here ---
        // bool moved = TryMove(dir);
        // if (moved) { SpawnTile(); PrintGrid(); }
        // else { Debug.Log("Invalid move - blocked"); }
    }
}