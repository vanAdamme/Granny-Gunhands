using UnityEngine;

public interface IPlayerContext
{
    // Health
    float MaxHealth { get; set; }
    void Heal(float amount);
    bool IsInvulnerable { get; set; }

    // Movement
    float MoveSpeed { get; set; }

    // Progression
    void AddExperience(int amount);

    // Weapons
    // Provide safe hooks instead of exposing collections directly
    void AddWeapon(Weapon weaponPrefab);
    bool TryGetActiveWeapon<T>(out T weapon) where T : MonoBehaviour;

    // Misc
    Transform Transform { get; }
}