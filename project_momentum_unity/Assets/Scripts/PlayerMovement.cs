using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Rendering;
using UnityEngine.VFX;
public class PlayerMovement : MonoBehaviour
{
    [Header("Components")]
    public CharacterController controller;
    public Transform playerCapsule;
    public Transform cameraPos;

    [Header("Heights")]
    public float standingHeight = 2f;
    public float crouchingHeight = 1f;
    public float slidingHeight = 0.6f;
    public float standingCamHeight = 0.8f;
    public float crouchingCamHeight = 0.2f;
    public float lerpSpeed = 10f;

    [Header("Movement")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 10f;
    public float crouchSpeed = 3f;
    public float acceleration = 20f;
    private float currentSpeed;
    private bool isSprinting;
    private bool isCrouching;

    [Header("Sliding")]
    public float slideSpeed = 15f;
    public float slideDuration = 0.75f;
    public float slideDeceleration = 10f;
    private float slideTimer;
    private float slideForwardSpeed;
    private bool isSliding;

    [Header("Jumping & Physics")]
    public float jumpHeight = 2f;
    public float gravity = -25f;
    private float verticalVelocity;
    private bool isGrounded;
    private bool wasGrounded; // Used for landing impact

    [Header("Camera Look & Tilt")]
    public float mouseSens = 2f;
    public float maxSlideSideTilt = 5f;
    public float maxSlideForwardTilt = 3f;
    public float tiltSpeed = 5f;
    private float xRot = 0f;
    private float currentSideTilt = 0f;
    private float currentForwardTilt = 0f;

    [Header("Headbob")]
    public float walkBobSpeed = 14f;
    public float walkBobAmount = 0.05f;
    public float crouchBobSpeed = 8f;
    public float crouchBobAmount = 0.025f;
    public float sprintBobSpeed = 18f;
    public float sprintBobAmount = 0.1f;
    private float defaultYPos;
    private float timer;

    [Header("Landing Impact")]
    public float landImpactAmount = 0.2f;
    public float landImpactSpeed = 15f;
    private float landOffset = 0f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        currentSpeed = walkSpeed;
        defaultYPos = standingCamHeight;
    }

    private void Update()
    {
        HandleLook();
        HandleMove();
        UpdateDimensions();
        HandleLandingImpact();
        HandleHeadbob();
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSens;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSens;

        xRot -= mouseY;
        xRot = Mathf.Clamp(xRot, -90f, 90f);

        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleMove()
    {
        isGrounded = controller.isGrounded;

        // Detect Landing
        if (isGrounded && !wasGrounded && verticalVelocity < -5f)
        {
            landOffset = landImpactAmount;
        }
        wasGrounded = isGrounded;

        if (isGrounded && verticalVelocity < 0) verticalVelocity = -2f;

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        isSprinting = Input.GetKey(KeyCode.LeftShift);
        isCrouching = Input.GetKey(KeyCode.C) && !isSliding;

        // Slide Trigger
        if (Input.GetKeyDown(KeyCode.C) && isSprinting && isGrounded && !isSliding && moveZ > 0.1f)
        {
            isSliding = true;
            slideTimer = slideDuration;
            slideForwardSpeed = slideSpeed;
        }

        Vector3 move;
        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            Vector3 slideDir = (transform.forward * Mathf.Max(0, moveZ) + transform.right * moveX * 0.5f).normalized;
            move = slideDir * slideForwardSpeed;
            slideForwardSpeed = Mathf.MoveTowards(slideForwardSpeed, crouchSpeed, slideDeceleration * Time.deltaTime);

            if (slideTimer <= 0 || !Input.GetKey(KeyCode.C))
            {
                isSliding = false;
                currentSpeed = slideForwardSpeed;
            }
        }
        else
        {
            float targetSpeed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
            move = (transform.forward * moveZ + transform.right * moveX).normalized * currentSpeed;
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            isSliding = false;
        }

        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);
        HandleTilt(moveX);
    }

    private void HandleTilt(float horizontalInput)
    {
        float targetSideTilt = isSliding ? -horizontalInput * maxSlideSideTilt : 0f;
        float targetForwardTilt = isSliding ? (slideForwardSpeed / slideSpeed) * maxSlideForwardTilt : 0f;

        currentSideTilt = Mathf.Lerp(currentSideTilt, targetSideTilt, tiltSpeed * Time.deltaTime);
        currentForwardTilt = Mathf.Lerp(currentForwardTilt, targetForwardTilt, tiltSpeed * Time.deltaTime);

        cameraPos.localRotation = Quaternion.Euler(xRot + currentForwardTilt, 0f, currentSideTilt);
    }

    private void UpdateDimensions()
    {
        bool lowered = isSliding || isCrouching;
        float targetH = lowered ? (isSliding ? slidingHeight : crouchingHeight) : standingHeight;
        float targetCamY = lowered ? crouchingCamHeight : standingCamHeight;

        controller.height = Mathf.Lerp(controller.height, targetH, lerpSpeed * Time.deltaTime);
        controller.center = new Vector3(0, controller.height / 2f, 0);

        defaultYPos = Mathf.Lerp(defaultYPos, targetCamY, lerpSpeed * Time.deltaTime);

        if (playerCapsule != null)
        {
            playerCapsule.localScale = new Vector3(playerCapsule.localScale.x, controller.height / 2f, playerCapsule.localScale.z);
            playerCapsule.localPosition = new Vector3(0, controller.height / 2f, 0);
        }
    }

    private void HandleLandingImpact()
    {
        // Smoothly return landOffset to 0
        landOffset = Mathf.Lerp(landOffset, 0f, landImpactSpeed * Time.deltaTime);
    }

    private void HandleHeadbob()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // We still apply the landOffset even if we aren't moving (e.g., jumping in place)
        float finalY = defaultYPos - landOffset;

        if (isGrounded && !isSliding && (Mathf.Abs(moveX) > 0.1f || Mathf.Abs(moveZ) > 0.1f))
        {
            float bobSpeed = isCrouching ? crouchBobSpeed : (isSprinting ? sprintBobSpeed : walkBobSpeed);
            float bobAmount = isCrouching ? crouchBobAmount : (isSprinting ? sprintBobAmount : walkBobAmount);

            timer += Time.deltaTime * bobSpeed;
            finalY += Mathf.Sin(timer) * bobAmount;
        }
        else
        {
            timer = 0;
        }

        cameraPos.localPosition = new Vector3(cameraPos.localPosition.x, finalY, cameraPos.localPosition.z);
    }
}