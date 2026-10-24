using System.Collections;
using UnityEngine;

// A door (or locker door) that swings around its hinge. The door model is a child of this object,
// which sits exactly on the hinge line.
// - Powered doors (the staff room) stay locked until the PowerGrid comes back on, then buzz, click
//   and swing open by themselves.
// - Other doors (the locker, the platform cabinet) open with a left click; some need the cabinet key.
public class HingedDoor : MonoBehaviour, IInteractable
{
    public float openAngle = 95f;          // degrees around the hinge (sign picks the direction)
    public float openTime = 1.2f;
    public PowerGrid needsPower;           // leave empty for a door that just opens when used
    public AudioClip unlockSound, openSound;
    public string doorName = "door";
    public bool needsCabinetKey;           // stays locked until the player has the cabinet key

    bool open, moving;
    Quaternion closedRotation;

    void Awake() => closedRotation = transform.localRotation;

    public bool IsOpen => open || moving;

    // Loading a save: snaps straight to open, no sound.
    public void OpenNow()
    {
        StopAllCoroutines();
        transform.localRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
        moving = false;
        open = true;
    }

    void Start()
    {
        if (needsPower) needsPower.Restored += OnPowerRestored;
    }

    public string Prompt
    {
        get
        {
            if (open || moving) return null;
            if (needsPower && !needsPower.IsOn) return "Locked - no power";
            if (needsCabinetKey && !HasKey()) return "Locked - needs a key";
            if (needsCabinetKey) return "Unlock " + doorName;
            return "Open " + doorName;
        }
    }

    public void Interact(Interactor player)
    {
        if (needsPower && !needsPower.IsOn) { Hud.Notice("The electric lock is dead."); return; }
        if (needsCabinetKey && !HasKey()) { Hud.Notice("It's locked. There must be a key somewhere."); return; }
        if (!open && !moving) StartCoroutine(Swing(0f));
    }

    static bool HasKey()
    {
        var inv = FindAnyObjectByType<PlayerInventory>();
        return inv && inv.hasCabinetKey;
    }

    void OnPowerRestored()
    {
        // Wait for the light wave to reach this door, then the lock buzzes and the door swings open.
        float delay = 1f + Vector3.Distance(transform.position, needsPower.Origin) * 0.03f;
        StartCoroutine(Swing(delay));
    }

    IEnumerator Swing(float delay)
    {
        moving = true;
        yield return new WaitForSeconds(delay);
        if (unlockSound) AudioSource.PlayClipAtPoint(unlockSound, transform.position, 1f);
        yield return new WaitForSeconds(unlockSound ? 0.4f : 0f);
        if (openSound) AudioSource.PlayClipAtPoint(openSound, transform.position, 1f);
        Quaternion target = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
        for (float t = 0; t < openTime; t += Time.deltaTime)
        {
            // Ease out: fast at first, slowing as it reaches the end.
            float k = 1f - Mathf.Pow(1f - t / openTime, 3f);
            transform.localRotation = Quaternion.Slerp(closedRotation, target, k);
            yield return null;
        }
        transform.localRotation = target;
        moving = false;
        open = true;
    }
}
