using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int rows = 4;
    public int cols = 4;

    [Header("Animation")]
    [SerializeField] private float moveAnimDuration = 0.12f;

    [Header("Events")]
    public UnityEvent OnInitialize;
    public UnityEvent OnBeforeMove;
    public UnityEvent<MoveResult> OnMovePlanned;
    public UnityEvent<int> OnMergeOccurred;
    public UnityEvent OnMoveCompleted;
    public UnityEvent OnMoveBlocked;

    public bool SuppressSpawn { get; set; } = false;

    private TileData[,] grid;
    private int nextTileId = 1;
    private bool isAnimating = false;

    private enum Direction { Up = 0, Down = 1, Left = 2, Right = 3 }

    // --- Public API ---
    public TileData[,] GetGridData() => grid;

    public void SetGridData(TileData[,] newGrid)
    {
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                grid[r, c] = newGrid[r, c];
        PrintGrid();
    }

    void Start()
    {
        grid = new TileData[rows, cols];
        nextTileId = 1;
        SpawnTile();
        SpawnTile();
        PrintGrid();
        OnInitialize?.Invoke();
    }

    public void HandleSwipe(int direction)
    {
        if (isAnimating) return;

        Direction dir = (Direction)direction;
        MoveResult plan = ComputeMove(dir);

        if (plan.IsEmpty)
        {
            OnMoveBlocked?.Invoke();
            return;
        }

        OnBeforeMove?.Invoke();
        StartCoroutine(ExecuteMove(plan));
    }

    private IEnumerator ExecuteMove(MoveResult plan)
    {
        isAnimating = true;

        OnMovePlanned?.Invoke(plan);
        yield return new WaitForSeconds(moveAnimDuration);

        // Commit the new grid
        grid = plan.newGrid;

        // Fire merge events (for score)
        foreach (var merge in plan.merges)
            OnMergeOccurred?.Invoke(merge.newValue);

        // Spawn a fresh tile (skip if power-up active)
        if (!SuppressSpawn) SpawnTile();

        PrintGrid();
        OnMoveCompleted?.Invoke();
        isAnimating = false;
    }

    // --- Spawn ---
    private void SpawnTile()
    {
        var empty = new List<Vector2Int>();
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                if (grid[r, c].IsEmpty) empty.Add(new Vector2Int(r, c));

        if (empty.Count == 0) return;

        Vector2Int pos = empty[Random.Range(0, empty.Count)];
        int value = Random.Range(0, 10) < 9 ? 2 : 4;
        int id = nextTileId++;

        grid[pos.x, pos.y] = new TileData { id = id, value = value };
        Debug.Log($"Spawned tile {id}={value} at ({pos.x},{pos.y})");
    }

    // --- Move computation ---
    private MoveResult ComputeMove(Direction dir)
    {
        var result = new MoveResult();
        var newGrid = new TileData[rows, cols];

        if (dir == Direction.Left || dir == Direction.Right)
        {
            for (int r = 0; r < rows; r++)
                ProcessLine(BuildLineCells(r, dir), newGrid, result);
        }
        else
        {
            for (int c = 0; c < cols; c++)
                ProcessLine(BuildLineCells(c, dir), newGrid, result);
        }

        result.newGrid = newGrid;
        return result;
    }

    private Vector2Int[] BuildLineCells(int index, Direction dir)
    {
        bool horizontal = dir == Direction.Left || dir == Direction.Right;
        int length = horizontal ? cols : rows;
        var cells = new Vector2Int[length];

        switch (dir)
        {
            case Direction.Left:
                for (int i = 0; i < cols; i++) cells[i] = new Vector2Int(index, i);
                break;
            case Direction.Right:
                for (int i = 0; i < cols; i++) cells[i] = new Vector2Int(index, cols - 1 - i);
                break;
            case Direction.Up:
                for (int i = 0; i < rows; i++) cells[i] = new Vector2Int(i, index);
                break;
            case Direction.Down:
                for (int i = 0; i < rows; i++) cells[i] = new Vector2Int(rows - 1 - i, index);
                break;
        }
        return cells;
    }

    private void ProcessLine(Vector2Int[] cells, TileData[,] newGrid, MoveResult result)
    {
        // Gather non-empty tiles in destination-order (index 0 = edge)
        var tiles = new List<(TileData data, Vector2Int pos)>();
        foreach (var cell in cells)
        {
            var t = grid[cell.x, cell.y];
            if (!t.IsEmpty) tiles.Add((t, cell));
        }

        int writeIdx = 0;
        int i = 0;

        while (i < tiles.Count)
        {
            Vector2Int dest = cells[writeIdx];

            if (i + 1 < tiles.Count && tiles[i].data.value == tiles[i + 1].data.value)
            {
                // --- Merge ---
                int mergedValue = tiles[i].data.value * 2;
                int mergedId = nextTileId++;

                result.movements.Add(new TileMovement
                {
                    tileId = tiles[i].data.id,
                    fromRow = tiles[i].pos.x,
                    fromCol = tiles[i].pos.y,
                    toRow = dest.x,
                    toCol = dest.y
                });
                result.movements.Add(new TileMovement
                {
                    tileId = tiles[i + 1].data.id,
                    fromRow = tiles[i + 1].pos.x,
                    fromCol = tiles[i + 1].pos.y,
                    toRow = dest.x,
                    toCol = dest.y
                });

                result.merges.Add(new MergeEvent
                {
                    tileIdA = tiles[i].data.id,
                    tileIdB = tiles[i + 1].data.id,
                    row = dest.x,
                    col = dest.y,
                    newValue = mergedValue,
                    newTileId = mergedId
                });

                newGrid[dest.x, dest.y] = new TileData { id = mergedId, value = mergedValue };
                i += 2;
            }
            else
            {
                // --- Plain move ---
                var t = tiles[i];

                if (t.pos.x != dest.x || t.pos.y != dest.y)
                {
                    result.movements.Add(new TileMovement
                    {
                        tileId = t.data.id,
                        fromRow = t.pos.x,
                        fromCol = t.pos.y,
                        toRow = dest.x,
                        toCol = dest.y
                    });
                }

                newGrid[dest.x, dest.y] = t.data;
                i += 1;
            }
            writeIdx++;
        }
    }

    [ContextMenu("Print Grid")]
    private void PrintGrid()
    {
        string str = "";
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
                str += (grid[r, c].IsEmpty ? "." : grid[r, c].value.ToString()) + "\t";
            str += "\n";
        }
        Debug.Log(str);
    }
}