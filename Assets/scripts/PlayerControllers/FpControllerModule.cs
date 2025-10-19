using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Events;

public class FpControllerModule : MonoBehaviour
{
    protected FpController Controller;
    protected FpControllerPreset Preset => Controller.Preset;

    protected virtual void Awake(){
        Controller = GetComponentInParent<FpController>();
    }
}
