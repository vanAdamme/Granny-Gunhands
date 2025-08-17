using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public static UIController Instance;

    [Header("Prefabs")]
    [SerializeField] private Slider playerHealthSlider;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Slider killChargeSlider;
    [SerializeField] private TMP_Text killChargeText;
    [SerializeField] private Slider playerExperienceSlider;
    [SerializeField] private TMP_Text experienceText;
    [SerializeField] private Image leftWeaponIcon;
    [SerializeField] private Image rightWeaponIcon;
    [SerializeField] private Image specialWeaponIcon;
    [SerializeField] private TMP_Text timerText;

    public GameObject gameOverPanel;
    public GameObject pausePanel;
    public GameObject levelUpPanel;
    public LevelUpButton[] levelUpButtons;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    public void UpdateHealthSlider()
    {
        var p = PlayerController.Instance;
        playerHealthSlider.maxValue = p.MaxHealth;
        playerHealthSlider.value = p.CurrentHealth;
        healthText.text = p.CurrentHealth + " / " + p.MaxHealth;
    }

    public void UpdateExperienceSlider()
    {
        playerExperienceSlider.maxValue = PlayerController.Instance.playerLevels[PlayerController.Instance.currentLevel - 1];
        playerExperienceSlider.value = PlayerController.Instance.experience;
        experienceText.text = playerExperienceSlider.value + " / " + playerExperienceSlider.maxValue;
    }

    public void UpdateTimer(float timer)
    {
        float min = Mathf.FloorToInt(timer / 60f);
        float sec = Mathf.FloorToInt(timer % 60f);

        timerText.text = min + ":" + sec.ToString("00");
    }

    public void UpdateKillCharge(int current, int required)
    {
        killChargeSlider.maxValue = required;
        killChargeSlider.value = current;
        killChargeText.text = current + " / " + required;
    }

    public void UpdateLeftWeaponIcon(Sprite wIcon)
    {
        leftWeaponIcon.sprite = wIcon;
    }

    public void UpdateRightWeaponIcon(Sprite wIcon)
    {
        rightWeaponIcon.sprite = wIcon;
    }

    public void UpdateSpecialWeaponIcon(Sprite wIcon)
    {
        specialWeaponIcon.sprite = wIcon;
    }
    public void LevelUpPanelOpen()
    {
        levelUpPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void LevelUpPanelClose()
    {
        levelUpPanel.SetActive(false);
        Time.timeScale = 1f;
    }
}
