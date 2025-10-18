using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterController))]
public class FpController : MonoBehaviour
{
    [Header("Movement Params")]
    public float MaxSpeed => SprintInput ? SprintSpeed : WalkSpeed;
    public float Acceleration = 15f;
    [Tooltip("Basic movement parameters")]
    [SerializeField] float WalkSpeed = 3.5f;
    [SerializeField] float SprintSpeed = 8f;
    [SerializeField] float JumpHeight = 2f;
    private int timesJumped = 0;
    [SerializeField] bool CanDoubleJump = true;

    public bool Sprinting => SprintInput && CurrentSpeed > 0.1f;

    [Header("Phys Params")]
    [SerializeField] float GravityScale = 3f;
    public float VerticalVelocity = 0f;

    public Vector3 CurrentVelocity {get; private set; }
    public float CurrentSpeed {get; private set; }
    public bool IsGrounded => CharacterController.isGrounded;//это нужно будет менять на рейкаст, чтобы менять звуки в зависимости от поверхности
    private bool WasGrounded = false; 

    [Header("Looking Params")]

    public Vector2 LookSensitivity = new Vector2(0.1f,0.1f);
    public float PitchLimit = 85f;
    [SerializeField] float currentPitch = 0f;

    public float CurrentPitch {
        get => currentPitch;
        set => currentPitch = Mathf.Clamp(value, -PitchLimit, PitchLimit);
    }
    
    [Header("Camera Config")]
    [SerializeField] float NormalFOV = 60f;
    [SerializeField] float SprintFOV = 80f;
    [SerializeField] float FOVSmoothing = 1f;

    public float TargetCameraFOV{
        get{
            return Sprinting ? SprintFOV : NormalFOV;
        }
    }
    
    [Header("Inputs")]
    public Vector2 MoveInput;
    public Vector2 LookInput;
    public bool SprintInput; 

    [Header("Components")]
    [SerializeField]CinemachineCamera FpCamera;
    [SerializeField]CharacterController CharacterController;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    [Header("Events")]
    public UnityEvent Landed;


    void OnValidate()
    {
        if (CharacterController == null)
        {
            CharacterController = GetComponent<CharacterController>();
        }
        if (FpCamera == null)
        {
            FpCamera = GetComponentInChildren<CinemachineCamera>();
        }
    }
    

    // Update is called once per frame
    void Update() { 
        MoveUpdate();
        LookUpdate();
        CameraUpdate();
        

        if(!WasGrounded&&IsGrounded){
            timesJumped = 0;
            Landed?.Invoke();
        }
        WasGrounded = IsGrounded;
    }

    void MoveUpdate(){
        Vector3 inputMotion = transform.forward * MoveInput.y + transform.right * MoveInput.x;
        inputMotion.y=0f;
        inputMotion.Normalize();

        if (inputMotion.sqrMagnitude >= 0.01f){
            CurrentVelocity = Vector3.MoveTowards(CurrentVelocity, inputMotion*MaxSpeed, Acceleration * Time.deltaTime);
        }else{
            CurrentVelocity = Vector3.MoveTowards(CurrentVelocity, Vector3.zero, Acceleration * Time.deltaTime);
        }

        if(IsGrounded&&VerticalVelocity <= 0.01f){
            VerticalVelocity = -3f;
        }else{
            VerticalVelocity += Physics.gravity.y * GravityScale * Time.deltaTime;
        }


        Vector3 fullVelocity = new Vector3(CurrentVelocity.x, VerticalVelocity, CurrentVelocity.z);

        CollisionFlags flags = CharacterController.Move(fullVelocity*Time.deltaTime);

        if((flags & CollisionFlags.Above)!=0 && VerticalVelocity>0.01f){
            VerticalVelocity=0f;
        }

        CurrentSpeed = CurrentVelocity.magnitude;
    }
    void LookUpdate(){
        Vector2 camInput = new Vector2(LookInput.x * LookSensitivity.x, LookInput.y * LookSensitivity.y);

        currentPitch -= camInput.y;// Up and Dwn
        CurrentPitch = Mathf.Clamp(CurrentPitch, -PitchLimit, PitchLimit);

        FpCamera.transform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);//L and R
        transform.Rotate(Vector3.up * camInput.x);
    }
    void CameraUpdate(){
        float targetFov = NormalFOV;

        if(Sprinting){
            targetFov = Mathf.Lerp(NormalFOV, SprintFOV, CurrentSpeed / SprintSpeed);
        }

        FpCamera.Lens.FieldOfView = Mathf.Lerp(FpCamera.Lens.FieldOfView, targetFov, FOVSmoothing * Time.deltaTime);
    }
    public void AttemptJump(){

    if (IsGrounded)
    {
        VerticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Physics.gravity.y * GravityScale);
        timesJumped = 1;
        return;
    }

    if (CanDoubleJump && timesJumped < 2 && VerticalVelocity <= 0.01f)
    {
        VerticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Physics.gravity.y * GravityScale);
        timesJumped++;
    }
    }
}