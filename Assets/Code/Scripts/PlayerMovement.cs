using Unity.Mathematics;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

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
    bool desiredJump;
    
    //references
    Rigidbody body;
    [SerializeField]
    Transform playerInputSpace;

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

        Vector3 camForward =  playerInputSpace.forward;
        Vector3 camRight = playerInputSpace.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward = camForward.normalized;
        camRight = camRight.normalized;


        desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;

        //fix relative movement
        //desiredVelocity = playerInputSpace.TransformDirection(playerInput.x, 0f, playerInput.y) * maxSpeed;

    }
	void FixedUpdate ()
    {
		velocity = body.linearVelocity;
		float maxSpeedChange = maxAcceleration * Time.deltaTime;
		velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);
		velocity.z = Mathf.MoveTowards(velocity.z, desiredVelocity.z, maxSpeedChange);
		body.linearVelocity = velocity;
    }
}
