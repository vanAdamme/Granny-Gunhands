using UnityEngine;

[CreateAssetMenu(menuName = "Powerups/Effects/Modify Weapon Cooldown")]
public class ModifyWeaponCooldownEffect : PowerUpEffectBase
{
    public float multiplier = 0.7f; // 30% faster

    private class Runtime : IPowerUpEffect
    {
        private readonly float m;
        private float original;
        private Weapon weapon;

        public Runtime(float m) { this.m = m; }

    public void Apply(IPlayerContext player)
    {
        if (player.TryGetActiveWeapon(Hand.Left, out weapon) || 
            player.TryGetActiveWeapon(Hand.Right, out weapon))
        {
            original = weapon.CooldownWindow;
            weapon.CooldownWindow = original * m;
        }
    }

    public void Remove(IPlayerContext player)
    {
        if (weapon) weapon.CooldownWindow = original;
    }

        private static float GetCooldown(Pistol p)
        {
            var f = typeof(Pistol).GetField("cooldownWindow",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return (float)f.GetValue(p);
        }
        private static void SetCooldown(Pistol p, float value)
        {
            var f = typeof(Pistol).GetField("cooldownWindow",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            f.SetValue(p, value);
        }
    }

    public override IPowerUpEffect CreateRuntime() => new Runtime(multiplier);
}