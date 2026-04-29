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
    [SerializeField, Range(0f, 90f)]
    float maxGroundAngle = 25f;
    [SerializeField]
    float leanMultiplier = 0.2f;

    //internal variables
    [Header("Internal Variables")]
    [SerializeField]
    Vector3 velocity;
    [SerializeField]
    Vector3 desiredVelocity;
    [SerializeField]
    Vector3 rotationVelocity;
    bool desiredJump;
    [SerializeField]
    bool jumping;
    [SerializeField]
    bool jumpBuffer = false;
    [SerializeField]
    bool jumpCutoff = false;
    [SerializeField]
    bool onGround;
    bool cursorLock = true;
    float minGroundDotProduct;
    Vector3 contactNormal;
    [SerializeField]
    float dustEmissionRate = 10f;
    quaternion directionRotation;
    quaternion leanRotation;

    ParticleSystem.EmissionModule dustEmission;

    //references

    Rigidbody body;
    [Header("References")]
    [SerializeField]
    Transform playerInputSpace;
    Vector3 cameraRelativeMovement;
    [SerializeField]
    CinemachineCamera freelookCam;
    [SerializeField]
    ParticleSystem dustTrail;
    [SerializeField]
    Transform pivotPoint;
 
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

        //camera vectors (not used right now)
        //Vector3 camForward = transform.InverseTransformVector(Camera.main.transform.forward);
        //Vector3 camRight = transform.InverseTransformVector(Camera.main.transform.right);

        //Vector3 rightRelativeVerticalInput = playerInput.x * camRight;
        //Vector3 forwardRelativeVerticalInput = playerInput.y * camForward;

        //cameraRelativeMovement = (forwardRelativeVerticalInput + rightRelativeVerticalInput);

        //desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;

        //need to fix relative movement
        desiredVelocity = playerInputSpace.TransformDirection(playerInput.x, 0f, playerInput.y) * maxSpeed;


        //jumping
        if(jumpAction.triggered == true)
        {
            desiredJump = true;

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



        //reset camera
        if (resetCameraAction.triggered == true)
        {
            //freelookCam.ForceCameraPosition(this.transform.position, this.transform.rotation);
        }

        //reset input
        if (resetGameAction.triggered == true)
        {
            Reset();
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

    }
	void FixedUpdate ()
    {
		//set rigidbody speeds 
        velocity = body.linearVelocity;

        float acceleration;
        if (onGround)
        {
            acceleration = maxAcceleration;
        }
        else
        {
            acceleration = maxAirAcceleration;

        }
        float maxSpeedChange = acceleration * Time.deltaTime;

        velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);
		velocity.z = Mathf.MoveTowards(velocity.z, desiredVelocity.z, maxSpeedChange);

        if (desiredJump)
        {
            desiredJump = false;
            Jump();
        }
        
        if (jumpCutoff)
        {
            //jumpCutoff = false;
            //velocity.y -= 10f;
            //velocity.y -= Mathf.Max(velocity.y, jumpHeight / 4);
        }

        if (!onGround && velocity.y < 0)
        {
            jumping = false;
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
        if(onGround)
        {
            velocity.y += jumpHeight;
            jumping = true;
        }
        else
        {
            //velocity.y = velocity.y / 4f;
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

}
