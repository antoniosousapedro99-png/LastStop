using UnityEngine;

// Decides when the first-person arms are on screen and what drives them:
// - While the shotgun is raised, ArmsRig bends them around the gun.
// - When the player uses something (picks up a fuse, opens the locker...) with the gun lowered,
//   the right hand plays the pack's quick "grab" animation, then the arms disappear again.
public class ArmsController : MonoBehaviour
{
    public Shotgun shotgun;
    public ArmsRig rig;              // IK that holds the gun
    public Animation clips;          // the arm pack's animations (legacy Animation component)
    public string grabClip = "ArmsRig|grab.R";
    public GameObject visuals;       // the arms model (shown/hidden)

    float grabUntil;

    void OnEnable() => Interactor.Interacted += OnInteracted;
    void OnDisable() => Interactor.Interacted -= OnInteracted;

    void Start()
    {
        clips.playAutomatically = false;
        clips.Stop();
    }

    void OnInteracted()
    {
        if (shotgun.RaiseAmount > 0.01f) return;    // hands are busy with the gun
        var state = clips[grabClip];
        if (state == null) return;
        visuals.SetActive(true);                   // must be visible before an animation can play
        clips.Stop();
        clips.Play(grabClip);
        grabUntil = Time.time + state.length;
    }

    void Update()
    {
        bool holdingGun = shotgun.RaiseAmount > 0.01f;
        bool grabbing = Time.time < grabUntil;
        if (holdingGun && grabbing) { clips.Stop(); grabbing = false; grabUntil = 0f; }

        rig.enabled = holdingGun;
        if (visuals.activeSelf != (holdingGun || grabbing)) visuals.SetActive(holdingGun || grabbing);
    }
}
