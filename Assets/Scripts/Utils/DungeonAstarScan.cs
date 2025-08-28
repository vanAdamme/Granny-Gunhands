using UnityEngine;
using Edgar.Unity;
using Pathfinding; // A* namespace

public class DungeonAstarScan : DungeonGeneratorPostProcessingGrid2D
{
    public override void Run(DungeonGeneratorLevelGrid2D level)
    {
        Debug.Log("[DungeonAstarScan] Dungeon generation finished → rescanning graph.");

        // Now it’s safe to rescan the A* grid
        AstarPath.active?.Scan();
    }
}