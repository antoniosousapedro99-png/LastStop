using System.Collections;
using UnityEngine;

// Falls hurt. Measures how far Sousa drops each time he leaves the ground (from the highest point reached)
// and, when he lands:
// - up to 'safeHeight': nothing;
// - higher: one hit (like Frankie's swipe: a jolt, the red flash, a pain grunt; painkillers heal it), a heavy
//   thud, and a short limp (he moves slowly for a moment);
// - above 'deadlyHeight': he dies.
// Landing in water at least waist-deep (a WaterSurface) breaks any fall. Teleports (moving him with the
// controller switched off) don't count as falls.
public class FallDamage : MonoBehaviour
{
    public float safeHeight = 3f, deadlyHeight = 7f;
    public float deepWater = 0.8f;             // metres of water above the feet that break a fall
    public float limpSeconds = 2f, limpSpeed = 0.45f;   // fraction of normal speed right after landing
    public AudioClip thud;

    CharacterController controller;
    PlayerMovement movement;
    PlayerHealth health;
    bool airborne;
    float peak;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        movement = GetComponent<PlayerMovement>();
        health = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (!controller.enabled || !movement.enabled || (health && health.Dead)) { airborne = false; return; }
        float y = transform.position.y;
        if (!controller.isGrounded)
        {
            if (!airborne) { airborne = true; peak = y; }
            else peak = Mathf.Max(peak, y);
            return;
        }
        if (!airborne) return;
        airborne = false;
        float drop = peak - y;
        if (drop > safeHeight && !WaterSurface.DeepAt(transform.position, deepWater)) Land(drop);
    }

    void Land(float drop)
    {
        if (thud) AudioSource.PlayClipAtPoint(thud, transform.position, 1f);
        if (!health) return;
        if (drop > deadlyHeight) { while (!health.Dead) health.Hit(transform.position); return; }
        health.Hit(transform.position);
        StopAllCoroutines();
        StartCoroutine(Limp());
    }

    // Slow for a moment, easing back to full speed.
    IEnumerator Limp()
    {
        for (float t = 0f; t < limpSeconds; t += Time.deltaTime)
        {
            movement.speedMultiplier = Mathf.Lerp(limpSpeed, 1f, t / limpSeconds);
            yield return null;
        }
        movement.speedMultiplier = 1f;
    }
}
