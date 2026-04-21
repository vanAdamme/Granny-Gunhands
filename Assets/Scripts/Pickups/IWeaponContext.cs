public interface IWeaponContext
{
    bool TryGetActiveWeapon(Hand hand, out Weapon weapon);
    bool TryGetActiveWeapon<T>(Hand hand, out T weapon) where T : Weapon;
}
