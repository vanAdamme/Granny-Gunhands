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
    bool TryGetActiveWeapon(Hand hand, out Weapon weapon);
    bool TryGetActiveWeapon<T>(Hand hand, out T weapon) where T : Weapon;

    // Misc
    Transform Transform { get; }
    ItemInventory ItemInventory { get; }
}