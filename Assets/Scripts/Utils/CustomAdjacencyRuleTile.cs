using UnityEngine;
using UnityEngine.Tilemaps;

/// Drop-in replacement for RuleTile with extra neighbour types.
/// Make an asset from this and set up rules in the Inspector as usual.
[CreateAssetMenu(menuName = "Tiles/Custom Adjacency RuleTile")]
public class CustomAdjacencyRuleTile : RuleTile<CustomAdjacencyRuleTile.Neighbor>
{
    // Optional: use a "group id" so different tiles that belong together
    // (e.g., wall family) can match as "SameGroup".
    [Header("Grouping")]
    [Tooltip("Tiles that share the same Group Id are considered the same group.")]
    public int groupId = 0;

    // You can point to a specific partner tile to express strict pairings.
    [Header("Pairing (optional)")]
    [Tooltip("If set, Some rules can require this exact tile as a neighbour.")]
    public TileBase specificPartner;

    // Extend the neighbour vocabulary beyond RuleTile’s default.
    public enum Neighbor
    {
        // Keep RuleTile’s semantics available:
        This,           // same tile asset as self (RuleTile default)
        NotThis,        // different tile
        Any,            // wildcard (no test)
        Empty,          // no tile at neighbour

        // New custom conditions:
        SameGroup,          // neighbour is CustomAdjacencyRuleTile with same groupId
        DifferentGroup,     // neighbour is CustomAdjacencyRuleTile with different groupId
        HasCollider,        // neighbour tile collider != None
        NoCollider,         // neighbour tile collider == None
        SpecificPartner,    // neighbour equals 'specificPartner'
    }

    /// Called by RuleTile when checking each neighbour slot.
    public override bool RuleMatch(int neighbor, TileBase other)
    {
        var n = (Neighbor)neighbor;

        switch (n)
        {
            case Neighbor.Any:
                return true;

            case Neighbor.Empty:
                return other == null;

            case Neighbor.This:
                return other == this;

            case Neighbor.NotThis:
                return other != this;

            case Neighbor.SameGroup:
                return IsSameGroup(other);

            case Neighbor.DifferentGroup:
                return IsDifferentGroup(other);

            case Neighbor.HasCollider:
                return HasCollider(other);

            case Neighbor.NoCollider:
                return !HasCollider(other);

            case Neighbor.SpecificPartner:
                return other == specificPartner;

            default:
                // Fallback to base (keeps compatibility with RuleTile internal checks if needed)
                return base.RuleMatch(neighbor, other);
        }
    }

    private bool IsSameGroup(TileBase other)
    {
        if (other is CustomAdjacencyRuleTile ct)
            return ct.groupId == this.groupId;
        return false;
    }

    private bool IsDifferentGroup(TileBase other)
    {
        if (other is CustomAdjacencyRuleTile ct)
            return ct.groupId != this.groupId;
        // Treat non-custom tiles as “different”
        return true;
    }

    private bool HasCollider(TileBase other)
    {
        if (other == null) return false;

        // Pull the tile data to check collider type.
        var dummy = new TileData();
        other.GetTileData(Vector3Int.zero, null, ref dummy);
        return dummy.colliderType != Tile.ColliderType.None;
    }
}