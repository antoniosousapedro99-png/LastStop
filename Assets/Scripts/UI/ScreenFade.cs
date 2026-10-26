using System.Collections;
using UnityEngine;

// A black fade over the whole screen (used for the train ride between stations).
public class ScreenFade : MonoBehaviour
{
    public static ScreenFade Current { get; private set; }
    float alpha;

    void Awake() { Current = this; alpha = 0f; }

    // Fades to 'target' (0 clear, 1 black) over 'seconds'; run it with yield return.
    public IEnumerator FadeTo(float target, float seconds)
    {
        float start = alpha;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            alpha = Mathf.Lerp(start, target, t / seconds);
            yield return null;
        }
        alpha = target;
    }

    void OnGUI()
    {
        if (alpha <= 0f) return;
        GUI.depth = -28;
        MenuStyle.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, alpha));
    }
}
