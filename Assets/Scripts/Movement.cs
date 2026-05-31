using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour {
    [Header("Movement")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float verticalSpeed = 3f;

    [Header("Look")]
    [SerializeField] float mouseSensitivity = 1.5f;
    [SerializeField] float maxPitch         = 89f;

    [Header("Bob")]
    [SerializeField] Transform cameraHolder;
    [SerializeField] float bobFrequency  = 1.4f;
    [SerializeField] float bobAmplitudeY = 0.05f;
    [SerializeField] float bobAmplitudeX = 0.025f;
    [SerializeField] float bobSmoothing  = 8f;

    float pitch;
    float bobTimer;
    Vector3 currentBob;
    Vector3 targetBob;

    void Awake() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
        
        cameraHolder = Camera.main.transform;
    }

    void Update() {
        Look();
        Move();
        Bob();
    }

    void Look() {
        float mouseX =  Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = -Input.GetAxis("Mouse Y") * mouseSensitivity;

        pitch = Mathf.Clamp(pitch + mouseY, -maxPitch, maxPitch);

        transform.Rotate(Vector3.up * mouseX);
        cameraHolder.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void Move() {
        Vector3 input = new Vector3(
            Input.GetAxis("Horizontal"),
            0f,
            Input.GetAxis("Vertical")
        );

        // vertical flight
        if (Input.GetKey(KeyCode.E)) input.y =  1f;
        if (Input.GetKey(KeyCode.Q)) input.y = -1f;

        if (input.magnitude > 1f) input.Normalize();

        // move relative to camera facing, including pitch for vertical flight feel
        Vector3 forward = cameraHolder.forward;
        Vector3 right   = cameraHolder.right;
        Vector3 move    = (forward * input.z + right * input.x) * moveSpeed
                        + Vector3.up * input.y * verticalSpeed;

        transform.position += move * Time.deltaTime;
    }

    void Bob() {
        Vector3 velocity = new Vector3(
            transform.position.x, 0f, transform.position.z
        );

        // approximate horizontal speed from position delta
        float speed = new Vector3(
            Input.GetAxis("Horizontal"),
            0f,
            Input.GetAxis("Vertical")
        ).magnitude * moveSpeed;

        bool isMoving = speed > 0.1f;

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
        cameraHolder.localPosition = currentBob;
    }
}