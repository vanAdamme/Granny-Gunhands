using UnityEngine;

[CreateAssetMenu(menuName = "Powerups/One‑Shot/Add XP")]
public class AddXpOnceEffect : OneShotEffectBase
{
    public int xp = 5;
    public override void ApplyOnce(IPlayerContext player)
    {
        player.AddExperience(xp);
    }
}