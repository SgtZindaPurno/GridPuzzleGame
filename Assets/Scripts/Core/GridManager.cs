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
        TryMove(dir);
    }
    private bool TryMove(Direction dir)
    {
        int[,] previousGrid = (int[,])grid.Clone(); // Snapshot to check if grid changed
        bool moved = false;

        switch (dir)
        {
            case Direction.Left: moved = MoveLeft(); break;
            case Direction.Right: moved = MoveRight(); break;
            case Direction.Up: moved = MoveUp(); break;
            case Direction.Down: moved = MoveDown(); break;
        }

        if (moved)
        {
            Debug.Log($"Move {dir} successful!");
            SpawnTile();
            PrintGrid();
            // Optional: Check win/lose condition here later
        }
        else
        {
            Debug.Log($"Move {dir} blocked - no changes");
        }

        return moved;
    }

    // --- Directional implementations ---
    private bool MoveLeft()
    {
        bool changed = false;
        for (int r = 0; r < rows; r++)
        {
            int[] line = GetRow(r);
            int[] processed = ProcessLine(line);
            if (SetRow(r, processed)) changed = true;
        }
        return changed;
    }

    private bool MoveRight()
    {
        bool changed = false;
        for (int r = 0; r < rows; r++)
        {
            int[] line = GetRow(r);
            System.Array.Reverse(line);
            int[] processed = ProcessLine(line);
            System.Array.Reverse(processed);
            if (SetRow(r, processed)) changed = true;
        }
        return changed;
    }

    private bool MoveUp()
    {
        bool changed = false;
        for (int c = 0; c < cols; c++)
        {
            int[] line = GetColumn(c);
            int[] processed = ProcessLine(line);
            if (SetColumn(c, processed)) changed = true;
        }
        return changed;
    }

    private bool MoveDown()
    {
        bool changed = false;
        for (int c = 0; c < cols; c++)
        {
            int[] line = GetColumn(c);
            System.Array.Reverse(line);
            int[] processed = ProcessLine(line);
            System.Array.Reverse(processed);
            if (SetColumn(c, processed)) changed = true;
        }
        return changed;
    }

    // --- Core Line Processing (Compact + Merge) ---
    private int[] ProcessLine(int[] line)
    {
        // Step 1: Compact (remove zeros)
        int[] compacted = new int[line.Length];
        int index = 0;
        for (int i = 0; i < line.Length; i++)
            if (line[i] != 0)
                compacted[index++] = line[i];

        // Step 2: Merge adjacent equal numbers
        for (int i = 0; i < compacted.Length - 1; i++)
        {
            if (compacted[i] != 0 && compacted[i] == compacted[i + 1])
            {
                compacted[i] *= 2;
                compacted[i + 1] = 0;
                i++; // Skip the next tile to avoid double-merging in one move
            }
        }

        // Step 3: Compact again (to push merged tiles to the side)
        int[] result = new int[line.Length];
        index = 0;
        for (int i = 0; i < compacted.Length; i++)
            if (compacted[i] != 0)
                result[index++] = compacted[i];

        return result;
    }

    // --- Helpers to get/set rows and columns ---
    private int[] GetRow(int row)
    {
        int[] line = new int[cols];
        for (int c = 0; c < cols; c++) line[c] = grid[row, c];
        return line;
    }

    private bool SetRow(int row, int[] line)
    {
        bool changed = false;
        for (int c = 0; c < cols; c++)
        {
            if (grid[row, c] != line[c]) changed = true;
            grid[row, c] = line[c];
        }
        return changed;
    }

    private int[] GetColumn(int col)
    {
        int[] line = new int[rows];
        for (int r = 0; r < rows; r++) line[r] = grid[r, col];
        return line;
    }

    private bool SetColumn(int col, int[] line)
    {
        bool changed = false;
        for (int r = 0; r < rows; r++)
        {
            if (grid[r, col] != line[r]) changed = true;
            grid[r, col] = line[r];
        }
        return changed;
    }
}