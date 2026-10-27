using System.Collections;
using UnityEngine;

// The emergency latch in the floor of Canal St's lower level: the way out of Station 2 (on to the
// sewers, Station 3, once it exists). Sealed until the backup generator brings the power back; then the
// bolts clunk and the lid swings up. Climbing down shows the end card.
// The lid is a child hinge ('lid') that turns up by 'openAngle' around its x axis.
public class EmergencyLatch : MonoBehaviour, IInteractable
{
    public PowerGrid grid;
    public Transform lid;
    public float openAngle = -110f;
    public float delay = 3f, seconds = 2.5f;   // after the power, then how long the lid takes to rise
    public AudioClip unlock, creak;

    bool opened, moving;
    Quaternion shut;

    void Awake() => shut = lid.localRotation;

    void Update()
    {
        if (opened || !grid || !grid.IsOn) return;
        opened = true;
        // Straight open if the power was already on when the level started (a loaded save).
        if (Time.timeSinceLevelLoad < 3f) lid.localRotation = shut * Quaternion.Euler(openAngle, 0f, 0f);
        else StartCoroutine(Open());
    }

    IEnumerator Open()
    {
        moving = true;
        yield return new WaitForSeconds(delay);
        if (unlock) AudioSource.PlayClipAtPoint(unlock, transform.position, 1f);
        yield return new WaitForSeconds(0.6f);
        if (creak) AudioSource.PlayClipAtPoint(creak, transform.position, 1f);
        Quaternion up = shut * Quaternion.Euler(openAngle, 0f, 0f);
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            lid.localRotation = Quaternion.Slerp(shut, up, Mathf.SmoothStep(0f, 1f, t / seconds));
            yield return null;
        }
        lid.localRotation = up;
        moving = false;
    }

    public string Prompt => moving ? null : opened ? "Climb down the emergency latch" : "Emergency latch";

    public void Interact(Interactor player)
    {
        if (!opened) { Hud.Notice("Sealed tight. The panel says: OPENS ON BACKUP POWER."); return; }
        if (moving) return;
        var body = player.GetComponentInParent<PlayerInventory>().transform;
        foreach (var b in new Behaviour[] { body.GetComponent<PlayerMovement>(), body.GetComponent<FlareHandler>(), player })
            if (b) b.enabled = false;
        var monster = FrankieAI.Active;
        if (monster) monster.enabled = false;   // no last-second death behind the end card
        FindAnyObjectByType<EndCard>()?.Show();
    }
}
