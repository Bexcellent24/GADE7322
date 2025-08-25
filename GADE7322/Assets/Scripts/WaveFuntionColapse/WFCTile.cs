using UnityEngine;

[CreateAssetMenu(fileName = "WFCTile", menuName = "WFC/Tile")]
public class WFCTile : ScriptableObject
{
    [Header("Visual")]
    public GameObject prefab;

    [Header("Helping")]
    public bool isRoofTile = false;
        
    [Header("Sockets per side")]
    public SocketType topSocket;
    public SocketType bottomSocket;
    public SocketType northSocket;
    public SocketType eastSocket;
    public SocketType southSocket;
    public SocketType westSocket;

    public SocketType GetSocket(Direction dir)
    {
        return dir switch
        {
            Direction.Top => topSocket,
            Direction.Bottom => bottomSocket,
            Direction.North => northSocket,
            Direction.South => southSocket,
            Direction.East => eastSocket,
            Direction.West => westSocket,
            _ => SocketType.None
        };
    }
}