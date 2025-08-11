using System.Collections;
using System.Runtime.ExceptionServices;
using UnityEngine;
using UnityEngine.UIElements;

public class ObjectGlow : MonoBehaviour
{
    [ColorUsage(true, true)]
    [SerializeField] private Color glowColour = Color.white;
    [SerializeField] private float glowTime = 0.25f;
    [SerializeField] private AnimationCurve glowSpeedCurve;

    private SpriteRenderer[] spriteRenderers;
    private Material[] materials;

    private Coroutine objectGlowCoroutine;

    private void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        Init();
        CallObjectGlow();
    }

    private void Init()
    {
        Debug.Log("init");
        materials = new Material[spriteRenderers.Length];

        // assign sprite render materials to materials
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            materials[i] = spriteRenderers[i].material;
        }
    }

    public void CallObjectGlow()
    {
        if (objectGlowCoroutine != null) StopCoroutine(objectGlowCoroutine);

        objectGlowCoroutine = StartCoroutine(ObjectGlower());
    }

    private IEnumerator ObjectGlower()
    {
        SetGlowColour();

        // Lerp glow amount
        float currentGlowAmount = 0f;
        float elapsedTime = 0f;
        while (elapsedTime < glowTime)
        {
            // iterate elapsedTime
            elapsedTime += Time.deltaTime;

            // lerp glow amount
            currentGlowAmount = Mathf.Lerp(1f, glowSpeedCurve.Evaluate(elapsedTime), (elapsedTime / glowTime));
            SetGlowAmount(currentGlowAmount);

            yield return null;
        }

    }

    private void SetGlowColour()
    {
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i].SetColor("GlowColour", glowColour);
        }
    }

    private void SetGlowAmount(float amount)
    {
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i].SetFloat("GlowAmount", amount);
        }
    }
}