using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private static UIManager _instance;

    #region Buttons

    [SerializeField] private Button button1;
    [SerializeField] private Text fpsText;
    private float deltaTime = 0.0f;

    #endregion
    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIManager>();
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject("UIManager");
                    _instance = singletonObject.AddComponent<UIManager>();
                }
            }
            return _instance;
        }
    }

    // List to hold the buttons
    public List<Button> buttons;

    // Start is called before the first frame update
    void Start()
    {
        // Initialize UIManager here if needed
    }

    void Update()
    {
        // Measure frames per second
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;

        // Update the FPS text
        if (fpsText != null)
        {
            fpsText.text = "FPS: " + Mathf.Round(fps);
        }
    }

    // Function to activate a specific button and deactivate others
    public void ActivateButton(Button activeButton)
    {
        foreach (Button button in buttons)
        {
            // Activate the provided button and deactivate others
            button.interactable = (button == activeButton);
        }
    }
}
