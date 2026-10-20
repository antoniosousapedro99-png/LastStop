using UnityEngine;

// The staff room PC (it replaced the night-shift logbook). It will become the save point (safe-room saving);
// for now pressing E only tells the player that saving comes later.
public class Logbook : MonoBehaviour, IInteractable
{
    public AudioClip pageSound;

    public string Prompt => "Use computer";

    public void Interact(Interactor player)
    {
        if (pageSound) AudioSource.PlayClipAtPoint(pageSound, transform.position, 1f);
        Hud.Notice("STATION 1 MAINTENANCE TERMINAL   (Saving here comes later.)", 4f);
    }
}
