using UnityEngine;
using UnityEngine.InputSystem;

// Turns the hand-held flashlight on and off with F, with a click.
// Like a real torch it has two parts: a narrow bright hotspot that throws far ("beam"),
// and a wide, dim ring around it ("spill") that shows the walls and floor nearby.
// Its brightness comes from the battery (FlashlightBattery), which drains while it's on.
// The light lags slightly behind the view, like a torch held in a hand.
public class Flashlight : MonoBehaviour
{
    public Light beam;
    public Light spill;
    public Light viewmodelFill;    // faint light for the held gun/arms (the torch itself skips them)
    public FlashlightBattery battery;
    public TorchInterference interference;
    public AudioClip clickOn, clickOff;
    public float followSpeed = 14f; // higher = the beam sticks to the view more tightly
    public float fullStrengthDistance = 7f;  // surfaces further than this get the full beam
    public float minCloseUp = 0.05f;         // strength right up against a wall (fraction of full)

    float closeUp = 1f;

    public bool IsOn { get; private set; } = true;

    AudioSource audioSource;
    Quaternion smoothedRotation;
    float beamBase, spillBase;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        smoothedRotation = transform.parent.rotation;
        beamBase = beam.intensity;
        spillBase = spill ? spill.intensity : 0f;
    }

    void Update()
    {
        if (Controls.Pressed(GameAction.Torch))
        {
            IsOn = !IsOn;
            if (audioSource) audioSource.PlayOneShot(IsOn ? clickOn : clickOff);
            if (IsOn && battery && battery.Empty) Hud.Notice("The battery is dead   [" + Controls.Label(GameAction.SwapBattery) + "] swap battery");
        }
        if (IsOn && battery) battery.Drain(Time.deltaTime);
    }

    // Loading a save: on or off, silently.
    public void SetOn(bool on) => IsOn = on;

    void LateUpdate()
    {
        float level = IsOn ? (battery ? battery.Brightness : 1f) : 0f;
        if (interference) level *= interference.Multiplier;   // stutters when the Pale Man is close

        // Light gets stronger the closer a surface is (distance squared), so a torch strong enough to
        // reach 60 m would blind you on a wall 2 m away. Like your eyes adjusting, soften the light when
        // the surface straight ahead is close; far away it shines at full strength.
        float near = 1f;
        if (PlayerRay.Cast(transform, transform.position, transform.forward, fullStrengthDistance, out var hit, includeTriggers: false))
            near = Mathf.Max(minCloseUp, hit.distance * hit.distance / (fullStrengthDistance * fullStrengthDistance));
        closeUp = Mathf.Lerp(closeUp, near, 1f - Mathf.Exp(-10f * Time.deltaTime));   // ease, no popping

        beam.enabled = level > 0f;
        beam.intensity = beamBase * level * closeUp;
        if (spill) { spill.enabled = level > 0f; spill.intensity = spillBase * level * Mathf.Sqrt(closeUp); }
        if (viewmodelFill) viewmodelFill.enabled = level > 0f;

        // Ease toward where the camera looks, so fast turns leave the beam a moment behind.
        smoothedRotation = Quaternion.Slerp(smoothedRotation, transform.parent.rotation,
                                            1f - Mathf.Exp(-followSpeed * Time.deltaTime));
        transform.rotation = smoothedRotation;
    }
}
