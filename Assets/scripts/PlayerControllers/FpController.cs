using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterController))]
public class FpController : MonoBehaviour
{
    public FpControllerPreset Preset;

    float MaxSpeed
    {
        get{
            if(Activity.IsActive(Sprint)){
                return Preset.SprintSpeed;
            }
            return Preset.WalkSpeed;
        }
    }
 
    public bool Sprinting {
        get{
            return Activity.IsActive(Sprint);
        }
    }

    [SerializeField] float currentPitch = 0f;
    public float CurrentPitch
    {
        get => currentPitch;
        set => currentPitch = Mathf.Clamp(value, -Preset.PitchLimit, Preset.PitchLimit);
    }

    public Vector3 CurrentVelocity { get; private set; }
    public float VerticalVelocity = 0f;
    private float airTime = 0f;
    private float lastFallSpeed = 0f;

    public float CurrentSpeed { get; private set; }
    public bool Grounded => CharacterController.isGrounded;
    private bool WasGrounded = false;

    [Header("Inputs")]
    public Vector2 MoveInput;
    public Vector2 LookInput;
    public bool SprintInput;

    [Header("Components")]
    [SerializeField] CinemachineCamera FpCamera;
    [SerializeField] CharacterController CharacterController;

    //public CharacterController CharacterController => CharacterController;
    public CinemachineCamera Camera => FpCamera;
    public Transform CameraTransform => FpCamera.transform;

    [Header("Activites")]
    public FpSprint Sprint;
    public FpCrouch Crouch;

    [Header("Events")]
    public UnityEvent Landed = new UnityEvent();
    public UnityEvent Jumped = new UnityEvent();
    public UnityEvent DoubleJumped = new UnityEvent();
    public UnityAction AttemptJump;


    [Header("Camera Parameters")]
    public Vector3 CurrentCameraPosition{get;private set;} = new Vector3(0f,1.6f,0f);

    void OnValidate()
    {
        if (CharacterController == null)
            CharacterController = GetComponent<CharacterController>();

        if (FpCamera == null)
            FpCamera = GetComponentInChildren<CinemachineCamera>();
    }

    void Update()
    {
        MoveUpdate();
        LookUpdate();
        CameraUpdate();

        Vector3 targetCameraPosition = Vector3.up * 1.6f;
        if(Activity.IsActive(Crouch)){
            targetCameraPosition = Vector3.up * 0.9f;
            CharacterController.height = 1f;
            CharacterController.center = Vector3.up * 0.5f;
        }else{
            CharacterController.height = 2f;
            CharacterController.center = Vector3.up * 1f;
        }
        CurrentCameraPosition = Vector3.Lerp(CurrentCameraPosition,targetCameraPosition, 7f * Time.deltaTime);

    if (!Grounded)
    {
    
    airTime += Time.deltaTime;
    lastFallSpeed = VerticalVelocity; // Negative while falling
    }
    else
    {
        if (!WasGrounded)
        {
        bool wasFalling = lastFallSpeed < -5f;
        bool fellLongEnough = airTime > 0.40f;

            if (wasFalling && fellLongEnough)
            {
                Landed?.Invoke();
                Debug.Log($"Landed (fall speed {lastFallSpeed:F2}, air time {airTime:F2})");
            }

        // Reset fall tracking
        airTime = 0f;
        lastFallSpeed = 0f;
        }
    }

    WasGrounded = Grounded;
    }

    void MoveUpdate()
    {
        Vector3 inputMotion = transform.forward * MoveInput.y + transform.right * MoveInput.x;
        inputMotion.y = 0f;
        inputMotion.Normalize();

        if (inputMotion.sqrMagnitude >= 0.01f)
            CurrentVelocity = Vector3.MoveTowards(CurrentVelocity, inputMotion * MaxSpeed, Preset.Acceleration * Time.deltaTime);
        else
            CurrentVelocity = Vector3.MoveTowards(CurrentVelocity, Vector3.zero, Preset.Acceleration * Time.deltaTime);

        if (Grounded && VerticalVelocity <= 0.01f)
            VerticalVelocity = -3f;
        else
            VerticalVelocity += Physics.gravity.y * Preset.GravityScale * Time.deltaTime;

        Vector3 fullVelocity = new Vector3(CurrentVelocity.x, VerticalVelocity, CurrentVelocity.z);
        CollisionFlags flags = CharacterController.Move(fullVelocity * Time.deltaTime);

        if ((flags & CollisionFlags.Above) != 0 && VerticalVelocity > 0.01f)
            VerticalVelocity = 0f;

        CurrentSpeed = CurrentVelocity.magnitude;
    }

    void LookUpdate()
    {
        Vector2 camInput = new Vector2(LookInput.x * Preset.LookSensitivity.x, LookInput.y * Preset.LookSensitivity.y);

        currentPitch -= camInput.y;
        CurrentPitch = Mathf.Clamp(CurrentPitch, -Preset.PitchLimit, Preset.PitchLimit);

        FpCamera.transform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        transform.Rotate(Vector3.up * camInput.x);
    }

    void CameraUpdate()
    {
        float targetFov = Preset.NormalFOV;
        if (Sprinting)
            targetFov = Mathf.Lerp(Preset.NormalFOV, Preset.SprintFOV, CurrentSpeed / Preset.SprintSpeed);

        FpCamera.Lens.FieldOfView = Mathf.Lerp(FpCamera.Lens.FieldOfView, targetFov, Preset.FOVSmoothing * Time.deltaTime);
        
        FpCamera.transform.localPosition = CurrentCameraPosition;
    }

}
