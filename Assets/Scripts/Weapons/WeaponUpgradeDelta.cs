using System;
using System.Text;

[Serializable]
public struct WeaponUpgradeDelta
{
    public OptFloat setDamage,          addDamage;
    public OptFloat setProjectileSpeed, addProjectileSpeed;
    public OptFloat setRange,           addRange;
    public OptInt   setMaxPierces,      addMaxPierces;
    public OptBool  setPierceThroughObstacles;
    public OptFloat setCooldown,        addCooldown;

    public bool IsEmpty =>
        !setDamage.enabled && !addDamage.enabled &&
        !setProjectileSpeed.enabled && !addProjectileSpeed.enabled &&
        !setRange.enabled && !addRange.enabled &&
        !setMaxPierces.enabled && !addMaxPierces.enabled &&
        !setPierceThroughObstacles.enabled &&
        !setCooldown.enabled && !addCooldown.enabled;

    // Used by UI for tooltip/preview
    public string ToMultiline()
    {
        var sb = new StringBuilder();
        void F(string n, OptFloat s, OptFloat a) {
            if (s.enabled) sb.AppendLine($"{n}: = {s.value}");
            if (a.enabled) sb.AppendLine($"{n}: {(a.value>=0?"+":"")}{a.value}");
        }
        void I(string n, OptInt s, OptInt a) {
            if (s.enabled) sb.AppendLine($"{n}: = {s.value}");
            if (a.enabled) sb.AppendLine($"{n}: {(a.value>=0?"+":"")}{a.value}");
        }
        if (setPierceThroughObstacles.enabled)
            sb.AppendLine($"Pierce Obstacles: = {(setPierceThroughObstacles.value ? "On" : "Off")}");
        F("Damage",        setDamage,          addDamage);
        F("Proj Speed",    setProjectileSpeed, addProjectileSpeed);
        F("Range",         setRange,           addRange);
        I("Max Pierces",   setMaxPierces,      addMaxPierces);
        F("Cooldown",      setCooldown,        addCooldown);
        return sb.ToString().TrimEnd();
    }
}