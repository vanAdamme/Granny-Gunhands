using UnityEngine;
using Pathfinding;

public class AStarMovementStrategy : MonoBehaviour, IMovementStrategy
{
    [SerializeField] private AIPath ai;
    [SerializeField] private float stopDistance = 0.2f;
    [SerializeField] private bool callSearchPath = true;

    void Reset()
    {
        if (!ai) ai = GetComponent<AIPath>();
    }

    void Awake()
    {
        if (!ai) ai = GetComponent<AIPath>();
        if (!ai) { Debug.LogError($"{name}: AStarMovementStrategy needs AIPath"); enabled = false; return; }

        // If your Enemy has a moveSpeed, sync it
        if (TryGetComponent<Enemy>(out var enemy))
            ai.maxSpeed = Mathf.Max(ai.maxSpeed, enemy.MoveSpeed);
    }

    public bool MoveTowards(IEnemyContext ctx, Vector2 destination, float dt)
    {
        if (!ai) return false;

        if ((ai.destination - (Vector3)destination).sqrMagnitude > 0.001f)
        {
            ai.destination = destination;
            if (callSearchPath) ai.SearchPath();
        }

        return Vector2.Distance(ctx.Transform.position, destination) > stopDistance;
    }
}