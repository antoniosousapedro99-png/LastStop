using UnityEngine;

// The sewers' supplies (the director's choices, 2026-10-31): 2 flares, 2 batteries, 2 painkillers.
// Most lie where sewer workers would have left them; two are rewards for draining the water.
//   battery:     on the control room desk;
//   flare:       on the electrical room floor, between the cabinets;
//   painkillers: on the pump room floor, in the far corner;
//   painkillers: in Valve 4's chamber (behind the powered flood gate), near the valve;
//   battery:     at the bottom of Valve 2's flooded pit, in the corner clear of its floating rubbish (dive for it,
//                or wait for the pit to drain);
//   flare:       on the cistern's floor, reachable only once all four valves have drained it.
public static partial class LookTestBuilder
{
    static void SewerSupplies(Transform sewers)
    {
        var g = new GameObject("Supplies").transform;
        g.SetParent(sewers);
        Physics.SyncTransforms();   // the rays below must see the rooms just built
        Vector3 o = SewerOrigin;

        if (Surface(o + new Vector3(25.4f, 1.5f, 3.5f), "battery, control room desk", out var p)) Battery(g, p, 20f);
        if (Surface(o + new Vector3(52.9f, 1f, 5f), "flare, electrical room", out p)) PlaceFlare(g, p, 80f);
        if (Surface(o + new Vector3(45f, 1f, 139.5f), "painkillers, pump room", out p)) PlacePainkillers(g, p, 10f);
        if (Surface(o + new Vector3(85.8f, 1f, 113f), "painkillers, Valve 4", out p)) PlacePainkillers(g, p, -30f);
        if (Surface(o + new Vector3(87.8f, 0.5f, 37.3f), "battery, Valve 2's pit", out p)) Battery(g, p, 60f);
        // On the cistern floor, halfway between the central platform's foot and the wall stairs.
        if (Surface(o + CisternCentre + new Vector3(-6.5f, CisternFloor + 3f, -6.5f), "flare, cistern floor", out p)) PlaceFlare(g, p, 130f);
    }
}
