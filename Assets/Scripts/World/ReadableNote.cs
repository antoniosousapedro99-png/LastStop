using UnityEngine;

// Anything with writing on it: missing-person posters, a newspaper, a note on a shop door.
// Just look at it (no click): the Interactor shows the text on screen while it's in view.
public class ReadableNote : MonoBehaviour
{
    [TextArea(3, 10)] public string text;
}
