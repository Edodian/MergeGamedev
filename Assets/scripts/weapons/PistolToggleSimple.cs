// PistolToggleSimple.cs
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PistolToggleSimple : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionProperty holsterAction;   // <-- Property, not Reference

    [Header("Animator")]
    [SerializeField] private Animator anim;
    [SerializeField] private string takeTrigger = "Take";
    [SerializeField] private string putTrigger = "Put";
    [SerializeField] private string takenStateName = "PistolTaken";
    [SerializeField] private string putterStateName = "PistolPutter";
    [SerializeField] private bool startsEquipped = false;

    bool equipped;

    void Awake()
    {
        if (!anim) anim = GetComponentInChildren<Animator>(true);
        equipped = startsEquipped;
    }

    void OnEnable()
    {
        var a = holsterAction.action;
        if (a == null) { Debug.LogError("Assign Player/Holster to 'holsterAction'."); return; }
        a.Enable();
        a.performed += OnToggle;
    }

    void OnDisable()
    {
        var a = holsterAction.action;
        if (a != null) { a.performed -= OnToggle; a.Disable(); }
    }

    void OnToggle(InputAction.CallbackContext _)
    {
        if (IsBusy()) return;
        if (!equipped) { anim.ResetTrigger(putTrigger); anim.SetTrigger(takeTrigger); equipped = true; }
        else { anim.ResetTrigger(takeTrigger); anim.SetTrigger(putTrigger); equipped = false; }
    }

    bool IsBusy()
    {
        if (!anim || !anim.isActiveAndEnabled) return false;
        if (anim.IsInTransition(0)) return true;
        var st = anim.GetCurrentAnimatorStateInfo(0);
        return st.IsName(takenStateName) || st.IsName(putterStateName);
    }
}
