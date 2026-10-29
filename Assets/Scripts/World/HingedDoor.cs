using System.Collections;
using UnityEngine;

// A door (or locker door) that swings around its hinge. The door model is a child of this object,
// which sits exactly on the hinge line.
// - Powered doors (the staff room) stay locked until the PowerGrid comes back on, then buzz, click
//   and swing open by themselves.
// - Other doors (the locker, the platform cabinet) open with a left click; some need the cabinet key.
// - Frankie opens room doors on his routes too (see FrankieAI.OpenDoor), but never one still locked.
public class HingedDoor : MonoBehaviour, IInteractable
{
    public float openAngle = 95f;          // degrees around the hinge (sign picks the direction)
    public float openTime = 1.2f;
    public PowerGrid needsPower;           // leave empty for a door that just opens when used
    public AudioClip unlockSound, openSound;
    public AudioClip bangSound;            // Frankie hammering on it or slamming it open
    public string doorName = "door";
    public bool needsCabinetKey;           // stays locked until the player has the cabinet key
    public bool needsOfficeKey;            // ...or the Canal St station master's key

    bool open, moving, unlocked;   // 'unlocked': opened once, so its key or power no longer matters
    Quaternion closedRotation;

    void Awake() => closedRotation = transform.localRotation;

    public bool IsOpen => open || moving;
    public bool Closing => open && moving;    // swinging shut ('open' only turns false once it's shut)

    // Still held shut by a key or the power (Frankie can't get through those either).
    public bool Locked => !unlocked && (needsPower || needsCabinetKey || needsOfficeKey);

    // When the player last shut it (Time.time): shut in Frankie's face mid-chase, he has to batter it first.
    public float ClosedAt { get; private set; } = -99f;

    // Frankie opens it: over 'seconds' (a slow creak, or a fast slam), its sound at 'volume'.
    public void OpenByFrankie(float seconds, float volume)
    {
        if (open || moving) return;
        StartCoroutine(Swing(0f, seconds, volume));
    }

    // One blow of his claws on the shut door.
    public void Bang()
    {
        if (bangSound) AudioSource.PlayClipAtPoint(bangSound, transform.position, 1f);
    }

    // Loading a save: snaps straight to open, no sound.
    public void OpenNow()
    {
        StopAllCoroutines();
        transform.localRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
        moving = false;
        open = true;
        unlocked = true;
    }

    void Start()
    {
        if (needsPower) needsPower.Restored += OnPowerRestored;
    }

    public string Prompt
    {
        get
        {
            if (moving) return null;
            if (open) return "Close " + doorName;
            if (unlocked) return "Open " + doorName;
            if (needsPower && !needsPower.IsOn) return "Locked - no power";
            if ((needsCabinetKey || needsOfficeKey) && !HasKey()) return "Locked - needs a key";
            if (needsCabinetKey || needsOfficeKey) return "Unlock " + doorName;
            return "Open " + doorName;
        }
    }

    public void Interact(Interactor player)
    {
        if (moving) return;
        if (open) { StartCoroutine(SwingShut()); return; }
        if (!unlocked && needsPower && !needsPower.IsOn) { Hud.Notice("The electric lock is dead."); return; }
        if (!unlocked && (needsCabinetKey || needsOfficeKey) && !HasKey()) { Hud.Notice("It's locked. There must be a key somewhere."); return; }
        StartCoroutine(Swing(0f));
    }

    bool HasKey()
    {
        var inv = FindAnyObjectByType<PlayerInventory>();
        return inv && (needsOfficeKey ? inv.hasOfficeKey : inv.hasCabinetKey);
    }

    void OnPowerRestored()
    {
        // Wait for the light wave to reach this door, then the lock buzzes and the door swings open.
        float delay = 1f + Vector3.Distance(transform.position, needsPower.Origin) * 0.03f;
        StartCoroutine(Swing(delay));
    }

    // 'time' and 'volume' left out: the door's own opening time, full volume, and the unlock click first.
    IEnumerator Swing(float delay, float time = -1f, float volume = 1f)
    {
        moving = true;
        yield return new WaitForSeconds(delay);
        bool byFrankie = time > 0f;
        if (!byFrankie) time = openTime;
        if (unlockSound && !byFrankie) AudioSource.PlayClipAtPoint(unlockSound, transform.position, 1f);
        yield return new WaitForSeconds(unlockSound && !byFrankie ? 0.4f : 0f);
        if (openSound) AudioSource.PlayClipAtPoint(openSound, transform.position, volume);
        Quaternion target = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
        for (float t = 0; t < time; t += Time.deltaTime)
        {
            // Ease out: fast at first, slowing as it reaches the end.
            float k = 1f - Mathf.Pow(1f - t / time, 3f);
            transform.localRotation = Quaternion.Slerp(closedRotation, target, k);
            yield return null;
        }
        transform.localRotation = target;
        moving = false;
        open = true;
        unlocked = true;
    }

    // Swings shut again (no unlock sound; it stays unlocked).
    IEnumerator SwingShut()
    {
        moving = true;
        ClosedAt = Time.time;
        if (openSound) AudioSource.PlayClipAtPoint(openSound, transform.position, 0.8f);
        Quaternion from = transform.localRotation;
        for (float t = 0; t < openTime; t += Time.deltaTime)
        {
            float k = 1f - Mathf.Pow(1f - t / openTime, 3f);
            transform.localRotation = Quaternion.Slerp(from, closedRotation, k);
            yield return null;
        }
        transform.localRotation = closedRotation;
        moving = false;
        open = false;
    }
}
