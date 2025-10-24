using UnityEngine;
using Unity.Cinemachine; 
using UnityEngine.Events;

public class FpCrouch : FpActivity
{
    protected override void Awake(){
        base.Awake();
        Controller.Crouch = this;
    }
    public override bool CanStartActivity(){
        if(IsActive(Controller.Sprint))
        {
            return false;
        }
        return true;
    }
    public override bool CanStopActivity(){
        Ray ray = new Ray(Controller.CameraTransform.position,Vector3.up);

        float standingHeight = 2f;
        float raycastDistance = standingHeight - Controller.CameraTransform.localPosition.y;

        raycastDistance = Mathf.Max(0f, raycastDistance);
        if (Physics.Raycast(ray, raycastDistance, Preset.ObstacleLayerMask)){
            return false;
        }
        return true;
    }
}
