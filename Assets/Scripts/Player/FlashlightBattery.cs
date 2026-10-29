using UnityEngine;
using UnityEngine.InputSystem;

// The flashlight's battery. It drains while the torch is on (a full battery lasts 5 minutes). It shines at
// full strength down to 20%, then dims steadily (with a slight stutter) until it goes out at zero. The moment
// it drops below 20%, Sousa says so ("I should switch out the battery...", or that he needs to find one if
// he has no spare). Press H to swap in a spare battery.
public class FlashlightBattery : MonoBehaviour
{
    public float lifetime = 300f;          // seconds of light from a full battery (5 minutes)
    public float lowThreshold = 0.2f;      // below this the light dims and flickers
    public PlayerInventory inventory;
    public AudioClip swapSound;

    [Range(0, 1)] public float charge = 1f;

    float flickerTimer, flickerLevel = 1f, lastCharge = 1f;

    public bool Empty => charge <= 0f;

    // How bright the torch should be right now, from 0 to 1.
    public float Brightness
    {
        get
        {
            if (Empty) return 0f;
            if (charge >= lowThreshold) return 1f;
            return Mathf.Lerp(0.08f, 1f, charge / lowThreshold) * flickerLevel;   // fading toward nothing
        }
    }

    public void Drain(float seconds) => charge = Mathf.Max(0f, charge - seconds / lifetime);

    void Update()
    {
        // Just dropped below 20%: a thought on screen.
        if (lastCharge >= lowThreshold && charge < lowThreshold && charge > 0f)
            Hud.Notice(inventory && inventory.spareBatteries > 0 ? "I should switch out the battery..." : "The battery's dying... I need to find another one.", 5f);
        lastCharge = charge;

        // A weak battery stutters: short random dips in brightness, more often as it empties.
        flickerTimer -= Time.deltaTime;
        if (flickerTimer <= 0f)
        {
            bool dip = charge < lowThreshold && Random.value < Mathf.Lerp(0.35f, 0.05f, charge / lowThreshold);
            flickerLevel = dip ? Random.Range(0.4f, 0.8f) : 1f;   // slight: the steady dimming is the main sign
            flickerTimer = dip ? Random.Range(0.04f, 0.12f) : Random.Range(0.2f, 0.8f);
        }

        if (Controls.Pressed(GameAction.SwapBattery))
        {
            if (inventory.spareBatteries <= 0) { Hud.Notice("No spare batteries"); return; }
            inventory.spareBatteries--;
            charge = 1f;
            if (swapSound) AudioSource.PlayClipAtPoint(swapSound, transform.position, 0.8f);
            Hud.Notice("Fresh battery (" + inventory.spareBatteries + " spare left)");
        }
    }
}
