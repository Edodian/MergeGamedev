using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(FpController))]
public class PlayerScript : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] FpController FpController;
    
    // Update is called once per frame
    void OnMove(InputValue value){
        FpController.MoveInput = value.Get<Vector2>();
    }

    void OnLook(InputValue value){
        FpController.LookInput = value.Get<Vector2>();
    }

    void OnSprint(InputValue value){
        FpController.SprintInput = value.isPressed;
    }

    void OnJump(InputValue value){
        if(value.isPressed){
            FpController.AttemptJump();
        }
    }

    void OnValidate()
    {
        if (FpController == null)
        {
            FpController = GetComponent<FpController>();
        }
    }
    void Start(){
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
