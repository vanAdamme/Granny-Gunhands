public interface IEnemyState
{
    void OnEnter(IEnemyContext ctx);
    void OnExit(IEnemyContext ctx);
    void Tick(IEnemyContext ctx, float dt);
    // Optional: void FixedTick(...) if you need physics-timed movement
}