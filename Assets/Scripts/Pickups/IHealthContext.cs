public interface IHealthContext
{
    float MaxHealth { get; set; }
    void Heal(float amount);
    bool IsInvulnerable { get; set; }
}
