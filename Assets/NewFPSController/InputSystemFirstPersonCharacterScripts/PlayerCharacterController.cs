using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XInput;

[RequireComponent(typeof(CharacterController))]
public class PlayerCharacterController : MonoBehaviour
{
    [NonSerialized] public InputSystemFirstPersonControls inputActions;

    private CharacterController controller;

    [SerializeField] public Camera cam;
    [SerializeField] private float movementSpeed = 2.0f;
    [SerializeField] public float lookSensitivity = 1.0f;

    private PlayerInput m_PlayerInput;
    private InputAction m_MoveInput;
    private InputAction m_LookInput;
    private InputAction m_SprintInput;
    private InputAction m_JumpInput;
    private InputAction m_CrouchInput;

    private PlayerInventoryManager m_PlayerInventoryManager;

    public bool usingGamepad = false;
    private XInputController m_Gamepad;

    public float groundDrag = .8f;
    public float aerialDrag = .9f;

    private float xRotation = 0f;
    private Vector3 m_LatestImpactSpeed;

    // Movement Vars
    public Vector3 CharacterVelocity;
    public float gravity = -9.81f;
    private bool isGrounded;
    private bool isCrouching;
    private bool isSprinting;

    // Zoom Vars - Zoom code adapted from @torahhorse's First Person Drifter scripts.
    public float zoomFOV = 35.0f;
    public float zoomSpeed = 9f;
    private float targetFOV;

    public bool isInMenu = false;

    public bool isADS { get; private set; }

    private float baseFOV;
    private float sprintFov;
    public float additionalFOVFromSprinting = 9f;

    // Crouch Vars
    private float initHeight;
    [SerializeField] private float crouchHeight;
    private float m_LastTimeJumped;
    private bool jumpWasPressed = false;
    private bool HasPressedJumpThisFrame = false;

    public float MaxSpeedOnGround = 10f;
    public float MaxSpeedInAir = 10f;
    public float MaxSpeedCrouchedRatio = .5f;
    public float AccelerationSpeedInAir = 25f;
    private float m_FootstepDistanceCounter;

    public float MovementSharpnessOnGround = 15f;

    public float JumpForce = 9f;
    public float SprintSpeedModifier = 1.5f;

    private void Awake()
    {
        inputActions = new InputSystemFirstPersonControls();

        //Initialize the essentially input manager
        m_PlayerInput = GetComponent<PlayerInput>();

        m_MoveInput = m_PlayerInput.actions["Move"];
        m_LookInput = m_PlayerInput.actions["Look"];
        m_SprintInput = m_PlayerInput.actions["Sprint"];
        m_JumpInput = m_PlayerInput.actions["Jump"];
        m_CrouchInput = m_PlayerInput.actions["Crouch"];

        m_PlayerInventoryManager = GetComponent<PlayerInventoryManager>();

        //Check the input type
        if (m_PlayerInput.devices[0] is XInputController)
        {
            usingGamepad = true;
            m_Gamepad = m_PlayerInput.devices[0] as XInputController;

            GetComponent<WeaponController>().m_Controller = m_Gamepad;
        }
    }

    private void Start()
    {
        Debug.Log("Startup");
        controller = GetComponent<CharacterController>();
        initHeight = controller.height;
        SetBaseFOV(cam.fieldOfView);
        SetSprintFov(baseFOV);
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void Update()
    {
        DoMovement();

        //DEBUG>> FOR PC SO YOU CAN SEE THE MOUSE
        if (m_PlayerInventoryManager.inventoryPanelOpen || m_PlayerInventoryManager.sellPanelOpen || m_PlayerInventoryManager.buyPanelOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            isInMenu = true;
        }
        else //Let the player move then
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            DoLooking();
            DoZoom();
            DoCrouch();

            isInMenu = false;
        }
    }

    private void DoLooking()
    {
        Vector2 looking = GetPlayerLookInput();
        float lookX = looking.x * lookSensitivity * Time.deltaTime;
        float lookY = looking.y * lookSensitivity * Time.deltaTime;

        xRotation -= lookY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        
        cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        
        transform.Rotate(Vector3.up * lookX);
    }

    private void DoMovement()
    {
        HasPressedJumpThisFrame = false;

        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f, LayerMask.GetMask("Ground")) || controller.isGrounded;

        isSprinting = GetPlayerSprintInput();

        float speedModifier = isSprinting ? SprintSpeedModifier : 1f;

        // converts move input to a worldspace vector based on our character's transform orientation
        Vector3 worldspaceMoveInput = transform.TransformVector(GetPlayerMovement());

        // handle grounded movement
        if (isGrounded)
        {
            // calculate the desired velocity from inputs, max speed, and current slope
            Vector3 targetVelocity = worldspaceMoveInput * MaxSpeedOnGround * speedModifier;
            // reduce speed if crouching by crouch speed ratio
            if (isCrouching)
                targetVelocity *= MaxSpeedCrouchedRatio;
            //targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, m_GroundNormal) *
            //                 targetVelocity.magnitude;
            

            // smoothly interpolate between our current velocity and the target velocity based on acceleration speed
            CharacterVelocity = Vector3.Lerp(CharacterVelocity, targetVelocity,
                MovementSharpnessOnGround * Time.deltaTime);

            
            // jumping
            if (isGrounded && GetPlayerJumpInputDown())
            {
                // force the crouch state to false
                if (SetCrouchingState(false, false))
                {
                    // start by canceling out the vertical component of our velocity
                    CharacterVelocity = new Vector3(CharacterVelocity.x, 0f, CharacterVelocity.z);

                    // then, add the jumpSpeed value upwards
                    CharacterVelocity += Vector3.up * JumpForce;

                    // play sound
                    //AudioSource.PlayOneShot(JumpSfx);

                    // remember last time we jumped because we need to prevent snapping to ground for a short time
                    m_LastTimeJumped = Time.time;
                    HasPressedJumpThisFrame = true;

                    // Force grounding to false
                    isGrounded = false;
                    //m_GroundNormal = Vector3.up;
                }
            }

            /*
            // footsteps sound
            float chosenFootstepSfxFrequency =
                (isSprinting ? FootstepSfxFrequencyWhileSprinting : FootstepSfxFrequency);
            if (m_FootstepDistanceCounter >= 1f / chosenFootstepSfxFrequency)
            {
                m_FootstepDistanceCounter = 0f;
                AudioSource.PlayOneShot(FootstepSfx);
            }*/

            // keep track of distance traveled for footsteps sound
            m_FootstepDistanceCounter += CharacterVelocity.magnitude * Time.deltaTime;
        }
        // handle air movement
        else
        {
            // add air acceleration
            CharacterVelocity += worldspaceMoveInput * AccelerationSpeedInAir * Time.deltaTime;

            // limit air speed to a maximum, but only horizontally
            float verticalVelocity = CharacterVelocity.y;
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(CharacterVelocity, Vector3.up);
            horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, MaxSpeedInAir * speedModifier);
            CharacterVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);

            // apply the gravity to the velocity
            CharacterVelocity += Vector3.down * gravity * Time.deltaTime;
        }

        // apply the final calculated velocity value as a character movement
        Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
        Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere(controller.height);
        controller.Move(CharacterVelocity * Time.deltaTime);

        // detect obstructions to adjust velocity accordingly
        m_LatestImpactSpeed = Vector3.zero;
        if (Physics.CapsuleCast(capsuleBottomBeforeMove, capsuleTopBeforeMove, controller.radius,
            CharacterVelocity.normalized, out RaycastHit hit, CharacterVelocity.magnitude * Time.deltaTime, -1,
            QueryTriggerInteraction.Ignore))
        {
            // We remember the last impact speed because the fall damage logic might need it
            m_LatestImpactSpeed = CharacterVelocity;

            CharacterVelocity = Vector3.ProjectOnPlane(CharacterVelocity, hit.normal);
        }
    }

    private void DoZoom()
    {
        if (m_PlayerInput.actions["Zoom"].ReadValue<float>() > 0)
        {
            targetFOV = zoomFOV;
            isADS = true;
        }
        else
        {
            targetFOV = baseFOV;
            isADS = false;
        }

        if (isSprinting) targetFOV = sprintFov;

        UpdateZoom();
    }

    private void DoCrouch()
    {
        if (m_CrouchInput.ReadValue<float>() > 0)
        {
            controller.height = crouchHeight;
        }
        else
        {
            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.up), 2.0f, -1))
            {
                controller.height = crouchHeight;
            }
            else
            {
                controller.height = initHeight;
            }
        }
    }

    private void UpdateZoom()
    {
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, zoomSpeed * Time.deltaTime);
    }

    public void SetBaseFOV(float fov)
    {
        baseFOV = fov;
    }

    public void SetSprintFov(float fov)
    {
        sprintFov = baseFOV + additionalFOVFromSprinting;
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    public Vector3 GetPlayerMovement()
    {
        Vector2 rawInput = m_MoveInput.ReadValue<Vector2>();

        //I think the laggy movement sometimes comes from input system sucking ass here
        //Debug.Log(rawInput.ToString()); 

        return new Vector3(rawInput.x, 0f, rawInput.y);
    }

    public Vector2 GetPlayerLookInput()
    {
        return m_LookInput.ReadValue<Vector2>();
    }

    public bool GetPlayerSprintInput()
    {
        return m_SprintInput.ReadValue<float>() == 1;
    }

    public bool GetPlayerJumpInputDown()
    {
        bool jumpRaw = m_JumpInput.ReadValue<float>() == 1;
        bool output = false;

        if (jumpRaw && !jumpWasPressed)
        {
            output = true;
            //Debug.Log("JUMP NIGGA");
        }
        else
        {
            output = false;
        }

        jumpWasPressed = jumpRaw;

        return output;
    }

    // Gets the center point of the bottom hemisphere of the character controller capsule    
    Vector3 GetCapsuleBottomHemisphere()
    {
        return transform.position + (transform.up * controller.radius);
    }

    // Gets the center point of the top hemisphere of the character controller capsule    
    Vector3 GetCapsuleTopHemisphere(float atHeight)
    {
        return transform.position + (transform.up * (atHeight - controller.radius));
    }

    bool SetCrouchingState(bool crouched, bool ignoreObstructions)
    {
        // set appropriate heights
        if (crouched)
        {
            //m_TargetCharacterHeight = CapsuleHeightCrouching;
        }
        else
        {
            /*
            // Detect obstructions
            if (!ignoreObstructions)
            {
                Collider[] standingOverlaps = Physics.OverlapCapsule(
                    GetCapsuleBottomHemisphere(),
                    GetCapsuleTopHemisphere(CapsuleHeightStanding),
                    controller.radius,
                    -1,
                    QueryTriggerInteraction.Ignore);
                foreach (Collider c in standingOverlaps)
                {
                    if (c != controller)
                    {
                        return false;
                    }
                }
            }/*

            //m_TargetCharacterHeight = CapsuleHeightStanding;
        }

       /* if (OnStanceChanged != null)
        {
            OnStanceChanged.Invoke(crouched);
        }*/

            
        }
        isCrouching = crouched;
        return true;
    }
}
