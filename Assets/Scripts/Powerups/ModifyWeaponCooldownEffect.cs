using UnityEngine;

[CreateAssetMenu(menuName = "Powerups/Effects/Modify Weapon Cooldown")]
public class ModifyWeaponCooldownEffect : PowerUpEffectBase
{
    public float multiplier = 0.7f; // 30% faster

    private class Runtime : IPowerUpEffect
    {
        private readonly float m;
        private float original;
        private Pistol pistol;

        public Runtime(float m) { this.m = m; }

        public void Apply(IPlayerContext player)
        {
            if (player.TryGetActiveWeapon<Pistol>(out pistol) && pistol != null)
            {
                original = GetCooldown(pistol);
                SetCooldown(pistol, original * m);
            }
        }

        public void Remove(IPlayerContext player)
        {
            if (pistol != null)
                SetCooldown(pistol, original);
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