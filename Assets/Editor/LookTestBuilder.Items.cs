using UnityEngine;

// Consumables: road flares (G strikes one and holds it, G again drops it; 30 s of red light) and
// painkiller bottles (collected only, until the player has health). Two of each in this area:
// flares on the staff locker shelf and deep in the west tunnel; painkillers on the staff desk and
// in the blue platform cabinet. There's no flare model yet, so a flare is a simple red stick.
public static partial class LookTestBuilder
{
    const string PillBottleFbx = ArtDir + "/Props/PillBottle/PillBottle.fbx";

    // The player's side: a hand point low on the right of the view and a hidden lit flare to copy.
    static void BuildFlareGear(GameObject player, Transform cam, PlayerInventory inventory)
    {
        var hand = new GameObject("FlareHand").transform;
        hand.SetParent(cam, false);
        hand.localPosition = new Vector3(0.22f, -0.27f, 0.45f);
        hand.localRotation = Quaternion.Euler(-55f, -10f, 0f);    // burning end tipped up and forward

        var template = FlareModel(hand, lit: true);
        template.name = "FlareTemplate";
        template.SetActive(false);

        var handler = player.AddComponent<FlareHandler>();
        handler.inventory = inventory;
        handler.template = template.GetComponent<Flare>();
        handler.hand = hand;
        handler.strikeSound = Load<AudioClip>(SfxDir + "/MatchStrike1.wav");
    }

    // A road flare about 24 cm long, lying along its local Z with the striker end at +Z.
    // Lit, it gets a glowing tip, a red light and a fizzing hiss.
    static GameObject FlareModel(Transform parent, bool lit)
    {
        var flare = new GameObject(lit ? "Flare" : "FlarePickup");
        flare.transform.SetParent(parent, false);
        var red = Mat("FlareRed", null, new Color(0.72f, 0.1f, 0.07f), 1f, 0.3f);
        var cap = Mat("FlareCap", null, new Color(0.12f, 0.11f, 0.1f), 1f, 0.2f);
        Stick("Body", flare.transform, 0f, 0.2f, 0.034f, red);
        Stick("Cap", flare.transform, -0.11f, 0.02f, 0.036f, cap);
        var tip = Stick("Tip", flare.transform, 0.11f, 0.02f, 0.03f, lit ? EmissiveMat("FlareTip", new Color(4f, 0.6f, 0.3f)) : cap);
        if (!lit) return flare;

        var lightObject = new GameObject("FlareLight");
        lightObject.transform.SetParent(flare.transform, false);
        lightObject.transform.localPosition = new Vector3(0, 0, 0.16f);
        var glow = lightObject.AddComponent<Light>();
        glow.type = LightType.Point; glow.color = new Color(1f, 0.22f, 0.1f);
        glow.intensity = 25f; glow.range = 14f; glow.shadows = LightShadows.None;   // light fades fast with distance; this reaches the tunnel walls

        var fizz = flare.AddComponent<AudioSource>();
        fizz.clip = Load<AudioClip>(SfxDir + "/FastWaterfallStreamLoop.wav");   // rushing water, sped up into a hiss
        fizz.loop = true; fizz.playOnAwake = true; fizz.volume = 0.15f; fizz.pitch = 2.2f;
        fizz.spatialBlend = 1f; fizz.rolloffMode = AudioRolloffMode.Linear; fizz.minDistance = 1f; fizz.maxDistance = 10f;

        var burn = flare.AddComponent<Flare>();
        burn.glow = glow; burn.tip = tip.GetComponent<Renderer>(); burn.fizz = fizz;
        burn.burnTime = 30f;
        return flare;
    }

    // One piece of the flare: a cylinder lying along Z, centred at 'z', with a length and a thickness.
    static GameObject Stick(string name, Transform parent, float z, float length, float thickness, Material mat)
    {
        var piece = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        piece.name = name;
        Object.DestroyImmediate(piece.GetComponent<Collider>());
        piece.transform.SetParent(parent, false);
        piece.transform.localPosition = new Vector3(0, 0, z);
        piece.transform.localRotation = Quaternion.Euler(90f, 0, 0);   // a cylinder stands along Y; lay it along Z
        piece.transform.localScale = new Vector3(thickness, length * 0.5f, thickness);
        piece.GetComponent<Renderer>().sharedMaterial = mat;
        return piece;
    }

    // An unlit flare lying on a surface, ready to pick up.
    static void PlaceFlare(Transform parent, Vector3 floorPoint, float yaw)
    {
        MakePickup(FlareModel(parent, lit: false), global::Pickup.Kind.Flare, floorPoint, yaw, "PlasticBox1");
    }

    // A painkiller bottle standing on a surface, ready to pick up.
    static void PlacePainkillers(Transform parent, Vector3 floorPoint, float yaw)
    {
        var bottle = Model(PillBottleFbx, null, parent, Tex2("Props/PillBottle/T_PillBottle_PSX.png"), 1f, 0f);
        bottle.name = "Painkillers";
        MakePickup(bottle, global::Pickup.Kind.Painkillers, floorPoint, yaw, "PillsBox1");
    }
}
