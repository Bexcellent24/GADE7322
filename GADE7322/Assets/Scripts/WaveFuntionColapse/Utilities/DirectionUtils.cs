using UnityEngine;

// Helper methods for directions.
public static class DirectionUtils
{
    public static Direction Opposite(Direction dir) => dir switch
    {
        Direction.Top => Direction.Bottom,
        Direction.Bottom => Direction.Top,
        Direction.North => Direction.South,
        Direction.South => Direction.North,
        Direction.East => Direction.West,
        Direction.West => Direction.East,
        _ => Direction.Top
    };

    public static Vector3Int DirectionToOffset(Direction dir) => dir switch
    {
        Direction.Top => new Vector3Int(0, 1, 0),
        Direction.Bottom => new Vector3Int(0, -1, 0),
        Direction.North => new Vector3Int(0, 0, 1),
        Direction.South => new Vector3Int(0, 0, -1),
        Direction.East => new Vector3Int(1, 0, 0),
        Direction.West => new Vector3Int(-1, 0, 0),
        _ => Vector3Int.zero
    };
}
