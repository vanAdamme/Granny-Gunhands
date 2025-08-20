using UnityEngine;

public interface IToastService
{
    void Show(string message, Sprite icon = null, float duration = 2.2f);
    void ShowUpgrade(string weaponName, int newLevel, Sprite icon = null, float duration = 2.4f);
}