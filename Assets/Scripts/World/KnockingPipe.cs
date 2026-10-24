using System.Collections;
using UnityEngine;

// A big pipe in the machinery hall. Knock on it and, the first times, something knocks back from far
// down the pipework: three knocks the first time, one slow knock the second, then never again.
public class KnockingPipe : MonoBehaviour, IInteractable
{
    public AudioSource here;               // on the pipe
    public AudioSource far;                // somewhere deep in the pipes
    public AudioClip knock;

    int knocks;
    bool busy;

    public string Prompt => busy ? null : "Knock on the pipe";

    public void Interact(Interactor player)
    {
        if (!busy) StartCoroutine(Knock());
    }

    IEnumerator Knock()
    {
        busy = true;
        for (int i = 0; i < 3; i++) { here.pitch = Random.Range(0.95f, 1.05f); here.PlayOneShot(knock, 0.9f); yield return new WaitForSeconds(0.32f); }
        knocks++;
        int replies = knocks == 1 ? 3 : knocks == 2 ? 1 : 0;
        if (replies > 0)
        {
            yield return new WaitForSeconds(knocks == 1 ? 2.2f : 4f);
            for (int i = 0; i < replies; i++) { far.pitch = Random.Range(0.8f, 0.9f); far.PlayOneShot(knock, 1f); yield return new WaitForSeconds(knocks == 1 ? 0.38f : 0.8f); }
        }
        yield return new WaitForSeconds(0.5f);
        busy = false;
    }
}
