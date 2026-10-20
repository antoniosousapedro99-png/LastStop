using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// The double-barrel shotgun, held in first person.
// Q raises/lowers it, left mouse fires, R reloads one shell at a time (it holds 2).
// Each shot: a bright muzzle flash that lights the area for a moment, a hard kick of the view and
// the gun, and dark dent marks where the pellets hit. (It will stun the stalker later; there is no
// shot sound until a real shotgun sound is found.)
public class Shotgun : MonoBehaviour
{
    public PlayerInventory inventory;
    public PlayerLook look;
    public Transform gun;                  // the gun model (moves for raise, kick and reload)
    public Transform muzzle;               // tip of the barrels
    public Light muzzleLight;
    public Renderer muzzleFlash;           // glowing blob shown for a split second
    public GameObject[] showWhenRaised;    // gun + arms renderers' roots
    public Material impactMaterial;
    public AudioClip raiseSound, lowerSound, reloadSound, dryFireSound;

    public int capacity = 2;
    public int loaded;
    public float timeBetweenShots = 0.35f;
    public float reloadTimePerShell = 0.75f;
    public int pellets = 8;
    public float spreadDegrees = 4f;
    public float range = 40f;
    public int maxImpactMarks = 80;

    // Gun positions relative to the camera.
    public Vector3 raisedPosition = new Vector3(0.16f, -0.2f, 0.32f);
    public Vector3 loweredPosition = new Vector3(0.22f, -0.75f, 0.2f);
    public Vector3 loweredTilt = new Vector3(35f, -20f, 0f);

    public bool Raised { get; private set; }
    public bool Reloading { get; private set; }

    float raise;         // 0 = lowered and hidden, 1 = up and ready
    float kick;          // jumps to 1 on a shot, settles back to 0
    float reloadDip;     // gun tilts down while a shell goes in
    float nextShotTime;
    readonly Queue<GameObject> marks = new Queue<GameObject>();

    void Start()
    {
        SetVisible(false);
        if (muzzleLight) muzzleLight.enabled = false;
        if (muzzleFlash) muzzleFlash.enabled = false;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (!inventory.hasShotgun) return;

        if (keyboard != null && keyboard.qKey.wasPressedThisFrame && !Reloading)
        {
            Raised = !Raised;
            Play(Raised ? raiseSound : lowerSound);
        }

        if (Raised && raise > 0.9f && !Reloading)
        {
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState == CursorLockMode.Locked)
                TryFire();
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
                TryReload();
        }

        // Animate: slide up/down, recoil and reload dip, all eased.
        raise = Mathf.MoveTowards(raise, Raised ? 1f : 0f, Time.deltaTime * 3.5f);
        kick = Mathf.MoveTowards(kick, 0f, Time.deltaTime * 4f);
        SetVisible(raise > 0.01f);
        float r = Mathf.SmoothStep(0f, 1f, raise);
        gun.localPosition = Vector3.Lerp(loweredPosition, raisedPosition, r) + new Vector3(0f, kick * 0.03f, -kick * 0.12f);
        gun.localRotation = Quaternion.Euler(Vector3.Lerp(loweredTilt, Vector3.zero, r)
                                             + new Vector3(-kick * 14f + reloadDip * 25f, 0f, reloadDip * -20f));
    }

    void TryFire()
    {
        if (Time.time < nextShotTime) return;
        if (loaded <= 0)
        {
            Play(dryFireSound);
            Hud.Notice(inventory.shells > 0 ? "Empty   [R] reload" : "Empty - no shells");
            nextShotTime = Time.time + 0.3f;
            return;
        }
        loaded--;
        nextShotTime = Time.time + timeBetweenShots;
        kick = 1f;
        look.AddRecoil(Random.Range(5f, 7f), Random.Range(-1.2f, 1.2f));
        StartCoroutine(Flash());

        var cam = look.cameraPivot;
        for (int i = 0; i < pellets; i++)
        {
            // Each pellet flies in a slightly random direction inside the spread cone.
            Vector2 offset = Random.insideUnitCircle * spreadDegrees;
            Vector3 dir = Quaternion.AngleAxis(offset.x, cam.up) * Quaternion.AngleAxis(offset.y, cam.right) * cam.forward;
            if (PlayerRay.Cast(cam, cam.position, dir, range, out var hit, includeTriggers: false))
                AddImpactMark(hit);
        }
    }

    void TryReload()
    {
        if (loaded >= capacity) return;
        if (inventory.shells <= 0) { Hud.Notice("No shells"); return; }
        StartCoroutine(Reload());
    }

    IEnumerator Reload()
    {
        Reloading = true;
        while (loaded < capacity && inventory.shells > 0)
        {
            // Tilt the gun down, push a shell in, come back up.
            for (float t = 0; t < reloadTimePerShell; t += Time.deltaTime)
            {
                reloadDip = Mathf.Sin(t / reloadTimePerShell * Mathf.PI);
                yield return null;
            }
            reloadDip = 0f;
            inventory.shells--;
            loaded++;
            Play(reloadSound);
        }
        Hud.Notice("Loaded " + loaded + "/" + capacity + "   (" + inventory.shells + " shells left)");
        Reloading = false;
    }

    IEnumerator Flash()
    {
        if (muzzleLight) muzzleLight.enabled = true;
        if (muzzleFlash)
        {
            muzzleFlash.enabled = true;
            muzzleFlash.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            muzzleFlash.transform.localScale = Vector3.one * Random.Range(0.12f, 0.18f);
        }
        yield return new WaitForSeconds(0.05f);
        if (muzzleFlash) muzzleFlash.enabled = false;
        yield return new WaitForSeconds(0.03f);
        if (muzzleLight) muzzleLight.enabled = false;
    }

    // A small dark dent stuck to the surface that was hit. The oldest marks disappear after a while.
    void AddImpactMark(RaycastHit hit)
    {
        var mark = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(mark.GetComponent<Collider>());
        mark.name = "ImpactMark";
        mark.transform.position = hit.point + hit.normal * 0.004f;
        mark.transform.rotation = Quaternion.LookRotation(-hit.normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        mark.transform.localScale = Vector3.one * Random.Range(0.05f, 0.09f);
        mark.GetComponent<Renderer>().sharedMaterial = impactMaterial;
        marks.Enqueue(mark);
        while (marks.Count > maxImpactMarks) Destroy(marks.Dequeue());
    }

    void SetVisible(bool visible)
    {
        foreach (var go in showWhenRaised) if (go.activeSelf != visible) go.SetActive(visible);
    }

    void Play(AudioClip clip)
    {
        if (clip) AudioSource.PlayClipAtPoint(clip, transform.position, 0.8f);
    }
}
