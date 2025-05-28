using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    // UI Elements
    [Header("UI Elements")]
    public TextMeshProUGUI sceneDescriptionText;
    public GameObject dialoguePanel;
    public TextMeshProUGUI studentNameText;
    public TextMeshProUGUI studentDialogueText;
    public Transform playerOptionsParent; // Parent GameObject for buttons
    public GameObject optionButtonPrefab; // Prefab for player choice buttons

    [Header("Game Progression UI")]
    public GameObject returnToARButton;

    // Dialogue Data Structure
    [System.Serializable]
    public class DialogueSegment
    {
        [TextArea(3, 6)] public string studentLine;
        public PlayerChoice[] choices;
    }

    [System.Serializable]
    public class PlayerChoice
    {
        public string choiceText;
        public int nextDialogueIndex; // Index in the dialogueSegments array
        public bool isCorrectAnswer; // Is this the correct answer to the question
    }

    [Header("Dialogue Data")]
    public string initialSceneDescription = "The university library is unnaturally quiet. Among the towering bookshelves, a hunched figure trembles at a desk, muttering to himself.";
    public DialogueSegment[] dialogueSegments;

    // Internal State
    private int currentDialogueIndex = 0;
    private int correctAnswersCount = 0;
    private const int requiredCorrectAnswers = 5; // We need 5 correct choices to finish the 4 segments

    public delegate void OnMiniGameComplete();
    public static event OnMiniGameComplete miniGameComplete;

    void Start()
    {
        dialoguePanel.SetActive(false); // Start with dialogue hidden
        sceneDescriptionText.text = initialSceneDescription; // Set initial scene description

        // return button is hidden at the start
        if (returnToARButton != null)
        {
            returnToARButton.SetActive(false);
        }

        StartCoroutine(StartMiniGameAfterDelay(2f)); // Delay to let player read initial description
    }

    IEnumerator StartMiniGameAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartDialogue();
    }

    public void StartDialogue()
    {
        dialoguePanel.SetActive(true);
        studentNameText.text = "Alex (Worried Student)";
        DisplayDialogueSegment(currentDialogueIndex);
    }

    void DisplayDialogueSegment(int index)
    {
        if (index >= 0 && index < dialogueSegments.Length)
        {
            currentDialogueIndex = index;
            DialogueSegment currentSegment = dialogueSegments[currentDialogueIndex];

            studentDialogueText.text = currentSegment.studentLine;

            // Clear previous options
            foreach (Transform child in playerOptionsParent)
            {
                Destroy(child.gameObject);
            }

            // Create new options
            for (int i = 0; i < currentSegment.choices.Length; i++)
            {
                GameObject buttonGO = Instantiate(optionButtonPrefab, playerOptionsParent);
                TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = currentSegment.choices[i].choiceText;
                }
                Button button = buttonGO.GetComponent<Button>();
                if (button != null)
                {
                    int choiceIndex = i; // Local copy for closure
                    button.onClick.AddListener(() => OnPlayerChoice(choiceIndex));
                }
            }
        }
        else
        {
            Debug.LogWarning("Dialogue index out of range: " + index);
            EndDialogue(); // This will be called when the last segment's correct choice leads to an out-of-bounds index like -1
        }
    }

    void OnPlayerChoice(int choiceIndex)
    {
        PlayerChoice chosen = dialogueSegments[currentDialogueIndex].choices[choiceIndex];

        if (chosen.isCorrectAnswer)
        {
            correctAnswersCount++;
            Debug.Log($"Correct choice made! Total correct: {correctAnswersCount}/{requiredCorrectAnswers}");

            if (correctAnswersCount >= requiredCorrectAnswers)
            {
                Debug.Log("Mini-game complete! Alex is calm and shared info.");
                // Ensure the final dialogue segment is displayed before ending
                DisplayDialogueSegment(chosen.nextDialogueIndex);
                // The EndDialogue() will be called when this DisplayDialogueSegment tries to go to -1
                return;
            }
            else
            {
                // Move to the next dialogue segment
                DisplayDialogueSegment(chosen.nextDialogueIndex);
            }
        }
        else // Incorrect answer
        {
            correctAnswersCount = 0; // Reset progress on incorrect choice
            Debug.Log("Incorrect choice. Progress reset. Alex is agitated.");
            // Loop the current dialogue segment by displaying it again
            DisplayDialogueSegment(chosen.nextDialogueIndex);
        }
    }

    void EndDialogue()
    {
        dialoguePanel.SetActive(false); // Hide the dialogue panel
        sceneDescriptionText.gameObject.SetActive(false);

        // Show the "Return to AR Game" button
        if (returnToARButton != null)
        {
            returnToARButton.SetActive(true);
            Debug.Log("Return to AR Game button is now visible.");
        }

        Debug.Log("Dialogue Ended.");
        if (miniGameComplete != null)
        {
            miniGameComplete(); // Trigger event for other systems like scene transition
        }
    }
}