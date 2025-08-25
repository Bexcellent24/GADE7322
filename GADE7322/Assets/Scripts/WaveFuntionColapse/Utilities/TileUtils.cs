using UnityEngine;

// Utility for tile socket compatibility checks.
public static class TileUtils
{
    public static bool AreSocketsCompatible(SocketType a, SocketType b)
    {
        if (a == SocketType.None && b == SocketType.None) return true;
        if (a == SocketType.None || b == SocketType.None) return false;
        return a == b;
    }
}

