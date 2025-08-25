public interface IHealthView
{
    void OnHealthChanged(float current, float max);
    void OnDeath();
}