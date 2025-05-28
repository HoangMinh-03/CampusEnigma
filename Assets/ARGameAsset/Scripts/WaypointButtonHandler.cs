using UnityEngine;
using UnityEngine.UI;

public class WaypointButtonHandler : MonoBehaviour
{
    [Tooltip("The unique ID of the waypoint this button is associated with.")]
    public int waypointId;

    private Button button;
    private ARLocation.MapboxRoutes.SampleProject.ArMenuController arMenuController; // Reference to the ArMenuController

    void Awake()
    {
        // Get the Button component attached to this GameObject
        button = GetComponent<Button>();

        // Find the ArMenuController
        arMenuController = FindObjectOfType<ARLocation.MapboxRoutes.SampleProject.ArMenuController>();
        if (arMenuController == null)
        {
            Debug.LogError("WaypointButtonHandler: ArMenuController not found in scene! Ensure it's in the scene.");
        }
    }

    void OnEnable()
    {
        // Add a listener to the button's onClick event
        if (button != null && arMenuController != null)
        {
            // When the button is clicked, call HandleButtonClick method
            button.onClick.AddListener(HandleButtonClick);
        }
    }

    void OnDisable()
    {
        // Remove the listener when the GameObject is disabled or destroyed
        if (button != null && arMenuController != null)
        {
            button.onClick.RemoveListener(HandleButtonClick);
        }
    }

    // Called when the button is clicked
    void HandleButtonClick()
    {
        if (arMenuController != null)
        {
            // Call the OnWaypointButtonPressed method on the ArMenuController,
            // passing this button's waypointId.
            arMenuController.OnWaypointButtonPressed(waypointId);
        }
    }
}
