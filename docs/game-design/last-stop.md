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

- **Main stalker: The Wrapper** (`Assets/Art/Models/Characters/TheWrapper`, 6 animations: walk, run, idle, attack lunge, scream, wall-slam recover). Imported but not placed yet. BagMan is no longer used.
- ~~Main stalker: BagMan~~ (the sack-headed killer; the only monster pack with walk/run/idle/attack animations). It hunts the player throughout the game. **BagMan is a placeholder:** the creative director will replace it later, so keep stalker code model-independent.
- **Rare other horrors** appear only in scripted moments.
- **Defense: a shotgun with very rare shells.** It **stuns BagMan but never kills it**. There are no other weapons.
  - **Shotgun on hold (2026-10-22):** the shotgun and shells are switched off (`ShotgunEnabled = false` in `LookTestBuilder.CoreLoop.cs`) because they are hard to balance for this kind of game. The code stays; the locker and cabinet are simply empty of them. The creative director wants more useful consumables and collectibles instead.
  - **Consumables (2026-10-23):** **road flares** replace the shotgun as the only "defence": G strikes one and holds it low in the right hand (bright, flickering red light and a fizz for 30 s, fading over the last 4 s); G again drops it, still burning, a little ahead of you. Later they will keep the stalker back. Two in this area: on the staff locker shelf and by the wall deep in the west tunnel. No flare model yet (a red stick made of basic shapes). **Painkillers**: two bottles (staff desk, blue platform cabinet), collected and counted only until the player has health (planned with the stalker).
  - **Inventory screen (2026-10-23):** Tab opens it (Tab or Esc closes it); the game pauses and goes quiet. Drawn on the FREE PSX UI frame (skin 1, dark wine). Left: the selected item's model turning slowly (a small chunky render), with its name below. Right: what you carry with counts (W/S, arrows, mouse wheel or click to select), and a short description. Bottom left (covering the frame's printed menu words): torch battery bar, fuse box progress, power on/off.
  - **Menus and settings (2026-10-23):** the game opens on a **title screen**: a slowly swaying view down the east tunnel at the stalled train's flickering cab light, "LAST STOP / LINE 4 - HOLLIS ST", press any key, then New game / Options / Credits / Quit (New game fades in to the street). **Esc** pauses (game frozen and silent): Resume / Options / Credits / Quit to title / Quit game (quits ask YES/NO, NO first). **Options:** master, music and effects-&-ambience volume; mouse sensitivity and invert Y; brightness, PS1 filter (0-30%), field of view (75-110, default 90), viewmodel FOV (45-75, default 60: held items are drawn in their own pass); item glow; centre dot; fullscreen and vsync (built game only); reset to defaults. Style: the FREE PSX UI look (wine panels, ">>" lists, ticked sliders, [ON / OFF]) with the **VCR OSD Mono** font, also used by the HUD and inventory. Key rebinding and controller support come next.
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
- **Sound pass (2026-10-22):** each area has its own background sound, crossfading as you walk (`AreaAmbience`): street = rain loop + quiet "Tube Wind" drone (the rain muffles down the stairs and dies out along the corridor); station = "Too Quiet"; tunnels = "Always Observing". Underground, a distant noise (rockfall, gravel, metal scrape, door creak) plays every 60-120 s from 15-30 m away (`DistantNoises`). A leaking pipe loops above the track puddle. Footsteps: wet gravel on the tracks, splashes in puddles, concrete elsewhere. Notes and posters rustle when you start reading them; fuses clink, the key rattles.

- **Ambience, tension and machine hums:** Helton Yan's Surreal Drones (53-second loops).
- **One-shot effects:** Darkworld Audio – Survival Effects (footsteps, doors, switches, water, breathing, gun handling).
- **Music:** "PSX Horror Music" by Svirls, used **rarely**: chases, big reveals, power coming back, and credits.
- **Shotgun blast:** silent until a real shotgun sound is found.
- **Still missing:** BagMan sounds (breathing, footsteps, screams), a shotgun blast, and footsteps on metal grates.

## Built so far

- **Look test** (`Assets/Scenes/LookTest.unity`, rebuilt by **Last Stop → Build Look Test**). A white-tiled corridor opens onto a platform with track pit, puddles, lamps (one dead, one flickering and buzzing), tunnel mouths (red emergency lamp in the east tunnel), and a player with flashlight and footsteps.
  - Walk 2.2 m/s, hold **Shift** to sprint at 4.2 m/s.
  - **C** toggles crouch: 1.2 m/s, body 1.1 m, eyes 0.95 m, footsteps half as loud. While crouched you can't sprint or jump, and you can't stand up under something low.
  - **Space** jumps 0.6 m: a short, heavy hop (raised from 0.4 m on 2026-10-22 so you can get onto crates).
  - Flashlight (**F**) works like a real torch: a narrow bright hotspot (32°, brightness 160) that throws about 60 m, plus a wide, soft spill ring (85°, brightness 30, 25 m) that lights tunnel walls and ceiling around you.
  - Platform benches face the tracks.
- **Core loop (test area):**
  - **Power starts off.** Only the flickering platform lamp (by the fuse box) and the red emergency lamp work. Everything else is dark, including the corridor, the vending machine and the tunnel work lamps. (The corridor's back wall is now the stairwell up to the street.)
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
  - **Controls (2026-10-21):** WASD, Shift sprint, C crouch (toggle), Space jump, **Q / E lean** (hold), **LMB use** (Amnesia-style), F torch, H battery, **G flare** (strike / drop), **Tab inventory**, **hold RMB aim shotgun**, LMB fire while aiming, R reload. Notes and posters show their text just by looking at them.
  - Speeds: walk 2.3, sprint 4.3, crouch 1.3 m/s. Battery lasts 5 minutes; the worker starts with 1 spare.
  - **Cabinet key puzzle:** the west train can be climbed into by jumping onto a wooden crate beside the track; it's blocked by collapsed seats halfway. A key lies on a bench (blood spatter above it) and opens the blue cabinet on the platform's east wall: second fuse, a battery and a shotgun shell.
  - **Item glow (2026-10-22):** fuses, batteries, shells and keys give off a faint, slowly breathing warm glow on their own surface only, with no light spilling around them (Amnesia-style) so they can be spotted in the dark. On by default; `GameSettings.ItemGlow` is the switch a future settings menu will show.
  - **Rain (2026-10-22):** a light drizzle from the "Rain Particles" pack (Game Seed Assets) with small splashes; it follows the player on the street and stops underground.
  - Station lamps throw light 50% further (and wider) once mains power is back.
  - Screen: PS1 filter at 15% (slight pixelation, colour banding, dither; raised from 9% on 2026-10-22).
  - **Station lamps (2026-10-22):** a wide, soft-edged pool downward plus a weak glow that reaches the ceiling and upper walls, so there is no hard line between light and dark. The "SUBWAY HOLLIS ST LINE 4" sign above the stairs was removed (its text showed through walls).
  - Vertical field of view 60° (about 90° across a 16:9 screen).

## Street level (intro area) — built 2026-10-20

- **Time and weather:** about 3 a.m., light drizzle, wet road with puddles, during the blackout.
  - Only three sodium street lamps still burn (one stutters), plus faint moonlight and a full-moon night sky.
  - The green subway globes light the entrance.
- **Layout:** one block of "Hollis St" (~90 m) with dead ends: an abandoned bus and concrete barriers to the west, police barriers, cones and a police car to the east.
  - Subway entrance mid-block on the south sidewalk.
  - 30 steps down (6 m) into the tiled corridor.
- **Landmarks:**
  - North side: laundromat, VHS rental, 6twelve, corner gas station.
  - South side: pizzeria plus apartment blocks, with a skyline behind.
  - Also: bus stop, trees, hedges, bins, dumpster, graffiti.
- **Opening:** the player starts beside the **maintenance van** at the west end. The van radio plays dispatch's message.
- **Wrapper lore — missing transit workers:** three Line 4 night-shift workers vanished over five weeks.
  - **Dale Kowalski** (54), **Rosa Mendez** (31), **Theo Banks** (26).
- **Lore drops** (all text is a draft for the creative director to rewrite):
  - MISSING posters on two lamp posts and the bus stop.
  - A newspaper on the bus-stop bench.
  - A "closed nights" note on the 6twelve door.
  - Red graffiti in the stairwell: "HE WRAPS THEM UP".
  - A payphone answering machine with Rosa's last message.
  - The van radio.
  - Police tape around the gas station.
- **Foreshadowing, hints only (no sighting):** blood drips down the subway stairs to a wrapped, cocooned shape lying on the steps. (The old graffiti and drag marks were removed.)
- **Grime (2026-10-23), very sparing:** four black, worn graffiti tags (one on an apartment block on the street, two on the stairwell walls, one beside the platform vending machine), plus three small blood marks: a smear dragged down the east tunnel wall, drops outside the staff room door, and a dried stain under the east platform bench (`LookTestBuilder.Grime.cs`).
- Lore text appears on screen when you look at a poster, newspaper or note.
- The street is boxed in by invisible walls; walking up to one shows "I should turn around...". Beyond them the road, buildings, dead lamps and cars carry on for two blocks each way, plus a skyline, drawn only when big enough on screen (LOD).
- Lit: four sodium street lamps (two stuttering), a failing tube under the gas station canopy, a light left on in the laundromat, the subway globes.

## Big reveal (world-building only, NOT to implement yet)

- The truth behind the Wrapper: a **giant spider** lives under Line 4 and is the final 'boss'.
- The 'wrapped' creatures that stalk the tunnels are **spider-zombies**: the missing workers from the posters (Dale Kowalski, Rosa Mendez, Theo Banks...), cocooned and turned. Exactly how is still undecided.
- Players should slowly realise the monsters are the people on the posters, and that they might be next.
- Needed: **cobweb / web models** (the creative director will source them). Keep cocoon and web imagery growing deeper underground.

## Tunnels and trains

- East tunnel (staff-room side) ~100 m, west tunnel ~85 m. Tunnel ceiling 4.3 m above the track.
- Each ends in a **stalled subway car**: dark, doors shut, with a faint flickering cab light. An invisible barrier at its end face stops the player, so the line seems to go on.

## Open questions (ask before building what they affect)

- How many shells exist in total.
- Safe-room look and count per station.
- BagMan's behaviour (patrol, hearing, how long the shotgun stuns it).
