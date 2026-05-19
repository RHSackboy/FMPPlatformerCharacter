using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Unity.Cinemachine;
using UnityEditor.UI;
using Unity.VisualScripting;
using UnityEngine.Android;

public class UserInt : MonoBehaviour
{

    InputAction unfocusAction;
    InputAction pauseGameAction;    
    public bool cursorLock = true;
    public bool paused = false;
    int frameRateTarget = 60;
    [SerializeField]
    UIDocument Menu;
    VisualElement mainMenu;
    Button resetButton;
    Button quitButton;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        Application.targetFrameRate = frameRateTarget;
        unfocusAction = InputSystem.actions.FindAction("Unfocus");
        pauseGameAction = InputSystem.actions.FindAction("Pause Game");
        
        
        mainMenu = Menu.rootVisualElement.Q<VisualElement>("MainMenu");
        resetButton = Menu.rootVisualElement.Q<Button>("Reset");
        quitButton = Menu.rootVisualElement.Q<Button>("Quit");
        resetButton.clicked += ResetGame;
        quitButton.clicked += QuitGame;

        mainMenu.visible = false;
        Time.timeScale = 1f;
    }

    // Update is called once per frame
    void Update()
    {
        CursorLock();

        if (pauseGameAction.triggered == true)
        {
            PauseGame();
            //ResetGame();
        }
    }

    public void CursorLock()
    {
        if (cursorLock)
        {
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }
        else
        {
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }

        if (unfocusAction.triggered == true || paused)
        {
            cursorLock = false;
        }

        if (Input.GetMouseButtonDown(0) && !paused)
        {
            cursorLock = true;
        }
    }

    public void PauseGame()
    {
        if(paused)
        {
            Time.timeScale = 1f;
            mainMenu.visible = false;
            //Menu.enabled = false;
            paused = false;
        }
        else
        {
            Time.timeScale = 0f;
            mainMenu.visible = true;
            //Menu.enabled = true;
            paused = true;
        }
    }

    public void ResetGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Debug.Log("Reset");
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit");
    }
    
}


