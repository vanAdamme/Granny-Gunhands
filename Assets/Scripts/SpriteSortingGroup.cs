using UnityEngine;

public class SpriteSortingGroup : MonoBehaviour
{
    public SpriteRenderer body;
    public SpriteRenderer[] alwaysAbove;

    void LateUpdate()
    {
        int baseOrder = Mathf.RoundToInt(-transform.position.y * 100);
        body.sortingOrder = baseOrder;

        for (int i = 0; i < alwaysAbove.Length; i++)
        {
            alwaysAbove[i].sortingOrder = baseOrder + i + 1;
        }
    }
}
