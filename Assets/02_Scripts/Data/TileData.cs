using UnityEngine;



public enum FloorType
{
    Default,
    Wood,
    Kitchen,
    Locked
}

public sealed class CellData
{
    public Vector2Int Position { get; }

    public FloorType FloorType { get; set; }

    public long FurnitureId { get; set; }

    public bool IsUnlocked { get; set; }

    public bool BlocksMovement { get; set; }

    public bool IsOccupied => FurnitureId != 0;

    public bool IsWalkable =>
        IsUnlocked &&
        !BlocksMovement;

    public CellData(Vector2Int position)
    {
        Position = position;
        FloorType = FloorType.Default;
        FurnitureId = 0;
        IsUnlocked = true;
        BlocksMovement = false;
    }
}