using UnityEngine;

// The driver's controls in the east train's cab: the way out of Station 1.
// - No power, or no reverser key: it says what's missing (a note in the cab says where the spare key is).
// - With both: using it slots the key in and starts the train (TrainDeparture). From then on the player
//   has to hold the Use button down on the dead man's handle, looking at the controls, for 10 seconds
//   in total; letting go pauses the countdown.
public class TrainConsole : MonoBehaviour, IInteractable
{
    public PowerGrid grid;
    public TrainDeparture departure;
    public AudioClip lockedSound;

    PlayerInventory inventory;
    Interactor interactor;

    PlayerInventory Inventory => inventory ? inventory : inventory = FindAnyObjectByType<PlayerInventory>();

    public string Prompt
    {
        get
        {
            if (departure.Leaving) return null;
            if (departure.Started) return "Hold the dead man's handle  " + Mathf.FloorToInt(departure.Progress * 100f) + "%";
            if (!grid.IsOn) return "Train controls - no power";
            if (!Inventory || !Inventory.hasReverserKey) return "Train controls - the reverser key is missing";
            return "Insert the reverser key and start the train";
        }
    }

    public void Interact(Interactor player)
    {
        interactor = player;
        if (departure.Started) return;
        if (!grid.IsOn || !Inventory.hasReverserKey)
        {
            if (lockedSound) AudioSource.PlayClipAtPoint(lockedSound, transform.position, 0.8f);
            Hud.Notice(!grid.IsOn ? "Dead. The train has no power." : "The reverser key slot is empty.", 3f);
            return;
        }
        departure.Begin();
    }

    void Update()
    {
        if (!departure.Started || departure.Leaving) return;
        if (!interactor) interactor = FindAnyObjectByType<Interactor>();
        // Held down, while actually at the controls (looking at them), and not in a menu.
        bool holding = Controls.Held(GameAction.Use) && interactor && ReferenceEquals(interactor.Target, this)
                       && Cursor.lockState == CursorLockMode.Locked;
        departure.Tick(holding);
    }
}
