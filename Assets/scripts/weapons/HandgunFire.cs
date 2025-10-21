// HandgunFire.cs — Unity 6 + Input System
// One shot per click + rotate pistol to match player's look (camera/head).
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class HandgunFire : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference attackAction;   // assign Player/Attack

    [Header("Refs")]
    [SerializeField] private Transform handgun;                   // the in-hand pistol transform
    [SerializeField] private Transform aimSource;                 // player head OR the real Camera transform
    [SerializeField] private Animator anim;                       // optional
    [SerializeField] private string fireAnim = "HandgunFire";
    [SerializeField] private AudioSource gunFire;                 // optional

    [Header("Fire Timing")]
    [SerializeField, Min(0.01f)] private float fireInterval = 0.5f; // seconds between shots
    private bool canFire = true;
    private bool armed = true;  // must release before next shot

    [Header("Aim Alignment")]
    [SerializeField] private bool alignEachFrame = true;
    [SerializeField, Min(0f)] private float alignSpeed = 20f;      // higher = snappier
    [SerializeField] private Vector3 rotationOffsetEuler = Vector3.zero; // fine-tune hand pose
    [SerializeField] private bool ignoreCameraRoll = true;          // keep up = world Up



    void Reset()
    {
        if (!handgun)
        {
            var a = GetComponentInChildren<Animator>();
            handgun = a ? a.transform : transform;
        }
        if (!anim) anim = GetComponentInChildren<Animator>();
        if (!gunFire) gunFire = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        var a = attackAction != null ? attackAction.action : null;
        if (a == null)
        {
            Debug.LogError("HandgunFire: assign Player/Attack to 'attackAction' in the Inspector.");
        }
        else
        {
            a.Enable();
            a.started += OnAttackStarted;   // fires on press
            a.canceled += OnAttackCanceled;  // re-arm on release
        }

        EnsureAimSource();
    }

    void OnDisable()
    {
        var a = attackAction != null ? attackAction.action : null;
        if (a != null)
        {
            a.started -= OnAttackStarted;
            a.canceled -= OnAttackCanceled;
            a.Disable();
        }
    }

    void Update()
    {
        // Camera can appear late (scene load), keep trying until found
        if (!aimSource) EnsureAimSource();
    }

    void LateUpdate()
    {
        if (alignEachFrame) AlignToAim(Time.deltaTime);
    }


    bool busy;
    public void Anim_BeginFire() { busy = true; }
    public void Anim_EndFire() { busy = false; }

    void OnAttackStarted(InputAction.CallbackContext _)
    {
        if (!armed || !canFire) return;
        armed = false;                       // require release before next shot
        StartCoroutine(FireRoutine());
    }

    void OnAttackCanceled(InputAction.CallbackContext _)
    {
        armed = true;
    }

    IEnumerator FireRoutine()
    {
        canFire = false;

        if (gunFire) gunFire.Play();
        if (anim) anim.Play(fireAnim, 0, 0f);

        // snap align at shot time too (feels responsive)
        AlignToAim(1f);

        yield return new WaitForSeconds(fireInterval);
        canFire = true;
    }

    // ---------- Aim alignment ----------
    void AlignToAim(float dt)
    {
        if (!handgun || !aimSource) return;

        Quaternion targetRot = ignoreCameraRoll
            ? Quaternion.LookRotation(aimSource.forward, Vector3.up)   // zero roll
            : aimSource.rotation;

        targetRot *= Quaternion.Euler(rotationOffsetEuler);

        // world-space align so it works even if handgun isn't parented to the camera
        handgun.rotation = Quaternion.Slerp(handgun.rotation, targetRot, dt * alignSpeed);
    }

    void EnsureAimSource()
    {
        if (aimSource) return;
        var cam = Camera.main ?? Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Exclude);
        if (cam) aimSource = cam.transform; // use the REAL render camera (with Camera + CinemachineBrain)
    }

    // Optional setters if another system wants to supply these at runtime
    public void SetHandgun(Transform t) => handgun = t;
    public void SetAimSource(Transform t) => aimSource = t;
}
