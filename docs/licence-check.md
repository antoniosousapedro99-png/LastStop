# Licence check (2 November 2026)

Every pack that `Assets/Art/CREDITS.md` marked "to confirm" (45 of 60 entries), sorted by what's left to do.
Checked against the asset library (licence and readme files inside the downloads, details stored inside the
model files) and Sketchfab's public listing. **Nothing here is legal advice**: when a release gets close, read
each pack's own page yourself.

What the licences mean, in short:

- **CC Attribution (CC BY 4.0):** free to use in any game, paid or not, and to change. You must credit the
  author (name, a link to the model, the licence name, and that you changed it if you did).
- **CC Attribution-ShareAlike (CC BY-SA 4.0):** the same, plus: if you share the *changed model itself*,
  it must be under the same licence. Shipping it inside a game is normally fine; just don't sell or hand out
  the model file on its own under other terms.
- **Sketchfab Standard / Free Standard:** use in your projects, including commercial ones; no credit
  required; you may not redistribute the model files themselves.
- **Pixabay Content Licence:** free, including commercial use; no credit required; you may not sell or
  hand out the sound on its own, unchanged.

## 1. Confirmed now: Sketchfab models (credit needed)

| Pack (CREDITS.md) | Author | Licence | Notes |
|---|---|---|---|
| Dirty Locker (256x256 Retro) | xylvnking | CC BY | we changed it (door split, peek window, shelf removed): say so in the credit |
| PSX Public Payphone | ZwiebelGames | CC BY | |
| Low Poly PSX Style Soviet Subway - Metro Props | keywizzz | CC BY | changed (ladder half, sign flipped) |
| Dry flower | kayaaku | CC BY | changed (simplified 490k to 12k triangles) |
| PSX Tower Key – Rusty Radio Tower Key | ZwiebelGames | CC BY | |
| PSX Low-poly Shopping Cart | korkskrew2000 | **CC BY-SA** | the only ShareAlike one: fine in the game, see above |
| 90's Radio - PSX | rhv8 | CC BY | matched by the download's name (`90s-radio-psx.zip`) |
| Low poly Psx Wall Lamp | SaukrDev | CC BY | |
| PSX Style Modular Vents | tophatpanda | CC BY | read from inside the model file; changed (split into pieces) |
| PSX Industrial Pack | **Tomitos_ (CC BY) or domi236 (Sketchfab Free Standard)** | either is fine for the game | two models share the name; open both pages and see which has the TransformerBox, rocks, crates and pipes we use |

## 1b. Confirmed now: itch.io pages

| Pack (CREDITS.md) | Page | Licence | Credit needed |
|---|---|---|---|
| The Ridgeback | codyanka.itch.io/the-ridgeback | **CC0** (public domain: anything goes) | no |
| PSX Derelict Furniture | daniel-jurys.itch.io/psx-derelict-furniture | **CC0** | no |
| PSX Style Cars | ggbot.itch.io/psx-style-cars | **CC0** | no |
| FREE PSX UI | rohhsa.itch.io/free-psx-ui | **CC0** ("copyright free + royalty free") | no (appreciated) |
| Helton Yan's Surreal Drones | heltonyan.itch.io/surrealdrones | **CC BY 4.0** | **yes** ("Helton Yan") |
| PSX Sewer Pack | tillion.itch.io/psx-sewer-pack | own terms: personal and commercial use, may modify, **no redistribution or resale** | no (appreciated: "PSX Sewer Pack by Tillion") |
| OFFICE pack | probably amos-makes.itch.io/psx-office-pack (contents match) | **CC BY 4.0** | **yes** ("Amos") |

Found on itch.io but **no terms written on the page** (ask the author, or treat as "use in your game, don't
redistribute the files"): 3D Retro Plumbing, Wiring & Machinery (chilly-durango.itch.io), Retro Tree Pack
(pizzadoggy.itch.io), PSX Soda Can Pack (sakepella.itch.io).

## 2. Pixabay sounds (very likely fine, no credit needed)

Downloaded from Pixabay (the file names carry the uploader and Pixabay's number), so they come under the
Pixabay Content Licence. To be thorough, open each page once (search the number on pixabay.com) and confirm
it's still listed.

- Phone scare sounds: answering machine 26679, telephone gibberish 99627, "alo alo" 94693, sickly breathing
  83152 (freesound_community); female crying 291098 (universfield); baby crying 434113 (u_ak0pt95kqu)
- Underground air conditioner 32100 (freesound_community)
- Breathing 6811, heavy breathing 14431, male breathing slow 14558 (freesound_community)
- Swimming sounds 331502 (Dragon Studio), dripping water in a cave 114694 (Solarmusic), rain sound 550289
  (SoundReality)

## 3. Sounds you provided: only you know where they came from

Journal sounds (NewNoteUpdate, JournalMenuFX), generator sounds (GeneratorFailStart1,
GeneratorWorkingDroneLoop), MetroDoor, FrankieWalking, and Frankie's two screams ("Frankie Scream Close",
"Frankie Scream Far away Echo"). If any came from Pixabay or similar, note the page; if you recorded them,
they're yours.

## 4. Known author, terms still to read on their page

| Pack | Where | What to check |
|---|---|---|
| VCR OSD Mono (font) | Riciery Leal, dafont | dafont lists it "100% Free" (personal and commercial); keep the credit |
| FoxTex texture pack | foxtex.com | the site's terms for the slab floor and water frames |
| Shotgun PS1 V2.0 | readme is empty | only matters if the shotgun comes back (it's switched off) |

## 5. No author, no licence file: find where you downloaded them

Most of your assets came from itch.io. A search there by name didn't find these with any certainty (several
packs share a name, or none matched). The quickest way: on itch.io, open your profile's **My Library** (free
downloads are listed there if you were signed in), find each pack and copy its licence line here.

Used in the game: **Metro PSX** (station textures, benches, vending machine: the biggest one; candidates
heyheythere's "PSX Metro Kit Free" and anastasismarinos' "[PSX] Metro Scene"), Survival pack, Trash and Debris,
Retro PSX Trash bag, PSX First Person Arms, The Man Who Never Clocked Out (shadow figures), the street packs
(Gas Station, 6twelve, VHS Shop, Laundry, Pizzeria, Buildings, Bus Stop), URBAN pack, All Bushes pack,
Industrial exterior v1 (crates, fences, pipes), NEUROZAM-9 PSX Pill Bottle, Mine pack (petrol cans), Cones &
Barriers, the blood textures and the four graffiti tags.

## 6. Not used any more

TheWrapper V2, Characters PSX 1.2 (the Pale Man) and Big Frankie aren't in the game since October. Unity only
puts files into a build when the game uses them, so they need no licence; removing them from the project
before release would make that certain (ShadowFigureModel still points at Big Frankie, unused).
