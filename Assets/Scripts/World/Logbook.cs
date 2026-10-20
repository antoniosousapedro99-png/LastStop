using UnityEngine;

// The night-shift logbook on the staff room desk. It will become the save point (safe-room saving);
// for now pressing E only tells the player that saving comes later.
public class Logbook : MonoBehaviour, IInteractable
{
    public AudioClip pageSound;

    public string Prompt => "Read logbook";

    public void Interact(Interactor player)
    {
        if (pageSound) AudioSource.PlayClipAtPoint(pageSound, transform.position, 1f);
        Hud.Notice("NIGHT SHIFT LOG - Station 1.   (Saving here comes later.)", 4f);
    }
}
