using System.Collections;
using UnityEngine;

// The emergency latch in the floor of Canal St's lower level: the way out of Station 2 (on to the
// sewers, Station 3, once it exists). Sealed until the backup generator brings the power back; then the
// bolts clunk and the lid swings up. Climbing down takes Sousa into the sewers (Station 3): a fade to black,
// the climb, and he stands at the bottom of the ladder (the game autosaves). Without a sewer arrival set, it
// shows the end card instead.
// The lid is a child hinge ('lid') that turns up by 'openAngle' around its x axis.
public class EmergencyLatch : MonoBehaviour, IInteractable
{
    public PowerGrid grid;
    public Transform lid;
    public float openAngle = -110f;
    public float delay = 3f, seconds = 2.5f;   // after the power, then how long the lid takes to rise
    public AudioClip unlock, creak;
    public Transform sewerArrival;               // where he lands in the sewers (set by the builder)
    public AudioClip climb;                      // a step on each rung on the way down

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
        if (sewerArrival) { StartCoroutine(Descend(body)); return; }
        var monster = FrankieAI.Active;
        if (monster) monster.enabled = false;   // no last-second death behind the end card
        FindAnyObjectByType<EndCard>()?.Show();
    }

    // Fade out, the sound of the climb down, then Sousa at the bottom of the sewer ladder. Frankie stays behind.
    System.Collections.IEnumerator Descend(Transform body)
    {
        moving = true;
        var fade = ScreenFade.Current;
        if (fade) yield return fade.FadeTo(1f, 1.2f);
        var monster = FindAnyObjectByType<FrankieAI>(FindObjectsInactive.Include);
        if (monster) monster.gameObject.SetActive(false);
        for (int i = 0; i < 6; i++)
        {
            if (climb) AudioSource.PlayClipAtPoint(climb, body.position, 0.6f);
            yield return new WaitForSeconds(0.45f);
        }
        var controller = body.GetComponent<CharacterController>();
        if (controller) controller.enabled = false;
        body.SetPositionAndRotation(sewerArrival.position, Quaternion.Euler(0f, sewerArrival.eulerAngles.y, 0f));
        var look = body.GetComponent<PlayerLook>();
        if (look) look.pitch = 0f;
        if (controller) controller.enabled = true;
        if (fade) yield return fade.FadeTo(0f, 1.5f);
        foreach (var b in new Behaviour[] { body.GetComponent<PlayerMovement>(), body.GetComponent<FlareHandler>(), body.GetComponentInChildren<Interactor>() })
            if (b) b.enabled = true;
        moving = false;
        if (SaveSystem.Write(SaveSystem.Autosave, "Autosave - The Sewers")) Hud.Notice("Game autosaved", 2.5f);
    }
}
