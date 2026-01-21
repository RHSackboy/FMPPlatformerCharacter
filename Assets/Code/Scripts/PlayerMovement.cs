using Unity.Mathematics;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.WSA;

public class PlayerMovement : MonoBehaviour
{
    //parameters
    [SerializeField, Range(0f, 100f)]
    float maxSpeed = 10f;
    [SerializeField, Range(0f, 100f)]
    float maxAcceleration = 10f;
    [SerializeField]
    Vector3 velocity;
    Vector3 desiredVelocity;
    [SerializeField]
    bool desiredJump;
    
    //references
    Rigidbody body;
    [SerializeField]
    Transform playerInputSpace;

    Vector3 cameraRelativeMovement;
 

    //input
    InputAction moveAction;
    InputAction jumpAction;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
    }
    void Awake()
    {
        body = GetComponent<Rigidbody>();
        
    }

    // Update is called once per frame
    void Update()
    {
        //player input
        Vector2 playerInput;
        playerInput.x = moveAction.ReadValue<Vector2>().x;
        playerInput.y = moveAction.ReadValue<Vector2>().y;
        playerInput = Vector2.ClampMagnitude(playerInput, 1f);

        //camera vectors
        Vector3 camForward = transform.InverseTransformVector(Camera.main.transform.forward);
        Vector3 camRight = transform.InverseTransformVector(Camera.main.transform.right);

        Vector3 rightRelativeVerticalInput = playerInput.x * camRight;
        Vector3 forwardRelativeVerticalInput = playerInput.y * camForward;

        cameraRelativeMovement = (forwardRelativeVerticalInput + rightRelativeVerticalInput);

        //desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;
        
        //fix relative movement
        desiredVelocity = playerInputSpace.TransformDirection(playerInput.x, 0f, playerInput.y) * maxSpeed;

        if(jumpAction.triggered == true)
        {
            desiredJump = true;
        }


    }
	void FixedUpdate ()
    {
		velocity = body.linearVelocity;
		float maxSpeedChange = maxAcceleration * Time.deltaTime;

        velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);
		velocity.z = Mathf.MoveTowards(velocity.z, desiredVelocity.z, maxSpeedChange);

        if (desiredJump)
        {
            desiredJump = false;
            Jump();
        }

        body.linearVelocity = velocity;
    }

    void Jump ()
    {
        velocity.y += 5f;
    }
}
