# LAST STOP — Game Design Document & Progress Report

*Snapshot: 28 October 2026. Written so another assistant (e.g. ChatGPT) can understand the game, what has been built so far, how it was built, and what is still open.*

---

## 0. How to read this document

- **Who's who:** the developer is a **beginner** and the game's **creative director**. They make every creative decision (names, numbers, look, feel). An AI coding assistant (Claude Code) builds the features in Unity and makes the technical choices.
- **Working method, used for every feature:**
  1. The director describes what they want.
  2. The assistant asks the creative questions as short multiple-choice lists, with a recommendation first.
  3. The assistant builds it, tests it inside the Unity Editor (scripted play-mode tests and screenshots) and explains in plain words how to try it.
  4. Git commits are made before and after each feature.
- **Status words:** "Built" means it's in the game and tested. "Planned" means agreed but not built. "Idea" means world-building that isn't to be implemented yet.

---

## 1. The game at a glance

| | |
|---|---|
| **Title** | Last Stop |
| **Genre** | First-person psychological / survival horror |
| **Length (target)** | 5–6 hours: 4 stations of about 1–1.5 h each, plus a final ending zone |
| **Engine** | Unity 6 (6000.6.5f1), Universal Render Pipeline |
| **Platform** | PC (Windows), keyboard + mouse and controller |
| **Look** | "Clean PS1": low-poly PSX-style models with crunchy pixel-sharp textures at full resolution; a light PS1 screen filter (15%); no vertex wobble |
| **Tone** | Slow dread, isolation, pools of light in near-black darkness, rare but sharp scares |
| **Progress** | **Stations 1 and 2 are playable end to end.** Friends have played a build twice; their feedback drove two rounds of fixes. |

### Premise

You play a **night-shift metro maintenance worker** in a 1990s, New-York-style city. After a city-wide blackout you're sent down **alone** to bring power back to the Line 4 subway stations. Line 4 has been run into the ground by years of cost-cutting, and tonight it all failed at once.

Something may be down there with him. Or it may be his own mind.

### Core loop (each station)

1. **Explore** a dark station with a flashlight whose battery runs down.
2. **Find the parts** needed to restore power (fuses, fuel, a spark plug, keys), often in side areas.
3. **Avoid the stalker, Frankie**, who hunts by sight and sound. Hide in lockers, break line of sight, or keep him back with road flares.
4. **Restore power.** Lights come back in a wave, and doors and machines wake up.
5. **Escape to the next station** through a scripted finale.

### The story: Sousa, the failing line, and fear (decided 28 October)

- **No giant spider, and no confirmed transformations**: nobody is confirmed to have been turned into a monster.
- **The real reason Line 4 is dead is mundane: years of cost-cutting.** Postponed repairs and old equipment until everything failed at once. The accidents feel real: the west train crushed by a tunnel collapse, dead power feeds, and further down the line floods and fires, all failures of neglect.
- **The haunting is never explained.** Frankie, the shadow figures and each section's creatures could be real, or Sousa's mind under stress and derealisation. The game gives evidence both ways and never decides.
- **The focus is Sousa:** the creatures embody his fears, one per section.
- **Creatures:**
  - **Frankie has the Ridgeback's body** (by Codyanka, since 28 October): same AI and behaviour.
  - Later options: the Wrapped and Clawman (Codyanka); "Necrothread: The Pulse of Rotting Flesh" and the eyeball spider creatures (imaginais).
- **Shadow figures** (built 28 October): every 3-5 minutes underground, a pitch-black silhouette of an ordinary man stands at the corner of your eye, facing you. The moment you look at it, it's gone, silently. Never explained.
- **Each section of the game focuses on one fear; the last is the player character's own**.
- **The player character (decided 2026-10-28):** a **Portuguese migrant worker**, estranged from his family and friends because of deep-seated mental struggles (ADHD, anger issues, depression, derealisation). He deeply regrets his own existence and how he handled things in the past with his **parents** and his **former girlfriend**. No kids: just regrets and a bitter life. That's why he takes the job: nothing left to lose, nothing worth losing, or so he thinks. He can still lose his mind.
  - **His fear: being forgotten.** The final section is built from it.
  - **Revealed by slow hints** scattered through every station, small personal things that only make sense at the end.
  - **Two endings** (see below).
  - **Name: J. Sousa.** Workmates, dispatch and badges use only his surname ("Sousa", "J. Sousa").
  - **Details are inspired, fictional:** the feelings are real, but names, places and events are made up.
- **Sousa, in detail (decided 2026-10-28; fiction inspired by the director):**
  - **Early 30s**, in the city about **two years**. He came **running from home** after the break with his family: the first job abroad he could find, in a city just far enough away.
  - **He carries an old photo**: creased, of him as a kid with his parents, the only one of him left. It can fade as the game goes on.
  - **He copes with cigarettes, a flask and silence.** (No OCD: removed 2026-10-28, it doesn't translate into gameplay.)
  - **His derealisation is left ambiguous:** it could be read as undiagnosed mild schizophrenia, but the game never names or diagnoses it. This is part of why the haunting is never explained: real, or his mind?
  - **He rarely speaks**: a few short muttered lines at key moments, some in Portuguese, shown as subtitles.
  - **The core memory the game builds toward: the fight with his father**, the night it all broke, which he has never forgiven himself for. The hopeful ending is making peace with it.
  - **His parents reach out through voicemails in Portuguese** (subtitled): his mother's messages on dead payphones and old station phones; his father never calls until the very end.
  - **Nobody else appears in person**: only voices (dispatch on the radio, messages, recordings). He's alone, as he always was.
- **Tester fixes and the journal (2026-10-29):**
  - **Torch battery:** full strength down to **20%**, then it **dims steadily** (a slight stutter) until it goes out at zero. The moment it drops below 20%, Sousa thinks: **"I should switch out the battery..."** (or, with no spare, **"The battery's dying... I need to find another one."**).
  - **The journal** (press **J**; D-pad right on a controller; also from the inventory, where "[J] JOURNAL" is shown): a worn notebook page titled "Notes", cream paper with ruled lines and a red margin, in blue-black ink, newest thought at the top. **First-person hints, no waypoints**, written as Sousa finds things or comes close to them (the fuse box, fuses, the cabinet key, the staff room, the train and its reverser key; the generator, jerry cans, the spark plug, the office key, the emergency latch; the sewers). Thoughts that no longer matter are greyed and struck through. A new thought shows "New note in your journal" on screen with the director's **NewNoteUpdate** sound; opening the journal plays **JournalMenuFX** (both in `Audio/SFX/UI`). The game pauses while it's open; it's kept in saves. All lines are drafts for the director to rewrite (they're in `JournalDirector.cs`).
  - **Lockers and the pause menu:** resuming while hiding no longer steps you out of the locker (the click on "Resume" was also counting as Use).
  - **Frankie can't camp the stairs:** if the player stays somewhere he can't walk (up on the street) for **20 s** while he's within 30 m, he backs off into the dark; once the player can't see him (or after 15 s) he's gone, and turns up at the furthest hideaway in the same station (deep tunnels, the service level), roaming again.
- **Fall damage (built 2026-10-28):** falls up to **3 m** are safe (platform to tracks, crates). Higher: **one hit** (the jolt, red flash and pain grunt of Frankie's swipe; painkillers heal it), a heavy **thud**, and a **2-second limp** (he starts at 45% speed and eases back). Above **7 m** he **dies**. Landing in water at least **waist-deep (0.8 m)** breaks any fall (the sewers' deep water). Teleports and scripted moves never count as falls.
- **Canal St fixes and the parked train (2026-10-28):**
  - **The parked train:** the north-east tunnel (left of escalator 1) runs **50 m further** (to x 145), and **three intact carriages** stand in it, east of the ceiling collapse. Every doorway is shut (grey sliding doors with dark windows; the two outer ends have closed steel doors) except **one pair of side doors forced half open** on the north side of the middle car (about a 1 m gap, the leaves wrenched crooked). **Vault up into it from the track bed.** Dim orange **emergency strips** still burn in each car. At the far east end, behind the driver's cab partition (its door open), **the fuel can lies on the cab floor by the controls** (it used to be in the station master's office; the generator log says so). **Frankie can follow**: he leaps in through the forced door and walks the whole train into the cab.
  - **Ticket machines** on the mezzanine face out into the hall (they faced the wall, so their text read backwards).
  - **The yellow-and-black paper bag litter is removed and must never be used again.**
  - **No note mentions Frankie** (no confirmation he's real): the squatters' fuse note now reads "Took the fuse so the maintenance guys can't turn the lights back on. If the power comes back they'll clear us out. We've got nowhere else to go."; the other: "Day 12 down here. So cold the candles barely burn. Ray went up the stairs to look on Tuesday. He never came back." **Ray's jacket and shoes are removed.** The departure train's cab note no longer mentions Dale ("went home with the day driver again").
- **Station 1 around death (built 2026-10-28):**
  - **Eddie Russo's memorial** on the platform's back wall, between the benches: a faded photo, a card ("IN MEMORY OF EDDIE RUSSO, 1951 - 1995. 31 years on Line 4. Killed by a faulty third rail they never fixed. We won't forget you, Eddie. - The night crew"), four burnt-down candles and two wilted bouquets on the floor. A real death caused by neglect.
  - **A faint heartbeat** (made with ffmpeg, ~70 bpm) under the held breath whenever you hide in a locker.
  - The flowers are simple shapes until a model is found. All text is a draft.
- **The family photo (built 2026-10-28):** always in Sousa's inventory, first in the list: "Family photo. Sunday lunch, years ago. I don't remember the last time we sat like this." It is the **director's own family photo**, shrunk to a PS1-style 112 x 98 picture with 40 colours so the faces are soft blocks (`Assets/Art/Textures/Personal/FamilyPhoto.png`; the original is not in the project). In the inventory it faces you and sways gently instead of spinning. **Every voicemail he lets ring out without answering fades it a little** (15% each, up to 75%; saved); answering that phone later brings it back.
- **Phones for now: scary sounds, no story (2026-10-29).** The director will write the family's messages himself later, so the placeholder lines are gone. The phones still **ring** and can still be **answered**, but each answer plays a **random scary sound** (an answering machine voice, telephone gibberish, sickly breathing, a baby crying, a woman crying; and only **5%** of the time a voice saying "alo, alo"). No subtitles, and it **counts for nothing**: not reaching out, and letting it ring out doesn't fade the photo. Answered phones ring again the next time you come close.
- **Voicemails (built 2026-10-28):** phones around the world **ring faintly** (within **8 m**, for up to **20 s**; ignored, they ring again the next time you come close) with messages from Sousa's family. Answering is **optional and easy to miss**; each one listened to is saved and will count toward the hopeful ending. **Two per station.** Lines appear as subtitles, **Portuguese first, English underneath** (a recorded voice can be added to each phone later). Mostly his **mother**, sometimes his **sister**; his **father only at the very end**. All lines are **drafts** for the director to rewrite.
  - **Hollis St 1, the street payphone** by the bus stop (it used to play the transit company's outage message): **his mother** ("Filho, sou eu, a mãe...").
  - **Hollis St 2, an old green maintenance phone on the squatter camp's north wall** (hidden): **his sister** ("Sou eu. A mãe pediu-me para te ligar...").
- **One fear per section (decided 2026-10-28):**
  - **Station 1, Hollis St: death.** The **Ridgeback** (undead, shambling on forever) hunts you from the start.
  - **Station 2, Canal St: spiders.** The Wrapped and eyeball / spider creatures, webs thickening the deeper you go.
  - **Station 3, the sewers: drowning / water.** Rising water, flooded tunnels, things moving under the surface.
  - **Station 4: losing your mind.** Derealisation made real: the station repeats, rooms rearrange, you can't trust what you see. The bridge into his own fear.
  - **The end: being forgotten**, Sousa's own fear.
- **The two endings are decided by reaching out:** optional moments of connection along the way (answering the calls, finding and keeping pieces of an unsent letter, and the like). Enough of them and **his parents** remember him: he walks out at dawn. Too few and he's forgotten and becomes one of the shadow figures.
- **The whole game is an allegory (decided 2026-10-28):** the metro is **Sousa's deep psyche**. Going down the line is searching through it; each station is a buried fear or wound; restoring the power is **working through his traumas bit by bit, in the dark, by himself, alone, as he always was**. He's a maintenance worker repairing his own mind. The title carries it too: the last stop is where he either gets off or doesn't.
- **The ending is "get better" or "die":** in the hopeful ending he climbs out, remembered by his parents; in the bleak one he walks off into the dark and becomes a ghost, one of the shadow figures (suicide, shown through that walk into the dark).

---

## 2. Player and controls (Built)

### Movement

- **Walk** 2.3 m/s.
- **Sprint** (hold Shift) 4.3 m/s.
- **Crouch** (C, a toggle) 1.3 m/s. Crouching makes footsteps half as loud, lowers the body to 1.1 m, and won't let you stand up under something low.
- **Jump** (Space): a short, heavy 0.6 m hop.
- **Stamina:**
  - Sprinting lasts about **8 s**. Then you're **winded** and can only jog at **3.0 m/s** until 40% has come back (about 3 s). It's full again after 8 s.
  - There's no stamina bar: heavy breathing builds up as stamina drains.
- **Vault:**
  - Hold Jump while facing a ledge between **0.5 and 1.4 m** high (the platform edge from the tracks, crates, low walls).
  - It takes **0.7 s** and makes noise (heard 15 m away).
  - It's impossible while winded.
- **Lean** (Q / E, hold).
- **Look back** (hold Left Alt): your head swings round to look behind you while you keep running forward.

### Other controls and survival rules

- **Use / interact:** Left mouse (Amnesia-style). A centre dot brightens when you can use what you're looking at.
- **Flashlight** (F):
  - It works like a real torch: a narrow bright hotspot plus a wide soft spill.
  - The battery lasts **5 minutes**, dims and stutters below 20%, and dies at 0.
  - **H** swaps in a spare battery. You start with one spare, and spares are scarce.
- **Road flares** (G):
  - G strikes one and holds it low, giving a bright, flickering red light for 30 s. G again drops it, still burning.
  - **Frankie keeps about 6 m away from any lit flare.**
- **Painkillers:** used from the inventory, they heal one hit.
- **Health:** **two hits kill you.**
  - While you're hurt, the screen edges stay red and you breathe hard.
  - Death fades to "YOU DIED" and returns to the title.
- **Hiding lockers:**
  - Step in and peek through the slats; you can turn about 55° each way, and crouch or stand inside.
  - Hiding only works if you broke Frankie's line of sight first.
- **Notes, posters and newspapers** show their text just by looking at them.
- **Controller support** has a fixed layout, and on-screen prompts switch to controller buttons. Keyboard keys can be rebound.

### Weapons

- **None currently.** A shotgun with very rare shells was built (it stuns but never kills), but it was switched off because it's hard to balance in this kind of game. The code is kept.
- The director prefers **useful consumables and collectibles** instead.

---

## 3. The threat: Frankie (Built)

**Frankie** (the squatters' name for him) now has the body of **the Ridgeback** (by Codyanka, since 28 October): a 2.0 m, pale, skeletal undead man, with his own idle, hunched walk, frantic chase, "notice" (his scream) and claw attack.

**History:**
- Earlier placeholders were tried and dropped: BagMan, then "The Wrapper", then "The Pale Man", then the "Big Frankie" model (24-28 October).
- On 28 October the Ridgeback's body replaced Big Frankie's; the name Frankie, the AI and the sounds stayed.
- The stalker code doesn't depend on any one model.

### Senses

**Sight depends on light.** He sees you:
- at about **4 m** in pitch black with your torch off;
- at about **6 m** by a candle or a red emergency lamp;
- at **15 m** under working lamps;
- at **25 m** with your torch on.

He only sees what's in front of him with a clear view.

**Hearing.** He hears:
- running within 15 m;
- walking within 6 m;
- crouching not at all;
- a struck flare within 8 m;
- a vault within 15 m.

### Moods

- **Wander:** 0.9 m/s, sometimes drifting toward you.
- **Investigate** noises.
- **Chase:** 3.5 m/s (your sprint is 4.3, but stamina runs out).
- **Search** where he lost you.
- **Attack:**
  - Within 2 m he **lunges about 1.5 m forward**, and the claws land after **0.5 s**.
  - The claws only catch you **in front of him** (a 50° cone), and once he starts he turns only slowly.
  - A sideways dodge early in the lunge makes him miss. Running straight away does not.
  - After a hit he takes a 3 s breather. After a miss he comes straight back.

### Navigation

- He jumps between the platforms and the tracks anywhere along the edges, in a short arc.
- He walks staircases and doorways using special routes.
- He follows you to lower levels only while their door is open.
- He never goes up to the street, which is safe.

### Lockers

- If he saw you within the **last 3 s of a chase**, he counts you as seen getting in.
- Then he **breaks in**: he slams the door three times (the view jolts), tears it open, drags you out and attacks.

### Presentation

- **First sighting** is staged: he stares from under a lamp, a jump-scare sting plays, and he walks off into a tunnel before he starts hunting.
- **Torch flicker:** when he's within 10 m and out of view, your torch stutters.
- **Sounds:**
  - a real monster **scream** when he spots you;
  - a distant echoing scream when he wakes;
  - wet breathing, bare fleshy footsteps, and joint cracks while he moves.
- **Chase music** plays only during chases.

---

## 4. World and levels

### 4.1 Street — "Hollis St" (intro, Built)

- **Setting:** about 3 a.m. in light drizzle, with a wet road and puddles, during the blackout. Only a few sodium street lamps still burn (some stutter), under faint moonlight.
- **Layout:**
  - One ~90 m block with dead ends: an abandoned bus and barriers to the west, police barriers and a police car to the east.
  - The subway entrance is mid-block, with green globe lamps and a chest-high **rough-stone wall** round the stairwell.
  - 30 steps lead down.
- **Landmarks:**
  - North side: laundromat, VHS rental, "6twelve" store, corner gas station (with police tape).
  - South side: pizzeria and apartment blocks.
  - Also: a bus stop, trees, a dumpster blocking an alley, and graffiti.
- **Start:** beside the maintenance van, whose radio plays dispatch's message.
- **Lore drops** (draft text, which the director will rewrite): the line was run into the ground by cost-cutting.
  - a newspaper on the bus-stop bench: "LINE 4 'HELD TOGETHER WITH TAPE', ENGINEERS WARN";
  - a "closed nights" note on a shop door;
  - the van radio: dispatch sends Sousa down ("Don't be a hero. That line's been falling apart for years.");
- **Unexplained:** blood drips down the subway stairs.
- **Boundaries:** invisible walls show "I should turn around…" when you walk into them. Beyond them, the streets and skyline carry on as scenery.

### 4.2 Station 1 — Hollis St (Built, playable to its ending)

**Layout:**
- A white-tiled corridor leads to a platform with a track pit and puddles.
- Two tunnels (east ~100 m, west ~85 m) each end in a stalled subway car.
- A staff room sits in the east tunnel.

**Goal:** fix the fuse box, which needs **2 fuses**.
- **Fuse 1** is in the **squatter camp on the service level**.
  - The service level is reached through a rusted maintenance door in the west tunnel and steel stairs 8 m down.
  - It's an industrial area: a machinery hall, a storage room and the camp, mostly dark.
  - In the camp: mattresses, candles, a radio hissing static, and notes such as "Took the fuse so the maintenance guys can't turn the lights back on. If the power comes back they'll clear us out. We've got nowhere else to go."
- **Fuse 2** is locked in a blue platform cabinet. Its key is on a bench inside the **west train**, which you climb into from a crate.

**Frankie wakes** 7 s after you first reach the bottom of the service stairs. His shriek echoes from the west tunnel, he appears there, then searches and roams both levels.

**Power returns** (insert both fuses, pull the breaker):
- lamps stutter on in a wave;
- a hum fades in and a music sting plays;
- the staff room's electric lock opens;
- the game autosaves.

**Staff room** (safe room):
- a 90s PC as the **save point**;
- a desk drawer with the **reverser key**.

**Finale:**
1. Power the **east train's** cab controls and use the reverser key.
2. The engine noise brings Frankie charging.
3. **Hold Use on the dead man's handle for 10 s in total** while he lunges in at the door. Letting go pauses the countdown, which gives you time to strike a flare.
4. The train pulls away, and the "END OF STATION 1" card appears.

**Service-level atmosphere:**
- footsteps crossing the floor above;
- a cough from the empty camp;
- a whisper on the stairs;
- a **knocking pipe** that knocks back;
- a chair and a candle that move when you're not looking.

**One-time scares:**
- a train rumble with dust;
- lamps popping out behind you after the power returns.

### 4.3 Station 2 — Canal St Junction (Built, playable to its ending)

**Look:** a busy interchange everyone fled in a panic.
- An 80 m white-tiled hall with a blue band, an island platform between two tracks, and columns.
- Escalators lead up to a mezzanine with ticket gates and a ticket booth.
- Litter is kicked about the floor (nothing standing neatly).
- Unopened soda cans are left on benches, and two chip bags.
- Bins are knocked over, cones and barriers kicked aside, and old ads and Line 4 notices hang on the walls.
- There's a little graffiti and a blood drag trail.

**Arrival:**
1. The train ride fades to black. Your car **stalls about 80 m short** of the station.
2. You walk the dark track toward red emergency lamps.
3. About **60 s** later, a thud on the stalled train's roof: Frankie has followed you.

**Goal:** restart the **backup generator** (in a service room off the mezzanine). It needs:
- **2 fuel cans**: one in the driver's cab of the train parked in the north-east tunnel (get in through a forced side door), one in the stores cage on the lower level. The station master's office is still the save room (a candle and a terminal).
- **a spark plug**, locked in the office. The office key is on the ticket booth's back counter.

The first pull of the starter cord only coughs (Frankie may hear it). The second roars into life.

**Lower level** (about twice the size of Station 1's, more "backrooms"):
- You reach it through a maintenance door in a tunnel, then steel stairs.
- A **maze of narrow service corridors** leads to:
  - a pump room;
  - an electrical room with a caged transformer;
  - a storeroom;
  - a 32 × 26 m **machinery hall** (generators, a turbine, a fenced pump bay, a caged transformer bay, the stores cage);
  - the **latch room**.
- It's industrial throughout:
  - pipes along every ceiling, connecting logically into walls, machines and the ceiling;
  - cable conduits ending in junction boxes;
  - control boxes with red and green lamps;
  - grey cabinets and chain-link cages.
- Lighting is **mostly dark**, with dim caged bulbs.
- Floors are grimy concrete.
- Three hiding lockers.

**Ending:**
1. Starting the generator restores Canal St's own power and releases the **emergency latch** in the latch room floor.
2. Climb down, and the "END OF STATION 2" card appears.
3. The latch is meant to lead to **Station 3, the sewers**. The street shutter at the top of the escalators is jammed for good.

**One-time scares:**
- a **phone rings** in the ticket booth. It stops dead when you walk up to it; answer it and there's only wet breathing, then a click;
- lamps die behind you as you climb the escalators;
- a **ceiling collapses** a few metres ahead in a tunnel and leaves rubble you must vault or walk round;
- on the lower level, a **locker bangs from inside** (it's empty);
- a corridor's **bulbs go out one by one toward you**, the last right overhead.

### 4.4 Station 3 – the sewers (Planned 28 October, not built)

- **Fear: drowning / water.** Reached by climbing down Canal St's **emergency latch** into the overflow tunnels under Line 4.
- **Size:** like Canal St, about 1-1.5 hours: a **pumping station hub** plus **three branches**.
- **Goal: drain the flood.** The sewers are flooding. **Open three valves, one in each branch.** Each valve lowers the water a little, which opens the way into the next branch. Then **restart the main pump** in the hub.
- **Water in play: it rises and falls.** Some areas are knee- or waist-deep: wading is slow and loud. Deep water can't be walked through, only crossed on ledges and gantries. Each valve, and finally the pump, lowers the water and opens new paths.
- **The danger is the water itself (no creature for now):** deep water drowns you if you stay under too long, with the breath shown **by sound only** (no meter on screen). Strong currents in some channels push you, and falling in is the main threat. **Frankie does not follow** into the sewers. A **new, water-based monster** will be added later by the director.
- **Exit: the river outfall.** Once the pump runs, the overflow tunnel drains and leads out to the river, as the latch notice says. Station 4 (losing your mind) starts back underground somewhere else.
- **Voicemails (2):** an old wall phone in the **pump control room** (visible), and an emergency phone **deep in a flooded branch** (hidden, reachable only after draining it). Who speaks is still to decide (his father stays for the very end).
- **Assets to use:** the Sewer pack's textures already in the project (brick, dirty concrete, rusty metal, mud), the industrial kit from the lower levels (pipes, pumps, control boxes, chain-link, caged bulbs), and the water and splash sounds.
- **First draft built (2026-10-28):** climbing down Canal St's emergency latch now fades out and leads into the sewers (replacing the "End of Station 2" card; it autosaves "Autosave - The Sewers"; Frankie stays behind). A walkable greybox in sewer brick, about 20 m below and beyond z 600:
  - **arrival chamber** with a ladder up to the shut hatch;
  - **main tunnel**: a walkway beside a knee-deep channel;
  - **pumping-station hub** (16 x 16 m, 6 m high): a central sump of water, the main pump (placeholder drum and motor), a control room in the north-west corner;
  - **west branch** (knee-deep), **east branch** (a step down, waist-deep), **north branch** (deep, crossed on a narrow ledge along the west wall), each ending in a **valve room** with a red valve wheel (placeholder);
  - beyond the north valve room, the **outfall tunnel**, barred by a grate with night and moonlight beyond;
  - dim caged bulbs, a red emergency lamp at the arrival, the sound of running water.
  - **Not working yet:** valves and pump do nothing, the water doesn't drop, wading isn't slowed, deep water can't drown you (the ledge isn't needed yet), no voicemails, no creature. Some walls overlap where rooms meet (identical brick, no visible flicker; to tidy later).
- **Open questions:** the branches' themes and layout, how the water level is shown and animated, what a valve looks like and how it's turned, the drowning rules (how long you can hold your breath), and the new monster.

### 4.5 Station 4 – the unfinished station (Planned 28 October, not built)

- **Fear: losing your mind**, derealisation made real. It's the bridge into Sousa's own fear (being forgotten) and the ending.
- **Place: an unfinished station** on a cancelled line extension, abandoned half-built (more cost-cutting). Sousa reaches it underground after leaving the sewers at the river outfall.
- **Look:** bare grey concrete, plastic tarps moving in a draught, scaffolding, work lights on stands, half-tiled walls. Liminal and cold.
- **How the station breaks the rules:**
  - **Looping corridors:** walk a corridor and come out where you started, subtly changed each time (a sign, a light, a stain).
  - **Rooms change behind you:** turn around and a doorway is gone, or a room has rearranged.
- **Goal: collect 3 old photos** of Sousa growing up, scattered through the loops. Each is a memory and fits the "forgotten" theme. **All three are needed at the end:** the final way out (into the ending) only opens once all three are found. They **do not count toward the ending**; only the voicemails decide that.
- **Threat: only the shadow figures**, more often and closer than before. Never a chase: pure dread before the ending.
- **Open questions:** the layout and how many loops; what changes on each loop; where the photos are and what each one shows; how the end door looks; any voicemails here (two per station so far; his father is saved for the very end).

### 4.6 The ending zone (Planned, not built)

- The ending: being forgotten, Sousa's own fear, and the two endings.

---

## 5. Systems (Built)

- **Saving:**
  - Save at **safe-room PCs** (the staff room and the station master's office), plus an **autosave** at key moments: power restored, arrival at a new station.
  - There are **5 named save slots** and one autosave slot. Load from the title screen or the pause menu.
  - A save keeps your position, items, battery, health, fuses and power, picked-up items, open doors, which scares have happened, and Frankie's state.
- **Inventory** (Tab; the game pauses):
  - a turning 3D preview of the selected item;
  - a list with counts and descriptions;
  - battery, fuse and power status.
- **Menus:**
  - Title screen: a slow, swaying view down a tunnel at a stalled train's flickering cab light.
  - Pause menu, Options, Credits, Quit.
  - Options: volumes; look sensitivity and invert Y; brightness; PS1 filter strength; field of view (default 90°); held-item field of view; item glow; centre dot; fullscreen and vsync; key bindings; reset.
  - Menus use a PSX-style UI kit (wine-coloured panels) and a VCR on-screen-display font.
- **Item glow:** key items (fuses, batteries, keys) give off a faint, breathing glow on their own surface so they can be spotted in the dark. It can be turned off.
- **Sound:**
  - each area has its own background sound, crossfading as you walk (rain on the street; low drones in the station, tunnels and service levels);
  - distant noises underground every 1–2 minutes;
  - footsteps change with the surface (gravel, puddles, concrete);
  - machine hums;
  - music used **rarely**: chases, big reveals, power restored, credits.

---

## 6. Look and feel rules

- **Lighting:** pools of light with near-black darkness between them.
  - Stations: cold, greenish fluorescent tubes.
  - Street: warm sodium-orange lamps.
  - Tunnels: red emergency lights.
  - Lower levels: dim caged bulbs.
- **Fog:** everything fades to black over about **25 m**.
- **Surfaces stay mid-bright** (darkness comes from missing light, not black textures), so the flashlight always has something to show.
- **Scares:** used sparingly and **one time each**. Graffiti and blood are kept sparse too.
- **Art sources:**
  - Free and licensed PSX-style asset packs from the director's own library; nothing is modelled by the assistant.
  - Some packs may **not be shared or redistributed**, so the project repository is private.
  - Every pack is credited in the game's Credits screen.

---

## 7. Development history (how we got here)

All of this happened between **20 and 28 October 2026**.

1. **Day 1 — foundations (20 Oct):**
   - an empty Unity project, then a "look test": a tiled corridor, a platform, lamps, puddles, and a player with a realistic flashlight;
   - movement (sprint, crouch, jump);
   - the first core loop: fuse box, pickups, staff room, battery, shotgun with first-person arms;
   - long tunnels ending in stalled trains;
   - the street intro with lore drops.
2. **Day 2 (21 Oct):** street polish, the cabinet-key puzzle, an enterable train, Use on left mouse, leaning, and the PS1 screen filter.
3. **Day 3 (22 Oct):** item glow, climbing onto crates, better rain, softer lighting.
4. **Day 4 (23 Oct):**
   - **sound pass** (area ambience, distant noises, surface footsteps);
   - **shotgun switched off**;
   - graffiti and blood;
   - **road flares and painkillers**;
   - **inventory screen**;
   - **title, pause, options and credits menus**;
   - **key rebinding and controller support**;
   - the first stalker (**the Pale Man**).
5. **Day 5 (24 Oct):**
   - stalker polish: staged first sighting, **hiding lockers**, torch flicker;
   - **saving and loading**;
   - the **Station 1 ending** (the train finale);
   - **Frankie replaces the Pale Man**;
   - an **atmosphere pass** of one-time scares;
   - the **service level** under the west tunnel.
6. **Day 6 (25 Oct):** street fixes, the look-back control, the first playtest fixes, square steel stairs, machine sounds.
7. **Day 7 (26 Oct):**
   - **Station 2, Canal St Junction**, first playable version;
   - a more dangerous Frankie (light-based sight, no giving up after one hit);
   - dressing Canal St as a station fled in panic;
   - real monster screams and player breathing;
   - street lamp fixes.
8. **Day 8 (27 Oct), friend playtest round 1** ("too easy, he never hit me"):
   - Frankie now **lunges**, jumps platform edges anywhere, and comes straight back after a miss;
   - **sprint stamina** added.
9. **Day 8, round 2:**
   - Canal St got bigger: longer tunnels and a **lower level** with an industrial "backrooms" look;
   - the **emergency latch ending**;
   - Frankie wakes earlier at Station 1 and searches both levels;
   - he can **break into lockers**;
   - lockers moved to the lower levels;
   - two-sided chain-link fences;
   - control boxes instead of fake fuse boxes;
   - a **playtest build zip** was sent to friends.
10. **Day 8, later:**
    - street fixes (dumpster, tree collisions only at the trunk, bus shelter);
    - **vaulting**;
    - crouching inside lockers;
    - PA announcement switched off (to be re-recorded);
    - **five Canal St scares**;
    - the Station 1 shadow-figure scare removed;
    - a stone wall round the street stairwell;
    - concrete lower-level floors;
    - a **pipe and machinery audit** (correct textures, logical connections);
    - messier litter.
11. **Day 9 (28 Oct):**
    - stairwell gaps closed with grey paving;
    - Frankie slower (3.5 m/s chase) and sprint longer (8 s);
    - every pipe fixed (they were drawn inside out), hung from hangers or strapped to walls, no loose ends;
    - no litter on the tracks, chip bags only on two benches;
    - **the story was rewritten**: fear made flesh, one fear per station, J. Sousa, the metro as his psyche, two endings;
    - **shadow figures** (the entity) built;
    - **the Ridgeback** became Frankie's body.

---

## 8. Open questions and to-do

**Design questions still open:**
- Before release: a **content note** at the start (suicide and self-harm themes) and a support line in the credits; the director writes the wording.
- How many safe rooms per station, and what they look like.
- Station 3 (the sewers), Station 4 and the ending: layout, goals, new scripted horrors.
- What, if anything, is behind the haunting (deliberately never answered).

**Content still missing:**
- web and cobweb models;
- a recorded **PA announcement** by the director;
- footsteps on metal grates;
- real breathing and footstep sounds for Frankie.

**Parked:**
- the shotgun (built, but switched off);
- any other weapons (none planned).

**Next likely steps:**
- a fresh playtest build with all the latest changes;
- then Station 3.

---

## 9. Technical notes (for context only)

- Levels are **generated by editor scripts**: code builds the walls, floors, props and lights, so a level can be rebuilt at any time. Hand edits to the scene would be lost.
- Walls and floors are boxes whose textures line up across pieces. An automatic check makes sure no surfaces overlap and flicker.
- Frankie uses Unity's navigation mesh, with custom routes for stairs, doorways and platform-edge jumps.
- Testing is done by an assistant that drives the Unity Editor from the command line: it enters Play mode, moves the player, runs game code and takes screenshots.
- Version control: Git with large files in Git LFS. The repository is **private** because some asset licences forbid sharing.
