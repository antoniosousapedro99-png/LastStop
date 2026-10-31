using System.Collections;
using UnityEngine;

// A barred double gate bolted on one side only: the shortcut out of Valve 4's room. From the far side it
// won't budge (you can see the bolt through the bars); from the bolt's side, slide the bolt and both leaves
// swing open, for good.
public class BoltedGate : MonoBehaviour, IInteractable
{
    public Transform leafA, leafB;        // each pivot sits on its hinge
    public float swingA = 90f, swingB = -90f;   // degrees round the hinge, into the bolt's side
    public Transform bolt;                // the bolt bar (slides along 'boltSlide' to open)
    public Vector3 boltSlide = new Vector3(0f, 0f, 0.25f);
    public Vector3 boltSide = Vector3.right;    // world direction from the gate toward the side with the bolt
    public AudioClip boltSound, swingSound, rattleSound;

    public bool IsOpen { get; private set; }
    bool moving;

    // Which side the player is on (the camera's position, as the prompt has no player to ask).
    bool OnBoltSide()
    {
        var cam = Camera.main;
        return cam && Vector3.Dot(cam.transform.position - transform.position, boltSide) > 0f;
    }

    public string Prompt => IsOpen || moving ? null : OnBoltSide() ? "Slide the bolt" : "Gate - bolted from the other side";

    public void Interact(Interactor player)
    {
        if (IsOpen || moving) return;
        if (!OnBoltSide())
        {
            if (rattleSound) AudioSource.PlayClipAtPoint(rattleSound, transform.position, 0.9f);
            Hud.Notice("It's bolted on the far side. I can see the bolt through the bars, but I can't reach it.", 4f);
            return;
        }
        StartCoroutine(Open());
    }

    IEnumerator Open()
    {
        moving = true;
        if (boltSound) AudioSource.PlayClipAtPoint(boltSound, bolt.position, 1f);
        Vector3 from = bolt.position;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.3f) { bolt.position = from + boltSlide * t; yield return null; }
        bolt.position = from + boltSlide;
        yield return new WaitForSeconds(0.3f);
        if (swingSound) AudioSource.PlayClipAtPoint(swingSound, transform.position, 0.9f);
        Quaternion a = leafA.localRotation, b = leafB.localRotation;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 1.6f)
        {
            float s = Mathf.SmoothStep(0f, 1f, t);
            leafA.localRotation = a * Quaternion.Euler(0f, swingA * s, 0f);
            leafB.localRotation = b * Quaternion.Euler(0f, swingB * s, 0f);
            yield return null;
        }
        OpenNow();
    }

    // Also used when loading a save.
    public void OpenNow()
    {
        StopAllCoroutines();
        if (!IsOpen && !moving) bolt.position += boltSlide;
        if (!IsOpen)
        {
            leafA.localRotation = Quaternion.Euler(0f, swingA, 0f);   // leaves are built shut at zero turn
            leafB.localRotation = Quaternion.Euler(0f, swingB, 0f);
        }
        IsOpen = true;
        moving = false;
    }
}
