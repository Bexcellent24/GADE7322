using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SceneLoaderButton : MonoBehaviour
{
    [Tooltip("Name of the scene to load when this button is clicked.")]
    public string sceneName;

    private void Awake()
    {
        Button button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("[SceneLoaderButton] No Button component found!");
            return;
        }
        
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(LoadScene);
    }

    private void LoadScene()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SceneLoaderButton] Scene name is empty!");
            return;
        }
        
        if (sceneName.ToLower() == "quit")
        {
            Debug.Log("Quitting application…");
            Application.Quit();
        }
        else
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(sceneName);
        }
    }
}