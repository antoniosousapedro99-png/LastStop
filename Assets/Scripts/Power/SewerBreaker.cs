using System.Collections;
using UnityEngine;

// The sewers' main breaker, in the electrical room off the control room. Its lever handle has been taken
// off (it lies in Valve 1's room): use the breaker with the handle to fit it, then use it again to throw it.
// The sewers' PowerGrid comes back: the sodium floodlights warm up in the cistern and the rooms, and the
// motorised flood gate to Valve 4 grinds open (PoweredGate).
public class SewerBreaker : MonoBehaviour, IInteractable
{
    public PowerGrid grid;
    public Transform lever;               // the fitted handle (pivot on its hinge, turning round its local X); hidden until fitted
    public float throwAngle = 110f;       // how far it swings down when thrown
    public AudioClip fitSound, throwSound;

    public bool HandleFitted { get; private set; }

    bool throwing;
    PlayerInventory inventory;
    PlayerInventory Inventory => inventory ? inventory : inventory = FindAnyObjectByType<PlayerInventory>();

    void Awake()
    {
        if (lever) lever.gameObject.SetActive(false);
    }

    public string Prompt
    {
        get
        {
            if (grid.IsOn || throwing) return null;
            if (HandleFitted) return "Throw the breaker";
            return Inventory && Inventory.hasBreakerHandle ? "Fit the breaker handle" : "Main breaker - the handle is missing";
        }
    }

    public void Interact(Interactor player)
    {
        if (grid.IsOn || throwing) return;
        if (!HandleFitted)
        {
            if (!player.inventory.hasBreakerHandle)
            {
                Hud.Notice("The breaker's handle has been pulled off. Somebody didn't want this switched on.", 4f);
                return;
            }
            player.inventory.hasBreakerHandle = false;
            Fit();
            if (fitSound) AudioSource.PlayClipAtPoint(fitSound, transform.position, 0.9f);
            return;
        }
        StartCoroutine(Throw());
    }

    void Fit()
    {
        HandleFitted = true;
        if (lever) lever.gameObject.SetActive(true);
    }

    IEnumerator Throw()
    {
        throwing = true;
        if (throwSound) AudioSource.PlayClipAtPoint(throwSound, transform.position, 1f);
        Quaternion from = lever.localRotation, to = from * Quaternion.Euler(throwAngle, 0f, 0f);
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.35f)
        {
            lever.localRotation = Quaternion.Slerp(from, to, t * t);   // slow at first, then it slams down
            yield return null;
        }
        lever.localRotation = to;
        grid.Restore(transform.position);   // the clunk, the floodlights, the gate
        throwing = false;
    }

    // Loading a save: the handle back on, and if the breaker was thrown, the power already on
    // (no clunk or music) and the powered gates already up.
    public void LoadState(bool fitted, bool on)
    {
        if (fitted || on) Fit();
        if (!on) return;
        lever.localRotation *= Quaternion.Euler(throwAngle, 0f, 0f);
        grid.RestoreQuietly(transform.position);
        foreach (var gate in FindObjectsByType<PoweredGate>())
            if (gate.grid == grid) gate.OpenNow();
    }
}
