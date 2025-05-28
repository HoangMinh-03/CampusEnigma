using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

public class StorySceneManager : MonoBehaviour
{
    [Header("UI References")]
    public Button ReturnButton;

    private int currentWaypointId = -1; // Store the ID read from PlayerPrefs

    void Start()
    {
        Debug.Log("StorySceneManager Start called.");

        // Read the waypoint ID saved before loading this scene
        currentWaypointId = PlayerPrefs.GetInt("ActiveWaypointID", -1);
        Debug.Log($"StorySceneManager: Read ActiveWaypointID from PlayerPrefs: {currentWaypointId}");

        // Button click listener
        if (ReturnButton != null)
        {
            ReturnButton.onClick.AddListener(OnReturnToMenuPressed);
            Debug.Log("StorySceneManager: Added listener to ReturnButton.");
        }
        else
        {
            Debug.LogError("StorySceneManager: ReturnButton not assigned in Inspector!");
        }
    }

    void OnReturnToMenuPressed()
    {
        Debug.Log("StorySceneManager: OnReturnToMenuPressed called.");

        //Mark the current waypoint as completed
        if (currentWaypointId != -1)
        {
            string completionKey = "WaypointCompleted_" + currentWaypointId;
            PlayerPrefs.SetInt(completionKey, 1);
            PlayerPrefs.Save();
            Debug.Log($"Marked waypoint ID {currentWaypointId} as completed (Key: {completionKey}).");
        }
        else
        {
            Debug.LogWarning("StorySceneManager: No valid waypoint ID to mark as completed.");
        }


        // Load the main menu scene
        string mainMenuSceneName = "Mapbox Route";

        Debug.Log($"StorySceneManager: Loading scene: {mainMenuSceneName}");

        try
        {
            SceneManager.LoadScene(mainMenuSceneName);
            Debug.Log($"StorySceneManager: SceneManager.LoadScene called for: {mainMenuSceneName}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"StorySceneManager: Error loading scene '{mainMenuSceneName}': {ex.Message}");
        }
    }
}