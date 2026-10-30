using UnityEngine;

// A bit of rubbish floating in the canals (FusionWater keeps it afloat): carried along by the canal's
// current (WaterCurrent), so it drifts toward the cistern and goes over the falls. Once it has gone over,
// washed up out of the water or got stuck, it quietly turns up again where it started, so the canals never
// run dry.
[RequireComponent(typeof(Rigidbody))]
public class RubbishDrift : MonoBehaviour
{
    public float carry = 1.5f;          // how quickly it takes up the current's speed
    public float returnAfter = 15f;     // seconds out of the water or stuck before it turns up again

    Rigidbody body;
    Vector3 home;
    Quaternion homeTurn;
    float away;

    void Start()
    {
        body = GetComponent<Rigidbody>();
        home = transform.position;
        homeTurn = transform.rotation;
    }

    void FixedUpdate()
    {
        Vector3 flow = WaterCurrent.At(transform.position);
        flow.y = 0f;
        bool afloat = FloodZone.SurfaceAt(transform.position, out float surface) && transform.position.y < surface + 0.3f;
        if (afloat)
        {
            Vector3 drift = body.linearVelocity; drift.y = 0f;
            body.AddForce((flow - drift) * carry, ForceMode.Acceleration);
        }
        // Gone: out of the water (washed up), down in the cistern (over the falls), or barely moving in a current.
        bool fallen = transform.position.y < home.y - 2.5f;
        bool stuck = flow.sqrMagnitude > 0.01f && body.linearVelocity.sqrMagnitude < 0.0025f;
        away = !afloat || fallen || stuck ? away + Time.fixedDeltaTime : 0f;
        if (away > returnAfter) Return();
    }

    void Return()
    {
        away = 0f;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.position = home;
        body.rotation = homeTurn;
    }
}
