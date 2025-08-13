using UnityEngine;
using System.Collections.Generic;

public class Breakable : Target
{
    private SpriteRenderer spriteRenderer;
    [SerializeField] List<Sprite> sprites;
    private int spriteCount;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteCount = sprites.Count;
        spriteRenderer.sprite = sprites[0];
    }

    private void Update()
    {
        spriteRenderer.sprite = sprites[(int)(spriteCount - (spriteCount * CurrentHealth / MaxHealth))];
    }
}