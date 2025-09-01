using UnityEngine;

public class UpgradeToastListener : MonoBehaviour
{
    [SerializeField] private MonoBehaviour toastServiceSource;  // IToastService
    [TextArea] [SerializeField] private string appliedTemplate = "{name} +{levels} level{s}"; // NEW
    private IToastService toast;

    private void Awake() => toast = toastServiceSource as IToastService;
    private void OnEnable()  => UpgradeEvents.OnApplied += Handle;
    private void OnDisable() => UpgradeEvents.OnApplied -= Handle;

    private void Handle(Weapon weapon, int appliedLevels)
    {
        if (toast == null || weapon == null) return;
        var name = weapon.Definition ? weapon.Definition.DisplayName : weapon.name;

        string msg = appliedTemplate
            .Replace("{name}",   name)
            .Replace("{levels}", appliedLevels.ToString())
            .Replace("{s}",      appliedLevels == 1 ? "" : "s");

        toast.Show(msg);
    }
}