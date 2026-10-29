using System.Linq;
using UnityEngine;

// Sousa's family reaching out: phones around the world that ring faintly as he passes (VoicemailPhone).
// Answering is optional, and each message listened to counts toward the hopeful ending. Two per station.
// The family's messages will be written by the creative director; until then every phone plays a random
// scary sound when answered (no text, no effect on the ending). Portuguese first, English subtitle under it,
// once written; a recorded voice can be dropped into each phone's 'voice' later.
public static partial class LookTestBuilder
{
    static void BuildVoicemails()
    {
        // ---- Hollis St 1: the street payphone by the bus stop (visible), his mother.
        var payphone = GameObject.Find("Payphone");
        VoicemailOn(payphone, "Hollis_Payphone_Mother", "Mãe", new string[0], new string[0]);

        // ---- Hollis St 2: an old maintenance phone on the squatter camp's north wall (hidden), his sister.
        float f = SvcFloor;
        var camp = new GameObject("CampPhone").transform;
        camp.SetParent(root);
        // The wall telephone from the "Low Poly PSX Soviet Subway Metro Props" pack (Sketchfab), in its own flat
        // colours (it has no textures): dark grey, red buttons, a pale display, a black handset.
        var body = Model(ArtDir + "/Props/WallPhone/WallPhone.fbx", null, camp, null, 1.2f, -90f);   // its keypad faces into the room
        body.name = "WallPhone";
        Color[] colours = { new Color(0.13f, 0.13f, 0.13f), new Color(0.35f, 0.05f, 0.04f), new Color(0.75f, 0.74f, 0.7f), new Color(0.03f, 0.03f, 0.03f) };
        foreach (var r in body.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            for (int m = 0; m < mats.Length && m < colours.Length; m++) mats[m] = Mat("WallPhone_" + m, null, colours[m], 1f, 0.35f);
            r.sharedMaterials = mats;
        }
        var b = Bounds(body);
        StandOn(body, new Vector3(-35.5f, f + 1.3f, 52f - b.extents.z));   // flat against the wall's face
        FitBoxCollider(body).size *= 1.4f;   // a little bigger: easier to aim at
        VoicemailOn(body, "Hollis_Camp_Sister", "Irmã", new string[0], new string[0]);
    }

    // Sousa's family photo for the inventory: Sunday lunch years ago (the director's own photo, shrunk to a
    // PS1-style 112 x 98 picture with 40 colours, so the faces are soft blocks). Cream paper on the back, and
    // a pale layer in front that PhotoFade makes more solid for every call he lets ring out.
    static GameObject FamilyPhotoModel(Transform parent)
    {
        var photo = new GameObject("FamilyPhoto");
        photo.transform.SetParent(parent, false);
        const float w = 0.15f, h = 0.13f;
        var front = Prim(PrimitiveType.Quad, "Picture", photo.transform, parent.position, new Vector3(w, h, 1f),
                         Mat("FamilyPhoto", Load<Texture2D>("Assets/Art/Textures/Personal/FamilyPhoto.png"), Color.white, 1f, 0.35f));
        front.transform.localRotation = Quaternion.identity;   // a quad shows its front along -Z, toward the preview camera
        var back = Prim(PrimitiveType.Quad, "Back", photo.transform, parent.position, new Vector3(w, h, 1f), Mat("PhotoBack", null, new Color(0.86f, 0.83f, 0.74f), 1f, 0.2f));
        back.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        back.transform.localPosition = new Vector3(0f, 0f, 0.001f);
        var fade = Prim(PrimitiveType.Quad, "Fade", photo.transform, parent.position, new Vector3(w, h, 1f), TransparentMat("PhotoFadeLayer", new Color(0f, 0f, 0f, 0f), 0.1f));
        fade.transform.localRotation = Quaternion.identity;
        fade.transform.localPosition = new Vector3(0f, 0f, -0.001f);
        photo.AddComponent<PhotoFade>().overlay = fade.GetComponent<Renderer>();
        photo.transform.localRotation = Quaternion.Euler(-8f, 0f, 3f);   // held a little tilted
        return photo;
    }

    // Makes 'phone' a voicemail phone: a faint looping ring at the phone, and the line in Sousa's ear.
    static void VoicemailOn(GameObject phone, string id, string speaker, string[] portuguese, string[] english)
    {
        var bell = phone.AddComponent<AudioSource>();
        bell.clip = Load<AudioClip>(ScareSfx + "/PhoneRing.wav");
        bell.loop = true; bell.playOnAwake = false; bell.volume = 0.45f; bell.spatialBlend = 1f;   // faint
        bell.rolloffMode = AudioRolloffMode.Linear; bell.minDistance = 1.5f; bell.maxDistance = 14f;
        var ear = new GameObject("Earpiece").AddComponent<AudioSource>();
        ear.transform.SetParent(phone.transform, false);
        ear.playOnAwake = false; ear.spatialBlend = 0f;   // in his ear, not in the room
        var vm = phone.AddComponent<VoicemailPhone>();
        vm.id = id; vm.speaker = speaker; vm.portuguese = portuguese; vm.english = english;
        vm.bell = bell; vm.earpiece = ear;
        vm.pickUp = Load<AudioClip>(SfxDir + "/SwitchButton1_On.wav");
        vm.hangUp = Load<AudioClip>(SfxDir + "/SwitchButton1_Off.wav");
        vm.lineHiss = Load<AudioClip>(ScareSfx + "/RadioStatic.wav");
        // Until the director writes the family's messages: a random scary sound each time (see VoicemailPhone).
        const string phoneSfx = "Assets/Art/Audio/SFX/Phone/";
        vm.scares = new[] { "PhoneAnsweringMachine", "PhoneGibberish", "PhoneSicklyBreathing", "PhoneBabyCrying", "PhoneWomanCrying" }
                    .Select(n => Load<AudioClip>(phoneSfx + n + ".ogg")).ToArray();
        vm.rareScare = Load<AudioClip>(phoneSfx + "PhoneAloAlo.ogg");   // only 5% of the time
    }
}
