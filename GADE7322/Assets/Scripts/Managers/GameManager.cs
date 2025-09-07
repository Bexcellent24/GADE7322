using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    public static event Action<bool> OnGamePaused;
    public static event Action<float, int> OnGameLost;

    public float TimeSurvived { get; private set; }
    public int EnemiesKilled { get; private set; }
    private bool isPaused = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        
        TimeSurvived += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }
    
    public void TogglePause()
    {
        isPaused = !isPaused;
        OnGamePaused?.Invoke(isPaused);
    }

    public void IncrementEnemiesKilled()
    {
        EnemiesKilled++;
    }
    
    public void LoseGame()
    {
        OnGameLost?.Invoke(TimeSurvived, EnemiesKilled);
    }
}