using UnityEngine;

[CreateAssetMenu(menuName = "Powerups/One‑Shot/Add XP")]
public class AddXpOnceEffect : OneShotEffectBase
{
    [Min(0)] public int xp = 5;

    public override bool ApplyOnce(IPlayerContext player)
    {
        if (player == null || xp <= 0) return false;
        player.AddExperience(xp);
        return true; // we actually added XP
    }
}