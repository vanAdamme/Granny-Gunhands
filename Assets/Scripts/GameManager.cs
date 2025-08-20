using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public float gameTime;
    public bool gameActive;

    [Header("Input")]
    [SerializeField] private MonoBehaviour inputServiceSource;
    private IInputService input;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        input = inputServiceSource as IInputService;
        if (input == null) input = FindFirstObjectByType<InputService>();
    }

    void OnEnable()
    {
        if (input != null) input.Pause += OnPause;
    }

    void OnDisable()
    {
        if (input != null) input.Pause -= OnPause;
    }

    void Start()
    {
        Time.timeScale = 1f;
        gameActive = true;
    }

    void Update()
    {
        if (gameActive) gameTime += Time.deltaTime;
        // Timer UI if you want
    }

    public void GameOver()
    {
        gameActive = false;
        StartCoroutine(ShowGameOverScreen());
    }

    IEnumerator ShowGameOverScreen()
    {
        yield return new WaitForSeconds(1.5f);
        UIController.Instance.gameOverPanel.SetActive(true);
        AudioController.Instance.PlaySound(AudioController.Instance.gameOver);
    }

    public void Restart()
    {
        Time.timeScale = 1f; //unpause physics before reload
        SceneManager.LoadScene("Game");
    }

    private void OnPause()
    {
        if (!UIController.Instance) return;
        if (UIController.Instance.levelUpPanel.activeSelf) return;

        bool showing = UIController.Instance.pausePanel.activeSelf;
        if (!showing)
        {
            UIController.Instance.pausePanel.SetActive(true);
            Time.timeScale = 0f;
            AudioController.Instance.PlaySound(AudioController.Instance.pause);
        }
        else
        {
            UIController.Instance.pausePanel.SetActive(false);
            Time.timeScale = 1f;
            AudioController.Instance.PlaySound(AudioController.Instance.unpause);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("Main Menu");
        Time.timeScale = 1f;
    }
}
