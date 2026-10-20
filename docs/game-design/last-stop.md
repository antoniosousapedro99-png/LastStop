# Last Stop — design blueprint

Short (5–6 hours) first-person psychological / survival horror game. **This file is the source of truth**: read it before planning any feature, and add every new creative decision here.

## Premise

- **Player:** a night-shift maintenance worker. After a city-wide blackout they are sent down alone to bring power back to the metro.
- **Setting:** a 1990s US-style city (NYC-like tiled stations, "Uptown & The Bronx"-style signs).
- **Route:** an eerie street → stairs down into the metro → a "Staff Only" door → maintenance tunnels.
- **Goal:** replace fuses in fuse boxes and fix generators to restore power to the area's stations.

## Structure

- **4 stations** (about 1–1.5 hours each), linked by tunnels, then a final ending zone.
- Each station is a zone whose power the player restores.

## Threat and survival

- **Main stalker: BagMan** (the sack-headed killer; the only monster pack with walk/run/idle/attack animations). It hunts the player throughout the game. **BagMan is a placeholder:** the creative director will replace it later, so keep stalker code model-independent.
- **Rare other horrors** appear only in scripted moments.
- **Defense: a shotgun with very rare shells.** It **stuns BagMan but never kills it**. There are no other weapons.
- **Flashlight:** the battery drains and spare batteries are scarce.
- **Saving:** only in **safe rooms** (e.g. a staff break room with a working lamp).
- **No sanity mechanic.**

## Look

- **Surface brightness:** concrete and gravel tints stay around 0.5–0.6. Near-black surfaces made the flashlight look useless in the tunnels.

- **Cleaner PS1:** low-poly PSX models with crunchy pixel-sharp textures, at **full resolution**, with no vertex wobble and no pixelation filter.
- **Lighting:** pools of light with near-black darkness between them.
  - Stations: mostly cold, greenish fluorescent tubes.
  - Street: warm sodium-orange lamps.
  - Tunnels: red emergency lights.
- **Fog:** things fade into black over about **25 m** (raised from 15 m after playtesting), but bright lamps still glow through from further away.

## Sound

- **Ambience, tension and machine hums:** Helton Yan's Surreal Drones (53-second loops).
- **One-shot effects:** Darkworld Audio – Survival Effects (footsteps, doors, switches, water, breathing, gun handling).
- **Music:** "PSX Horror Music" by Svirls, used **rarely**: chases, big reveals, power coming back, and credits.
- **Shotgun blast:** silent until a real shotgun sound is found.
- **Still missing:** BagMan sounds (breathing, footsteps, screams), a shotgun blast, and footsteps on metal grates.

## Built so far

- **Look test** (`Assets/Scenes/LookTest.unity`, rebuilt by **Last Stop → Build Look Test**). A white-tiled corridor opens onto a platform with track pit, puddles, lamps (one dead, one flickering and buzzing), tunnel mouths (red emergency lamp in the east tunnel), and a player with flashlight and footsteps.
  - Walk 2.2 m/s, hold **Shift** to sprint at 4.2 m/s.
  - **C** toggles crouch: 1.2 m/s, body 1.1 m, eyes 0.95 m, footsteps half as loud. While crouched you can't sprint or jump, and you can't stand up under something low.
  - **Space** jumps 0.4 m: a short, heavy hop.
  - Flashlight (**F**) works like a real torch: a narrow bright hotspot (32°, brightness 160) that throws about 60 m, plus a wide, soft spill ring (85°, brightness 30, 25 m) that lights tunnel walls and ceiling around you.
  - Platform benches face the tracks.
- **Core loop (test area):**
  - **Power starts off.** Only the flickering platform lamp (by the fuse box) and the red emergency lamp work. Everything else is dark, including the corridor, the vending machine and the tunnel work lamps.
  - **Fuse box** on the platform's west end wall. It needs **2 fuses**, one at the far end of each tunnel (on crates). Press E to insert each fuse, then E again to **pull the breaker**.
  - **Power returns:** a breaker clunk, then lamps stutter on one by one in a wave spreading from the fuse box. The flickering lamp steadies, an electrical hum fades in, and the **"Powerbox"** music sting plays.
  - **Tunnels** are about 50 m each side, with caged work lamps every 12 m.
  - **Staff room** inside the east tunnel. Its electric lock is dead until power returns; then it buzzes, clicks and swings open.
    - Inside: a desk with a **90s staff PC** (future save point; for now it says saving comes later), a half-eaten pizza, a cola can, a placeholder cigarette pack and a Zippo, and an office chair.
    - A steel **locker** (E opens it) holds the **shotgun** and **3 loose shells**.
  - **Flashlight battery:** lasts **4 minutes**, dims and stutters below 20%, dies at 0. **2 spare batteries** in the area (one by the west bench, one on a crate in the west tunnel). **H** swaps a spare in.
  - **Shotgun:**
    - **Q** raises/lowers it, **left mouse** fires, **R** reloads one shell at a time.
    - Holds **2** shells; 8 pellets per shot.
    - Each shot: a muzzle flash that lights the area, recoil, and dark dent marks where pellets hit.
    - It's silent until a shotgun sound is found; it stuns the stalker later.
  - **Arms:** PSX first-person arms, **bare skin** (gloves looked too tactical).
    - Right hand on the grip. Left hand cups the forend: **palm under it, thumb along its left side, fingers wrapped up its right side**, sitting flush; the left elbow angles out to the left.
    - Arms appear only while the shotgun is up, plus a quick hand **grab** when picking something up or using things.
  - **Fuse box** sits at eye level (centre 1.5 m), and a small dot marks the centre of the screen (brighter when you can use what you're looking at). Its prompt says how many fuses you carry.
  - **Controls:** WASD, Shift sprint, C crouch, Space jump, F torch, H battery, E use, Q shotgun, LMB fire, R reload.
  - Vertical field of view 60° (about 90° across a 16:9 screen).

## Open questions (ask before building what they affect)

- How many shells exist in total.
- Safe-room look and count per station.
- BagMan's behaviour (patrol, hearing, how long the shotgun stuns it).
