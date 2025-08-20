public interface ISpecialCharge
{
    /// Returns true if an activation occurred (enough charge, ability actually triggered).
    bool TryActivate();

    /// Add kill/charge progress (e.g., +1 per kill).
    void AddCharge(int amount);
}