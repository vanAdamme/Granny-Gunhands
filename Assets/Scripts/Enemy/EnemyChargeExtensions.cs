public static class EnemyChargeExtensions
{
    public static float GetKillValue(this Enemy e)
    {
        // Map experience or health to charge if desired
        // (For now, simple  return 12f;  or tie to a public field you add to Enemy.)
        return 12f;
    }
}
