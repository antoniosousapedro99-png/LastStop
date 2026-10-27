using System.Collections;
using UnityEngine;

// Scare: a chunk of tunnel ceiling gives way a few metres ahead of the player: concrete crashes down onto
// the tracks in a cloud of dust, the view shakes, and the rubble stays there (solid; it can be vaulted).
// The builder places the rubble where it ends up on the floor; until the scare it hangs hidden in the
// ceiling. A loaded save where it already happened leaves it on the floor (AlreadyHappened).
public class CeilingCollapse : MonoBehaviour, IScare, IScareAftermath
{
    public Transform[] pieces;             // the rubble, in its final place on the floor
    public GameObject hole;                // the dark gap left in the ceiling
    public float ceilingY = 2.9f;
    public ParticleSystem dust;
    public AudioSource crash;
    public float shake = 1.5f;             // degrees of view shake

    Vector3[] landed;
    Quaternion[] landedTurn;

    void Awake()
    {
        landed = new Vector3[pieces.Length];
        landedTurn = new Quaternion[pieces.Length];
        for (int i = 0; i < pieces.Length; i++)
        {
            landed[i] = pieces[i].position;
            landedTurn[i] = pieces[i].rotation;
            Show(pieces[i], false);
        }
        hole.SetActive(false);
    }

    public void AlreadyHappened()
    {
        for (int i = 0; i < pieces.Length; i++) { pieces[i].SetPositionAndRotation(landed[i], landedTurn[i]); Show(pieces[i], true); }
        hole.SetActive(true);
    }

    public void Play(Transform player) => StartCoroutine(Fall());

    IEnumerator Fall()
    {
        hole.SetActive(true);
        if (crash) crash.Play();
        if (dust) dust.Play();
        PlayerLook.Shake = shake;
        // Each piece drops from the ceiling under gravity, a moment after the one before.
        for (int i = 0; i < pieces.Length; i++) StartCoroutine(Drop(i, i * 0.08f));
        yield return new WaitForSeconds(1.2f);
        PlayerLook.Shake = 0f;
    }

    IEnumerator Drop(int i, float delay)
    {
        yield return new WaitForSeconds(delay);
        var piece = pieces[i];
        Vector3 from = new Vector3(landed[i].x, ceilingY, landed[i].z);
        Quaternion spinFrom = landedTurn[i] * Quaternion.Euler(Random.Range(-60f, 60f), 0f, Random.Range(-60f, 60f));
        float fallTime = Mathf.Sqrt(2f * (ceilingY - landed[i].y) / 9.81f);
        piece.SetPositionAndRotation(from, spinFrom);
        Show(piece, true);
        for (float t = 0f; t < fallTime; t += Time.deltaTime)
        {
            float k = t / fallTime;
            piece.SetPositionAndRotation(Vector3.Lerp(from, landed[i], k * k), Quaternion.Slerp(spinFrom, landedTurn[i], k));
            yield return null;
        }
        piece.SetPositionAndRotation(landed[i], landedTurn[i]);
    }

    static void Show(Transform piece, bool on)
    {
        foreach (var r in piece.GetComponentsInChildren<Renderer>()) r.enabled = on;
        foreach (var c in piece.GetComponentsInChildren<Collider>()) c.enabled = on;
    }
}
