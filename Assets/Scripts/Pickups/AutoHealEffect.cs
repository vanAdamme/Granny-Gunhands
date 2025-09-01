using UnityEngine;

[CreateAssetMenu(menuName = "Powerups/One‑Shot/Auto Heal")]
public class AutoHealEffect : OneShotEffectBase
{
    [SerializeField] private float amount = 20f;

    public override bool ApplyOnce(IPlayerContext ctx)
    {
        var mono = ctx as MonoBehaviour;
        var player = mono ? mono.GetComponentInParent<PlayerController>() : null;
        if (!player) return false;

        // Assume Target exposes CurrentHealth and MaxHealth; if not, adapt to your API.
        float before = player.CurrentHealth;
        if (before >= player.MaxHealth) return false; // nothing to do

        player.Heal(amount);
        return player.CurrentHealth > before;         // true only if HP actually increased
    }
}