using UnityEngine;
using System.Collections;

public class FpSounds : FpControllerModule
{
    [Header("Footstep Settings")]
    [SerializeField] private string WalkSFXTitle1 = "Metal1";
    [SerializeField] private string WalkSFXTitle2 = "Metal2";

    private bool isStepPlaying = false;

    private void OnEnable()
    {
        if (Controller == null)
            Controller = GetComponentInParent<FpController>();

        Controller.Landed.AddListener(() => LandingSnd("Landing"));
        Controller.Jumped.AddListener(() => Play("Jump"));
        Controller.DoubleJumped.AddListener(() => Play("DoubleJump"));
    }

    private void Update()
    {
        HandleFootsteps();
    }

    private void HandleFootsteps()
    {
        bool grounded = Controller.Grounded;
        bool moving = Controller.CurrentSpeed > 0.2f;

        if (grounded && moving && !isStepPlaying && SoundManager.sndm != null)
        {
            StartCoroutine(PlayStep());
        }
    }

    private IEnumerator PlayStep()
    {
        isStepPlaying = true;
        // alternate between two footstep sounds
        string footstepSound = (Random.value < 0.5f) ? WalkSFXTitle1 : WalkSFXTitle2;
        float volumeScale = Mathf.Lerp(0.5f, 2f, Controller.CurrentSpeed / Preset.SprintSpeed);
        SoundManager.sndm.PlayWithVolume(footstepSound, volumeScale);

        // choose interval based on running or walking
        bool running = Controller.Sprinting;
        float wait = running ? Preset.FootstepSprintRate : Preset.FootstepWalkRate;
        yield return new WaitForSeconds(wait);

        isStepPlaying = false;
    }

    private void Play(string soundName)
    {
        if (SoundManager.sndm == null)
        {
            Debug.LogError("No SoundManager in FpSounds!");
            return;
        }

        SoundManager.sndm.Play(soundName);
    }

    private void LandingSnd(string soundName)
    {
        if (SoundManager.sndm == null)
        {
            Debug.LogError("No SoundManager in FpSounds!");
            return;
        }

        if (Controller.VerticalVelocity >= -5f)
            return;

        float scale = Mathf.InverseLerp(-5f, -20f, Controller.VerticalVelocity);
        SoundManager.sndm.PlayWithVolume(soundName, scale);
    }
}
