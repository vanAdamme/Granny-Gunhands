using UnityEngine;

public class GameSystems : MonoBehaviour, IPlayerProvider
{
    public static GameSystems Instance { get; private set; }

    public PlayerController Player { get; private set; }
    public event System.Action<PlayerController> PlayerChanged;

    [SerializeField] private bool dontDestroyOnLoad = true;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
    }

    public void RegisterPlayer(PlayerController player)
    {
        if (Player == player) return;
        Player = player;
        PlayerChanged?.Invoke(Player);
    }

    public void UnregisterPlayer(PlayerController player)
    {
        if (Player != player) return;
        Player = null;
        PlayerChanged?.Invoke(null);
    }

    public static PlayerController GetPlayer()
    {
        if (!Instance)
            Instance = FindFirstObjectByType<GameSystems>(FindObjectsInactive.Include);
        return Instance ? Instance.Player : null;
    }
}