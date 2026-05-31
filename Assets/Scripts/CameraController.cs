using UnityEngine;

public class CameraController : MonoBehaviour
{
    // Movement
    public float moveSpeed = 5f;
    public float sprintSpeed = 10f;
    
    // Mouse Look
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 90f;
    
    private float _currentXRotation;

    void Start()
    {
        // Lock cursor to center of screen
        Cursor.lockState = CursorLockMode.Locked;
    }

    // void Update()
    // {
    //     HandleMovement();
    //     HandleMouseLook();
    //     HandleCursorToggle();
    //     HandleZooming();
    // }

    void HandleZooming()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            Camera.main.fieldOfView -= scroll * 10f; // Adjust zoom speed as needed
            Camera.main.fieldOfView = Mathf.Clamp(Camera.main.fieldOfView, 20f, 100f); // Limit zoom range
        }
    }

    void HandleMovement()
    {
        // Get input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        float verticalMovement = 0f;
        
        // Space for up, Ctrl for down
        if (Input.GetKey(KeyCode.Space))
            verticalMovement = 1f;
        if (Input.GetKey(KeyCode.LeftControl))
            verticalMovement = -1f;
        
        // Determine speed
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;
        
        // Calculate position
        Vector3 move = new Vector3(horizontal, verticalMovement, vertical);
        transform.Translate(move * currentSpeed * Time.deltaTime, Space.Self);
    }

    void HandleMouseLook()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Rotate around X axis (pitch)
        _currentXRotation -= mouseY;
        _currentXRotation = Mathf.Clamp(_currentXRotation, -maxLookAngle, maxLookAngle);
        
        // Apply rotation
        transform.localRotation = Quaternion.Euler(_currentXRotation, transform.eulerAngles.y, 0f);
        transform.RotateAround(transform.position, Vector3.up, mouseX);
    }

    void HandleCursorToggle()
    {
        // Press Escape to unlock cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }
}
