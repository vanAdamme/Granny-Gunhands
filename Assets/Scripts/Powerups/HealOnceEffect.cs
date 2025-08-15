using UnityEngine;

[CreateAssetMenu(menuName = "Powerups/One‑Shot/Heal")]
public class HealOnceEffect : OneShotEffectBase
{
    public float amount = 20f;
    public override void ApplyOnce(IPlayerContext player)
    {
        player.Heal(amount);
    }
}