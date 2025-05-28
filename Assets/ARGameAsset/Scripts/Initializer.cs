using UnityEngine;
using UnityEngine.SceneManagement;


// Clear all PlayerPrefs data and load menu scene
public class Initializer : MonoBehaviour
{
    [Tooltip("The name of the main menu scene to load after initialization.")]
    public string mainMenuSceneName = "Mapbox Route";


    void Awake()
    {
        //Debug.Log("Initializer Scene: Clearing PlayerPrefs for a new game session.");
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        //Debug.Log($"Initializer Scene: Loading main menu scene: {mainMenuSceneName}");
        // Load the main menu scene
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
