using UnityEngine;

// The staff room PC (it replaced the night-shift logbook): the save point. Using it opens the save
// screen (SaveMenu), where the player picks a slot and names the save.
public class Logbook : MonoBehaviour, IInteractable
{
    public AudioClip pageSound;
    public string terminalName = "STATION 1 MAINTENANCE TERMINAL";

    public string Prompt => "Use computer (save game)";

    public void Interact(Interactor player)
    {
        if (pageSound) AudioSource.PlayClipAtPoint(pageSound, transform.position, 1f);
        SaveMenu.Current.Open(SaveMenu.Mode.Save, pauseGame: true, terminal: terminalName);
    }
}
