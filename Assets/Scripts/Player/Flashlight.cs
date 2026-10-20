using UnityEngine;
using UnityEngine.InputSystem;

// Turns the hand-held flashlight on and off with F, with a click.
// Like a real torch it has two parts: a narrow bright hotspot that throws far ("beam"),
// and a wide, dim ring around it ("spill") that shows the walls and floor nearby.
// The light lags slightly behind the view, like a torch held in a hand.
// Battery drain comes later, with the gameplay.
public class Flashlight : MonoBehaviour
{
    public Light beam;
    public Light spill;
    public AudioClip clickOn, clickOff;
    public float followSpeed = 14f; // higher = the beam sticks to the view more tightly

    AudioSource audioSource;
    Quaternion smoothedRotation;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        smoothedRotation = transform.parent.rotation;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
        {
            beam.enabled = !beam.enabled;
            if (spill) spill.enabled = beam.enabled;
            if (audioSource) audioSource.PlayOneShot(beam.enabled ? clickOn : clickOff);
        }
    }

    void LateUpdate()
    {
        // Ease toward where the camera looks, so fast turns leave the beam a moment behind.
        smoothedRotation = Quaternion.Slerp(smoothedRotation, transform.parent.rotation,
                                            1f - Mathf.Exp(-followSpeed * Time.deltaTime));
        transform.rotation = smoothedRotation;
    }
}
