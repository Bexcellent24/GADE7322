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
        // Subscribe to relevant events
        CurrencyManager.OnCurrencyChanged += UpdateCurrencyHandler;
        GameManager.OnGameLost += HandleGameLost;
        GameManager.OnGamePaused += HandlePauseStateChanged;

        // Subscribe to the new adaptive wave spawner events
        AdaptiveEnemyWaveSpawner.OnWaveStarted += HandleWaveStarted;
        AdaptiveEnemyWaveSpawner.OnWaveCountdown += HandleWaveCountdown;
    }

    private void OnDisable()
    {
        // Unsubscribe when disabled
        CurrencyManager.OnCurrencyChanged -= UpdateCurrencyHandler;
        GameManager.OnGameLost -= HandleGameLost;
        GameManager.OnGamePaused -= HandlePauseStateChanged;

        AdaptiveEnemyWaveSpawner.OnWaveStarted -= HandleWaveStarted;
        AdaptiveEnemyWaveSpawner.OnWaveCountdown -= HandleWaveCountdown;
    }

    private void Start()
    {
        pauseMenu.SetActive(false);
        gameOverMenu.SetActive(false);
        countdownText.text = "";
    }

    // Called when the game is lost
    private void HandleGameLost(float timeSurvived, int enemiesKilled)
    {
        Time.timeScale = 0;

        TimeSpan t = TimeSpan.FromSeconds(timeSurvived);
        timeSurvivedText.text = $"{t.Minutes:D2}:{t.Seconds:D2}";
        enemiesKilledText.text = enemiesKilled.ToString();
        wavesText.text = wave.ToString();

        gameOverMenu.SetActive(true);
    }

    // Updates currency display
    private void UpdateCurrencyHandler(int amount)
    {
        if (currencyText)
            currencyText.text = amount.ToString();
    }

    // Called when a new wave starts
    private void HandleWaveStarted(int waveIndex)
    {
        wave = waveIndex;
        if (waveText)
            waveText.text = waveIndex + "";
        if (countdownText)
            countdownText.text = "0s";
    }

    // Called repeatedly during countdown to next wave
    private void HandleWaveCountdown(int nextWave, float timeRemaining)
    {
        if (countdownText)
            countdownText.text = $"{Mathf.CeilToInt(timeRemaining)}s";
    }
    
    // Handles pause toggle
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
