using System.Collections;
using UnityEngine;

// A tall locker the player can hide in. Use it to step inside: the door shuts behind you and you peek
// out through its slats, able to turn your head a little. Use it again to step out.
// While you're inside, Frankie can't see you, unless he watched you get in: then he walks over,
// yanks the door open and drags you out (FrankieAI does the walking and calls PullOut).
// This object sits at the centre of the locker's floor, facing out through the door.
public class HidingLocker : MonoBehaviour, IInteractable
{
    public Transform door;                 // the door's hinge; the door model is its child
    public float openAngle = -105f;        // degrees the door swings open
    public float lookRange = 55f;          // degrees you can turn left or right while inside
    public AudioClip openSound, closeSound, slamSound;

    // The locker the player is hiding in right now (null when they aren't hiding).
    public static HidingLocker Occupied { get; private set; }
    // Whether Frankie saw the player while they were getting in.
    public static bool SeenGettingIn { get; private set; }

    // Where Frankie stands to pull someone out: just in front of the door.
    public Vector3 StandPoint => transform.position + transform.forward * 0.9f;
    Vector3 OutsidePoint => transform.position + transform.forward * 0.6f;

    bool busy;
    Quaternion closedRotation;
    Transform player;
    PlayerLook look;

    void Awake()
    {
        closedRotation = door.localRotation;
        Occupied = null;            // statics survive between Play runs here, so start every run fresh
        SeenGettingIn = false;
    }

    public string Prompt
    {
        get
        {
            if (busy) return null;
            if (Occupied == this) return "Step out";
            return Occupied == null ? "Hide in locker" : null;
        }
    }

    public void Interact(Interactor who)
    {
        if (busy) return;
        if (Occupied == this) StartCoroutine(GetOut());
        else if (Occupied == null) StartCoroutine(GetIn(who.GetComponentInParent<PlayerMovement>().transform));
    }

    IEnumerator GetIn(Transform who)
    {
        busy = true;
        player = who;
        look = player.GetComponent<PlayerLook>();
        SeenGettingIn = false;
        Watch();
        SetControls(false);
        yield return Swing(openAngle, 0.35f, openSound);

        // Step in, turning round to face out of the door.
        Vector3 from = player.position;
        float fromYaw = player.eulerAngles.y, toYaw = transform.eulerAngles.y;
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / 0.5f);
            player.position = Vector3.Lerp(from, transform.position, k);
            player.rotation = Quaternion.Euler(0f, Mathf.LerpAngle(fromYaw, toYaw, k), 0f);
            Watch();
            yield return null;
        }
        player.SetPositionAndRotation(transform.position, Quaternion.Euler(0f, toYaw, 0f));
        look.pitch = 0f;

        yield return Swing(0f, 0.3f, closeSound, watch: true);
        Occupied = this;
        look.limitYaw = true;
        look.yawCentre = toYaw;
        look.yawRange = lookRange;
        busy = false;
    }

    IEnumerator GetOut()
    {
        busy = true;
        Occupied = null;
        look.limitYaw = false;
        yield return Swing(openAngle, 0.35f, openSound);
        yield return Step(OutsidePoint, 0.4f);
        SetControls(true);
        yield return Swing(0f, 0.3f, closeSound);
        busy = false;
    }

    // Frankie found you: the door flies open and you're dragged out in front of him.
    public void PullOut()
    {
        if (Occupied != this) return;
        StopAllCoroutines();
        StartCoroutine(Yank());
    }

    IEnumerator Yank()
    {
        busy = true;
        Occupied = null;
        look.limitYaw = false;
        yield return Swing(openAngle, 0.12f, slamSound);
        yield return Step(OutsidePoint, 0.25f);
        SetControls(true);
        yield return new WaitForSeconds(1.5f);
        yield return Swing(0f, 0.5f, closeSound);
        busy = false;
    }

    IEnumerator Step(Vector3 to, float seconds)
    {
        Vector3 from = player.position;
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            player.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds));
            yield return null;
        }
        player.position = to;
    }

    IEnumerator Swing(float angle, float seconds, AudioClip sound, bool watch = false)
    {
        if (sound) AudioSource.PlayClipAtPoint(sound, door.position, 0.9f);
        Quaternion from = door.localRotation, to = closedRotation * Quaternion.Euler(0f, angle, 0f);
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            door.localRotation = Quaternion.Slerp(from, to, t / seconds);
            if (watch) Watch();
            yield return null;
        }
        door.localRotation = to;
    }

    // Remembers if Frankie catches sight of the player while they're climbing in.
    static void Watch()
    {
        if (FrankieAI.Active && FrankieAI.Active.SeesPlayer) SeenGettingIn = true;
    }

    // Walking, crouching, leaning and footsteps are switched off while hiding (and while stepping in or out).
    void SetControls(bool on)
    {
        player.GetComponent<CharacterController>().enabled = on;
        foreach (var part in new MonoBehaviour[] { player.GetComponent<PlayerMovement>(), player.GetComponent<PlayerCrouch>(),
                                                   player.GetComponent<PlayerLean>(), player.GetComponent<Footsteps>() })
            if (part) part.enabled = on;
        if (on) return;
        look.roll = 0f;                    // undo any lean, so the head is upright in the middle of the locker
        var head = look.cameraPivot;
        head.localPosition = new Vector3(0f, head.localPosition.y, head.localPosition.z);
    }
}
