using UnityEngine;

// Anything with text the player can read by pressing E: missing-person posters, a newspaper,
// a note taped to a shop door, graffiti. The text shows on screen for a few seconds.
public class ReadableNote : MonoBehaviour, IInteractable
{
    public string title = "Note";
    [TextArea(3, 10)] public string text;
    public AudioClip sound;

    public string Prompt => "Read " + title;

    public void Interact(Interactor player)
    {
        if (sound) AudioSource.PlayClipAtPoint(sound, transform.position, 0.8f);
        Hud.Notice(text, 7f);
    }
}
