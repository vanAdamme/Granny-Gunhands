using UnityEngine;

public class InventoryUIToggle : MonoBehaviour
{
    [SerializeField] private InventoryUI ui;

    void Update()
    {
        if (Input.GetKeyDown(ui.toggleKey))
            ui.Toggle();
    }
}