using UnityEngine;

// Road flares. The flare key (G) with nothing in hand strikes a new one (if any are carried) and holds it low in the
// right hand; the flare key again drops it where you stand, still burning. A flare that burns out in the hand is
// thrown away automatically.
public class FlareHandler : MonoBehaviour
{
    public PlayerInventory inventory;
    public Flare template;               // an unlit flare kept hidden; each strike copies it
    public Transform hand;               // where a held flare sits (child of the camera)
    public AudioClip strikeSound;

    Flare held;

    void Update()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;   // a menu or the inventory is open

        if (held != null && held.BurntOut) Drop();   // nothing left to hold

        if (!Controls.Pressed(GameAction.Flare)) return;
        if (held != null) Drop();
        else Strike();
    }

    public void Strike()
    {
        if (inventory.flares <= 0) { Hud.Notice("No flares"); return; }
        inventory.flares--;
        held = Instantiate(template, hand);
        held.name = "Flare";
        held.transform.localPosition = Vector3.zero;
        held.transform.localRotation = Quaternion.identity;
        held.gameObject.SetActive(true);
        if (strikeSound) AudioSource.PlayClipAtPoint(strikeSound, hand.position, 0.9f);
        Noise.Make(hand.position, 8f);   // the strike and the fizz can be heard
        Hud.Notice("Flare lit (" + inventory.flares + " left)   [" + Controls.Label(GameAction.Flare) + "] drop it");
    }

    public void Drop()
    {
        if (held == null) return;
        // A small toss forward so it lands in front of you, not on your feet.
        held.Drop(transform.forward * 1.2f + Vector3.up * 0.5f);
        held = null;
    }
}
