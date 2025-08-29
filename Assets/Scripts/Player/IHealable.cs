public interface IHealable
{
    float CurrentHealth { get; }
    float MaxHealth { get; }
    void Heal(float amount);
}