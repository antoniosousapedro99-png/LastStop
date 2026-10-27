using UnityEditor;
using UnityEngine;

// Lockers the player can hide in (HidingLocker): a pair against the platform's back wall here; three more
// on the service level (LookTestBuilder.ServiceIndustrial.cs). Plain steel boxes with a slatted door, built from simple shapes.
public static partial class LookTestBuilder
{
    static void BuildHidingLockers()
    {
        var group = Group("HidingLockers");
        // Positions are the centre of each locker's floor; yaw is the way its door faces.
        HidingLockerAt(group, "HidingLocker_Platform_A", new Vector3(5.5f, 0f, 14.32f), 0f);
        HidingLockerAt(group, "HidingLocker_Platform_B", new Vector3(6.15f, 0f, 14.32f), 0f);
    }

    // Built at the world origin facing +Z (so the world-space helpers work), then moved into place.
    static void HidingLockerAt(Transform parent, string name, Vector3 position, float yaw)
    {
        var steel = Mat("LockerSteel", Tex("metal_04.png"), new Color(0.5f, 0.55f, 0.58f), 1f, 0.45f, 0.6f);
        var locker = new GameObject(name).transform;
        const float w = 0.31f, d = 0.3f, h = 1.95f, t = 0.03f;   // half width, half depth, height, panel thickness
        Span("Back", locker, new Vector3(-w, 0f, -d), new Vector3(w, h, -d + t), steel);
        Span("SideL", locker, new Vector3(-w, 0f, -d + t), new Vector3(-w + t, h, d), steel);
        Span("SideR", locker, new Vector3(w - t, 0f, -d + t), new Vector3(w, h, d), steel);
        Span("Bottom", locker, new Vector3(-w + t, 0f, -d + t), new Vector3(w - t, 0.05f, d), steel);
        Span("Top", locker, new Vector3(-w + t, h - t, -d + t), new Vector3(w - t, h, d), steel);

        // The door hangs on the right-hand edge. Solid below, slats at eye height to peek through.
        var hinge = new GameObject("Door").transform;
        hinge.SetParent(locker);
        hinge.position = new Vector3(w, 0f, d + 0.0125f);
        float z0 = d + 0.005f, z1 = d + 0.025f, x0 = -w + 0.005f, x1 = w - 0.005f, stile = 0.06f;
        Span("Panel", hinge, new Vector3(x0, 0.01f, z0), new Vector3(x1, 1.35f, z1), steel);
        Span("StileL", hinge, new Vector3(x0, 1.35f, z0), new Vector3(x0 + stile, 1.85f, z1), steel);
        Span("StileR", hinge, new Vector3(x1 - stile, 1.35f, z0), new Vector3(x1, 1.85f, z1), steel);
        Span("TopRail", hinge, new Vector3(x0, 1.85f, z0), new Vector3(x1, h - 0.01f, z1), steel);
        for (int i = 0; i < 4; i++)   // four slats with gaps between them; the eye (1.65 m) looks through a gap
        {
            float y = 1.40f + i * 0.12f;
            Span("Slat" + i, hinge, new Vector3(x0 + stile, y, z0 + 0.004f), new Vector3(x1 - stile, y + 0.03f, z1 - 0.004f), steel);
        }
        // An invisible trigger over the whole door, so the crosshair finds the locker even through a gap.
        var aim = hinge.gameObject.AddComponent<BoxCollider>();
        aim.isTrigger = true;
        aim.center = hinge.InverseTransformPoint(new Vector3(0f, h * 0.5f, (z0 + z1) * 0.5f));
        aim.size = new Vector3(2f * w, h, 0.04f);

        var hiding = locker.gameObject.AddComponent<HidingLocker>();
        hiding.door = hinge;
        hiding.openAngle = 105f;   // swings outward on its right-hand hinge
        hiding.openSound = Load<AudioClip>(SfxDir + "/MetalCabinet1.wav");
        hiding.closeSound = Load<AudioClip>(SfxDir + "/MetalCabinet1.wav");
        hiding.slamSound = Load<AudioClip>(SfxDir + "/DesignedPunch2.wav");

        locker.SetParent(parent, false);
        locker.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
    }
}
