using UnityEngine;

[CreateAssetMenu(menuName="scripts/PlayerControllers/FpControllerPreset")]
public class FpControllerPreset : ScriptableObject
{
    [Header("Movement Params")]
    public float Acceleration = 15f;
    public float WalkSpeed = 3.5f;
    public float SprintSpeed = 10f;
    [Header("Footsteps")]
    public float FootstepWalkRate = 0.6f;
    public float FootstepSprintRate = 0.4f;
    [Header("Jump Params")]
    public float JumpHeight = 2f;
    public bool CanDoubleJump = true;
    public float CoyoteTime = 0.3f;

    [Header("Looking Params")]
    public Vector2 LookSensitivity = new Vector2(0.1f, 0.1f);
    public float PitchLimit = 85f;

    [Header("Cam Params")]
    public float NormalFOV = 60f;
    public float SprintFOV = 68f;
    public float FOVSmoothing = 5f;

    [Header("Phys Params")]
    public float GravityScale = 2f;
    public LayerMask ObstacleLayerMask = Physics.DefaultRaycastLayers;
}
