using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Rendering;
using UnityEngine.VFX;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;

    public Transform playerCapsule;
    public float standingCapsuleHeight = 1f;
    public float crouchingCapsuleHeight = 0.5f;
    public float slidingCapsuleHeight = 0.4f;
    public float capsuleLerpSpeed = 10f;

    float targetCapsuleHeight;


    [Header("Camera")] 
    public Transform cameraPos;
    public float standingCamHeight = 1.6f;
    public float crouchingCamHeight = 1.0f;
    public float crouchLerpSpeed = 8f;
    float targetCamHeight;
    public float mouseSens = 200f;
    float xRot = 0f;

    [Header("PlayerMovement")]
    public float walkSpeed = 6f;

    public bool isSprinting;
    public float sprintSpeed = 10f;

    public bool isCrouching;
    public float crouchSpeed = 3f;

    public bool isSliding;
    float slideTimer;
    public float slideSpeed = 12f;
    public float slideDuration = 0.5f;

    private float currentSpeed = 0f;
    public float acceleration = 20f;



    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        targetCamHeight = standingCamHeight;
        targetCapsuleHeight = standingCapsuleHeight;
    }
    private void Update()
    {
        Look();
        Move();
        HandleCrouch();
        HandleSprint();
        HandleCameraHeight();
        HandleCapsuleHeight();
    }

    void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSens * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSens * Time.deltaTime;

        xRot -= mouseY;

        xRot = Mathf.Clamp(xRot, -90f, 90f);

        cameraPos.localRotation = Quaternion.Euler(xRot, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    private void Move()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 inputDir = transform.right * moveX + transform.forward * moveZ;
        inputDir.Normalize();

        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0)
            {
                isSliding = false;
            }
        }

        float targetSpeed;
        float targetCapsHeight;
        float targetCamH;

        if (isSliding)
        {
            targetSpeed = slideSpeed;
            targetCapsHeight = slidingCapsuleHeight;
            targetCamH = crouchingCamHeight;
        }
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            isCrouching = true;
            targetSpeed = crouchSpeed;
            targetCapsHeight = crouchingCapsuleHeight;
            targetCamH = crouchingCamHeight;

            controller.height = 1f;
            controller.center = new Vector3(0, 0.5f, 0);
        }
        else
        {
            isCrouching = false;
            targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
            targetCapsHeight = standingCapsuleHeight;
            targetCamH = standingCamHeight;

            controller.height = 2f;
            controller.center = new Vector3(0, 1f, 0);
        }

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

        controller.Move(inputDir * currentSpeed * Time.deltaTime);

        Vector3 scale = playerCapsule.localScale;
        scale.y = Mathf.Lerp(scale.y, targetCapsHeight, capsuleLerpSpeed * Time.deltaTime);
        playerCapsule.localScale = scale;

        Vector3 pos = playerCapsule.localPosition;
        pos.y = scale.y / 2f;
        playerCapsule.localPosition = pos;

        Vector3 camPos = cameraPos.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, targetCamH, crouchLerpSpeed * Time.deltaTime);
        cameraPos.localPosition = camPos;
    }

    void HandleSprint()

    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            isSprinting = true;
        }

        else
        {
            isSprinting = false;
        }
    }

    void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift))
        {
            StartSlide();
        }
    }



    void HandleCameraHeight()
    {
        Vector3 pos = cameraPos.localPosition;

        pos.y = Mathf.Lerp(pos.y, targetCamHeight, crouchLerpSpeed * Time.deltaTime);

        cameraPos.localPosition = pos;
    }

    void HandleCapsuleHeight()
    {
        Vector3 scale = playerCapsule.localScale;
        scale.y = Mathf.Lerp(scale.y, targetCapsuleHeight, capsuleLerpSpeed * Time.deltaTime);
        playerCapsule.localScale = scale;

        Vector3 pos = playerCapsule.localPosition;
        pos.y = scale.y / 2f;
        playerCapsule.localPosition = pos;
    }

    void StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;
        targetCapsuleHeight = slidingCapsuleHeight;
    }
}
