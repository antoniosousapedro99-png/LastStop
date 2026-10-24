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
  - **Menus and settings (2026-10-23):** the game opens on a **title screen**: a slowly swaying view down the east tunnel at the stalled train's flickering cab light, "LAST STOP / LINE 4 - HOLLIS ST", press any key, then New game / Options / Credits / Quit (New game fades in to the street). **Esc** pauses (game frozen and silent): Resume / Options / Credits / Quit to title / Quit game (quits ask YES/NO, NO first). **Options:** master, music and effects-&-ambience volume; mouse sensitivity and invert Y; brightness, PS1 filter (0-30%), field of view (75-110, default 90), viewmodel FOV (45-75, default 60: held items are drawn in their own pass); item glow; centre dot; fullscreen and vsync (built game only); reset to defaults. Style: the FREE PSX UI look (wine panels, ">>" lists, ticked sliders, [ON / OFF]) with the **VCR OSD Mono** font, also used by the HUD and inventory.
  - **Key bindings and controller (2026-10-23):** Options / Controls / **Key bindings** lists every action (move, sprint, crouch, jump, lean, use, flashlight, swap battery, flare, inventory, pause); choose one and press the new key or mouse button (Esc cancels; a key already in use swaps places); Reset keys restores them. **Controller** (fixed layout, shown under Controller layout): left stick move, right stick look, L3 sprint (stays on until you stop), A jump, B crouch, X use, Y flare, LB/RB lean, d-pad up flashlight, d-pad down battery, View inventory, Menu pause; menus use d-pad/stick, A, B. On-screen prompts name the controller buttons when it was used last. "Mouse sensitivity" became "Look sensitivity" (it also scales the right stick).
  - **Frankie replaces the Pale Man (2026-10-24):** the monster is now **Frankie** (the Big Frankie model: a gaunt, long-armed creature with bulging eyes and bared teeth), **2.1 m** tall, his **top hat removed**. He uses **his own animations**: a hunched walk, a run, a "scary idle", a roar (kept as his scream) and an attack cut from his door animation (a crouched lunge with a claw reach; the hit lands 0.8 s in). Everything below written for the Pale Man (senses, moods, speeds, flares, lockers, staged first sighting, torch flicker, sounds, the train finale) now applies to Frankie unchanged. Animation playback is matched to his measured foot speeds (walk 0.8 m/s, run about 5.5 m/s, so his run plays a little slower than authored at 3.8 m/s).
  - **The Pale Man (2026-10-23), the stalker (now Frankie):** a tall, thin, pale figure with a wide black grin (Character_Monster from Characters PSX 1.2; it replaces TheWrapper as the monster, whose animations he uses: nervous walk, frantic run, watchful idle, scream, lunge). He is nowhere until the fuse box is fixed; **5-10 s after the power returns** (was 15-20 s until 2026-10-24) every lamp stutters, a shriek echoes from deep in the west tunnel and he starts hunting from there. **Senses:** sees you within ~15 m (25 m if your torch is on), in front of him, if nothing blocks the view; hears running within 15 m, walking within 6 m, crouching not at all, and a struck flare within 8 m. **Moods:** wanders at 0.9 m/s (slowed from 1.4 on 2026-10-24 so his feet stay planted; sometimes drifting toward you), investigates noises, chases (3.8 m/s; your sprint is 4.3), searches where he lost you (gives up after ~8 s without sight), lunges when within ~1.6 m, then backs off for 6 s so hits never come back to back. **Flares:** he keeps about 6 m from any lit flare (held or dropped) and backs away from it. **Health:** two hits kill you (red flash and a jolt; while hurt the screen edges stay red and you breathe hard); painkillers from the inventory heal one hit; death fades to black ("YOU DIED") and returns to the title (load a save from there). He walks only underground (the street is safe) and cannot reach the staff room (its door is shut when his walkable area is made). Chase music (Svirls, "Chase 1") plays only during chases. His sounds are reshaped from the Darkworld pack with ffmpeg (a wet rattling breath, a reversed in-breath sucking into a two-voice shriek, bare fleshy footsteps, joint cracks every 5-12 s while he moves) until real monster sounds are found.
  - **Pale Man polish (2026-10-24):** **First sighting (staged):** after the arrival shriek the lamps stutter again and he appears under the lamp at the platform end further from the player, staring; when the player first sees him a sting plays (Svirls, "Jumpscare"), he stares 2.5 s more, then walks into that end's tunnel without chasing, and only then starts hunting (he also leaves if the player comes within 5 m or after 30 s). **Hiding lockers:** four (a pair on the platform's back wall, one in each tunnel); use one to step in and peek through the door slats (you can turn about 55 degrees each way); he can't see you inside unless he saw you get in, in which case he walks over, yanks the door open and attacks. **Torch flicker:** within 10 m and out of view, your torch stutters, more often the closer he is. Difficulty kept as tested. **Animation (2026-10-24):** his feet no longer skate: the walk and run animations play faster to match his real speed (measured: the walk's feet move 0.28 m/s, the run's 1.07 m/s; playback capped at 3.25x walking, 3x running), walk/run switching is smoothed, he turns more slowly (180 degrees/s), and footstep sounds land on the animation's footfalls.
- **Flashlight:** the battery drains and spare batteries are scarce.
- **Saving:** only in **safe rooms** (e.g. a staff break room with a working lamp).
  - **Built (2026-10-24):** the staff room PC is the save point ("Use computer (save game)"), plus **one autosave** 4 s after the power comes back (its own slot, so it never overwrites the player's). **5 named slots.** Choosing a slot opens a name box pre-filled with "<area> - <time>" (e.g. "Staff Room - 14:32"); Enter keeps it or type your own (24 letters max). **Load game** is on the title and in the pause menu (from the pause menu it asks first). A save keeps: where you stand, what you carry, torch battery and on/off, hits taken, fuses in the box, power, picked-up items, open doors, and whether the Pale Man is awake and where.
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
  - **Fuse box** on the platform's west end wall. It needs **2 fuses**: one in the squatter camp on the service level (since 2026-10-24; it used to lie on a crate deep in the west tunnel), the other locked in the blue platform cabinet (its key is in the west train). Press E to insert each fuse, then E again to **pull the breaker**.
  - **Power returns:** a breaker clunk, then lamps stutter on one by one in a wave spreading from the fuse box. The flickering lamp steadies, an electrical hum fades in, and the **"Powerbox"** music sting plays.
  - **Tunnels** are about 50 m each side, with caged work lamps every 12 m.
  - **Staff room** inside the east tunnel. Its electric lock is dead until power returns; then it buzzes, clicks and swings open.
    - Inside: a desk with a **90s staff PC** (the save point), a half-eaten pizza, a cola can, a placeholder cigarette pack and a Zippo, and an office chair.
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
- **Station 1 exit (2026-10-24):** the way out is the **train stalled in the east tunnel** (now an open car like the west one, its open end facing the platform, a crate to climb in by, a small driver's cab with the controls and a flickering cab light). The controls need **power** and the **reverser key**; a note taped above them says the spare is in the **staff room desk drawer** (a small steel drawer unit beside the desk). With both, using the controls slots the key in: a clunk, the cab light steadies, the engine whines, and the noise brings the **Pale Man charging** down the tunnel (woken early if he hadn't arrived). The player must **hold the Use button on the dead man's handle for 10 s in total** while looking at the controls; letting go pauses the countdown (time to strike a flare). He can't fit into the cab, but in this finale he lunges in from the crate at the door, with normal hits (two kill). When the countdown fills, the train screeches away and he is left at the door; the screen fades to the **end card** ("END OF STATION 1 / HOLLIS ST", time played, "TO BE CONTINUED"), then the title. The old sealed east train is gone; the title camera now looks at the open car.
- **Atmosphere pass (2026-10-24):** one-time scares at set spots (each happens once; whether it has happened is saved). **Before the power:** the PA speakers on the platform's back wall crackle on as you first step out of the corridor (a slowed, broken chime, static, on-screen lines "...attention... all... night staff...", "...Hollis Street is... closed... closed...", "...do not... go past... the trains...", and a slow breath through the speakers); a **train rumble** as you first enter the west tunnel (deep rumble, the view shakes slightly, dust trickles from the ceiling; no train ever comes). **After the power:** lamps **pop and die behind you** for 4 s at three spots (corridor, platform east end, east tunnel past the staff room); a **shadow figure** (Frankie's silhouette, near black, faintly backlit) stands on the tracks just before the staff room door when you enter the east tunnel, and vanishes with a whisper when your torch finds him (or if you come within 6 m); a second rumble near the east train. Sounds made with ffmpeg from the Darkworld pack (plus a synthesised chime) in `Audio/SFX/Darkworld/Scares`.
- **Service level (2026-10-24):** service tunnels under the west tunnel, in the spirit of Fallout 3's metro maintenance areas, **industrial rather than station-like** (the Sewer pack's dirty concrete, brick and rusty metal; no tiles). **Psychological horror only:** it's explored before the power is fixed and lies outside Frankie's walkable area, so he never comes down. **Layout:** a rusted **maintenance door** in the west tunnel's outer wall (x -55, opens by hand, no power) leads to a landing and a **spiral staircase** (35 rusty metal steps round a centre pole, one and a half turns, 8 m down, one dying caged bulb halfway); a short corridor; the **machinery hall** (24 x 14 m: generator, turbine, two transformers, pumps, pipe runs, barrels, crates; lit only by one battery work light on a tripod); the **storage room** to the west (shelves, crates, cardboard, barrels; dark); and the **squatter camp** to the east (two mattresses, a couch, a chair, a dead fridge, rubbish and bin bags, candles still burning, a radio hissing static, two notes and a scrawl "DONT LET THEM TURN THE LIGHTS ON"). **The west fuse** now lies on the camp's crate table beside a note: "Took the fuse so the maintenance guys can't turn the lights back on. When the lights come back on, IT wakes up." The other note: "Day 9 down here. Ray swears he hears it walking the tracks at night. Not a train. Feet. Ray went up to look on Tuesday." **Sounds with no source (once each):** slow footsteps crossing the floor above as you enter the hall; a cough from the empty camp as you step into the storage room; a whisper from the landing behind you halfway down the stairs. **Knocking pipe:** a fat pipe in the hall's corner; knock on it and something knocks back from deep in the pipework (three knocks the first time, one slow knock the second, then never again). **Things change when you look away:** once you've seen them and left the camp, the chair turns to face the doorway and a candle moves to just inside the doorway. **Lighting:** mostly dark (the torch does the work), plus the work light, the stair bulb and the candles. The service level has its own background hum (Helton Yan, "Thrusters Inside").
- **Polish pass (2026-10-24):** **Street:** the ground up to and under the shops is now paved like the sidewalk (the old streaky "lot" concrete made the buildings look like they stood on a separate floor); street lamps moved back from the curb so their bases stand on the sidewalk; cars no longer get extra wheels (their models have wheels built in, so they were doubled); trees and bushes cut out their see-through leaf edges instead of showing black squares; the pizzeria's cellar tread no longer hovers. **Frankie** has his textures again (his silhouette copy for the shadow figure had been wiping them). **Service level pickups:** two spare batteries (on a crate in the machinery hall, by the storage shelves), a flare (by the storage barrels) and painkillers (by the camp mattress). The fuse note now lies on the floor beside the crate table (it hung over the crate's edge); the flat cardboard in the camp no longer flickers against the floor.
- **Inventory list (2026-10-24):** seven slim rows (names about two thirds of the old size); the frame's four printed item boxes are covered and replaced by one slim wine box per row, empty rows dim, the selected row glowing.
- Players should slowly realise the monsters are the people on the posters, and that they might be next.
- Needed: **cobweb / web models** (the creative director will source them). Keep cocoon and web imagery growing deeper underground.

## Tunnels and trains

- East tunnel (staff-room side) ~100 m, west tunnel ~85 m. Tunnel ceiling 4.3 m above the track.
- Each ends in a **stalled subway car**: dark, doors shut, with a faint flickering cab light. An invisible barrier at its end face stops the player, so the line seems to go on.

## Open questions (ask before building what they affect)

- How many shells exist in total.
- Safe-room look and count per station.
- BagMan's behaviour (patrol, hearing, how long the shotgun stuns it).
