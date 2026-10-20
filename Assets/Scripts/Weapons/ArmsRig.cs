using UnityEngine;

// Bends the first-person arms (WRAD ARMS) so the hands hold the gun.
// 1) Each arm is solved every frame with "two-bone IK": given the shoulder, the upper-arm and forearm
//    lengths, and where the hand must be, it works out where the elbow goes (bending toward a hint
//    point, so elbows point down and out) and turns the bones to match.
// 2) The hand is turned so its fingers and thumb point in chosen directions (given in the grip
//    point's own space, e.g. "fingers forward and down, thumb up"), measured from the model itself.
// 3) The forearm's twist bones share the hand's roll, like a real forearm (otherwise the wrist pinches).
// 4) The fingers curl around the gun, joint by joint, and the thumb wraps.
public class ArmsRig : MonoBehaviour
{
    [System.Serializable]
    public class Arm
    {
        public Transform upper, lower, hand;    // bicep, forearm, wrist bones
        public Transform twist0, twist1;        // forearm twist bones (elbow half, wrist half)
        public Transform target;                // where the hand should be (a point on the gun)
        public Transform elbowHint;             // the elbow bends toward this point
        public Vector3 fingersDirection = Vector3.forward;  // where the knuckles point (target space)
        public Vector3 thumbDirection = Vector3.up;         // which side the thumb is on (target space)
        public Transform[] fingers;             // index, middle, ring, pinky: 3 bones each, base first
        public Transform[] thumb;               // 3 thumb bones, base first
        public float fingerCurl = 60f;          // degrees per finger joint (positive = toward the palm)
        public float thumbCurl = 25f;

        [HideInInspector] public Quaternion[] fingerRest, thumbRest;
        [HideInInspector] public Quaternion upperRest, lowerRest;
        [HideInInspector] public float upperLength, lowerLength;
        [HideInInspector] public Vector3 fingerAxisLocal, thumbSideLocal;   // measured hand directions
        [HideInInspector] public Vector3 forearmSideLocal;                  // the thumb side at rest, in forearm space
        [HideInInspector] public Quaternion twist0Rest, twist1Rest;
    }

    public Arm right, left;
    [Range(0, 1)] public float forearmRollShare = 0.75f;   // how much of the hand's roll the forearm takes

    Transform[] allBones;
    Quaternion[] allRest;
    Vector3[] allRestPos;

    void Awake()
    {
        // Remember every bone's rest pose, so poses left over from an animation (e.g. a grab)
        // are wiped before the arms are bent around the gun.
        allBones = GetComponentsInChildren<Transform>();
        allRest = new Quaternion[allBones.Length];
        allRestPos = new Vector3[allBones.Length];
        for (int i = 0; i < allBones.Length; i++) { allRest[i] = allBones[i].localRotation; allRestPos[i] = allBones[i].localPosition; }
        Init(right);
        Init(left);
    }

    static void Init(Arm arm)
    {
        arm.upperLength = Vector3.Distance(arm.upper.position, arm.lower.position);
        arm.lowerLength = Vector3.Distance(arm.lower.position, arm.hand.position);
        arm.upperRest = arm.upper.localRotation;
        arm.lowerRest = arm.lower.localRotation;
        arm.fingerRest = new Quaternion[arm.fingers.Length];
        for (int i = 0; i < arm.fingers.Length; i++) arm.fingerRest[i] = arm.fingers[i].localRotation;
        arm.thumbRest = new Quaternion[arm.thumb.Length];
        for (int i = 0; i < arm.thumb.Length; i++) arm.thumbRest[i] = arm.thumb[i].localRotation;

        // In the hand's own space: "toward the fingers" = wrist -> middle-finger knuckle,
        // "toward the thumb" = pinky knuckle -> index knuckle.
        Transform index = arm.fingers[0], middle = arm.fingers[3], pinky = arm.fingers[9];
        arm.fingerAxisLocal = arm.hand.InverseTransformDirection(middle.position - arm.hand.position).normalized;
        arm.thumbSideLocal = arm.hand.InverseTransformDirection(index.position - pinky.position).normalized;
        arm.forearmSideLocal = arm.lower.InverseTransformDirection(arm.hand.TransformDirection(arm.thumbSideLocal));
        if (arm.twist0) arm.twist0Rest = arm.twist0.localRotation;
        if (arm.twist1) arm.twist1Rest = arm.twist1.localRotation;
    }

    void LateUpdate()
    {
        for (int i = 1; i < allBones.Length; i++)   // [0] is this object itself
        {
            allBones[i].localRotation = allRest[i];
            allBones[i].localPosition = allRestPos[i];
        }
        Solve(right);
        Solve(left);
    }

    void Solve(Arm arm)
    {
        // Start from the model's rest pose each frame so small errors never pile up.
        arm.upper.localRotation = arm.upperRest;
        arm.lower.localRotation = arm.lowerRest;
        if (arm.twist0) arm.twist0.localRotation = arm.twist0Rest;
        if (arm.twist1) arm.twist1.localRotation = arm.twist1Rest;
        for (int i = 0; i < arm.fingers.Length; i++) arm.fingers[i].localRotation = arm.fingerRest[i];
        for (int i = 0; i < arm.thumb.Length; i++) arm.thumb[i].localRotation = arm.thumbRest[i];

        Vector3 a = arm.upper.position;
        Vector3 t = arm.target.position;
        float la = arm.upperLength, lb = arm.lowerLength;

        // Distance shoulder -> target, kept just inside what the arm can reach.
        Vector3 toTarget = t - a;
        float d = Mathf.Clamp(toTarget.magnitude, Mathf.Abs(la - lb) + 0.001f, la + lb - 0.001f);
        Vector3 dir = toTarget.normalized;

        // Law of cosines: the angle at the shoulder between "toward target" and "toward elbow".
        float cosA = Mathf.Clamp((la * la + d * d - lb * lb) / (2f * la * d), -1f, 1f);
        float sinA = Mathf.Sqrt(1f - cosA * cosA);
        Vector3 hintDir = Vector3.ProjectOnPlane(arm.elbowHint.position - a, dir).normalized;
        Vector3 elbow = a + dir * (la * cosA) + hintDir * (la * sinA);

        // Turn the upper arm to point at the elbow, then the forearm to point at the target.
        arm.upper.rotation = Quaternion.FromToRotation(arm.lower.position - a, elbow - a) * arm.upper.rotation;
        Vector3 b = arm.lower.position;
        arm.lower.rotation = Quaternion.FromToRotation(arm.hand.position - b, a + dir * d - b) * arm.lower.rotation;

        Vector3 wantFingers = arm.target.TransformDirection(arm.fingersDirection);
        Vector3 wantThumb = arm.target.TransformDirection(arm.thumbDirection);

        // A real forearm rolls with the hand (try turning your palm up: the whole forearm turns).
        // Measure how far the hand must roll around the forearm, let the forearm itself take most of
        // it and the twist bones near the wrist the rest, so the wrist stays straight (no torn seam).
        Vector3 forearmAxis = (arm.hand.position - arm.lower.position).normalized;
        Vector3 restSide = Vector3.ProjectOnPlane(arm.lower.TransformDirection(arm.forearmSideLocal), forearmAxis);
        Vector3 nowSide = Vector3.ProjectOnPlane(wantThumb, forearmAxis);
        float roll = Vector3.SignedAngle(restSide, nowSide, forearmAxis);
        arm.lower.rotation = Quaternion.AngleAxis(roll * forearmRollShare, forearmAxis) * arm.lower.rotation;
        float rest = roll * (1f - forearmRollShare);
        if (arm.twist0) arm.twist0.rotation = Quaternion.AngleAxis(rest * 0.5f, forearmAxis) * arm.twist0.rotation;
        if (arm.twist1) arm.twist1.rotation = Quaternion.AngleAxis(rest * 0.5f, forearmAxis) * arm.twist1.rotation;

        // Turn the hand: its measured finger/thumb directions onto the wanted ones.
        arm.hand.rotation = Quaternion.LookRotation(wantFingers, wantThumb)
                            * Quaternion.Inverse(Quaternion.LookRotation(arm.fingerAxisLocal, arm.thumbSideLocal));

        // Curl each finger joint around the hand's side-to-side axis (children follow their parents).
        Vector3 hinge = arm.hand.TransformDirection(arm.thumbSideLocal);
        foreach (var bone in arm.fingers)
            bone.rotation = Quaternion.AngleAxis(arm.fingerCurl, hinge) * bone.rotation;
        Vector3 along = arm.hand.TransformDirection(arm.fingerAxisLocal);
        foreach (var bone in arm.thumb)
            bone.rotation = Quaternion.AngleAxis(arm.thumbCurl, along) * bone.rotation;
    }
}
