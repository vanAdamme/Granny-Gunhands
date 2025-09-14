public interface IRangeAware
{
    float MinPreferredRange { get; }  // e.g., FleeRange
    float MaxPreferredRange { get; }  // e.g., IdealMaxRange
}