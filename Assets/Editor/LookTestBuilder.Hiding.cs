using UnityEditor;
using UnityEngine;

// Lockers the player can hide in (HidingLocker): a pair against the platform's back wall here; more on the
// service levels and at Canal St. The "Dirty Locker" model (Sketchfab), split by Tools/Blender/locker_door.py
// into a body and a hinged door, with a window cut behind the upper vent slats to peek out through.
public static partial class LookTestBuilder
{
    static void BuildHidingLockers()
    {
        var group = Group("HidingLockers");
        // Positions are the centre of each locker's floor; yaw is the way its door faces.
        HidingLockerAt(group, "HidingLocker_Platform_A", new Vector3(5.5f, 0f, 14.32f), 0f);
        HidingLockerAt(group, "HidingLocker_Platform_B", new Vector3(6.2f, 0f, 14.32f), 0f);
    }

    // Built at the world origin facing +Z (so the world-space helpers work), then moved into place.
    static void HidingLockerAt(Transform parent, string name, Vector3 position, float yaw)
    {
        var locker = new GameObject(name).transform;
        var model = Model(ArtDir + "/Props/HidingLocker/HidingLocker.fbx", null, locker, Tex2("Props/HidingLocker/LockerColor.png"), 1f, 0f);
        model.transform.localPosition = new Vector3(0f, 0f, 0.045f);   // it's a little deeper than the old one: same back wall
        var body = model.transform.Find("LockerBody");
        var door = model.transform.Find("LockerDoor");   // its origin is on the hinge line
        body.gameObject.AddComponent<MeshCollider>();
        // A solid box over the door: blocks the way in while shut, and the crosshair finds the locker by it.
        door.gameObject.AddComponent<BoxCollider>();

        var hiding = locker.gameObject.AddComponent<HidingLocker>();
        hiding.door = door;
        hiding.openAngle = -105f;   // swings outward on its hinges (on the left, seen from inside)
        hiding.openSound = Load<AudioClip>(SfxDir + "/MetalCabinet1.wav");
        hiding.closeSound = Load<AudioClip>(SfxDir + "/MetalCabinet1.wav");
        hiding.slamSound = Load<AudioClip>(SfxDir + "/DesignedPunch2.wav");

        locker.SetParent(parent, false);
        locker.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
    }
}
