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

- **Main stalker: BagMan** (the sack-headed killer; the only monster pack with walk/run/idle/attack animations). It hunts the player throughout the game.
- **Rare other horrors** appear only in scripted moments.
- **Defense: a shotgun with very rare shells.** It **stuns BagMan but never kills it**. There are no other weapons.
- **Flashlight:** the battery drains and spare batteries are scarce.
- **Saving:** only in **safe rooms** (e.g. a staff break room with a working lamp).
- **No sanity mechanic.**

## Look

- **Cleaner PS1:** low-poly PSX models with crunchy pixel-sharp textures, at **full resolution**, with no vertex wobble and no pixelation filter.
- **Lighting:** pools of light with near-black darkness between them.
  - Stations: mostly cold, greenish fluorescent tubes.
  - Street: warm sodium-orange lamps.
  - Tunnels: red emergency lights.
- **Fog:** medium. Things fade into black over about 15 m, but bright lamps still glow through from further away.

## Sound

- **Ambience, tension and machine hums:** Helton Yan's Surreal Drones (53-second loops).
- **One-shot effects:** Darkworld Audio – Survival Effects (footsteps, doors, switches, water, breathing, gun handling).
- **Music:** "PSX Horror Music" by Svirls, used **rarely**: chases, big reveals, power coming back, and credits.
- **Shotgun blast:** silent until a real shotgun sound is found.
- **Still missing:** BagMan sounds (breathing, footsteps, screams), a shotgun blast, and footsteps on metal grates.

## Built so far

- **Look test** (`Assets/Scenes/LookTest.unity`, rebuilt by **Last Stop → Build Look Test**). A white-tiled corridor opens onto a platform with track pit, puddles, lamps (one dead, one flickering and buzzing), tunnel mouths (red emergency lamp in the east tunnel), and a player with flashlight and footsteps.
  - Walk speed 2.2 m/s, Shift for 4 m/s.
  - Vertical field of view 60° (about 90° across a 16:9 screen).

## Open questions (ask before building what they affect)

- Flashlight battery length, and how many spare batteries.
- How many shells exist in total.
- Safe-room look and count per station.
- BagMan's behaviour (patrol, hearing, how long the shotgun stuns it).
