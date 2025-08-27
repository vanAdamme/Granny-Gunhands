using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(GridLayoutGroup))]
[RequireComponent(typeof(LayoutElement))]
public class GridAutoHeight : MonoBehaviour
{
    void OnEnable()                     => Recalc();
    void OnTransformChildrenChanged()   => Recalc();
    void OnRectTransformDimensionsChange() => Recalc();
#if UNITY_EDITOR
    void Update(){ if(!Application.isPlaying) Recalc(); }
#endif
    public void Recalc()
    {
        var grid = GetComponent<GridLayoutGroup>();
        var le   = GetComponent<LayoutElement>();

        int childCount = 0;
        for (int i = 0; i < transform.childCount; i++)
            if (transform.GetChild(i).gameObject.activeInHierarchy) childCount++;

        int cols = Mathf.Max(1, grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount && grid.constraintCount > 0
                                ? grid.constraintCount
                                : Mathf.FloorToInt((((RectTransform)transform).rect.width + grid.spacing.x) /
                                                    (grid.cellSize.x + grid.spacing.x)));

        int rows = Mathf.CeilToInt(childCount / (float)cols);
        float h = grid.padding.top + grid.padding.bottom
                + rows * grid.cellSize.y
                + Mathf.Max(0, rows - 1) * grid.spacing.y;

        le.preferredHeight = h;
    }
}