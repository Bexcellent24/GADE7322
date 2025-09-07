using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("UI HUD Stuff")]
    [SerializeField] private TMP_Text CurrencyText;
    
    [Header("UI Pause Stuff")]
    [SerializeField] private GameObject pauseMenu;
    
    [Header("UI End Game Stuff")]
    [SerializeField] private GameObject endGameMenu;
    [SerializeField] private TMP_Text timeSurvivedText;
    [SerializeField] private TMP_Text EnemiesKilledText;

    private void Start()
    {
        pauseMenu.SetActive(false);
        endGameMenu.SetActive(false);
    }

    private void OnEnable()
    {
        CurrencyManager.OnCurrencyChanged += UpdateCurrencyHandler;
        GameManager.OnPauseToggle += TogglePauseHandler;
        GameManager.OnGameLost += GameLostHandler;
    }
    
    private void OnDestroy()
    {
        CurrencyManager.OnCurrencyChanged -= UpdateCurrencyHandler;
        GameManager.OnPauseToggle -= TogglePauseHandler;
        GameManager.OnGameLost -= GameLostHandler;
    }

    private void GameLostHandler(float TimeSurvived, int enemiesKilled)
    {
        endGameMenu.SetActive(true);
        TimeSpan t = TimeSpan.FromSeconds(TimeSurvived);
        timeSurvivedText.text = string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
        EnemiesKilledText.text = enemiesKilled.ToString();
        
    }
    
    private void UpdateCurrencyHandler(int amount)
    {
        CurrencyText.text = amount.ToString();
    }

    private void TogglePauseHandler()
    {
        pauseMenu.SetActive(!pauseMenu.activeSelf);
        Time.timeScale = pauseMenu.activeSelf ? 0 : 1;
    }

    public void Resume()
    {
        pauseMenu.SetActive(false);
        Time.timeScale = 1;
    }
    
    public void Restart()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
