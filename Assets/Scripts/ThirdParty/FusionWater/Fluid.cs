// FusionWater by nailuj05 (MIT, see LICENSE.txt): buoyancy on top of Unity physics. Patched for Unity 6
// (linearDamping / angularDamping / linearVelocity) and with its unused UnityEditor using removed so it builds.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Fluid
{
    [AddComponentMenu("FusionWater/Fluid")]
    public class Fluid : MonoBehaviour
    {
        public float density = 1;

        public float drag = 1;

        public float angularDrag = 1f;

        public Collider coll { get; private set; }

        private void Awake()   // (patched from Start: triggers can fire before Start, and the surface comes from this)
        {
            coll = GetComponent<Collider>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if(other.TryGetComponent(out BaseFluidInteractor fluidInteractor))
            {
                fluidInteractor.EnterFluid(this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out BaseFluidInteractor fluidInteractor))
            {
                fluidInteractor.ExitFluid(this);
            }
        }
    }
}
