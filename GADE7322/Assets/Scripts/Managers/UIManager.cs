using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject gameOverMenu;

    [Header("HUD")]
    [SerializeField] private TMP_Text currencyText;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text countdownText;
    
    [Header("End Game UI")]
    [SerializeField] private TMP_Text wavesText;
    [SerializeField] private TMP_Text timeSurvivedText;
    [SerializeField] private TMP_Text enemiesKilledText;

    private int wave;
    private void OnEnable()
    {
        CurrencyManager.OnCurrencyChanged += UpdateCurrencyHandler;
        GameManager.OnGameLost += HandleGameLost;
        GameManager.OnGamePaused += HandlePauseStateChanged;
        EnemyWaveSpawner.OnWaveStarted += HandleWaveStarted;
        EnemyWaveSpawner.OnWaveCountdown += HandleWaveCountdown;
    }

    private void OnDisable()
    {
        CurrencyManager.OnCurrencyChanged -= UpdateCurrencyHandler;
        GameManager.OnGameLost -= HandleGameLost;
        GameManager.OnGamePaused -= HandlePauseStateChanged;
        EnemyWaveSpawner.OnWaveStarted -= HandleWaveStarted;
        EnemyWaveSpawner.OnWaveCountdown -= HandleWaveCountdown;
    }

    private void Start()
    {
        pauseMenu.SetActive(false);
        gameOverMenu.SetActive(false);
    }

    private void HandleGameLost(float timeSurvived, int enemiesKilled)
    {
        Time.timeScale = 0;
        
        TimeSpan t = TimeSpan.FromSeconds(timeSurvived);
        timeSurvivedText.text = $"{t.Minutes:D2}:{t.Seconds:D2}";
        enemiesKilledText.text = enemiesKilled.ToString();
        wavesText.text = wave.ToString();
        
        gameOverMenu.SetActive(true);
    }

    private void UpdateCurrencyHandler(int amount)
    {
        currencyText.text = amount.ToString();
    }
    private void HandleWaveStarted(int waveIndex)
    {
        waveText.text = waveIndex.ToString();
        countdownText.text = "0s";

        wave = waveIndex;
    }

    private void HandleWaveCountdown(int nextWave, float timeRemaining)
    {
        countdownText.text = $"{Mathf.FloorToInt(timeRemaining)}s";
    }
    
    private void HandlePauseStateChanged(bool isPaused)
    {
        pauseMenu.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
    }

    // UI Buttons
    public void Resume()
    {
        GameManager.Instance.TogglePause();
    }
    public void Quit()
    {
        Application.Quit();
    }
}

