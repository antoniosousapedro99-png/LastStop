using UnityEngine;

// Canal St's street shutter: jammed shut for good, so the player looks for another way out
// (the emergency latch on the lower level).
public class JammedShutter : MonoBehaviour, IInteractable
{
    public AudioClip rattle;

    public string Prompt => "Lift the shutter";

    public void Interact(Interactor player)
    {
        if (rattle) AudioSource.PlayClipAtPoint(rattle, transform.position, 0.8f);
        Hud.Notice("It's jammed solid. There has to be another way out.");
    }
}
