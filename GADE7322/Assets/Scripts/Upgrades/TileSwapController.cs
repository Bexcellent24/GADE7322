using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Handles visual upgrades for Defenders that use WFC tile-based generation.
// Swaps out individual tiles with upgraded variants.
// Level 1: Swaps X% of tiles to upgraded variant
// Level 2: Swaps MORE tiles (cumulative total) to upgraded variant
public class TileSwapController : MonoBehaviour, IVisualUpgradeStrategy
{
    private List<TileInstance> tileInstances = new List<TileInstance>();
    private bool isInitialized = false;
    
    private class TileInstance
    {
        public GameObject gameObject;
        public WFCTile originalTile;
        public Vector3Int gridPosition;
        public bool isSwapped = false; // Track if this tile has been upgraded
    }
    

    // Called after WFC generation completes to register all tiles
    public void RegisterTiles(GameObject[,,] instantiatedTiles, DefenderGenerator.Cell[,,] grid, int sizeX, int height, int sizeZ)
    {
        tileInstances.Clear();
        
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    var go = instantiatedTiles[x, y, z];
                    if (go != null && grid != null && grid[x, y, z] != null)
                    {
                        var cell = grid[x, y, z];
                        if (cell.PossibleCount > 0)
                        {
                            tileInstances.Add(new TileInstance
                            {
                                gameObject = go,
                                originalTile = cell.possible.First(),
                                gridPosition = new Vector3Int(x, y, z),
                                isSwapped = false
                            });
                        }
                    }
                }
            }
        }
        
        isInitialized = tileInstances.Count > 0;
        
        if (!isInitialized)
        {
            Debug.LogWarning("[TileSwapController] No tiles were registered!");
        }
        else
        {
            Debug.Log($"[TileSwapController] Registered {tileInstances.Count} tiles for upgrade");
        }
    }
    
    public void ApplyVisualUpgrade(int upgradeLevel, UpgradeConfiguration config)
    {
        if (!IsValid())
        {
            Debug.LogError("[TileSwapController] Cannot apply visual upgrade - controller not initialized!");
            return;
        }
        
        if (config == null)
        {
            Debug.LogError("[TileSwapController] UpgradeConfiguration is null!");
            return;
        }
        
        if (config.upgradeType != UpgradeType.TileSwap)
        {
            Debug.LogWarning($"[TileSwapController] Config upgrade type is {config.upgradeType}, expected TileSwap. Aborting.");
            return;
        }
        
        // Get the target percentage for this level
        float targetSwapPercentage = upgradeLevel == 1 
            ? config.level1TileSwapPercentage 
            : config.level2TileSwapPercentage;
        
        // Calculate how many tiles should be swapped in total at this level
        int targetSwappedCount = Mathf.RoundToInt(tileInstances.Count * targetSwapPercentage);
        
        // Count how many are already swapped
        int currentSwappedCount = tileInstances.Count(t => t.isSwapped);
        
        // Calculate how many MORE tiles we need to swap
        int additionalSwapsNeeded = targetSwappedCount - currentSwappedCount;
        
        if (additionalSwapsNeeded <= 0)
        {
            Debug.LogWarning($"[TileSwapController] No additional swaps needed for level {upgradeLevel}. Already swapped: {currentSwappedCount}/{tileInstances.Count}");
            return;
        }
        
        // Filter tiles that haven't been swapped yet AND have an upgraded variant available
        var swappableTiles = tileInstances
            .Where(t => !t.isSwapped && t.gameObject != null && HasVariant(t.originalTile, config))
            .ToList();
        
        if (swappableTiles.Count == 0)
        {
            Debug.LogWarning($"[TileSwapController] No swappable tiles found for level {upgradeLevel}");
            return;
        }
        
        // Clamp to available tiles
        int tilesToSwap = Mathf.Min(additionalSwapsNeeded, swappableTiles.Count);
        
        // SHUFFLE THE LIST PROPERLY - convert to array, shuffle it, then take what we need
        TileInstance[] shuffledArray = swappableTiles.ToArray();
        
        // Fisher-Yates shuffle for truly random selection
        for (int i = shuffledArray.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            TileInstance temp = shuffledArray[i];
            shuffledArray[i] = shuffledArray[randomIndex];
            shuffledArray[randomIndex] = temp;
        }
        
        // Take the first N tiles from the shuffled array
        var tilesToSwapList = shuffledArray.Take(tilesToSwap).ToList();
        
        Debug.Log($"[TileSwapController] Level {upgradeLevel}: Swapping {tilesToSwap} additional tiles (Total: {currentSwappedCount + tilesToSwap}/{tileInstances.Count})");
        
        // Log which tiles are being swapped for debugging
        string swapPositions = string.Join(", ", tilesToSwapList.Select(t => t.gridPosition.ToString()));
        Debug.Log($"[TileSwapController] Swapping tiles at positions: {swapPositions}");
        
        foreach (var tile in tilesToSwapList)
        {
            SwapSingleTile(tile, config);
        }
    }
    
    private void SwapSingleTile(TileInstance tile, UpgradeConfiguration config)
    {
        if (tile.gameObject == null)
        {
            Debug.LogWarning("[TileSwapController] Tile GameObject is null, skipping swap");
            return;
        }
        
        var mapping = config.tileUpgradeMappings?.FirstOrDefault(m => m.baseTile == tile.originalTile);
        if (mapping == null)
        {
            Debug.LogWarning($"[TileSwapController] No mapping found for tile {tile.originalTile?.name}");
            return;
        }
        
        WFCTile upgradedTile = mapping.upgradedVariant;
        if (upgradedTile == null || upgradedTile.prefab == null)
        {
            Debug.LogWarning($"[TileSwapController] No upgraded variant prefab found");
            return;
        }
        
        // Store transform info
        Vector3 pos = tile.gameObject.transform.position;
        Quaternion rot = tile.gameObject.transform.rotation;
        Transform parent = tile.gameObject.transform.parent;
        
        // Store reference to old object
        GameObject oldObject = tile.gameObject;
        
        // Instantiate new FIRST
        GameObject newObject = Instantiate(upgradedTile.prefab, pos, rot, parent);
        
        // Update reference
        tile.gameObject = newObject;
        tile.isSwapped = true;
        
        // Destroy old AFTER new is created (deferred destruction)
        if (oldObject != null)
        {
            Destroy(oldObject);
        }
        
        Debug.Log($"[TileSwapController] Successfully swapped tile at {tile.gridPosition}");
    }
    
    private bool HasVariant(WFCTile tile, UpgradeConfiguration config)
    {
        if (tile == null || config.tileUpgradeMappings == null) return false;
        
        var mapping = config.tileUpgradeMappings.FirstOrDefault(m => m.baseTile == tile);
        if (mapping == null) return false;
        
        return mapping.upgradedVariant != null && mapping.upgradedVariant.prefab != null;
    }
    
    public bool IsValid()
    {
        return isInitialized && tileInstances.Count > 0;
    }
}