using UnityEngine;

// Sousa's family reaching out: phones around the world that ring faintly as he passes (VoicemailPhone).
// Answering is optional, and each message listened to counts toward the hopeful ending. Two per station.
// All lines are DRAFTS for the creative director to rewrite (Portuguese first, English subtitle under it);
// a recorded voice can be dropped into each phone's 'voice' later.
public static partial class LookTestBuilder
{
    static void BuildVoicemails()
    {
        // ---- Hollis St 1: the street payphone by the bus stop (visible), his mother.
        var payphone = GameObject.Find("Payphone");
        VoicemailOn(payphone, "Hollis_Payphone_Mother", "Mãe",
            new[]
            {
                "Filho, sou eu, a mãe. Já nem sei se este número ainda é teu...",
                "O teu pai não diz nada, mas pergunta por ti. À maneira dele.",
                "Liga-me quando puderes. Só para eu saber que estás bem. Beijinhos.",
            },
            new[]
            {
                "Son, it's me, Mom. I don't even know if this number is still yours...",
                "Your father doesn't say anything, but he asks about you. In his own way.",
                "Call me when you can. Just so I know you're okay. Love you.",
            });

        // ---- Hollis St 2: an old maintenance phone on the squatter camp's north wall (hidden), his sister.
        float f = SvcFloor;
        var camp = new GameObject("CampPhone").transform;
        camp.SetParent(root);
        var green = Mat("MaintenancePhone", null, new Color(0.22f, 0.3f, 0.24f), 1f, 0.35f, 0.4f);
        var black = Mat("PhoneHandset", null, new Color(0.08f, 0.08f, 0.08f), 1f, 0.4f, 0.1f);
        Vector3 wall = new Vector3(-35.5f, f + 1.45f, 52f);   // on the wall's face
        var body = Prim(PrimitiveType.Cube, "WallPhone", camp, wall + new Vector3(0f, 0f, -0.045f), new Vector3(0.22f, 0.3f, 0.09f), green);
        var handset = Prim(PrimitiveType.Cube, "Handset", camp, wall + new Vector3(-0.15f, 0.02f, -0.06f), new Vector3(0.06f, 0.26f, 0.06f), black);
        Object.DestroyImmediate(handset.GetComponent<Collider>());
        var cord = Prim(PrimitiveType.Cylinder, "Cord", camp, wall + new Vector3(-0.12f, -0.22f, -0.06f), new Vector3(0.015f, 0.12f, 0.015f), black);
        Object.DestroyImmediate(cord.GetComponent<Collider>());
        body.GetComponent<BoxCollider>().size = new Vector3(2.2f, 1.2f, 1.6f);   // easier to aim at (covers the handset)
        VoicemailOn(body, "Hollis_Camp_Sister", "Irmã",
            new[]
            {
                "Sou eu. A mãe pediu-me para te ligar, por isso não te armes.",
                "Fizeste anos e nem atendeste. Ela fez bolo na mesma.",
                "Não sei o que se passa contigo, Jota. Mas fugir não resolve nada.",
            },
            new[]
            {
                "It's me. Mom asked me to call you, so don't get any ideas.",
                "It was your birthday and you didn't even pick up. She baked a cake anyway.",
                "I don't know what's going on with you, J. But running away doesn't fix anything.",
            });
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
    }
}
