using UnityEditor;
using UnityEngine;

// The far half of Hollis St's open west train, past the key bench and the debris wall: crushed by a tunnel
// collapse. Rubble pours in at the far end up to the ceiling, ceiling panels hang torn and sagging, grab
// poles are bent, dust trickles down, and one torn-down light shorts out now and then (SparkingLight).
// It can only be seen, not entered (the debris wall stays). No blood.
public static partial class LookTestBuilder
{
    // 'farX' is the car's far (west) end, 'nearX' the debris wall halfway down it; 'floorY' its floor.
    static void TrainWreck(Transform parent, float farX, float nearX, float floorY, float wallN, float wallS)
    {
        var g = new GameObject("TrainWreck").transform;
        g.SetParent(parent);
        var concrete = Mat("TunnelConcrete", Tex("ConcreteBare.png"), new Color(0.55f, 0.53f, 0.5f), 2f, 0.1f);
        var dark = Mat("WreckRock", Tex("ConcreteBare.png"), new Color(0.32f, 0.3f, 0.28f), 1.5f, 0.05f);
        var panel = Mat("WreckPanel", null, new Color(0.62f, 0.62f, 0.6f), 1f, 0.3f, 0.3f);
        var steel = Mat("WreckSteel", null, new Color(0.6f, 0.6f, 0.62f), 1f, 0.55f, 0.8f);
        var wire = Mat("WreckWire", null, new Color(0.12f, 0.12f, 0.12f), 1f, 0.3f, 0.2f);
        float ceilY = floorY + 2.2f, midZ = (wallN + wallS) * 0.5f, width = wallS - wallN;
        float start = farX + 0.3f, moundLength = 4.5f;
        var rng = new System.Random(19);
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        // ---- the rubble mound: up to the ceiling at the far end, sloping down toward the middle of the car
        for (int i = 0; i < 46; i++)
        {
            float x = start + R(0f, moundLength);
            float top = (ceilY - floorY) * (1f - (x - start) / moundLength) + 0.15f;   // the slope's height here
            float size = R(0.25f, 0.85f);
            // Rocks, and every fourth a broken cement block (the industrial pack's Rock and BrickCement).
            var chunk = IndustrialPiece(g, i % 4 == 0 ? "BrickCement" : "Rock", new Vector3(size, size * R(0.5f, 0.9f), size * R(0.7f, 1.2f)));
            chunk.name = "Rubble";
            chunk.transform.position = new Vector3(x, floorY + R(0f, Mathf.Max(0.05f, top - size * 0.4f)), wallN + R(0.2f, width - 0.2f));
            chunk.transform.rotation = Quaternion.Euler(R(-40f, 40f), R(0f, 360f), R(-40f, 40f));
        }
        // Loose bits spilled across the floor toward the debris wall.
        for (int i = 0; i < 14; i++)
        {
            float size = R(0.1f, 0.3f);
            var bit = IndustrialPiece(g, i % 2 == 0 ? "Brick" : "Rock", new Vector3(size, size * 0.6f, size));
            bit.name = "RubbleBit";
            bit.transform.rotation = Quaternion.Euler(R(-20f, 20f), R(0f, 360f), R(-20f, 20f));
            StandOn(bit, new Vector3(R(start + moundLength - 0.5f, nearX - 0.5f), floorY, wallN + R(0.2f, width - 0.2f)));
        }
        // A low spread of dirt and debris where the mound meets the floor (the industrial pack's PileDebris).
        var spread = IndustrialPiece(g, "PileDebris", new Vector3(1.8f, 0.3f, width - 0.3f));
        spread.name = "RubbleSpread";
        StandOn(spread, new Vector3(start + moundLength + 0.2f, floorY - 0.05f, midZ));

        // ---- the ceiling caved in: torn panels hanging down at angles, one end still up at the ceiling
        (float x, float z, float droop, float yaw)[] panels =
        {
            (start + moundLength + 0.4f, midZ - 0.3f, 38f, 8f), (start + moundLength + 1.6f, midZ + 0.4f, 22f, -12f),
            (start + moundLength + 2.9f, midZ - 0.1f, 14f, 5f), (start + 2.2f, midZ + 0.2f, 30f, 170f),
        };
        foreach (var p in panels)
        {
            var hang = new GameObject("CeilingPanel").transform;
            hang.SetParent(g);
            hang.position = new Vector3(p.x, ceilY - 0.02f, p.z);
            hang.rotation = Quaternion.Euler(0f, p.yaw, 0f) * Quaternion.Euler(0f, 0f, -p.droop);   // hinged at its far edge
            var slab = Box("Panel", hang, Vector3.zero, new Vector3(1.3f, 0.03f, 1.5f), panel);
            slab.transform.localPosition = new Vector3(-0.65f, 0f, 0f);
            slab.transform.localRotation = Quaternion.identity;
            Object.DestroyImmediate(slab.GetComponent<Collider>());
        }
        // The roof itself caved in over the mound: two big sections bent down from the ceiling onto the rubble.
        foreach (var (z, w, tilt) in new[] { (midZ - width * 0.22f, width * 0.5f, 6f), (midZ + width * 0.24f, width * 0.48f, -5f) })
        {
            Vector3 top = new Vector3(start + moundLength + 0.8f, ceilY - 0.02f, z), foot = new Vector3(start + 1.2f, floorY + 1.3f, z);
            var roof = Box("CavedRoof", g, (top + foot) * 0.5f, new Vector3(Vector3.Distance(top, foot), 0.06f, w), panel);
            roof.transform.rotation = Quaternion.Euler(tilt, 0f, Mathf.Atan2(top.y - foot.y, top.x - foot.x) * Mathf.Rad2Deg);
            Object.DestroyImmediate(roof.GetComponent<Collider>());
        }
        // Wires hanging loose from the gaps.
        for (int i = 0; i < 5; i++)
        {
            float x = R(start + moundLength, nearX - 0.6f), z = wallN + R(0.4f, width - 0.4f), len = R(0.4f, 1.1f);
            Pipe(g, "LooseWire", new Vector3(x, ceilY, z), new Vector3(x + R(-0.25f, 0.25f), ceilY - len, z + R(-0.2f, 0.2f)), 0.012f, wire);
        }

        // ---- grab poles bent by the weight from above
        foreach (float t in new[] { 0.35f, 0.7f })
        {
            float x = Mathf.Lerp(start + moundLength, nearX, t), z = midZ + (t < 0.5f ? -0.25f : 0.3f);
            Vector3 knee = new Vector3(x + R(-0.45f, 0.45f), floorY + R(0.9f, 1.3f), z + R(-0.35f, 0.35f));
            PipeRun(g, "BentPole", 0.022f, steel, new Vector3(x, floorY, z), knee, new Vector3(x + R(-0.2f, 0.2f), ceilY, z));
        }

        // ---- dust trickling down over the rubble, now and then
        var dustObject = new GameObject("DustTrickle");
        dustObject.transform.SetParent(g);
        dustObject.transform.position = new Vector3(start + 2f, ceilY - 0.05f, midZ);
        var dust = dustObject.AddComponent<ParticleSystem>();
        var main = dust.main;
        main.loop = true; main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.startColor = new Color(0.55f, 0.52f, 0.48f, 0.8f);
        main.gravityModifier = 0.12f;
        var emission = dust.emission;
        emission.rateOverTime = 6f;
        var shape = dust.shape;
        shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(3f, 0.05f, width * 0.8f);
        dustObject.GetComponent<ParticleSystemRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/M_Dust.mat");

        // ---- one light torn down, hanging by its wires, shorting out
        Vector3 mount = new Vector3(nearX - 3.2f, ceilY, midZ + 0.35f);   // close enough to the rubble to light it
        var fitting = new GameObject("SparkingFitting").transform;
        fitting.SetParent(g);
        fitting.position = mount + new Vector3(0.2f, -0.75f, 0f);
        fitting.rotation = Quaternion.Euler(0f, 0f, 32f);   // one end dropped lower than the other
        var housing = Box("Housing", fitting, fitting.position, new Vector3(1.1f, 0.07f, 0.16f), panel);
        housing.transform.rotation = fitting.rotation;
        Object.DestroyImmediate(housing.GetComponent<Collider>());
        var tube = Prim(PrimitiveType.Cylinder, "Tube", fitting, fitting.position - fitting.up * 0.05f, new Vector3(0.05f, 0.5f, 0.05f), EmissiveMat("WreckTube", new Color(0.85f, 0.95f, 1f) * 2f));
        tube.transform.rotation = fitting.rotation * Quaternion.Euler(0f, 0f, 90f);
        Object.DestroyImmediate(tube.GetComponent<Collider>());
        foreach (float side in new[] { -0.45f, 0.45f })   // the two wires it hangs from
        {
            Vector3 end = fitting.TransformPoint(new Vector3(side, 0.04f, 0f));
            Pipe(g, "HangingWire", end, new Vector3(mount.x + side * 0.6f, ceilY, mount.z), 0.01f, wire);
        }
        var lightObject = new GameObject("Light");
        lightObject.transform.SetParent(fitting, false);
        lightObject.transform.localPosition = new Vector3(0f, -0.15f, 0f);
        var lamp = lightObject.AddComponent<Light>();
        lamp.type = LightType.Point; lamp.color = new Color(0.85f, 0.93f, 1f);
        lamp.intensity = 3.5f; lamp.range = 7f; lamp.shadows = LightShadows.Soft;
        // Sparks: small bright orange points that fall and fade.
        var sparkObject = new GameObject("Sparks");
        sparkObject.transform.SetParent(fitting, false);
        var sparks = sparkObject.AddComponent<ParticleSystem>();
        sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var sm = sparks.main;
        sm.playOnAwake = false; sm.loop = false;
        sm.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        sm.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.2f);
        sm.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.035f);
        sm.startColor = new Color(1f, 0.75f, 0.35f) * 3f;
        sm.gravityModifier = 1f;
        var se = sparks.emission; se.rateOverTime = 0f;
        var ss = sparks.shape; ss.shapeType = ParticleSystemShapeType.Sphere; ss.radius = 0.05f;
        sparkObject.GetComponent<ParticleSystemRenderer>().sharedMaterial = EmissiveMat("Spark", new Color(1f, 0.75f, 0.35f) * 4f);
        var crackle = sparkObject.AddComponent<AudioSource>();
        crackle.clip = Load<AudioClip>(ScareSfx + "/SparkCrackle.wav");
        crackle.playOnAwake = false; crackle.spatialBlend = 1f; crackle.volume = 0.6f;
        crackle.rolloffMode = AudioRolloffMode.Linear; crackle.minDistance = 1.5f; crackle.maxDistance = 14f;
        var spark = fitting.gameObject.AddComponent<SparkingLight>();
        spark.lamp = lamp; spark.tube = tube.GetComponent<Renderer>(); spark.tubeGlow = new Color(0.85f, 0.95f, 1f) * 2f;
        spark.sparks = sparks; spark.crackle = crackle;
    }
}
