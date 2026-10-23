using UnityEngine;
using UnityEngine.InputSystem;

// The flashlight's battery. It drains while the torch is on (a full battery lasts 5 minutes),
// dims and stutters in its last 20%, and dies at zero. Press H to swap in a spare battery.
public class FlashlightBattery : MonoBehaviour
{
    public float lifetime = 300f;          // seconds of light from a full battery (5 minutes)
    public float lowThreshold = 0.2f;      // below this the light dims and flickers
    public PlayerInventory inventory;
    public AudioClip swapSound;

    [Range(0, 1)] public float charge = 1f;

    float flickerTimer, flickerLevel = 1f;

    public bool Empty => charge <= 0f;

    // How bright the torch should be right now, from 0 to 1.
    public float Brightness
    {
        get
        {
            if (Empty) return 0f;
            if (charge >= lowThreshold) return 1f;
            return Mathf.Lerp(0.25f, 1f, charge / lowThreshold) * flickerLevel;
        }
    }

    public void Drain(float seconds) => charge = Mathf.Max(0f, charge - seconds / lifetime);

    void Update()
    {
        // A weak battery stutters: short random dips in brightness, more often as it empties.
        flickerTimer -= Time.deltaTime;
        if (flickerTimer <= 0f)
        {
            bool dip = charge < lowThreshold && Random.value < Mathf.Lerp(0.6f, 0.1f, charge / lowThreshold);
            flickerLevel = dip ? Random.Range(0.05f, 0.6f) : 1f;
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
