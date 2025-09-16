#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class RoomTemplateLayerGuard
{
    // Edit this list once; keep your naming consistent with the template and Edgar layer mapping.
    private static readonly string[] RequiredTilemaps = {
        "Floor", "Walls", "Magma", 
    };

    [MenuItem("Tools/Rooms/Validate Selected Room Template %#r")] // Ctrl/Cmd+Shift+R
    private static void ValidateSelected()
    {
        var go = Selection.activeGameObject;
        if (!go)
        {
            Debug.LogWarning("[RoomTemplateLayerGuard] No GameObject selected.");
            return;
        }

        // Try to find the Grid that holds your tilemaps in the template
        var grid = go.GetComponentInChildren<Grid>();
        if (!grid)
        {
            Debug.LogWarning($"[RoomTemplateLayerGuard] '{go.name}' has no Grid child.");
            return;
        }

        var childNames = grid.GetComponentsInChildren<Transform>(true)
                             .Select(t => t.name).ToHashSet();

        var missing = RequiredTilemaps.Where(req => !childNames.Contains(req)).ToList();
        if (missing.Count == 0)
        {
            Debug.Log($"[RoomTemplateLayerGuard] OK: '{go.name}' has all required tilemaps.");
        }
        else
        {
            Debug.LogError($"[RoomTemplateLayerGuard] '{go.name}' is missing: {string.Join(", ", missing)}");
        }
    }
}
#endif