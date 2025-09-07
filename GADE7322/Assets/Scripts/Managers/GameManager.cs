using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Events
    public static event Action OnPauseToggle;
    public static event Action<float, int> OnGameLost;

    // Stats
    public float TimeSurvived { get; private set; } = 0f;
    public int EnemiesKilled { get; private set; } = 0;

    private bool gameActive = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        Health.OnGameOverTriggered += LoseGame;
        Health.OnEnemyKilled += TrackEnemyKill;  // Enemy should invoke this when it dies
    }

    private void OnDisable()
    {
        Health.OnGameOverTriggered -= LoseGame;
        Health.OnEnemyKilled -= TrackEnemyKill;
    }

    private void Update()
    {
        if (!gameActive) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Paused");
            OnPauseToggle?.Invoke();
        }
        
        TimeSurvived += Time.deltaTime;
    }

    private void TrackEnemyKill()
    {
        EnemiesKilled++;
    }

    private void LoseGame()
    {
        gameActive = false;
        OnGameLost?.Invoke(TimeSurvived, EnemiesKilled);
    }
    
    public void ResetStats()
    {
        TimeSurvived = 0f;
        EnemiesKilled = 0;
        gameActive = true;
    }
}