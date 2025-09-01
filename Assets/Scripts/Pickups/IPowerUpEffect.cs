public interface IPowerUpEffect
{
    // Called once when the powerup starts
    void Apply(IPlayerContext player);

    // Called once when the powerup ends (for timed buffs)
    void Remove(IPlayerContext player);
}