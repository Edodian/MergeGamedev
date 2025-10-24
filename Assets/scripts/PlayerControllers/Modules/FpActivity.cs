using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class FpActivity : Activity
{
    protected FpController Controller;
    protected FpControllerPreset Preset => Controller.Preset;
    protected virtual void Awake()
    {
        Controller = GetComponentInParent<FpController>();
    }
}
