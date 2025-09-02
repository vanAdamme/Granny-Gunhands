using UnityEngine;
using System.Text;

[System.Serializable]
public struct WeaponUpgradeDelta
{
    public float? setDamage,            addDamage;
    public float? setProjectileSpeed,   addProjectileSpeed;
    public float? setRange,             addRange;
    public int?   setMaxPierces,        addMaxPierces;
    public bool?  setPierceThroughObstacles;
    public float? setCooldown,          addCooldown;

    public bool IsEmpty =>
        setDamage == null && addDamage == null &&
        setProjectileSpeed == null && addProjectileSpeed == null &&
        setRange == null && addRange == null &&
        setMaxPierces == null && addMaxPierces == null &&
        setPierceThroughObstacles == null &&
        setCooldown == null && addCooldown == null;

    // Used by WeaponItemButton tooltip
    public string ToMultiline()
    {
        var sb = new StringBuilder();

        void Line(string name, float? set, float? add)
        {
            if (set.HasValue) sb.AppendLine($"{name}: = {set.Value}");
            if (add.HasValue) sb.AppendLine($"{name}: {(add.Value >= 0 ? "+" : "")}{add.Value}");
        }

        void LineInt(string name, int? set, int? add)
        {
            if (set.HasValue) sb.AppendLine($"{name}: = {set.Value}");
            if (add.HasValue) sb.AppendLine($"{name}: {(add.Value >= 0 ? "+" : "")}{add.Value}");
        }

        if (setPierceThroughObstacles.HasValue)
            sb.AppendLine($"Pierce Obstacles: = {(setPierceThroughObstacles.Value ? "On" : "Off")}");

        Line("Damage",             setDamage,          addDamage);
        Line("Proj Speed",         setProjectileSpeed, addProjectileSpeed);
        Line("Range",              setRange,           addRange);
        LineInt("Max Pierces",     setMaxPierces,      addMaxPierces);
        Line("Cooldown",           setCooldown,        addCooldown);

        return sb.ToString().TrimEnd();
    }
}