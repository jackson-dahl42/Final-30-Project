using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

[RequireComponent(typeof(CharacterController))]
public class FPSController : NetworkBehaviour
{
    [Header("Base setup")]
    [SerializeField] private float walkingSpeed = 7.5f; // Walking speed of the character
    [SerializeField] private float runningSpeed = 11.5f; // Running speed of the character
    [SerializeField] private float jumpSpeed = 8.0f; // Jump speed of the character
    [SerializeField] private float gravity = 2.5f; // Gravity applied to the character
    [SerializeField] private float lookSpeed = 2.0f; // Speed of camera rotation
    [SerializeField] private float lookXLimit = 45.0f; // Limit of camera rotation on the X-axis
    [SerializeField] private float customGravity = 100.0f; 

    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero; // Direction of character movement
    private float rotationX = 0; // Rotation value for camera rotation
    private bool isGravityNormal = true; // Flag to indicate whether normal or custom gravity is applied

    [HideInInspector]
    public bool CanMove { get; set; } = true; // Flag to indicate whether the character can move or not

    [SerializeField] private float cameraYOffset = 0.4f; // Offset of the player camera from the character's position
    private Camera playerCamera;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsServer)
        {
            PlayerManager.instance.players.Add(gameObject.GetInstanceID(), new PlayerManager.Player() { health = 100, playerObject = gameObject, connection = GetComponent<NetworkObject>().Owner });
        }

        if (base.IsOwner)
        {
            playerCamera = Camera.main;
            playerCamera.transform.position = new Vector3(transform.position.x, transform.position.y + cameraYOffset, transform.position.z - 2.0f);
            playerCamera.transform.SetParent(transform);
        }
        else
        {
            gameObject.GetComponent<FPSController>().enabled = false;
        }
    }

    private void Start()
    {
        characterController = GetComponent<CharacterController>();

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        bool isRunning = false;

        // Press Left Shift to run
        isRunning = Input.GetKey(KeyCode.LeftShift);

        // We are grounded, so recalculate move direction based on axis
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        float curSpeedX = CanMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Vertical") : 0; // Calculate the movement speed on the X-axis
        float curSpeedY = CanMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Horizontal") : 0; // Calculate the movement speed on the Y-axis
        float movementDirectionY = moveDirection.y; // Store the current movement direction on the Y-axis
        moveDirection = (forward * curSpeedX) + (right * curSpeedY); // Calculate the new movement direction

        if (Input.GetButton("Jump") && CanMove && characterController.isGrounded)
        {
            moveDirection.y = jumpSpeed; // Apply jump speed if the Jump button is pressed and the character is grounded
        }
        else
        {
            moveDirection.y = movementDirectionY; // Maintain the previous movement direction on the Y-axis
        }

        if (!characterController.isGrounded)
        {
            float currentGravity = isGravityNormal ? gravity : customGravity; // Determine the current gravity based on the flag
            moveDirection.y -= currentGravity * Time.deltaTime; // Apply gravity to the character
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            isGravityNormal = !isGravityNormal; // Toggle between normal and custom gravity when G key is pressed
        }

        // Move the controller
        Physics.SyncTransforms();
        characterController.Move(moveDirection * Time.deltaTime); // Move the character based on the moveDirection vector

        // Player and Camera rotation
        if (CanMove && playerCamera != null)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed; // Calculate the rotation on the X-axis based on mouse input
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit); // Clamp the rotation value within the specified limit
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0); // Rotate the player camera on the X-axis
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0); // Rotate the character based on mouse input
        }
    }
}