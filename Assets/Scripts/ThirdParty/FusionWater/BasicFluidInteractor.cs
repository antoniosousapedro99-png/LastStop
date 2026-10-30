// FusionWater by nailuj05 (MIT, see LICENSE.txt): buoyancy on top of Unity physics. Patched for Unity 6
// (linearDamping / angularDamping / linearVelocity) and with its unused UnityEditor using removed so it builds.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Fluid {
    [AddComponentMenu("FusionWater/BasicFluidInteractor")]
    public class BasicFluidInteractor : BaseFluidInteractor
    {
        public override void FluidUpdate()
        {
            if (!fluid) return;   // (patched: between leaving one fluid and entering the next)
            var fluidSurface = fluid.coll ? fluid.coll.bounds.max.y : fluid.transform.position.y;
            float difference = transform.position.y - fluidSurface;

            if(difference < 0)
            {
                Vector3 buoyancy = Vector3.up * floatStrength * Mathf.Abs(difference) * Physics.gravity.magnitude * volume * fluid.density;

                if (simulateWaterTurbulence)
                {
                    buoyancy += GenerateTurbulence();

                    rb.AddTorque(GenerateTurbulence() * 0.5f);
                }

                rb.AddForceAtPosition(buoyancy, transform.position, ForceMode.Force);
                rb.AddForceAtPosition(-rb.linearVelocity * dampeningFactor * volume, transform.position, ForceMode.Force);
            }
        }
    }
}