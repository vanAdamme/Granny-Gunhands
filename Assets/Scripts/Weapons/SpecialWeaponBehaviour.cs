using UnityEngine;

public abstract class SpecialWeaponBehaviour : MonoBehaviour
{
    // [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] public Sprite icon;

    public abstract void ToggleActivate();

    // protected virtual void Awake()
    // {
    //     spriteRenderer ??= GetComponentInChildren<SpriteRenderer>();
    // }
}