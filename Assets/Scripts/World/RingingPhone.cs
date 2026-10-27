using System.Collections;
using UnityEngine;

// Scare: an old phone in Canal St's ticket booth starts ringing as the player reaches the mezzanine.
// It stops dead the moment they get right up to it (or after a while). If they answer it while it's still
// ringing, there's only slow, wet breathing on the line, then a click.
public class RingingPhone : MonoBehaviour, IScare, IInteractable
{
    public AudioSource bell;               // looping ring, heard across the mezzanine
    public AudioSource earpiece;           // the line, close to the player's ear
    public AudioClip pickUp, breathing, hangUp;
    public float stopDistance = 1f;        // metres: walk this close and it stops by itself
    public float giveUpAfter = 30f;        // seconds of ringing before it stops anyway

    bool ringing, answered;
    float ringStarted;
    Transform player;

    public void Play(Transform who)
    {
        player = who;
        ringing = true;
        ringStarted = Time.time;
        bell.Play();
    }

    void Update()
    {
        if (!ringing || !player) return;
        Vector3 d = player.position - transform.position; d.y = 0f;
        if (d.magnitude < stopDistance || Time.time - ringStarted > giveUpAfter) StopRinging();
    }

    void StopRinging()
    {
        ringing = false;
        bell.Stop();                       // cut off mid-ring
    }

    public string Prompt => ringing ? "Answer the phone" : null;

    public void Interact(Interactor who)
    {
        if (!ringing || answered) return;
        answered = true;
        StopRinging();
        StartCoroutine(Line());
    }

    IEnumerator Line()
    {
        if (pickUp) earpiece.PlayOneShot(pickUp, 0.8f);
        yield return new WaitForSeconds(0.6f);
        if (breathing) { earpiece.PlayOneShot(breathing, 1f); yield return new WaitForSeconds(breathing.length); }
        if (hangUp) earpiece.PlayOneShot(hangUp, 0.9f);
        Hud.Notice("...the line goes dead.");
    }
}
