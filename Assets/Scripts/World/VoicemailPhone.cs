using System.Collections;
using UnityEngine;

// A phone in the world (a payphone, a wall phone) that rings faintly when Sousa passes within a few metres.
// Answer it and a message from his family plays: their voice (once recorded) and subtitles, Portuguese
// first with the English beneath. Ignore it and it stops after a while, ringing again next time he comes
// close. Listening to it counts as "reaching out" (Heard, saved), which will decide the ending.
public class VoicemailPhone : MonoBehaviour, IInteractable
{
    public string id = "voicemail";            // unique; what the save remembers
    public string speaker = "Mãe";             // shown before each line (mixed case: the HUD font has no capital Ã)
    [TextArea(2, 4)] public string[] portuguese;
    [TextArea(2, 4)] public string[] english;  // the same lines, translated (same order)
    public AudioClip voice;                    // the recorded message, when there is one (otherwise text only)
    public float secondsPerLine = 5.5f;
    public AudioSource bell;                   // the ring (looping), at the phone
    public AudioSource earpiece;               // the line: hiss, click, voice
    public AudioClip pickUp, hangUp, lineHiss;
    public float ringDistance = 8f, ringFor = 20f, rearmDistance = 10f;
    // For now (until the director writes the family's messages) every phone plays something scary instead:
    // a random one of these each time it's answered, and only rarely 'rareScare'. Answering then counts for
    // nothing (no 'reaching out', and letting it ring out doesn't fade the photo), and it rings again later.
    public AudioClip[] scares;
    public AudioClip rareScare;
    public float rareChance = 0.05f;
    bool ScareMode => scares != null && scares.Length > 0;

    public bool Heard { get; private set; }
    public bool Ignored { get; private set; }  // it rang out without being answered (fades his family photo)
    bool ringing, armed = true, playing;
    float ringStarted;
    Transform player;

    public string Prompt => playing ? null : ringing ? "Answer the phone" : Heard ? "Pick up the phone" : null;

    void Update()
    {
        if (!player)
        {
            var move = FindAnyObjectByType<PlayerMovement>();
            if (!move) return;
            player = move.transform;
        }
        float distance = Vector3.Distance(player.position, transform.position);
        if (ringing)
        {
            if (Time.time - ringStarted > ringFor) { StopRinging(); if (!ScareMode) Ignored = true; }
        }
        else if (!Heard && !playing && armed && distance < ringDistance)
        {
            ringing = true; armed = false; ringStarted = Time.time;
            bell.Play();
        }
        if (!armed && !ringing && distance > rearmDistance) armed = true;   // rings again next time he comes close
    }

    void StopRinging()
    {
        ringing = false;
        bell.Stop();
    }

    public void Interact(Interactor who)
    {
        if (playing || (!ringing && !Heard)) return;
        StopRinging();
        StartCoroutine(Message());
    }

    IEnumerator Message()
    {
        playing = true;
        if (pickUp) earpiece.PlayOneShot(pickUp, 0.8f);
        yield return new WaitForSeconds(0.7f);
        if (lineHiss) { earpiece.clip = lineHiss; earpiece.loop = true; earpiece.volume = 0.12f; earpiece.Play(); }
        if (ScareMode)
        {
            var clip = rareScare && Random.value < rareChance ? rareScare : scares[Random.Range(0, scares.Length)];
            earpiece.PlayOneShot(clip, 1f);
            yield return new WaitForSeconds(clip.length);
            earpiece.Stop(); earpiece.loop = false; earpiece.volume = 1f;
            if (hangUp) earpiece.PlayOneShot(hangUp, 0.8f);
            playing = false;
            yield break;
        }
        if (voice) AudioSource.PlayClipAtPoint(voice, transform.position, 1f);
        for (int i = 0; i < portuguese.Length; i++)
        {
            string translated = i < english.Length ? "\n(" + english[i] + ")" : "";
            Hud.Notice(speaker + ": " + portuguese[i] + translated, secondsPerLine);
            yield return new WaitForSeconds(secondsPerLine + 0.3f);
        }
        earpiece.Stop(); earpiece.loop = false; earpiece.volume = 1f;
        if (hangUp) earpiece.PlayOneShot(hangUp, 0.8f);
        Heard = true;
        playing = false;
    }

    // Loading a save: already listened to, so it stays quiet (it can still be picked up again).
    public void SetHeard() => Heard = true;
    public void SetIgnored() => Ignored = true;
}
