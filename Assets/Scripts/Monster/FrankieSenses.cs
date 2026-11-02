using System.Collections.Generic;
using UnityEngine;

// What Frankie notices.
// - Sight: depends on light. In pitch black he only sees the player within 'darkSightRange' metres; the
//   better lit they are (lamps, candles, flares: see LightLevel) the further, up to 'sightRange'; with the
//   torch on they give themselves away from 'torchSightRange'. Always inside a wide cone in front of him,
//   with nothing solid in between, except right next to him: within 'closeRange' he notices the player
//   whichever way he faces (breath, footsteps, the smell of them), unless they're crouched.
// - Hearing: any Noise made close enough (running carries far, walking less, crouching makes none).
public class FrankieSenses : MonoBehaviour
{
    public Transform eyes;                  // a point at head height
    public float darkSightRange = 4f;       // the player in pitch black, torch off
    public float sightRange = 15f;          // the player standing in a well-lit spot
    public float fullyLit = 1f;             // LightLevel that counts as well lit
    public float torchSightRange = 25f;
    public float sightAngle = 120f;         // degrees, the whole cone
    public float closeRange = 2f;           // metres: this close he notices a standing player in any direction

    public Transform Player { get; private set; }
    public bool HasHeard { get; private set; }
    public Vector3 HeardAt { get; private set; }

    Flashlight torch;
    PlayerMovement movement;
    readonly List<Light> torchLights = new List<Light>();

    void OnEnable() => Noise.Made += OnNoise;
    void OnDisable() => Noise.Made -= OnNoise;

    void FindPlayer()
    {
        if (Player) return;
        var go = GameObject.Find("Player");
        if (!go) return;
        Player = go.transform;
        torch = go.GetComponentInChildren<Flashlight>(true);
        movement = go.GetComponent<PlayerMovement>();
        torchLights.Clear();
        if (torch) foreach (var l in new[] { torch.beam, torch.spill, torch.viewmodelFill }) if (l) torchLights.Add(l);
    }

    // The player's head, for line-of-sight checks.
    public Vector3 PlayerHead => Player.position + Vector3.up * 1.5f;

    public bool CanSeePlayer()
    {
        FindPlayer();
        if (!Player || !Player.gameObject.activeInHierarchy) return false;
        if (HidingLocker.Occupied) return false;   // hidden in a locker
        Vector3 to = PlayerHead - eyes.position;
        float range = SightRange();
        if (to.magnitude > range) return false;
        Vector3 flat = new Vector3(to.x, 0f, to.z);
        bool rightBesideHim = flat.magnitude < closeRange && !(movement && movement.IsCrouched);
        if (!rightBesideHim && Vector3.Angle(transform.forward, flat) > sightAngle * 0.5f) return false;
        // Walls, the train, crates... anything solid that isn't the player blocks the view.
        if (Physics.Raycast(eyes.position, to.normalized, out var hit, to.magnitude, ~0, QueryTriggerInteraction.Ignore))
            return hit.collider.transform.IsChildOf(Player);
        return true;
    }

    // How far away he can see the player right now, from how lit they are.
    public float SightRange()
    {
        if (torch && torch.IsOn) return torchSightRange;
        float lit = Mathf.Clamp01(LightLevel.At(PlayerHead, torchLights) / fullyLit);
        return Mathf.Lerp(darkSightRange, sightRange, lit);
    }

    // The other way round: is he on the player's screen (near the middle of the view, within 30 m, with
    // nothing solid between the camera and his chest)? Used for his first appearance and the torch flicker.
    public bool PlayerCanSeeMe()
    {
        var cam = Camera.main;
        if (!cam || !cam.isActiveAndEnabled) return false;
        Vector3 chest = transform.position + Vector3.up * 1.3f;
        Vector3 to = chest - cam.transform.position;
        if (to.magnitude > 30f || Vector3.Angle(cam.transform.forward, to) > 35f) return false;
        if (Physics.Raycast(cam.transform.position, to.normalized, out var hit, to.magnitude, ~0, QueryTriggerInteraction.Ignore))
            return hit.collider.transform.IsChildOf(transform) || (Player && hit.collider.transform.IsChildOf(Player));
        return true;
    }

    void OnNoise(Vector3 where, float carries)
    {
        if (Vector3.Distance(where, transform.position) > carries) return;
        HasHeard = true;
        HeardAt = where;
    }

    public void ForgetNoise() => HasHeard = false;
}
