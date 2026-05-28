using System;
using UnityEngine;

namespace Assets
{
    public class HeadUpController : MonoBehaviour
    {
        [Tooltip("Scale torque by this value")]
        public float TorqueScale = 0.3f;

        Rigidbody body;

        void Start()
        {
            body = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            if (body.isKinematic) return;

            var target = Vector3.up;
            var current = body.transform.forward;
            // Axis of rotation
            var x = Vector3.Cross(current, target);
            float xMag = x.magnitude;
            if (xMag < 0.0001f) return; // vectors are parallel, no torque needed
            var theta = Mathf.Asin(xMag);
            // Change in angular velocity
            var w = (x / xMag) * (theta / Time.fixedDeltaTime * TorqueScale);
            // Current rotation in world space
            var q = body.transform.rotation * body.inertiaTensorRotation;
            // Transform to local space
            w = Quaternion.Inverse(q) * w;
            // Calculate torque and convert back to world space
            var T = q * Vector3.Scale(body.inertiaTensor, w);
            body.AddTorque(T, ForceMode.Force);

            //var p = body.position;
            //Debug.DrawRay(p, target, Color.white);
            //Debug.DrawRay(p, x, Color.cyan);
            //Debug.DrawRay(p, w, Color.yellow);
            //Debug.DrawRay(p, T, Color.magenta);
            //Debug.DrawRay(p, current, Color.red);
        }
    }
}