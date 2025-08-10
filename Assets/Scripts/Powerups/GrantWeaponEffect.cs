using UnityEngine;

[CreateAssetMenu(menuName = "Powerups/Effects/Grant Weapon")]
public class GrantWeaponEffect : PowerUpEffectBase
{
    public Weapon weaponPrefab;

    private class Runtime : IPowerUpEffect
    {
        private readonly Weapon prefab;
        public Runtime(Weapon w) { prefab = w; }
        public void Apply(IPlayerContext player)  => player.AddWeapon(prefab);
        public void Remove(IPlayerContext player) { /* no-op */ }
    }

    public override IPowerUpEffect CreateRuntime() => new Runtime(weaponPrefab);
}