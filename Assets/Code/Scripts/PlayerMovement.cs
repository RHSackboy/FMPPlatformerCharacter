using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Unity.Cinemachine;
using UnityEditor.UI;
using Unity.VisualScripting;

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
    float maxLeanAngle;
    [SerializeField]
    int frameRateTarget = 60;
    [SerializeField]
    float camFollowYLerpValue;
    [SerializeField]
    float defaultGravityScale = 2f;
    [SerializeField]
    float jumpGravityScale = 1f;

    //states
    [Header("States")]
    [SerializeField]
    Vector3 velocity;
    [SerializeField]
    Vector3 desiredVelocity;
    [SerializeField]
    Vector3 angularVelocity;
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
    float noCoyoteTimeDelay = 0.05f;
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
    ParticleSystem.EmissionModule dustEmission;
    [SerializeField]
    bool recentreing;
    float recentreTime = 0.2f;
    [SerializeField]
    float camFollowY;
    Vector3 rotationVelocity;
    Vector3 rotationLast;
    float acceleration;
    float maxSpeedChange;
    [SerializeField]
    float leaningMultiplier = 1f;
    [SerializeField]
    float leaningMax = 20f;
    [SerializeField]
    float leaningFilterSmoothing = 0.1f;
    float lowPassFilterAverage;
    float lowPassFilterPrevious;
    [SerializeField]
    float gravityScale = 2f;
    [SerializeField]
    bool buttonheld;

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
        Application.targetFrameRate = frameRateTarget;
        ReferenceActionMap();
        dustEmission = dustTrail.emission;
        body.useGravity = false;
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
        
        Inputs();
        CameraY();
        CursorLock();
        DustTrail();
        VariableJumpHeight();

    }
	void FixedUpdate ()
    {
		//set rigidbody speeds 
        velocity = body.linearVelocity;

        if (onGround)
        {
            noCoyoteTime = false;
            coyoteTime = false;
            Timer.Cancel(coyoteTimeTimer);
        }
        else
        {
            contactNormal = Vector3.up;
        }

        AdjustVelocity();
        JumpTriggers();
        CoyoteTime();
        CameraReset();
        LookRotation();  
        Leaning();

        body.linearVelocity = velocity;
        onGround = false;

        //customisable gravity
        body.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);

    }

    //collision detection
    void OnCollisionEnter(Collision collision)
    {
        EvaluateCollision(collision);
        if (collision.gameObject.name == "Death Plane")
        {
            ResetGame();
        }
    }

    void OnCollisionStay(Collision collision)
    {
        EvaluateCollision(collision);
    }

    //differentiate floors and walls for jumping
	void EvaluateCollision (Collision collision)
    {
		for (int i = 0; i < collision.contactCount; i++) {
			Vector3 normal = collision.GetContact(i).normal;
            if (normal.y >= minGroundDotProduct)
            {
                onGround = true;
                contactNormal = normal;
            }
        }
	}

    void OnValidate()
    {
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
    }

    Vector3 ProjectOnContactPlane (Vector3 vector)
    {
		return vector - contactNormal * Vector3.Dot(vector, contactNormal);
	}

    void AdjustVelocity ()
    {
		Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
		Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

        float currentX = Vector3.Dot(velocity, xAxis);
		float currentZ = Vector3.Dot(velocity, zAxis);

		acceleration = onGround ? maxAcceleration : maxAirAcceleration;
		maxSpeedChange = acceleration * Time.deltaTime;

		float newX = Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
		float newZ = Mathf.MoveTowards(currentZ, desiredVelocity.z, maxSpeedChange);

        velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
	}

    void Inputs()
    {
        Vector2 playerInput;
        playerInput.x = moveAction.ReadValue<Vector2>().x;
        playerInput.y = moveAction.ReadValue<Vector2>().y;
        playerInput = Vector2.ClampMagnitude(playerInput, 1f);

        //need to fix relative movement
        desiredVelocity = playerInputSpace.TransformDirection(playerInput.x, 0f, playerInput.y) * maxSpeed;
        
        //no relative movement
        //desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;

        //jump input
        if(jumpAction.triggered == true)
        {
            jumpTrigger = true;
        }
        
        //camera reset input
        if (resetCameraAction.triggered == true && !recentreing)
        {
            recentreing = true;
            Timer.Register(recentreTime, () => recentreing = false);
        }

        //reset game input
        if (resetGameAction.triggered == true)
        {
            ResetGame();
        }
    }

    void Jump ()
    {
        if(onGround || coyoteTime)
        {
            jumpBuffer = false;
            Timer.Cancel(coyoteTimeTimer);
            coyoteTime = false;
            jumping = true;
            //gravityScale = jumpGravityScale;
            
            float jumpSpeed = Mathf.Sqrt(-2f * (Physics.gravity.y * gravityScale) * jumpHeight);
            float alignedSpeed = Vector3.Dot(velocity, contactNormal);
            if (velocity.y > 0f)
            {
                jumpSpeed = Mathf.Max(jumpSpeed - velocity.y, 0f);
			}
            
            //velocity += contactNormal * jumpSpeed;
            velocity += Vector3.up * jumpSpeed;
            
            Timer.Register(noCoyoteTimeDelay, () => noCoyoteTime = true);
        }
        else
        {
            jumpBuffer = true;
            Timer.Cancel(jumpBufferTimer);
            jumpBufferTimer = Timer.Register(jumpBufferTime, () => jumpBuffer = false);
        }
    }

    void JumpTriggers()
    {
        if (jumpTrigger)
        {
            jumpTrigger = false;
            Jump();
        }

        //jump buffering
        if(jumpBuffer && onGround)
        {
            jumpTrigger = true;
        }
    }

    void VariableJumpHeight()
    {
        if(jumpAction.IsPressed() && jumping)
        {
            gravityScale = jumpGravityScale;
            buttonheld = true;
        }
        else
        {
            gravityScale = defaultGravityScale;
            buttonheld = false;
        }
    }

    void CoyoteTime()
    {
        if (!onGround && velocity.y <= 0)
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
    }

    void LookRotation()
    {
        rotationVelocity.x = Mathf.MoveTowards(rotationVelocity.x, desiredVelocity.x, maxSpeedChange);
		rotationVelocity.z = Mathf.MoveTowards(rotationVelocity.z, desiredVelocity.z, maxSpeedChange);

        if (moveAction.ReadValue<Vector2>() != Vector2.zero)
        {
            directionRotation = Quaternion.LookRotation(new Vector3(rotationVelocity.x, 0, rotationVelocity.z), Vector3.up);
        }
        transform.rotation = directionRotation;
    }
    
    void Leaning()
    {
        //calculate angular velocity for leaning
        //fix jittering
        angularVelocity = transform.rotation.eulerAngles - rotationLast;
        rotationLast = transform.rotation.eulerAngles;
        
        //leaning
        float leaningValue;

        if(onGround)
        {
            leaningValue = LowPassFilter(Mathf.Clamp(-angularVelocity.y * leaningMultiplier, -leaningMax, leaningMax), leaningFilterSmoothing);
        }
        else
        {
            leaningValue = Mathf.MoveTowards(pivotPoint.localRotation.z, 0f, maxSpeedChange);
        }
        pivotPoint.localRotation = Quaternion.Euler(0, 0, leaningValue);
    }

    float LowPassFilter(float input, float smoothingFactor)
    {
        lowPassFilterAverage = smoothingFactor * input + (1 - smoothingFactor) * lowPassFilterPrevious;
        lowPassFilterPrevious = lowPassFilterAverage;
        return lowPassFilterAverage;
    }

    void DustTrail()
    {
        if (onGround && velocity != Vector3.zero)
        {
            dustEmission.rateOverTime = dustEmissionRate;
        }
        else
        {
            dustEmission.rateOverTime = 0f;
        }
    }

    void CameraY()
    {
        //add delta time to lerp
        playerCamFollow.position = new Vector3 (gameObject.transform.position.x, Mathf.Lerp(playerCamFollow.position.y, camFollowY, camFollowYLerpValue), gameObject.transform.position.z);
        playerCamFollow.rotation = gameObject.transform.rotation;

        if(onGround)
        {
            camFollowY = gameObject.transform.position.y;
        }
    }

    void CameraReset()
    {
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
    }

    void CursorLock()
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

        if (unfocusAction.triggered == true)
        {
            cursorLock = false;
        }

        if (Input.GetMouseButtonDown(0))
        {
            cursorLock = true;
        }
    }

    void ResetGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    void ReferenceActionMap()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        resetCameraAction = InputSystem.actions.FindAction("Reset Camera");
        resetGameAction = InputSystem.actions.FindAction("Reset Game");
        unfocusAction = InputSystem.actions.FindAction("Unfocus");
    }

}
