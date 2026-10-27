using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

// A walking route for Frankie along one of his jump links (NavMeshLink), for places his walkable area
// can't reach by itself: steel staircases too narrow for his body, and doorways. Instead of leaping,
// FrankieAI walks the 'points' in order (or backwards, coming the other way).
// With a 'gate' door, the route only exists while that door is open: close it and he can't follow.
[RequireComponent(typeof(NavMeshLink))]
public class LinkPath : MonoBehaviour
{
    public Vector3[] points;               // world positions on the walking surface, from the link's start to its end
    public HingedDoor gate;

    static readonly List<LinkPath> all = new List<LinkPath>();
    NavMeshLink link;

    void Awake() => link = GetComponent<NavMeshLink>();
    void OnEnable() => all.Add(this);
    void OnDisable() => all.Remove(this);

    void Update()
    {
        bool open = !gate || gate.IsOpen;
        if (link.activated != open) link.activated = open;
    }

    // The route whose ends are at 'a' and 'b' (either way round), or null for an ordinary jump link.
    public static LinkPath Find(Vector3 a, Vector3 b)
    {
        foreach (var path in all)
        {
            if (path.points == null || path.points.Length < 2) continue;
            Vector3 first = path.points[0], last = path.points[path.points.Length - 1];
            if ((Near(a, first) && Near(b, last)) || (Near(a, last) && Near(b, first))) return path;
        }
        return null;
    }

    static bool Near(Vector3 p, Vector3 q) => (p - q).sqrMagnitude < 1.5f * 1.5f;
}
