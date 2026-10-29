using System.Collections;
using UnityEngine;

// A pair of sliding carriage doors: Use slides the two leaves apart into the wall (open) or back together
// (shut). Open, there's a gap wide enough to step through; shut, the leaves block the doorway.
public class SlidingTrainDoor : MonoBehaviour, IInteractable
{
    public Transform leafA, leafB;            // leafA slides toward -along, leafB toward +along
    public Vector3 along = Vector3.right;     // the direction the doorway runs
    public float apart = 0.48f;               // how far each leaf slides when open
    public float slideTime = 0.7f;
    public bool startOpen;
    public AudioClip slideSound;

    bool open, moving;
    Vector3 shutA, shutB;

    void Awake()
    {
        // (A door built open has its leaves already slid apart: their shut places are worked back from there.)
        shutA = startOpen ? leafA.position + along * apart : leafA.position;
        shutB = startOpen ? leafB.position - along * apart : leafB.position;
        open = startOpen;
    }

    public bool IsOpen => open;
    public string Prompt => moving ? null : open ? "Close the doors" : "Open the doors";

    public void Interact(Interactor who)
    {
        if (!moving) StartCoroutine(Slide(!open));
    }

    IEnumerator Slide(bool toOpen)
    {
        moving = true;
        if (slideSound) AudioSource.PlayClipAtPoint(slideSound, transform.position, 0.8f);
        Vector3 a0 = leafA.position, b0 = leafB.position;
        Vector3 a1 = toOpen ? shutA - along * apart : shutA, b1 = toOpen ? shutB + along * apart : shutB;
        for (float t = 0f; t < slideTime; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / slideTime);
            leafA.position = Vector3.Lerp(a0, a1, k);
            leafB.position = Vector3.Lerp(b0, b1, k);
            yield return null;
        }
        leafA.position = a1; leafB.position = b1;
        open = toOpen;
        moving = false;
    }
}
