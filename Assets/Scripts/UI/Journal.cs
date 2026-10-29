using System.Collections.Generic;
using UnityEngine;

// Sousa's notebook: short first-person thoughts about what he's found and what he might do next (no
// waypoints, just hints). JournalDirector decides when a thought is written and when it's done (crossed
// out); JournalScreen shows the page. The thoughts written so far are kept in saves.
public class Journal : MonoBehaviour
{
    public class Entry { public string id, text; public bool done; }

    public AudioClip scribble;                 // a pencil scratch when a new thought is written
    public readonly List<Entry> entries = new List<Entry>();   // oldest first

    static Journal current;
    public static Journal Current { get { if (!current) current = FindAnyObjectByType<Journal>(); return current; } }

    public bool Has(string id) => entries.Exists(e => e.id == id);

    // Writes a new thought (once per id). 'quiet' skips the on-screen note and sound (loading a save).
    public void Add(string id, string text, bool quiet = false)
    {
        if (Has(id)) return;
        entries.Add(new Entry { id = id, text = text });
        if (quiet) return;
        Hud.Notice("Journal updated", 2.5f);
        if (scribble) AudioSource.PlayClipAtPoint(scribble, transform.position, 0.5f);
    }

    public void SetDone(string id, bool done)
    {
        var e = entries.Find(x => x.id == id);
        if (e != null) e.done = done;
    }

    // ---- saving: the ids and texts written so far, in order (JournalDirector recomputes what's done)
    public void Save(List<string> ids, List<string> texts)
    {
        foreach (var e in entries) { ids.Add(e.id); texts.Add(e.text); }
    }

    public void Load(List<string> ids, List<string> texts)
    {
        entries.Clear();
        for (int i = 0; i < ids.Count && i < texts.Count; i++) entries.Add(new Entry { id = ids[i], text = texts[i] });
    }
}
