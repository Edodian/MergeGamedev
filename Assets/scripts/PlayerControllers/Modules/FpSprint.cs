using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Events;

public class FpSprint : FpActivity
{
    protected override void Awake(){
        base.Awake();

        Controller.Sprint = this;
    }
    public override bool CanStartActivity(){
        bool crouch = Activity.IsActive(Controller.Crouch);
        return !crouch;
        //return true;
    }
    private void Update()
    {
        if(Controller.SprintInput && Controller.CurrentSpeed > 0.1f)
        {
            TryStartActivity();
        }
        if(Controller.SprintInput == false||Controller.CurrentSpeed <= 0.1f)
        {
            TryStopActivity();
        }
    }
}
