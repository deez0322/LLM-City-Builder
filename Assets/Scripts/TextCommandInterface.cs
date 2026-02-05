using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AITransformer; // Add this namespace

public class TextCommandInterface : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button submitButton;
    [SerializeField] private KeyCode toggleKey = KeyCode.Return;
    [SerializeField] private KeyCode submitKey = KeyCode.Return;

    [SerializeField] private TaskCreator taskCreator; // Add this field

    private bool isPanelActive = false;

    private void Awake()
    {
        // Find references if not assigned in inspector
        if (inputField == null)
            inputField = GetComponentInChildren<TMP_InputField>();

        if (submitButton == null)
            submitButton = GetComponentInChildren<Button>();

        // Find TaskCreator if not assigned in inspector
        if (taskCreator == null)
            taskCreator = FindObjectOfType<TaskCreator>();

        // Set up the button click event
        submitButton.onClick.AddListener(SubmitCommand);
    }

    private void Update()
    {
        // Toggle command panel with toggle key
        if (Input.GetKeyDown(toggleKey) && !inputField.isFocused)
        {
            ToggleCommandPanel();
        }

        // Submit with Enter key when input field is focused
        if (isPanelActive && inputField.isFocused && Input.GetKeyDown(submitKey))
        {
            SubmitCommand();
        }
    }

    public void ToggleCommandPanel()
    {
        isPanelActive = !isPanelActive;

        if (isPanelActive)
        {
            inputField.text = "";
            inputField.Select();
            inputField.ActivateInputField();
        }
    }

    public void SubmitCommand()
    {
        string userCommand = inputField.text;

        if (string.IsNullOrWhiteSpace(userCommand))
            return;

        Debug.Log($"Processing command: {userCommand}");

        // Clear the input field
        inputField.text = "";

        // Process the command using your existing system
        ProcessUserCommand(userCommand);
    }

    private void ProcessUserCommand(string command)
    {
        try
        {
            if (taskCreator != null)
            {
                StartCoroutine(taskCreator.ProcessTextToTask(command));
            }
            else
            {
                Debug.LogError("TaskCreator reference is missing!");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing command: {e.Message}");
        }
    }
}