using System.Collections;
using UnityEngine;

// The way out of the sewers: the barred grate at the end of the overflow tunnel. The final valve opens it
// (it grinds up out of the way); walking up to the opening ends Station 3 (EndCard).
public class RiverExit : MonoBehaviour
{
    public Transform grate;                // the bars and rail, lifted together
    public float lift = 3.4f;
    public Bounds exitArea;                // walking in here, once open, ends the station
    public AudioClip grind;
    public EndCard endCard;

    public bool IsOpen { get; private set; }
    Transform player;

    public void Open(bool loud)
    {
        if (IsOpen) return;
        IsOpen = true;
        if (loud) StartCoroutine(Lift());
        else grate.position += Vector3.up * lift;
    }

    IEnumerator Lift()
    {
        yield return new WaitForSeconds(6f);   // once the purge has started to drain
        if (grind) AudioSource.PlayClipAtPoint(grind, grate.position, 1f);
        Vector3 from = grate.position, to = from + Vector3.up * lift;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 3f)
        {
            grate.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        grate.position = to;
    }

    void Update()
    {
        if (!IsOpen || (endCard && endCard.Showing)) return;
        if (!player) { var inv = FindAnyObjectByType<PlayerInventory>(); if (inv) player = inv.transform; else return; }
        if (!endCard) endCard = FindAnyObjectByType<EndCard>(FindObjectsInactive.Include);   // built after the sewers
        if (!exitArea.Contains(player.position + Vector3.up * 0.9f)) return;
        foreach (var b in new Behaviour[] { player.GetComponent<PlayerMovement>(), player.GetComponent<PlayerSwim>() })
            if (b) b.enabled = false;
        if (endCard) { endCard.title = "END OF STATION 3"; endCard.subtitle = "THE SEWERS"; endCard.Show(); }
    }
}
