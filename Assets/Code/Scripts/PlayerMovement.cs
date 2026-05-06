using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Unity.Cinemachine;
using UnityEditor.UI;

public class PlayerMovement : MonoBehaviour
{
    //parameters
    [Header("Parameters")]
    [SerializeField, Range(0f, 100f)]
    float maxSpeed = 10f;
    [SerializeField, Range(0f, 100f)]
    float maxAcceleration = 10f;
    [SerializeField, Range(0f, 100f)]
    float maxAirAcceleration = 10f;
    [SerializeField, Range(0f, 10f)]
	float jumpHeight = 5f;
    [SerializeField, Range(0f,1f)]
    float jumpBufferTime = 0.5f;
    [SerializeField, Range(0f,1f)]
    float coyoteTimeTime = 0.5f;
    [SerializeField, Range(0f, 90f)]
    float maxGroundAngle = 25f;
    [SerializeField]
    float leanMultiplier = 0.2f;

    //states
    [Header("States")]
    [SerializeField]
    Vector3 velocity;
    [SerializeField]
    Vector3 desiredVelocity;
    [SerializeField]
    Vector3 rotationVelocity;
    bool jumpTrigger;
    [SerializeField]
    bool jumping;
    [SerializeField]
    bool jumpBuffer = false;
    Timer jumpBufferTimer;
    [SerializeField]
    bool coyoteTime;
    [SerializeField]
    bool noCoyoteTime;
    Timer coyoteTimeTimer;
    [SerializeField]
    bool coyoteTimeTrigger;
    bool jumpCutoff = false;
    [SerializeField]
    bool onGround;
    [SerializeField]
    bool landingTrigger = false;
    bool cursorLock = true;
    float minGroundDotProduct;
    Vector3 contactNormal;
    [SerializeField]
    float dustEmissionRate = 10f;
    quaternion directionRotation;
    quaternion leanRotation;
    ParticleSystem.EmissionModule dustEmission;
    [SerializeField]
    bool recentreing;
    [SerializeField]
    float camFollowY;

    //references
    Rigidbody body;
    [Header("References")]
    [SerializeField]
    Transform playerInputSpace;
    Vector3 cameraRelativeMovement;
    [SerializeField]
    CinemachineOrbitalFollow orbitalFollow;
    [SerializeField]
    CinemachineInputAxisController inputAxisController;
    [SerializeField]
    ParticleSystem dustTrail;
    [SerializeField]
    Transform pivotPoint;
    [SerializeField]
    Transform playerCamFollow;
 
    //input
    InputAction moveAction;
    InputAction jumpAction;
    InputAction resetCameraAction;
    InputAction resetGameAction;
    InputAction unfocusAction;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //reference action map
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        resetCameraAction = InputSystem.actions.FindAction("Reset Camera");
        resetGameAction = InputSystem.actions.FindAction("Reset Game");
        unfocusAction = InputSystem.actions.FindAction("Unfocus");
        dustEmission = dustTrail.emission;
    }
    void Awake()
    {
        //initialise rigid body
        body = GetComponent<Rigidbody>();
        OnValidate();
    }

    // Update is called once per frame
    void Update()
    {
        //player input
        Vector2 playerInput;
        playerInput.x = moveAction.ReadValue<Vector2>().x;
        playerInput.y = moveAction.ReadValue<Vector2>().y;
        playerInput = Vector2.ClampMagnitude(playerInput, 1f);

        //need to fix relative movement
        desiredVelocity = playerInputSpace.TransformDirection(playerInput.x, 0f, playerInput.y) * maxSpeed;

        //jump input
        if(jumpAction.triggered == true)
        {
            jumpTrigger = true;

        }
        
        //camera reset input
        if (resetCameraAction.triggered == true && !recentreing)
        {
            recentreing = true;
            Timer.Register(0.2f, () => recentreing = false);
        }

        //reset game input
        if (resetGameAction.triggered == true)
        {
            Reset();
        }

        //dust trail
        if (onGround && velocity != Vector3.zero)
        {
            dustEmission.rateOverTime = dustEmissionRate;
        }
        else
        {
            dustEmission.rateOverTime = 0f;
        }

        //cursor lock
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

        if (unfocusAction.triggered == true)
        {
            cursorLock = false;
        }

        if (Input.GetMouseButtonDown(0))
        {
            cursorLock = true;
        }
        

        //variable jumping
        if(jumpAction.WasReleasedThisFrame() && jumping)
        {
            Debug.Log("Release!");
            //add removal of fall speed here
            //VariableJump();
            //jumpCutoff = true;
        }

        playerCamFollow.position = new Vector3 (gameObject.transform.position.x, camFollowY, gameObject.transform.position.z);
        playerCamFollow.rotation = gameObject.transform.rotation;

        if(onGround)
        {
            camFollowY = Mathf.Lerp(camFollowY, gameObject.transform.position.y, 0.01f);
        }

    }
	void FixedUpdate ()
    {
		//set rigidbody speeds 
        velocity = body.linearVelocity;

        float acceleration;
        
        if (onGround)
        {
            acceleration = maxAcceleration;
            noCoyoteTime = false;
            coyoteTime = false;
            Timer.Cancel(coyoteTimeTimer);
        }
        else
        {
            acceleration = maxAirAcceleration;

        }
       
       float maxSpeedChange = acceleration * Time.deltaTime;

        velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);
		velocity.z = Mathf.MoveTowards(velocity.z, desiredVelocity.z, maxSpeedChange);

        //jumping
        if (jumpTrigger)
        {
            jumpTrigger = false;
            Jump();
        }
        
        if (jumpCutoff)
        {
            jumpCutoff = false;
            //velocity.y -= 10f;
            velocity.y -= Mathf.Max(velocity.y, jumpHeight / 4);
        }

        //jump buffering
        if(jumpBuffer && onGround)
        {
            jumpTrigger = true;
        }

        if (landingTrigger)
        {
            landingTrigger = false;
        }

        //coyote time 
        if (!onGround && velocity.y < 0)
        {
            jumping = false;

            if(!noCoyoteTime)
            {
                coyoteTimeTrigger = true;
            }
            else
            {
                coyoteTimeTrigger = false;
            }
        }
        
        if(coyoteTimeTrigger)
        {
            coyoteTimeTrigger = false;
            coyoteTime = true;
            coyoteTimeTimer = Timer.Register(coyoteTimeTime, () => coyoteTime = false);
        }

        //camera reset
        if(recentreing)
        {
            inputAxisController.enabled = false;
            orbitalFollow.HorizontalAxis.Recentering.Enabled = true;
            orbitalFollow.VerticalAxis.Recentering.Enabled = true;
        }
        else
        {
            inputAxisController.enabled = true;
            orbitalFollow.HorizontalAxis.Recentering.Enabled = false;
            orbitalFollow.VerticalAxis.Recentering.Enabled = false;
        }

        body.linearVelocity = velocity;
        onGround = false;

        //look in movement direction
        if (moveAction.ReadValue<Vector2>() != Vector2.zero)
        {
            directionRotation = Quaternion.LookRotation(new Vector3(velocity.x, 0, velocity.z), Vector3.up);
        }
        
        transform.rotation = directionRotation;

    }


    //collision detection
    void OnCollisionEnter(Collision collision)
    {
        //onGround = true;
        EvaluateCollision(collision);
        if (collision.gameObject.name == "Death Plane")
        {
            Reset();
        }
    }
    void OnCollisionStay(Collision collision)
    {
        //onGround = true;
        EvaluateCollision(collision);
    }

    //differentiate floors and walls for jumping
	void EvaluateCollision (Collision collision) {
		for (int i = 0; i < collision.contactCount; i++) {
			Vector3 normal = collision.GetContact(i).normal;
            //onGround |= normal.y >= minGroundDotProduct;
            if (normal.y >= minGroundDotProduct)
            {
                onGround = true;
                contactNormal = normal;

                //jumping = false;
            }
        }
	}

    //jump when on ground
    void Jump ()
    {
        if(onGround || coyoteTime)
        {
            jumpBuffer = false;
            Timer.Cancel(coyoteTimeTimer);
            coyoteTime = false;
            jumping = true;
            velocity.y += jumpHeight;
            Timer.Register(0.05f, () => noCoyoteTime = true);
        }
        else
        {
            jumpBuffer = true;
            Timer.Cancel(jumpBufferTimer);
            jumpBufferTimer = Timer.Register(jumpBufferTime, () => jumpBuffer = false);
        }
    }
    
    void VariableJump ()
    {
        //velocity.y -= 10f;
    }

    //reset game
    public void Reset()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    void OnValidate()
    {
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
    }


    //enable cursor lock when focused
    private void OnApplicationFocus(bool focus)
    {
        //cursorLock = focus;
    }

    void WithinScreenbounds(Transform transform)
    {

    }


}
