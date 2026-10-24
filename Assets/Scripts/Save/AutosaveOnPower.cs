using System.Collections;
using UnityEngine;

// The one automatic save: a few seconds after the breaker is pulled (once the lights and the staff door
// have had time to come on), the game saves to the autosave slot, so dying soon after doesn't send the
// player all the way back. It waits if the player is hiding in a locker at that moment.
public class AutosaveOnPower : MonoBehaviour
{
    public PowerGrid grid;
    public float delay = 4f;

    void Awake() => grid.Restored += OnPower;
    void OnDestroy() { if (grid) grid.Restored -= OnPower; }

    void OnPower() => StartCoroutine(SaveSoon());

    IEnumerator SaveSoon()
    {
        yield return new WaitForSeconds(delay);
        while (HidingLocker.Occupied) yield return null;
        var health = FindAnyObjectByType<PlayerHealth>();
        if (health && health.Dead) yield break;
        if (SaveSystem.Write(SaveSystem.Autosave, "Autosave - Power restored")) Hud.Notice("Game autosaved", 2.5f);
    }
}
