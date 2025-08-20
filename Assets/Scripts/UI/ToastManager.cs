using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class ToastManager : MonoBehaviour, IToastService
{
    [Header("Prefabs & Layout")]
    [SerializeField] private ToastView toastPrefab;   // small UI element (see below)
    [SerializeField] private Transform container;      // usually this GO’s RectTransform

    [Header("Behaviour")]
    [SerializeField] private int maxOnScreen = 3;
    [SerializeField] private float verticalSpacing = 4f;
    [SerializeField] private bool newestOnTop = true;

    private readonly Queue<ToastRequest> queue = new();
    private readonly List<ToastView> live = new();

    [System.Serializable]
    private class ToastRequest
    {
        public string message;
        public Sprite icon;
        public float duration;
        public Color color;
    }

    public void Show(string message, Sprite icon = null, float duration = 2.2f)
        => Enqueue(message, icon, duration, Color.white);

    public void ShowUpgrade(string weaponName, int newLevel, Sprite icon = null, float duration = 2.4f)
        => Enqueue($"{weaponName} → Lv.{newLevel}", icon, duration, new Color(0.9f, 1f, 0.6f));

    private void Enqueue(string msg, Sprite icon, float duration, Color color)
    {
        if (string.IsNullOrWhiteSpace(msg)) return;
        queue.Enqueue(new ToastRequest { message = msg, icon = icon, duration = duration, color = color });
        TrySpawn();
    }

    private void TrySpawn()
    {
        // Ensure we don’t exceed max; when a slot frees, we spawn the next
        while (queue.Count > 0 && live.Count < maxOnScreen)
        {
            var req = queue.Dequeue();
            var view = Instantiate(toastPrefab, container ? container : transform);
            view.gameObject.SetActive(true);
            view.Show(req.message, req.icon, req.color, req.duration, onFinished: () =>
            {
                live.Remove(view);
                Destroy(view.gameObject);
                TrySpawn(); // backfill from queue
            });

            if (newestOnTop) live.Insert(0, view); else live.Add(view);
            Relayout();
        }
    }

    private void Relayout()
    {
        // Simple vertical stack reposition
        float y = 0f;
        for (int i = 0; i < live.Count; i++)
        {
            var v = newestOnTop ? live[i] : live[live.Count - 1 - i];
            v.SetStackOffset(y);
            y += v.Height + verticalSpacing;
        }
    }
}