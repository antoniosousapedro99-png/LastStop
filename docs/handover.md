# Handover to the new PC (written 2026-11-01)

The old PC (HO-JOAOPEDRO) is being wiped. This note tells the Claude session on the new PC how the director
likes to work and where the project stands. Read `CLAUDE.md` and `docs/game-design/last-stop.md` first; this
note only adds what those files don't say.

## How the director works with Claude

- They're a **beginner** and the **creative director**. Use plain words and explain any jargon.
- **Every request:** first a short round of **multiple-choice creative questions**, recommendation first. Then
  build it, test it in the Editor, commit, and end with plain "how to try it" steps.
- **Commit before and after each feature.** Write the message to `Temp/msg.txt` and use `git commit -F`.
  End it with a `Co-Authored-By: Claude ...` line.
- **Push only when they say "push"** in that conversation. Never push on your own guess.
- When asked to "pack it for my testers", build to `Builds/LastStop - V.x`. Then rename the
  `..._BackUpThisFolder_ButDontShipItWithYourGame` folder to `..._DebugInfo_KeepPrivate`, add a
  `READ ME FIRST.txt` (controls and what's new), and zip the build with `Compress-Archive`. Bump `bundleVersion`
  in ProjectSettings first (last build: **0.2.6**). The build eval often reports "errors=1": that's the eval's
  5 s timeout, not a game error. Check the log.

## What is NOT in this repo (bring it from the backup drive)

| What | Old place | Why it matters |
|---|---|---|
| Asset library (5.4 GB) | `Documents\PSX horror\assets` | Every pack the game draws from. `CLAUDE.md` expects it at that path. |
| `docs/private/sousa-notes.md` | inside the project | The director's personal story notes. Git-ignored on purpose: **never commit, publish or quote it.** |
| Tester builds (1.6 GB) | `Documents\PSX horror\Builds` | Old builds and their private debug info. |
| `Backups` (0.6 GB) | `Documents\PSX horror\Backups` | Older project backups. |
| Witchbrew logos and video | `Documents\PSX horror\*.png/.jpeg/.mp4` | Studio branding. |

Put them back at the same paths under `Documents\PSX horror\`.

## Tools the project expects

- Git with **Git LFS** (run `git lfs install` once; then `git lfs pull` if models show up as tiny text files).
- **Unity 6000.6.5f1** with Windows Build Support, through Unity Hub. The first time it opens, Unity rebuilds
  the `Library` folder: that takes a while, which is normal.
- **Blender 5.1** at `C:\Program Files\Blender Foundation\Blender 5.1\` (for FBX conversion; its bundled Python
  is used for small patch scripts, run with `-I`).
- **ffmpeg** (`winget install ffmpeg`) and **7-Zip** at `C:\Program Files\7-Zip\7z.exe` (to read packs).
- The `unity` command line (com.unity.pipeline) to drive the open Editor. See `CLAUDE.md`.

## Small workflow lessons from the old PC

- Unity **empties `Temp/` whenever the Editor restarts**, so keep reusable eval helpers somewhere else (e.g.
  the session scratch folder) and copy them into `Temp/` before each run.
- Rebuilding the level (**Last Stop → Build Look Test**, `LookTestBuilder.Build()`) re-saves some LookTest
  materials (line endings, ItemGlow's `_EMISSION`) and `Monster.controller`. Those changes are harmless:
  restore them with `git checkout` rather than committing, except a material you changed on purpose.
- Unity may add `cloudProjectId` / `organizationId` to `ProjectSettings.asset` when you sign in. Don't commit that.
- Tests: `editor_play`, then a guarded eval that sets `runInBackground = true` and starts a New game. When done,
  run `editor_stop`, set `PlayerSettings.runInBackground = false`, and check `ProjectSettings.asset` has no diff.

## Where the game stands (2026-11-01)

- **Station 1, Hollis St:** done and polished (furniture, drips, lamp buzz, distant trains, rippling puddles).
- **Station 2, Canal St Junction:** done and polished (lit stalled train, concrete stairs instead of escalators,
  kickable clutter, 2 flares / 2 batteries / 2 painkillers).
- **Station 3, the sewers:** playable to the end (water, swimming, valves, power, gates, supplies).
  **Stage 5 (sewer voicemails and journal lines) is postponed until much later:** don't start it unless asked.
- **Station 4 and the ending:** planned in the blueprint, not built.
- Last tester build: `LastStop - V.0.2.6.zip`.
- Open questions are at the bottom of the blueprint. Ask before building anything they touch, especially the
  **content note** (the game deals with suicide and self-harm; the director writes that wording).

## Care points

- **J. Sousa is the director's self-insert.** Handle the story with warmth and care. In-game details stay
  fictional unless they say otherwise.
- **Keep the GitHub repo private.** Several packs forbid sharing their files (see `Assets/Art/CREDITS.md`).
  Assets marked "to confirm before release" still need their licences checked.
- Never type or handle the director's passwords; they sign in to Unity, GitHub and so on themselves.
- **Witchbrew** (the director's other game) is a separate project with its own rules. Don't mix them.
