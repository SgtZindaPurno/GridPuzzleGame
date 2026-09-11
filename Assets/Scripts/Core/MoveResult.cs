using System.Collections.Generic;
using UnityEngine;

public class MoveResult
{
    public TileData[,] newGrid;
    public List<TileMovement> movements = new List<TileMovement>();
    public List<MergeEvent> merges = new List<MergeEvent>();

    public bool IsEmpty => movements.Count == 0;
}

public struct TileMovement
{
    public int tileId;
    public int fromRow, fromCol;
    public int toRow, toCol;
}

public struct MergeEvent
{
    public int tileIdA;      // consumed
    public int tileIdB;      // consumed
    public int row, col;     // merge cell
    public int newValue;
    public int newTileId;    // brand-new id for the merged result
}