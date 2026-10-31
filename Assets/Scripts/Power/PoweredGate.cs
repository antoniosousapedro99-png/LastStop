using System.Collections;
using UnityEngine;

// A motorised steel flood gate across a doorway (the way into Valve 4's room). Dead until its PowerGrid
// comes back on; then, once the wave of lights has reached it, its motor whines up and the gate grinds up
// into the wall above the doorway, and stays there.
public class PoweredGate : MonoBehaviour, IInteractable
{
    public PowerGrid grid;
    public Transform gate;                // the steel plate (with its collider)
    public float lift = 4.2f;             // how far it rises
    public float seconds = 6f;            // how long it takes
    public float delay = 2.5f;            // after the power comes back
    public AudioSource motor;             // loops while it rises (on the gate)
    public AudioClip clank;               // when it locks open

    bool open;
    Vector3 shut;

    void Awake() => shut = gate.position;

    void Start()
    {
        if (grid) grid.Restored += OnPower;
    }

    void OnDestroy()
    {
        if (grid) grid.Restored -= OnPower;
    }

    void OnPower()
    {
        if (!open) StartCoroutine(Rise());
    }

    public string Prompt => open || grid.IsOn ? null : "Flood gate - no power";

    public void Interact(Interactor player)
    {
        if (!open && !grid.IsOn) Hud.Notice("A motorised flood gate, shut tight. Its motor's dead: there's no power down here.", 4f);
    }

    IEnumerator Rise()
    {
        open = true;
        yield return new WaitForSeconds(delay);
        if (motor) { motor.volume = 0.8f; motor.Play(); }
        for (float t = 0f; t < 1f; t += Time.deltaTime / seconds)
        {
            gate.position = shut + Vector3.up * (lift * Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        gate.position = shut + Vector3.up * lift;
        if (motor) motor.Stop();
        if (clank) AudioSource.PlayClipAtPoint(clank, gate.position, 1f);
    }

    // Loading a save made after the power came back: straight up, silently.
    public void OpenNow()
    {
        StopAllCoroutines();
        if (motor) motor.Stop();
        gate.position = shut + Vector3.up * lift;
        open = true;
    }
}
