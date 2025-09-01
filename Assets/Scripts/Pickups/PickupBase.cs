using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public abstract class PickupBase : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] protected SpriteRenderer spriteRenderer;

    [Header("Feedback (optional)")]
    [SerializeField] private MonoBehaviour toastServiceSource;   // IToastService
    [Tooltip("Use tokens like {name}, {amount}, {levels}. Leave blank to use a sensible default.")]
    [TextArea] [SerializeField] private string toastTemplate = "";

    [Header("Behaviour")]
    [SerializeField] private bool hideSpriteOnConsume = true;
    [SerializeField] private bool disableColliderOnConsume = true;

    protected IToastService toast;
    protected Collider2D col2d;
    protected bool consumed;

    protected virtual void Awake()
    {
        col2d = GetComponent<Collider2D>();
        if (col2d) col2d.isTrigger = true;

        toast = toastServiceSource as IToastService;
        tag = "Item";
        SyncVisual();
    }

#if UNITY_EDITOR
    protected virtual void OnValidate() => SyncVisual();
#endif

    protected void SyncVisual()
    {
        var icon = GetIcon();
        if (spriteRenderer && icon) spriteRenderer.sprite = icon;
    }

    protected void ShowToast(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
            toast?.Show(message);
    }

    protected void ShowToastTemplate(string fallback, params (string key, object value)[] tokens)
    {
        string msg = string.IsNullOrWhiteSpace(toastTemplate) ? fallback : toastTemplate;

        if (tokens != null)
        {
            foreach (var (key, value) in tokens)
                msg = msg.Replace("{" + key + "}", value?.ToString() ?? "");
        }

        // simple plural helper for {s}
        msg = msg.Replace("{s}", NeedsPlural(tokens) ? "s" : "");

        ShowToast(msg);
    }

    private static bool NeedsPlural((string key, object value)[] tokens)
    {
        foreach (var (k, v) in tokens)
        {
            if ((k == "amount" || k == "levels") && int.TryParse(v?.ToString(), out int n))
                return n != 1;
        }
        return false;
    }

    protected IEnumerator Consume()
    {
        consumed = true;
        if (disableColliderOnConsume && col2d) col2d.enabled = false;
        if (hideSpriteOnConsume && spriteRenderer) spriteRenderer.enabled = false;
        yield return null;
        Destroy(gameObject);
    }

    protected abstract Sprite GetIcon();
}