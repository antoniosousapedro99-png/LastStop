using System.Collections;
using UnityEngine;

// A drawer that slides out when used (the staff room desk drawer with the reverser key in it).
// This object is the drawer itself (front, tray and anything lying in it); it slides by 'slide'
// (in its parent's space).
public class SlidingDrawer : MonoBehaviour, IInteractable
{
    public Vector3 slide = new Vector3(0f, 0f, -0.35f);   // how far and which way it slides out
    public float openTime = 0.4f;
    public AudioClip openSound;

    bool open, moving;
    Vector3 closedPosition;

    void Awake() => closedPosition = transform.localPosition;

    public bool IsOpen => open || moving;

    public string Prompt => open || moving ? null : "Open drawer";

    public void Interact(Interactor player)
    {
        if (!open && !moving) StartCoroutine(Slide());
    }

    IEnumerator Slide()
    {
        moving = true;
        if (openSound) AudioSource.PlayClipAtPoint(openSound, transform.position, 0.8f);
        Vector3 target = closedPosition + slide;
        for (float t = 0f; t < openTime; t += Time.deltaTime)
        {
            transform.localPosition = Vector3.Lerp(closedPosition, target, 1f - Mathf.Pow(1f - t / openTime, 3f));   // eases out
            yield return null;
        }
        transform.localPosition = target;
        moving = false;
        open = true;
    }

    // Loading a save: already pulled out.
    public void OpenNow()
    {
        StopAllCoroutines();
        transform.localPosition = closedPosition + slide;
        moving = false;
        open = true;
    }
}
