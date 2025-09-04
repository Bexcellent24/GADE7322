using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class DefenderGenerator : MonoBehaviour
{
    #region Inspector Fields

    [Header("Grid Settings")]
    [Tooltip("Height of the tower in cells.")]
    public int height = 4;
    public float tileSize = 2f;
    public Transform parent;

    [Header("Tiles")]
    public List<WFCTile> allTiles;

    #endregion

    #region Private Fields

    private Cell[,,] grid;
    private GameObject[,,] instantiatedTiles;

    private const int sizeX = 2;
    private const int sizeZ = 2;

    #endregion

    #region Nested Types
    
    // Represents a single cell in the WFC grid.
    // Stores possible tiles it can collapse into.
    private class Cell
    {
        public HashSet<WFCTile> possible;

        public Cell(IEnumerable<WFCTile> tiles)
        {
            possible = new HashSet<WFCTile>(tiles);
        }

        public int PossibleCount => possible.Count;
    }

    #endregion

    #region Public API

    void Start()
    {
        Generate();
    }
    
    // Call this to generate the tower.
    public void Generate()
    {
        InitializeGrid();
        ApplyBoundaryConstraints();
        ApplyTopLayerCapsOnly();
        StartCoroutine(RunWFC());
    }

    #endregion

    #region Grid Setup

    private void InitializeGrid()
    {
        grid = new Cell[sizeX, height, sizeZ];
        instantiatedTiles = new GameObject[sizeX, height, sizeZ];

        if (parent == null) parent = transform;

        // Clear previous children
        foreach (Transform child in parent) DestroyImmediate(child.gameObject);

        // Fill grid with all possible tiles
        for (int x = 0; x < sizeX; x++)
            for (int y = 0; y < height; y++)
                for (int z = 0; z < sizeZ; z++)
                    grid[x, y, z] = new Cell(allTiles);
    }

    
    // Only allow cap tiles at the top layer.
    private void ApplyTopLayerCapsOnly()
    {
        int topY = height - 1;

        for (int x = 0; x < sizeX; x++)
            for (int z = 0; z < sizeZ; z++)
            {
                var cell = grid[x, topY, z];
                cell.possible.RemoveWhere(t => !t.isRoofTile);
                if (cell.possible.Count == 0)
                    Debug.LogWarning($"No cap tiles available at {x},{topY},{z}!");
            }
    }

    
    // Prevent edges from connecting to non-existent neighbours.
    private void ApplyBoundaryConstraints()
    {
        for (int x = 0; x < sizeX; x++)
            for (int y = 0; y < height; y++)
                for (int z = 0; z < sizeZ; z++)
                {
                    var cell = grid[x, y, z];
                    cell.possible.RemoveWhere(t =>
                        (x == 0 && t.GetSocket(Direction.West) != SocketType.None) ||
                        (x == sizeX - 1 && t.GetSocket(Direction.East) != SocketType.None) ||
                        (z == 0 && t.GetSocket(Direction.South) != SocketType.None) ||
                        (z == sizeZ - 1 && t.GetSocket(Direction.North) != SocketType.None)
                    );
                }
    }

    #endregion

    #region WFC Algorithm

    private IEnumerator RunWFC()
    {
        var propagationQueue = new Queue<Vector3Int>();

        while (true)
        {
            var candidates = GetLowestEntropyCells();
            if (candidates.Count == 0)
            {
                InstantiateResult();
                yield break;
            }

            // Collapse random candidate
            Vector3Int chosenPos = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            var chosenCell = grid[chosenPos.x, chosenPos.y, chosenPos.z];
            WFCTile pickedTile = chosenCell.possible.ElementAt(UnityEngine.Random.Range(0, chosenCell.PossibleCount));
            chosenCell.possible = new HashSet<WFCTile> { pickedTile };

            propagationQueue.Enqueue(chosenPos);

            // Instantiate immediately
            InstantiateTile(chosenPos, pickedTile);
            yield return new WaitForSeconds(0.1f); 

            // Propagation
            while (propagationQueue.Count > 0)
            {
                Vector3Int current = propagationQueue.Dequeue();
                PropagateConstraints(current, propagationQueue);
            }

            yield return null;
        }
    }

    private List<Vector3Int> GetLowestEntropyCells()
    {
        int minOptions = int.MaxValue;
        var candidates = new List<Vector3Int>();

        for (int x = 0; x < sizeX; x++)
            for (int y = 0; y < height; y++)
                for (int z = 0; z < sizeZ; z++)
                {
                    var cell = grid[x, y, z];
                    if (cell.PossibleCount == 0)
                        Debug.LogWarning($"Contradiction at {x},{y},{z}");
                    else if (cell.PossibleCount > 1)
                    {
                        if (cell.PossibleCount < minOptions)
                        {
                            minOptions = cell.PossibleCount;
                            candidates.Clear();
                            candidates.Add(new Vector3Int(x, y, z));
                        }
                        else if (cell.PossibleCount == minOptions)
                            candidates.Add(new Vector3Int(x, y, z));
                    }
                }

        return candidates;
    }

    private void PropagateConstraints(Vector3Int current, Queue<Vector3Int> queue)
    {
        var currentCell = grid[current.x, current.y, current.z];

        foreach (Direction dir in Enum.GetValues(typeof(Direction)))
        {
            Vector3Int neighborPos = current + DirectionUtils.DirectionToOffset(dir);
            if (!IsInBounds(neighborPos)) continue;

            var neighborCell = grid[neighborPos.x, neighborPos.y, neighborPos.z];
            var allowed = new HashSet<WFCTile>();

            foreach (var curTile in currentCell.possible)
            {
                SocketType socketA = curTile.GetSocket(dir);

                foreach (var neighTile in neighborCell.possible)
                {
                    SocketType socketB = neighTile.GetSocket(DirectionUtils.Opposite(dir));
                    if (TileUtils.AreSocketsCompatible(socketA, socketB))
                        allowed.Add(neighTile);
                }
            }

            if (allowed.Count == 0)
            {
                Debug.LogWarning($"Contradiction at {neighborPos}");
                continue;
            }

            if (allowed.Count < neighborCell.PossibleCount)
            {
                neighborCell.possible = allowed;
                queue.Enqueue(neighborPos);
            }
        }
    }

    #endregion

    #region Tile Instantiation

    private void InstantiateResult()
    {
        for (int x = 0; x < sizeX; x++)
            for (int y = 0; y < height; y++)
                for (int z = 0; z < sizeZ; z++)
                {
                    var cell = grid[x, y, z];
                    if (cell.PossibleCount == 1)
                        InstantiateTile(new Vector3Int(x, y, z), cell.possible.First());
                }
    }

    private void InstantiateTile(Vector3Int pos, WFCTile tile)
    {
        if (instantiatedTiles[pos.x, pos.y, pos.z] != null)
            Destroy(instantiatedTiles[pos.x, pos.y, pos.z]);

        // pick the transform that owns the grid
        Transform p = parent != null ? parent : transform;

        // centre the 2 by 2 grid around local origin
        // half.x and half.z are 0.5 for a 2 by 2
        Vector3 half = new Vector3((sizeX - 1) * 0.5f, 0f, (sizeZ - 1) * 0.5f);

        // build the offset in LOCAL space, use local Y for height
        Vector3 localPos =
            new Vector3((pos.x - half.x) * tileSize,
                pos.y * tileSize,
                (pos.z - half.z) * tileSize);

        // convert to world using the parent’s rotation and position
        Vector3 worldPos = p.TransformPoint(localPos);

        // use parent rotation so tiles align with the tower orientation
        // if your tile prefabs have a local tweak, multiply it in
        Quaternion rot = p.rotation; // or p.rotation * tile.prefab.transform.localRotation;

        instantiatedTiles[pos.x, pos.y, pos.z] =
            Instantiate(tile.prefab, worldPos, rot, p);
    }


    #endregion

    #region Utilities

    private bool IsInBounds(Vector3Int v) => v.x >= 0 && v.x < sizeX && v.y >= 0 && v.y < height && v.z >= 0 && v.z < sizeZ;
    #endregion
}


