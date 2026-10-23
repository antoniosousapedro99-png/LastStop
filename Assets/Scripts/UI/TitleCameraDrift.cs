using UnityEngine;

// The title screen's camera: it breathes very slightly (a slow sway and bob), as if someone is
// standing in the tunnel holding it, so the still view never feels frozen.
public class TitleCameraDrift : MonoBehaviour
{
    public float sway = 0.6f;    // degrees
    public float bob = 0.02f;    // metres

    Vector3 startPosition;
    Quaternion startRotation;

    void Awake()
    {
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }

    void Update()
    {
        float t = Time.unscaledTime;
        transform.localPosition = startPosition + Vector3.up * Mathf.Sin(t * 0.7f) * bob;
        transform.localRotation = startRotation * Quaternion.Euler(Mathf.Sin(t * 0.37f) * sway * 0.5f, Mathf.Sin(t * 0.23f) * sway, 0f);
    }
}
