// Anything the player can look at and press E on (fuses, the fuse box, doors, the logbook...).
// The Interactor on the camera finds these with a short ray from the centre of the screen.
public interface IInteractable
{
    // Text shown at the bottom of the screen while looking at it, e.g. "Pick up fuse".
    // Return null to show nothing (and ignore E).
    string Prompt { get; }

    // Called when the player presses E while looking at it.
    void Interact(Interactor player);
}
