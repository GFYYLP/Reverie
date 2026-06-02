using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Movement : MonoBehaviour {
    [Header("Movement")]
    [SerializeField] private float moveSpeed    = 5f;
    [SerializeField] private float jumpForce    = 5f;
    [SerializeField] private float gravity      = 9.81f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 1.5f;
    [SerializeField] private float maxPitch         = 89f;

    [Header("Bob")]
    [SerializeField] private float bobFrequency  = 1.4f;
    [SerializeField] private float bobAmplitudeY = 0.05f;
    [SerializeField] private float bobAmplitudeX = 0.025f;
    [SerializeField] private float bobSmoothing  = 8f;

    [Header("Sway")]
    [SerializeField] private float swayAmount    = 0.01f;
    [SerializeField] private float swaySpeed     = 0.8f;
    [SerializeField] private float swaySmoothing = 3f;

    Vector3 currentSway;
    
    private CharacterController cc;
    private Transform cam;
    private float pitch;
    private float yaw;
    private float verticalVelocity;
    private float bobTimer;
    private Vector3 currentBob;

    void Awake() {
        cc  = GetComponent<CharacterController>();
        cam = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    void Update() {
        Look();
        Move();
        Bob();
        Sway();
    }

    void Look() {
        float mouseX =  Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = -Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch += mouseY;
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

        //horizontal/yaw rotation (around y-axis) on the player body
        transform.rotation = Quaternion.Euler(0, yaw, 0);   //transform.Rotate(Vector3.up * mouseX);
        cam.localRotation = Quaternion.Euler(pitch, 0, 0);   //localRotation to pitch-rotate relative to the player's yaw
    }

    void Move() {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = (transform.right * h + transform.forward * v).normalized * moveSpeed;

        //floaty jump
        if (cc.isGrounded) {
            verticalVelocity = Input.GetKey(KeyCode.Space) ? jumpForce : -2f;
        } else {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        move.y = verticalVelocity;
        cc.Move(move * Time.deltaTime);
    }

    void Bob() {
        float speed = new Vector3(
            Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical")
        ).magnitude * moveSpeed;

        bool isMoving = speed > 0.1f && cc.isGrounded;

        Vector3 targetBob;
        if (isMoving) {
            bobTimer += Time.deltaTime * bobFrequency;
            targetBob = new Vector3(
                Mathf.Sin(bobTimer * 0.5f) * bobAmplitudeX,
                Mathf.Sin(bobTimer)        * bobAmplitudeY,
                0f
            );
        } else {
            targetBob = Vector3.zero;
        }

        currentBob = Vector3.Lerp(currentBob, targetBob, Time.deltaTime * bobSmoothing);
        cam.localPosition = currentBob + currentSway;
    }
    
    void Sway() {
        float timeX = Time.time * swaySpeed;
        float timeY = Time.time * swaySpeed + 100f;  // offset so X and Y aren't in sync

        Vector3 targetSway = new Vector3(
            (Mathf.PerlinNoise(timeX,        0f) - 0.5f) * swayAmount,
            (Mathf.PerlinNoise(timeY,        0f) - 0.5f) * swayAmount,
            (Mathf.PerlinNoise(timeX + timeY, 0f) - 0.5f) * swayAmount * 0.5f  // subtle roll
        );

        currentSway = Vector3.Lerp(currentSway, targetSway, Time.deltaTime * swaySmoothing);
    }
}
