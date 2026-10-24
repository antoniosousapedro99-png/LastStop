using System.Collections;
using UnityEngine;

// Plays a short sequence of radio lines as on-screen text, with a crackle before each one.
// Used for the van radio at the start (it plays once by itself) and the payphone's answering
// machine (it plays when the player presses E on it).
public class RadioMessages : MonoBehaviour, IInteractable
{
    public string label = "radio";
    [TextArea(2, 6)] public string[] lines;
    public float secondsPerLine = 5f;
    public bool playOnStart;
    public AudioClip crackle;

    bool playing, played;

    public string Prompt => playing ? null : (played ? "Replay " : "Listen to ") + label;

    void Start()
    {
        if (playOnStart && !SaveSystem.LoadedGame) StartCoroutine(Play(2f));   // not again after loading a save
    }

    public void Interact(Interactor player)
    {
        if (!playing) StartCoroutine(Play(0f));
    }

    IEnumerator Play(float delay)
    {
        playing = true;
        yield return new WaitForSeconds(delay);
        foreach (var line in lines)
        {
            if (crackle) AudioSource.PlayClipAtPoint(crackle, transform.position, 0.6f);
            Hud.Notice(line, secondsPerLine);
            yield return new WaitForSeconds(secondsPerLine + 0.4f);
        }
        playing = false;
        played = true;
    }
}
