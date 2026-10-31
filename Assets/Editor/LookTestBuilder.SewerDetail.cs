using System.Collections.Generic;
using UnityEngine;

// The sewers' last dressing pass (stage 4): white depth gauge boards down the cistern wall beside each
// doorway, dark tide lines where the water has stood, rusty iron rungs in the canal walls, sagging cables
// and dead caged bulbs along the tunnels, wall pipes with valve wheels and gauges, and junk washed up on the
// walkways (bags, cans, crates, bricks, a shopping trolley). All of it is scenery: no colliders.
public static partial class LookTestBuilder
{
    static void SewerDetail(Transform parent)
    {
        var g = new GameObject("Detail").transform;
        g.SetParent(parent);
        Physics.SyncTransforms();
        DepthGauges(g);
        TideLines(g);
        DeadBulbs(parent);

        // Each long straight stretch of canal (junction pieces are short and skipped).
        var rng = new System.Random(404);
        int stretch = 0;
        foreach (var (a, b) in kitChannels)
        {
            Vector3 along = b - a; along.y = 0f;
            float length = along.magnitude;
            if (length < 10f) continue;
            Vector3 dir = along / length, across = Vector3.Cross(Vector3.up, dir);
            Vector3 start = new Vector3(a.x, SewerOrigin.y, a.z);   // on the channel's middle line, at the walkways' height
            CanalLadders(g, start, dir, across, length, stretch);
            TunnelCable(g, start, dir, across, length);
            TunnelPipe(g, start, dir, -across, length, rng);
            WashedUpJunk(g, start, dir, length, rng, stretch);
            stretch++;
        }

        // A shopping trolley left against the wall in the tunnel down to Valve 4 (on its side it floated on its
    // wheels and handle, and its one-sided wire basket vanished from below).
        var cart = Model(ArtDir + "/Props/ShoppingCart/ShoppingCart.fbx", null, g, Tex2("Props/ShoppingCart/ShoppingCart.png"), 1f, 0f);
        cart.name = "ShoppingCart";
        cart.transform.localScale *= 1.05f / Mathf.Max(0.01f, Bounds(cart).size.y);   // about a metre tall standing
        cart.transform.rotation = Quaternion.Euler(0f, 0f, -6f) * Quaternion.Euler(0f, 25f, 0f) * cart.transform.rotation;   // turned a little, leaning a touch toward the wall
        StandOn(cart, SewerOrigin + new Vector3(83f, 0f, 88f));
        foreach (var col in cart.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
    }

    // ---- the cistern: a white enamel gauge board down the wall beside each doorway, a black tick every metre
    // and a number every 2 m (metres below the gallery), read from across the shaft
    static void DepthGauges(Transform g)
    {
        Vector3 c = SewerOrigin + CisternCentre;
        var enamel = Mat("GaugeEnamel", null, new Color(0.86f, 0.84f, 0.77f), 1f, 0.5f);
        var ink = Mat("GaugeInk", null, new Color(0.06f, 0.06f, 0.06f), 1f, 0.3f);
        const float top = -0.45f, width = 1f;
        float bottom = CisternFloor + 0.05f;
        for (int k = 0; k < 4; k++)
        {
            // The wall section just clockwise of each doorway, 1.6 m toward the door from its middle.
            var rot = Quaternion.Euler(0f, k * 90f + 22.5f, 0f);
            Vector3 d = rot * Vector3.forward, right = rot * Vector3.right;
            Vector3 face = c + d * CisternRadius - right * 1.6f;     // on the wall's inner face
            var holder = new GameObject("DepthGauge_" + k).transform;
            holder.SetParent(g);
            holder.SetPositionAndRotation(face, Quaternion.LookRotation(d));   // looking into the wall: text reads from the shaft; y 0 = the gallery
            Vector3 P(float x, float y, float z) => holder.TransformPoint(new Vector3(x, y, z));
            NoCollider(Turned(Box("GaugeBoard", holder, P(0f, (top + bottom) * 0.5f, -0.03f), new Vector3(width, top - bottom, 0.03f), enamel), holder.rotation));
            NoCollider(Turned(Box("GaugeEdge", holder, P(-width * 0.5f + 0.04f, (top + bottom) * 0.5f, -0.048f), new Vector3(0.02f, top - bottom, 0.006f), ink), holder.rotation));
            for (int m = 1; m <= 31; m++)
            {
                float y = -m;
                bool labelled = m % 2 == 0;
                float tick = labelled ? 0.45f : 0.25f;
                NoCollider(Turned(Box("GaugeTick", holder, P(-width * 0.5f + 0.04f + tick * 0.5f, y, -0.048f), new Vector3(tick, 0.05f, 0.006f), ink), holder.rotation));
                if (!labelled) continue;
                var number = new GameObject("GaugeNumber").transform;
                number.SetParent(holder);
                number.SetPositionAndRotation(P(0.24f, y, -0.05f), holder.rotation);
                Label(number, m.ToString(), Vector3.zero, 0.08f, new Color(0.06f, 0.06f, 0.06f), 0f);
            }
        }
    }

    // ---- the cistern: a dark wet stain with a strip of green slime just above each level the water stopped at
    static void TideLines(Transform g)
    {
        Vector3 c = SewerOrigin + CisternCentre;
        var stain = TransparentMat("TideStain", new Color(0.04f, 0.045f, 0.03f, 0.6f), 0.6f);
        var slime = TransparentMat("TideSlime", new Color(0.16f, 0.25f, 0.07f, 0.8f), 0.7f);
        float width = 2f * CisternRadius * Mathf.Tan(11.25f * Mathf.Deg2Rad) + 0.02f;   // one wall section's inner face
        for (int level = 0; level < 4; level++)
        {
            float y = CisternStart - level * CisternStep;
            for (int i = 0; i < 16; i++)
            {
                var rot = Quaternion.Euler(0f, i * 22.5f, 0f);
                Vector3 d = rot * Vector3.forward;
                NoCollider(Turned(Box("TideStain", g, c + d * (CisternRadius - 0.012f) + Vector3.up * (y + 0.3f), new Vector3(width, 0.6f, 0.004f), stain), rot));
                NoCollider(Turned(Box("TideSlime", g, c + d * (CisternRadius - 0.02f) + Vector3.up * (y + 0.03f), new Vector3(width, 0.16f, 0.004f), slime), rot));
            }
        }
    }

    // ---- a third of the tunnel bulbs dead (no light, dark glass); every other dead one hangs loose, askew
    static void DeadBulbs(Transform sewers)
    {
        var deadGlass = Mat("DeadBulbGlass", null, new Color(0.12f, 0.11f, 0.09f), 1f, 0.6f);
        var bulbs = new List<Transform>();
        foreach (Transform t in sewers) if (t.name.StartsWith("SewerBulb_")) bulbs.Add(t);
        for (int i = 0; i < bulbs.Count; i++)
        {
            if (i % 3 != 1) continue;
            var bulb = bulbs[i];
            foreach (var light in bulb.GetComponentsInChildren<Light>()) Object.DestroyImmediate(light.gameObject);   // its flicker goes with it
            var glass = bulb.Find("Bulb");
            glass.GetComponent<Renderer>().sharedMaterial = deadGlass;
            if (i % 6 != 1) continue;
            var plate = bulb.Find("Base");   // swings from the top of its cable
            Vector3 pivot = plate.position + Vector3.up * 0.9f;
            foreach (Transform part in bulb) part.RotateAround(pivot, Vector3.right, 22f);
        }
    }

    // ---- rusty iron rungs set into the canal wall about every 20 m (alternate sides), with a grab hoop above
    static void CanalLadders(Transform g, Vector3 start, Vector3 dir, Vector3 across, float length, int stretch)
    {
        var iron = pipeRust;
        int count = Mathf.Max(1, Mathf.FloorToInt(length / 20f));
        for (int k = 0; k < count; k++)
        {
            Vector3 at = start + dir * ((k + 0.5f) * length / count);
            Vector3 side = (k + stretch) % 2 == 0 ? across : -across;
            if (!Physics.Raycast(at + Vector3.down * 1.2f, side, out var hit, ChannelHalf + 1f, ~0, QueryTriggerInteraction.Ignore)) continue;
            Vector3 wall = new Vector3(hit.point.x, 0f, hit.point.z);
            var ladder = new GameObject("CanalLadder").transform;
            ladder.SetParent(g);
            ladder.position = wall + Vector3.up * SewerOrigin.y;
            for (float y = -2.05f; y < -0.1f; y += 0.32f)
            {
                Vector3 mid = wall + Vector3.up * (SewerOrigin.y + y) - side * 0.13f;
                NoCollider(Box("Rung", ladder, mid, Abs(dir) * 0.42f + Vector3.up * 0.035f + Abs(side) * 0.035f, iron));
                foreach (float s in new[] { -0.2f, 0.2f })
                    NoCollider(Box("RungLeg", ladder, mid + dir * s + side * 0.07f, Abs(dir) * 0.03f + Vector3.up * 0.028f + Abs(side) * 0.14f, iron));
            }
            // The grab hoop: two posts up from the walkway's edge and a bar across.
            Vector3 lip = wall + Vector3.up * SewerOrigin.y;
            foreach (float s in new[] { -0.22f, 0.22f })
                NoCollider(Box("HoopPost", ladder, lip + dir * s + side * 0.08f + Vector3.up * 0.45f, new Vector3(0.04f, 0.9f, 0.04f), iron));
            NoCollider(Box("HoopBar", ladder, lip + side * 0.08f + Vector3.up * 0.9f, Abs(dir) * 0.46f + new Vector3(0f, 0.032f, 0f) + Abs(side) * 0.032f, iron));
        }
    }

    // ---- a black cable along the ceiling over one walkway, clipped every 4 m and sagging between the clips
    static void TunnelCable(Transform g, Vector3 start, Vector3 dir, Vector3 across, float length)
    {
        var cable = new GameObject("TunnelCable").transform;
        cable.SetParent(g);
        var points = new List<Vector3>();
        for (float s = 1.5f; s <= length - 1.5f; s += 4f)
        {
            Vector3 below = start + dir * s + across * 3.1f + Vector3.up * 1.5f;
            if (Physics.Raycast(below, Vector3.up, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore)) points.Add(hit.point + Vector3.down * 0.12f);
        }
        for (int i = 0; i + 1 < points.Count; i++)
        {
            Vector3 a = points[i], b = points[i + 1];
            NoCollider(Box("CableClip", cable, a + Vector3.up * 0.06f, new Vector3(0.08f, 0.12f, 0.08f), conduit));
            Vector3 prev = a;
            for (int k = 1; k <= 4; k++)
            {
                float t = k / 4f;
                Vector3 next = Vector3.Lerp(a, b, t) + Vector3.down * (0.35f * 4f * t * (1f - t));   // a gentle sag
                Pipe(cable, "Cable", prev, next, 0.018f, conduit);
                prev = next;
            }
        }
    }

    // ---- a pipe along one tunnel wall (the builder's pipe-support pass straps it to the wall), with a small
    // valve wheel and a gauge in a long stretch
    static void TunnelPipe(Transform g, Vector3 start, Vector3 dir, Vector3 toWall, float length, System.Random rng)
    {
        const float height = 2.75f;
        if (!Physics.Raycast(start + dir * (length * 0.5f) + toWall * 2.5f + Vector3.up * height, toWall, out var hit, 4f, ~0, QueryTriggerInteraction.Ignore)) return;
        float out_ = Vector3.Dot(hit.point - start, toWall) - 0.16f;   // the pipe's middle, 16 cm off the wall
        var pipeMat = rng.Next(2) == 0 ? pipeRust : pipeSteel;
        Vector3 Line(float s) => start + dir * s + toWall * out_ + Vector3.up * height;
        Pipe(g, "WallPipe", Line(1.2f), Line(length - 1.2f), 0.09f, pipeMat);
        if (length < 20f) return;
        // The wheel on a short stem below the pipe, and a gauge beside it.
        Vector3 w = Line(length * 0.5f) + Vector3.down * 0.45f - toWall * 0.08f;
        Pipe(g, "ValveStem", Line(length * 0.5f), w, 0.03f, pipeMat);
        var red = Mat("ValveRed", null, new Color(0.5f, 0.12f, 0.08f), 1f, 0.4f, 0.4f);
        var wheel = Prim(PrimitiveType.Cylinder, "SmallValveWheel", g, w, new Vector3(0.36f, 0.015f, 0.36f), red);
        wheel.transform.rotation = Quaternion.FromToRotation(Vector3.up, -toWall);
        NoCollider(wheel);
        var gauge = Prim(PrimitiveType.Cylinder, "PipeGauge", g, Line(length * 0.5f + 0.7f) - toWall * 0.12f, new Vector3(0.16f, 0.02f, 0.16f), Mat("GaugeFace", null, new Color(0.85f, 0.83f, 0.76f), 1f, 0.3f));
        gauge.transform.rotation = Quaternion.FromToRotation(Vector3.up, -toWall);
        NoCollider(gauge);
    }

    // ---- junk washed up against the tunnel walls: a bag or two, cans, a crate or bricks
    static void WashedUpJunk(Transform g, Vector3 start, Vector3 dir, float length, System.Random rng, int stretch)
    {
        string[] brands = { "generic_cola", "pepso", "sprit", "fantom_orange", "dr_piper", "red_bullet" };
        int piles = length > 25f ? 2 : 1;
        for (int p = 0; p < piles; p++)
        {
            Vector3 across = Vector3.Cross(Vector3.up, dir) * ((p + stretch) % 2 == 0 ? 1f : -1f);
            Vector3 at = start + dir * (length * (p == 0 ? 0.3f : 0.72f)) + across * 2.9f;
            for (int i = 0; i < 4; i++)
            {
                GameObject item;
                Vector3 spot = at + dir * ((i - 1.5f) * 0.55f + ((float)rng.NextDouble() - 0.5f) * 0.25f) + across * ((float)rng.NextDouble() - 0.5f) * 0.4f;
                float yaw = (float)rng.NextDouble() * 360f;
                if (i == 0) { item = Model(SewerKitFbx, "Garbage_bag", g, null, 1f, yaw, sewerKitTextures); Fit(item, 0.8f); }
                else if (i == 3) { item = Model(SewerKitFbx, rng.Next(2) == 0 ? "Bricks" : "Box_T_01", g, null, 1f, yaw, sewerKitTextures); Fit(item, 0.75f); }
                else
                {
                    item = Model(ArtDir + "/Props/SodaCans/Can_Standard330.fbx", null, g, Tex2("Props/SodaCans/can_" + brands[rng.Next(brands.Length)] + ".png"), 1f, yaw);
                    item.transform.localScale *= 0.12f / Mathf.Max(0.001f, Bounds(item).size.y);
                    item.transform.rotation = Quaternion.Euler(90f, 0f, 0f) * item.transform.rotation;   // lying on its side
                }
                item.name = "WashedUpJunk";
                StandOn(item, spot);
                foreach (var col in item.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
            }
        }
        void Fit(GameObject item, float size)
        {
            var b = Bounds(item);
            item.transform.localScale *= size / Mathf.Max(b.size.x, b.size.z, 0.01f);
        }
    }

    static GameObject NoCollider(GameObject go)
    {
        var col = go.GetComponent<Collider>();
        if (col) Object.DestroyImmediate(col);
        return go;
    }
}
