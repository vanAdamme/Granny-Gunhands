using UnityEngine;

public abstract class ItemDefinition : ScriptableObject
{
    [SerializeField] private string id = System.Guid.NewGuid().ToString();
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;
    [SerializeField] private bool stackable = true;

    public string Id => id;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public bool Stackable => stackable;
}