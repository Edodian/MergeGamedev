using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Events;

public class FpJump : FpControllerModule
{
    [SerializeField]bool hasDoubleJumped = false;
    [SerializeField]float CoyoteTimer = 0f;
    private void Start(){
        Controller.AttemptJump += OnAttemptJump;

        Controller.Landed.AddListener(OnLanded);

    }

    private void Update(){

        if(Controller.Grounded){
            CoyoteTimer = 0f;
        }else{
             CoyoteTimer+=Time.deltaTime;
        }

    }

    void OnLanded(){
        hasDoubleJumped = false;
    }
    
    void OnAttemptJump()
    {
        if (CoyoteTimer<=Preset.CoyoteTime)
         {
            Jump();
            Controller.Jumped?.Invoke();
            return;
         }
        if(!Preset.CanDoubleJump){return;}
        if(!hasDoubleJumped){
            Jump();
            Controller.DoubleJumped?.Invoke();
            hasDoubleJumped=true;
        }
        // if (Controller.Grounded)
        // {
        //     Controller.VerticalVelocity = Mathf.Sqrt(Preset.JumpHeight * -2f * Physics.gravity.y * Preset.GravityScale);
        //     timesJumped = 1;
        //     return;
        // }

        // if (Preset.CanDoubleJump && timesJumped < 2 && Controller.VerticalVelocity <= 0.01f)
        // {
        //     Controller.VerticalVelocity = Mathf.Sqrt(Preset.JumpHeight * -2f * Physics.gravity.y * Preset.GravityScale);
        //     timesJumped++;
        // }
    }
    void Jump(){
        Controller.VerticalVelocity = Mathf.Sqrt(Preset.JumpHeight * -2f * Physics.gravity.y * Preset.GravityScale);
      //  timesJumped++;
    }
}
