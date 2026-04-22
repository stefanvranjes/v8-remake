using UnityEngine;
using V8Remake.Core;

namespace V8Remake.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleController : MonoBehaviour
    {
        [Header("References")]
        public InputHandler inputHandler;
        public Transform centerOfMass;
        
        [Header("Wheel Colliders")]
        public WheelCollider frontLeftWheel;
        public WheelCollider frontRightWheel;
        public WheelCollider rearLeftWheel;
        public WheelCollider rearRightWheel;

        [Header("Wheel Meshes")]
        public Transform frontLeftMesh;
        public Transform frontRightMesh;
        public Transform rearLeftMesh;
        public Transform rearRightMesh;

        [Header("Engine Power")]
        public float motorTorque = 2500f;
        public float brakeTorque = 5000f;
        public float maxSteerAngle = 35f;
        public float topSpeed = 120f; // km/h

        [Header("Arcade Physics (Studio Tatsu Style)")]
        public float downforce = 100f;
        public float antiRoll = 5000f;
        public float driftFrictionMultiplier = 0.5f;

        private Rigidbody rb;
        private WheelFrictionCurve defaultForwardFriction;
        private WheelFrictionCurve defaultSidewaysFriction;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            if (centerOfMass != null)
            {
                rb.centerOfMass = centerOfMass.localPosition;
            }

            if (inputHandler == null)
            {
                inputHandler = FindObjectOfType<InputHandler>();
            }

            // Store default friction for drifting logic
            defaultForwardFriction = rearLeftWheel.forwardFriction;
            defaultSidewaysFriction = rearLeftWheel.sidewaysFriction;
        }

        void FixedUpdate()
        {
            if (inputHandler == null) return;

            ApplyMotor();
            ApplySteering();
            ApplyAntiRoll();
            ApplyDownforce();
            HandleDrifting();
            UpdateWheelMeshes();
        }

        private void ApplyMotor()
        {
            float speedMultiplier = 1f - (rb.linearVelocity.magnitude * 3.6f / topSpeed);
            speedMultiplier = Mathf.Clamp01(speedMultiplier);

            float currentTorque = inputHandler.MoveInput.y * motorTorque * speedMultiplier;
            
            // Four wheel drive for better arcade handling
            frontLeftWheel.motorTorque = currentTorque;
            frontRightWheel.motorTorque = currentTorque;
            rearLeftWheel.motorTorque = currentTorque;
            rearRightWheel.motorTorque = currentTorque;

            // Brakes
            float braking = inputHandler.Handbrake ? brakeTorque : 0f;
            frontLeftWheel.brakeTorque = braking;
            frontRightWheel.brakeTorque = braking;
            rearLeftWheel.brakeTorque = braking;
            rearRightWheel.brakeTorque = braking;
        }

        private void ApplySteering()
        {
            float steer = inputHandler.MoveInput.x * maxSteerAngle;
            frontLeftWheel.steerAngle = steer;
            frontRightWheel.steerAngle = steer;
        }

        private void ApplyAntiRoll()
        {
            // Apply anti-roll bar forces to keep the heavy vehicle grounded during sharp turns
            ApplyAntiRollToAxle(frontLeftWheel, frontRightWheel);
            ApplyAntiRollToAxle(rearLeftWheel, rearRightWheel);
        }

        private void ApplyAntiRollToAxle(WheelCollider left, WheelCollider right)
        {
            WheelHit hit;
            float travelL = 1.0f;
            float travelR = 1.0f;

            bool groundedL = left.GetGroundHit(out hit);
            if (groundedL)
                travelL = (-left.transform.InverseTransformPoint(hit.point).y - left.radius) / left.suspensionDistance;

            bool groundedR = right.GetGroundHit(out hit);
            if (groundedR)
                travelR = (-right.transform.InverseTransformPoint(hit.point).y - right.radius) / right.suspensionDistance;

            float antiRollForce = (travelL - travelR) * antiRoll;

            if (groundedL)
                rb.AddForceAtPosition(left.transform.up * -antiRollForce, left.transform.position);
            if (groundedR)
                rb.AddForceAtPosition(right.transform.up * antiRollForce, right.transform.position);
        }

        private void ApplyDownforce()
        {
            // Artificial downforce for that heavy, planted feel
            rb.AddForce(-transform.up * downforce * rb.linearVelocity.magnitude);
        }

        private void HandleDrifting()
        {
            if (inputHandler.Handbrake)
            {
                // Reduce rear sideways friction to allow power slides
                SetRearFriction(defaultSidewaysFriction.stiffness * driftFrictionMultiplier);
            }
            else
            {
                // Restore grip
                SetRearFriction(defaultSidewaysFriction.stiffness);
            }
        }

        private void SetRearFriction(float stiffness)
        {
            WheelFrictionCurve curve = rearLeftWheel.sidewaysFriction;
            curve.stiffness = Mathf.Lerp(curve.stiffness, stiffness, Time.deltaTime * 5f);
            
            rearLeftWheel.sidewaysFriction = curve;
            rearRightWheel.sidewaysFriction = curve;
        }

        private void UpdateWheelMeshes()
        {
            UpdateSingleWheelMesh(frontLeftWheel, frontLeftMesh);
            UpdateSingleWheelMesh(frontRightWheel, frontRightMesh);
            UpdateSingleWheelMesh(rearLeftWheel, rearLeftMesh);
            UpdateSingleWheelMesh(rearRightWheel, rearRightMesh);
        }

        private void UpdateSingleWheelMesh(WheelCollider collider, Transform mesh)
        {
            if (mesh == null) return;
            
            Vector3 position;
            Quaternion rotation;
            collider.GetWorldPose(out position, out rotation);
            
            mesh.position = position;
            mesh.rotation = rotation;
        }
        
        // Studio Tatsu style heavy impact handling
        void OnCollisionEnter(Collision collision)
        {
            if (collision.impulse.magnitude > 1000f)
            {
                // Trigger camera shake or sparks here based on impact force
                Debug.Log($"Heavy Impact! Force: {collision.impulse.magnitude}");
            }
        }
    }
}
