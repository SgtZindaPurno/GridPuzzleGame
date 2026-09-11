using UnityEngine;
using UnityEngine.Events;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int rows;
    public int cols;

    // The core data model: 0 = empty, >0 = tile value
    private int[,] grid;

  

    [Header("Events")]
    public UnityEvent OnInitialize;              // Grid ready
    public UnityEvent OnBeforeMove;              // Fires BEFORE any grid mutation (history capture)
    public UnityEvent<int> OnMergeOccurred;      // Fires per merge, with merged value
    public UnityEvent OnMoveCompleted;           // A valid move finished (visualizer refresh)
    public UnityEvent OnMoveBlocked;             // Swipe produced no change

    public bool SuppressSpawn = false;
    void Start()
    {
        InitializeGrid();
        SpawnTile();
        SpawnTile();
        PrintGrid();
        OnInitialize?.Invoke();
    }

    // --- Public grid API ---
    public int[,] GetGridData() => grid;

    public void SetGridData(int[,] newGrid)
    {
        // Deep copy so external systems can't mutate our internal state
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                grid[r, c] = newGrid[r, c];

        PrintGrid();
    }

    private void InitializeGrid()
    {
        grid = new int[rows, cols];
        Debug.Log($"Grid initialized: {rows}x{cols}");
    }

    private void SpawnTile()
    {
        int emptyCount = 0;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                if (grid[r, c] == 0) emptyCount++;

        if (emptyCount == 0)
        {
            Debug.LogWarning("No empty cells to spawn!");
            return;
        }

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
                gridStr += grid[r, c] + "\t";
            gridStr += "\n";
        }
        Debug.Log(gridStr);
    }

    private enum Direction { Up, Down, Left, Right }

    public void HandleSwipe(int direction)
    {
        OnBeforeMove?.Invoke();
        bool moved = TryMove((Direction)direction);

        if (moved) OnMoveCompleted?.Invoke();
        else OnMoveBlocked?.Invoke();
    }

    private bool TryMove(Direction dir)
    {
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
            if (!SuppressSpawn) SpawnTile();
            PrintGrid();
            Debug.Log($"Move {dir} successful!");
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
            int[] processed = ProcessLine(GetRow(r));
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
            int[] processed = ProcessLine(GetColumn(c));
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

    // --- Line processing ---
    private int[] ProcessLine(int[] line)
    {
        int[] compacted = new int[line.Length];
        int index = 0;
        for (int i = 0; i < line.Length; i++)
            if (line[i] != 0) compacted[index++] = line[i];

        for (int i = 0; i < compacted.Length - 1; i++)
        {
            if (compacted[i] != 0 && compacted[i] == compacted[i + 1])
            {
                compacted[i] *= 2;
                OnMergeOccurred?.Invoke(compacted[i]);   // <-- Score hook
                compacted[i + 1] = 0;
                i++;
            }
        }

        int[] result = new int[line.Length];
        index = 0;
        for (int i = 0; i < compacted.Length; i++)
            if (compacted[i] != 0) result[index++] = compacted[i];

        return result;
    }

    // --- Helpers ---
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