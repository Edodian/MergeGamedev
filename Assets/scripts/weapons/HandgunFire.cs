// HandgunFire.cs — one shot per click (Unity 6 + Input System)
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class HandgunFire : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference attackAction; // assign Player/Attack

    [Header("Animation & FX")]
    [SerializeField] private Animator anim;              // assign (or on this object)
    [SerializeField] private string fireAnim = "HandgunFire";
    //[SerializeField] private AudioSource gunFire;        // optional

    [Header("Timing")]
    [SerializeField, Min(0.01f)] private float fireInterval = 0.5f;

    bool canFire = true;      // cooldown
    bool armed = true;       // must release to re-arm (prevents auto while held)

    void Reset()
    {
        if (!anim) anim = GetComponentInChildren<Animator>();
        //if (!gunFire) gunFire = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        if (attackAction == null || attackAction.action == null)
        {
            Debug.LogError("HandgunFire: assign Player/Attack to 'attackAction'.");
            return;
        }

        var a = attackAction.action;
        a.Enable();

        // fire on button DOWN
        a.started += OnAttackStarted;
        // re-arm only on button UP
        a.canceled += OnAttackCanceled;
    }

    void OnDisable()
    {
        var a = attackAction != null ? attackAction.action : null;
        if (a == null) return;
        a.started -= OnAttackStarted;
        a.canceled -= OnAttackCanceled;
        a.Disable();
    }

    void OnAttackStarted(InputAction.CallbackContext _)
    {
        if (!armed || !canFire) return;   // block if still held or on cooldown
        armed = false;                    // require release before next shot
        StartCoroutine(FireRoutine());
    }

    void OnAttackCanceled(InputAction.CallbackContext _)
    {
        armed = true;                     // after release you can shoot again
    }

    IEnumerator FireRoutine()
    {
        canFire = false;

        //if (gunFire) gunFire.Play();
        if (anim) anim.Play(fireAnim, 0, 0f);

        yield return new WaitForSeconds(fireInterval);
        canFire = true;
    }
}
