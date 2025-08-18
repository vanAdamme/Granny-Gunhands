using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class UIButtonPulse : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [Header("Scales")]
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float pressedScale = 0.98f;
    [SerializeField, Min(0.01f)] private float tweenTime = 0.08f;

    RectTransform rt;
    Coroutine tween;
    bool isHover;

    void Awake() => rt = (RectTransform)transform;

    void OnEnable()  => SetScale(1f);
    void OnDisable() => SetScale(1f);

    public void OnPointerEnter(PointerEventData _) { isHover = true;  StartTween(hoverScale); }
    public void OnPointerExit (PointerEventData _) { isHover = false; StartTween(1f);        }
    public void OnPointerDown (PointerEventData _) { StartTween(pressedScale);               }
    public void OnPointerUp   (PointerEventData _) { StartTween(isHover ? hoverScale : 1f);  }

    void SetScale(float s) => rt.localScale = new Vector3(s, s, 1f);

    void StartTween(float to)
    {
        if (tween != null) StopCoroutine(tween);
        tween = StartCoroutine(TweenScale(to));
    }

    IEnumerator TweenScale(float to)
    {
        Vector3 from = rt.localScale;
        Vector3 target = new Vector3(to, to, 1f);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / tweenTime;
            rt.localScale = Vector3.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        rt.localScale = target;
        tween = null;
    }
}