[System.Serializable]
public struct TileData
{
    public int id;      // 0 = empty cell
    public int value;   // 0 when empty

    public bool IsEmpty => id == 0;
}