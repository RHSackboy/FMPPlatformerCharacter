using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine.Android;
using NUnit.Framework.Constraints;
using System.Text.RegularExpressions;

public class UserInt : MonoBehaviour
{

    InputAction unfocusAction;
    InputAction pauseGameAction;
    InputAction lookAction;  

    InputAction moveAction;
    InputAction jumpAction;
    InputAction resetCameraAction;

    [SerializeField]
    CinemachineInputAxisController camInput;


    public bool cursorLock = true;
    public bool paused = false;
    public bool mapOrSettings = true;
    [SerializeField]
    int frameRateTarget = 60;


    Slider camSensitivitySlider;
    Label camSensitivityLabel;
    [SerializeField]
    UIDocument Menu;
    VisualElement mainMenu;
    VisualElement MapMenu;
    VisualElement SettingsMenu;
    VisualElement playerIcon;
    Button continueButton;
    Button mapButton;
    Button settingsButton;
    Button resetButton;
    Button quitButton;
    DropdownField frameRateCap;

    Button moveFBinding;
    Button moveBBinding;
    Button moveRBinding;
    Button moveLBinding;
    Button jumpBinding;
    Button resetCamBinding;

    private InputActionRebindingExtensions.RebindingOperation moveFRebindOperation;
    private InputActionRebindingExtensions.RebindingOperation moveBRebindOperation;
    private InputActionRebindingExtensions.RebindingOperation moveLRebindOperation;
    private InputActionRebindingExtensions.RebindingOperation moveRRebindOperation;
    private InputActionRebindingExtensions.RebindingOperation jumpRebindOperation;
    private InputActionRebindingExtensions.RebindingOperation resetCamRebindOperation;

    public Toggle invertX;
    public Toggle invertY;
    [SerializeField]
    float mapScale;
    [SerializeField]
    float slidervalue;
    [SerializeField]
    float maxSensitivity = 0.2f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        //Application.targetFrameRate = frameRateTarget;
        //SetFrameRateCap();
        unfocusAction = InputSystem.actions.FindAction("Unfocus");
        pauseGameAction = InputSystem.actions.FindAction("Pause Game");
        lookAction = InputSystem.actions.FindAction("Look");

        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        resetCameraAction = InputSystem.actions.FindAction("Reset Camera");


        mainMenu = Menu.rootVisualElement.Q<VisualElement>("MainMenu");
        MapMenu = Menu.rootVisualElement.Q<VisualElement>("MapMenu");
        SettingsMenu = Menu.rootVisualElement.Q<VisualElement>("SettingsMenu");
        continueButton = Menu.rootVisualElement.Q<Button>("Continue");
        mapButton = Menu.rootVisualElement.Q<Button>("Map");
        settingsButton = Menu.rootVisualElement.Q<Button>("Settings");
        resetButton = Menu.rootVisualElement.Q<Button>("Reset");
        quitButton = Menu.rootVisualElement.Q<Button>("Quit");
        playerIcon = Menu.rootVisualElement.Q<VisualElement>("PlayerIcon");
        camSensitivitySlider = Menu.rootVisualElement.Q<Slider>("CameraSensitivity");
        camSensitivityLabel = Menu.rootVisualElement.Q<Label>("SensitivityNumber");
        frameRateCap = Menu.rootVisualElement.Q<DropdownField>("FrameRateCap");
        invertX = Menu.rootVisualElement.Q<Toggle>("CamX");
        invertY = Menu.rootVisualElement.Q<Toggle>("CamY");

        moveFBinding = Menu.rootVisualElement.Q<Button>("MoveFBinding");
        moveBBinding = Menu.rootVisualElement.Q<Button>("MoveBBinding");
        moveRBinding = Menu.rootVisualElement.Q<Button>("MoveRBinding");
        moveLBinding = Menu.rootVisualElement.Q<Button>("MoveLBinding");
        jumpBinding = Menu.rootVisualElement.Q<Button>("JumpBinding");
        resetCamBinding = Menu.rootVisualElement.Q<Button>("ResetCamBinding");



        continueButton.clicked += PauseGame;
        mapButton.clicked += SetMapMenu;
        settingsButton.clicked += SetSettingsMenu;
        resetButton.clicked += ResetGame;
        quitButton.clicked += QuitGame;
        
        
        moveFBinding.clicked += MoveFKeyBinding;
        moveBBinding.clicked += MoveBKeyBinding;
        moveRBinding.clicked += MoveRKeyBinding;
        moveLBinding.clicked += MoveLKeyBinding;
        jumpBinding.clicked += JumpKeyBinding;
        resetCamBinding.clicked += ResetCamKeyBinding;

        moveFBinding.text = moveAction.GetBindingDisplayString(2);
        moveBBinding.text = moveAction.GetBindingDisplayString(3);
        moveRBinding.text = moveAction.GetBindingDisplayString(5);
        moveLBinding.text = moveAction.GetBindingDisplayString(4);
        jumpBinding.text = jumpAction.GetBindingDisplayString(group:"Keyboard&Mouse");
        resetCamBinding.text = resetCameraAction.GetBindingDisplayString(group:"Keyboard&Mouse");

        mapOrSettings = false;

        Application.targetFrameRate = frameRateTarget;

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
        slidervalue = Mathf.Round(camSensitivitySlider.value) * (maxSensitivity / 100);
        camSensitivityLabel.text = Mathf.Round(camSensitivitySlider.value).ToString();

        RebindOperations();

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
            //cursorLock = false;
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
            SetFrameRateCap();
            SetInvertCamera();
            setSensitivity();
            MapMenu.visible = false;
            SettingsMenu.visible = false;
            mainMenu.visible = false;
            moveAction.Enable();
            jumpAction.Enable();
            resetCameraAction.Enable();
            cursorLock = true;
            Time.timeScale = 1f;
            paused = false;
        }
        else
        {
            Time.timeScale = 0f;

            playerIcon.transform.position = new Vector3(296 + (transform.position.x * mapScale), 47 + (-transform.position.z * mapScale), 0);
            playerIcon.transform.rotation = Quaternion.Euler(0f, 0f, transform.rotation.eulerAngles.y);

            if(mapOrSettings)
            {
                SetSettingsMenu();
                //SettingsMenu.visible = true;
            }
            else
            {
                SetMapMenu();
                //MapMenu.visible = true;
            }

            moveAction.Disable();
            jumpAction.Disable();
            resetCameraAction.Disable();
            cursorLock = false;
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

    public void SetMapMenu()
    {
        mapOrSettings = false;
        MapMenu.visible = true;
        SettingsMenu.visible = false;
    }

    public void SetSettingsMenu()
    {
        mapOrSettings = true;
        MapMenu.visible = false;
        SettingsMenu.visible = true;
    }

    public void SetFrameRateCap()
    {
        if(frameRateCap.index == 0)
        {
            frameRateTarget = -1;
        }
        
        if(frameRateCap.index == 1)
        {
            frameRateTarget = 30;
        }
        
        if(frameRateCap.index == 2)
        {
            frameRateTarget = 40;
        }
        
        if(frameRateCap.index == 3)
        {
            frameRateTarget = 60;
        }
        
        if(frameRateCap.index == 4)
        {
            frameRateTarget = 120;
        }


        Application.targetFrameRate = frameRateTarget;
    }

    public void SetInvertCamera()
    {

        // need to change cinemachine input provider
        if(invertX.value == true)
        {
            lookAction.ApplyParameterOverride("invertVector2:x", true);
        }
        else
        {
            lookAction.ApplyParameterOverride("invertVector2:x", false);
        }

        if(invertY.value == true)
        {
            lookAction.ApplyParameterOverride("invertVector2:y", true);
        }
        else
        {
            lookAction.ApplyParameterOverride("invertVector2:y", false);
        }
    }

    public void setSensitivity()
    {
        //lookAction.ApplyParameterOverride("scaleVector2:x", slidervalue, InputBinding.MaskByGroup("Mouse"));
        //lookAction.ApplyParameterOverride("scaleVector2:y", slidervalue, InputBinding.MaskByGroup("Mouse"));

        foreach(var c in camInput.Controllers)
        {
            if(c.Name == "Look Orbit X")
            {
                if(invertX.value == true)
                {
                    c.Input.Gain = -slidervalue;
                }
                else
                {
                    c.Input.Gain = slidervalue;
                }
            }
            if(c.Name == "Look Orbit Y")
            {
                if(invertY.value == true)
                {
                    c.Input.Gain = slidervalue;
                }
                else
                {
                    c.Input.Gain = -slidervalue;
                }
            }

        }
    }





    public void MoveFKeyBinding()
    {
        //moveAction.Disable();
        moveFRebindOperation = moveAction.PerformInteractiveRebinding(2).WithControlsExcluding("Mouse").WithCancelingThrough("<Keyboard>/escape").Start();
    }

    public void MoveBKeyBinding()
    {
        //moveAction.Disable();
        moveBRebindOperation = moveAction.PerformInteractiveRebinding(3).WithControlsExcluding("Mouse").WithCancelingThrough("<Keyboard>/escape").Start();
    }

    public void MoveRKeyBinding()
    {
        //moveAction.Disable();
        moveRRebindOperation = moveAction.PerformInteractiveRebinding(5).WithControlsExcluding("Mouse").WithCancelingThrough("<Keyboard>/escape").Start();
    }

    public void MoveLKeyBinding()
    {
        //moveAction.Disable();
        moveLRebindOperation = moveAction.PerformInteractiveRebinding(4).WithControlsExcluding("Mouse").WithCancelingThrough("<Keyboard>/escape").Start();
    }

    public void JumpKeyBinding()
    {
        //jumpAction.Disable();
        jumpRebindOperation = jumpAction.PerformInteractiveRebinding(0).WithControlsExcluding("Mouse").WithCancelingThrough("<Keyboard>/escape").Start();
    }
    public void ResetCamKeyBinding()
    {
        //resetCameraAction.Disable();
        resetCamRebindOperation = resetCameraAction.PerformInteractiveRebinding(0).WithControlsExcluding("Mouse").WithCancelingThrough("<Keyboard>/escape").Start();
    }


    public void RebindOperations()
    {
        if(moveFRebindOperation != null && moveFRebindOperation.completed)
        {
            //moveAction.Enable();
            moveFRebindOperation.Dispose();
        }

        if(moveFRebindOperation.started)
        {
            moveFBinding.text = "Rebind";
        }
        else
        {
            moveFBinding.text = moveAction.GetBindingDisplayString(2);
        }

        if(moveBRebindOperation != null && moveBRebindOperation.completed)
        {
            //moveAction.Enable();
            moveBRebindOperation.Dispose();
        }

        if(moveBRebindOperation.started)
        {
            moveBBinding.text = "Rebind";
        }
        else
        {
            moveBBinding.text = moveAction.GetBindingDisplayString(3);
        }
        

        if(moveRRebindOperation != null && moveRRebindOperation.completed)
        {
            //moveAction.Enable();
            moveRRebindOperation.Dispose();
        }

        if(moveRRebindOperation.started)
        {
            moveRBinding.text = "Rebind";
        }
        else
        {
            moveRBinding.text = moveAction.GetBindingDisplayString(5);
        }



        if(moveLRebindOperation != null && moveLRebindOperation.completed)
        {
            //moveAction.Enable();
            moveLRebindOperation.Dispose();
        }

        if(moveLRebindOperation.started)
        {
            moveLBinding.text = "Rebind";
        }
        else
        {
            moveLBinding.text = moveAction.GetBindingDisplayString(4);
        }




        
        if(jumpRebindOperation != null && jumpRebindOperation.completed)
        {
            //jumpAction.Enable();
            jumpRebindOperation.Dispose();
        }

        if(jumpRebindOperation.started)
        {
            jumpBinding.text = "Rebind";
        }
        else
        {
            jumpBinding.text = jumpAction.GetBindingDisplayString(group:"Keyboard&Mouse");
        }


        if(resetCamRebindOperation != null && resetCamRebindOperation.completed)
        {
            //resetCameraAction.Enable();
            resetCamRebindOperation.Dispose();
        }

        if(resetCamRebindOperation.started)
        {
            resetCamBinding.text = "Rebind";
        }
        else
        {
            resetCamBinding.text = resetCameraAction.GetBindingDisplayString(group:"Keyboard&Mouse");
        }

        

    }
}


