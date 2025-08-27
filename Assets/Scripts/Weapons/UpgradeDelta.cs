using UnityEngine;

[System.Serializable]
public struct UpgradeDelta
{
    public float damage;          // + more damage
    public float fireRate;        // + faster (shots/s)
    public int   projectiles;     // + more projectiles per shot
    public float reload;          // - faster reload time (seconds)
    public float critChance;      // + % points

    public float projectileSpeed; // + faster bullets
    public float range;           // + further travel
    public int   pierces;         // + more pierces

    public bool IsEmpty =>
        Mathf.Approximately(damage, 0f) &&
        Mathf.Approximately(fireRate, 0f) &&
        projectiles == 0 &&
        Mathf.Approximately(reload, 0f) &&
        Mathf.Approximately(critChance, 0f) &&
        Mathf.Approximately(projectileSpeed, 0f) &&
        Mathf.Approximately(range, 0f) &&
        pierces == 0;

    public string ToMultiline()
    {
        System.Text.StringBuilder sb = new();

        Line(sb, "Damage",        damage);
        Line(sb, "Fire rate",     fireRate);
        Line(sb, "Projectiles",   projectiles);
        Line(sb, "Reload",        reload, invertGood: true);
        Line(sb, "Crit",          critChance, suffix: "%");
        Line(sb, "Bullet speed",  projectileSpeed);
        Line(sb, "Range",         range);
        Line(sb, "Pierces",       pierces);

        return sb.Length == 0 ? "No change." : sb.ToString();
    }

    static void Line(System.Text.StringBuilder s, string label, float v, bool invertGood = false, string suffix = "")
    {
        if (Mathf.Approximately(v, 0f)) return;
        string sign = v > 0 ? "+" : "−";
        float mag = Mathf.Abs(v);
        string good = invertGood
            ? (v < 0 ? " <color=#7CFC00>(better)</color>" : "")
            : (v > 0 ? " <color=#7CFC00>(better)</color>" : "");
        s.AppendLine($"{label}: {sign}{mag:0.##}{suffix}{good}");
    }

    static void Line(System.Text.StringBuilder s, string label, int v)
    {
        if (v == 0) return;
        string sign = v > 0 ? "+" : "−";
        s.AppendLine($"{label}: {sign}{Mathf.Abs(v)}");
    }
}
