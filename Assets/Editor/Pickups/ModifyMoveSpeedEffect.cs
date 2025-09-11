using UnityEngine;

[CreateAssetMenu(menuName = "Powerups/Effects/Modify Move Speed")]
public class ModifyMoveSpeedEffect : PowerUpEffectBase
{
    [Tooltip("e.g., 1.3 = +30%")]
    public float multiplier = 1.3f;

    private class Runtime : IPowerUpEffect
    {
        private readonly float m;
        private float original;

        public Runtime(float m) { this.m = m; }

        public void Apply(IPlayerContext player)
        {
            original = player.MoveSpeed;
            player.MoveSpeed = original * m;
        }

        public void Remove(IPlayerContext player)
        {
            player.MoveSpeed = original;
        }
    }

    public override IPowerUpEffect CreateRuntime() => new Runtime(multiplier);
}