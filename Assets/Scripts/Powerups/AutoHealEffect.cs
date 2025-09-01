using UnityEngine;

[CreateAssetMenu(menuName = "Powerups/One‑Shot/Auto Heal")]
public class AutoHealEffect : OneShotEffectBase
{
    [Min(1f)] public int healAmount;

    public override void ApplyOnce(IPlayerContext player)
    {
        if (player == null) return;
        if (player is PlayerController pc && pc.CurrentHealth >= pc.MaxHealth) return;
        player.Heal(healAmount);
        UIController.Instance?.ShowToast($"+{healAmount} HP", null, 1.2f);
    }
}