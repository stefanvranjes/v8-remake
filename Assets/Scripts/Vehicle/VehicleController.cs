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
        public float antiRoll = 3000f;
        public float driftFrictionMultiplier = 0.5f;
        public float uprightForce = 500f;

        private Rigidbody rb;
        private WheelFrictionCurve defaultForwardFriction;
        private WheelFrictionCurve defaultSidewaysFriction;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        void Start()
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // 1. Force Scale to 1,1,1
            foreach (var t in GetComponentsInChildren<Transform>())
            {
                t.localScale = Vector3.one;
            }
            
            // 2. CENTERED & LOW Center of Mass (Forces the car to sit flat)
            if (centerOfMass != null)
                rb.centerOfMass = centerOfMass.localPosition;
            else
                rb.centerOfMass = new Vector3(0, -1.0f, 0.0f);

            // 3. Ignore Internal Collisions
            Collider[] allCols = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < allCols.Length; i++)
            {
                for (int j = i + 1; j < allCols.Length; j++)
                {
                    Physics.IgnoreCollision(allCols[i], allCols[j]);
                }
            }

            // 4. Sanitize WheelColliders
            SanitizeWheel(frontLeftWheel, false);
            SanitizeWheel(frontRightWheel, false);
            SanitizeWheel(rearLeftWheel, true);
            SanitizeWheel(rearRightWheel, true);
            
            if (inputHandler == null) inputHandler = FindObjectOfType<InputHandler>();

            // Store default friction for drifting logic
            if (rearLeftWheel != null)
            {
                defaultForwardFriction = rearLeftWheel.forwardFriction;
                defaultSidewaysFriction = rearLeftWheel.sidewaysFriction;
            }
        }

        void EnablePhysics()
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Re-enable visual meshes now that physics are active
            if (frontLeftMesh != null) frontLeftMesh.gameObject.SetActive(true);
            if (frontRightMesh != null) frontRightMesh.gameObject.SetActive(true);
            if (rearLeftMesh != null) rearLeftMesh.gameObject.SetActive(true);
            if (rearRightMesh != null) rearRightMesh.gameObject.SetActive(true);

            // Re-enable body colliders now that we have settled
            foreach (var col in GetComponentsInChildren<Collider>(true))
            {
                if (!(col is WheelCollider)) col.enabled = true;
            }

            Debug.Log("[Vehicle] Physics, Visuals, and Colliders activated.");
        }

        private void SanitizeWheel(WheelCollider wheel, bool isRear)
        {
            if (wheel == null) return;
            
            wheel.mass = 20f;
            wheel.radius = 0.4f; 
            wheel.wheelDampingRate = 1.0f; // High damping for stability
            wheel.suspensionDistance = 0.2f;
            
            JointSpring spring = wheel.suspensionSpring;
            // Rear suspension is stiffer to prevent falling back
            spring.spring = isRear ? 45000f : 35000f;
            spring.damper = isRear ? 6000f : 4500f;
            spring.targetPosition = 0.5f;
            wheel.suspensionSpring = spring;

            WheelFrictionCurve friction = wheel.sidewaysFriction;
            friction.stiffness = 0.9f;
            wheel.sidewaysFriction = friction;
        }

        void Update()
        {
            // Update visuals in Update for maximum smoothness (FixedUpdate causes jitter)
            UpdateWheelMeshes();

            // Manual Reset (Panic Button)
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetVehicle();
            }
        }

        void FixedUpdate()
        {
            if (inputHandler == null) return;

            ApplyMotor();
            ApplySteering();
            ApplyAntiRoll();
            ApplyDownforce();
            ApplyAntiWheelie(); 
            ApplyUprightTorque();
            HandleDrifting();
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
            if (groundedL && left.suspensionDistance > 0)
            {
                float dist = Vector3.Dot(left.transform.up, left.transform.position - hit.point);
                travelL = (dist - left.radius) / left.suspensionDistance;
            }

            bool groundedR = right.GetGroundHit(out hit);
            if (groundedR && right.suspensionDistance > 0)
            {
                float dist = Vector3.Dot(right.transform.up, right.transform.position - hit.point);
                travelR = (dist - right.radius) / right.suspensionDistance;
            }

            // Clamp travel to avoid infinite forces
            travelL = Mathf.Clamp01(travelL);
            travelR = Mathf.Clamp01(travelR);

            if (groundedL && groundedR)
            {
                float antiRollForce = (travelL - travelR) * antiRoll;
                
                // Clamp force to a safe range (50% of weight)
                float maxForce = rb.mass * 9.81f * 0.5f;
                antiRollForce = Mathf.Clamp(antiRollForce, -maxForce, maxForce);

                rb.AddForceAtPosition(left.transform.up * -antiRollForce, left.transform.position);
                rb.AddForceAtPosition(right.transform.up * antiRollForce, right.transform.position);
            }
        }

        private void ApplyDownforce()
        {
            bool anyGrounded = frontLeftWheel.isGrounded || frontRightWheel.isGrounded || 
                              rearLeftWheel.isGrounded || rearRightWheel.isGrounded;

            if (anyGrounded)
            {
                rb.AddForce(-transform.up * downforce * rb.linearVelocity.magnitude);
            }
        }

        private void ApplyAntiWheelie()
        {
            // If front wheels are in the air but rear wheels are on the ground
            bool frontGrounded = frontLeftWheel.isGrounded || frontRightWheel.isGrounded;
            bool rearGrounded = rearLeftWheel.isGrounded || rearRightWheel.isGrounded;

            if (!frontGrounded && rearGrounded)
            {
                // Apply a downwards force to the front of the car to pull it back down
                rb.AddForceAtPosition(-Vector3.up * 5000f, frontLeftWheel.transform.position);
                rb.AddForceAtPosition(-Vector3.up * 5000f, frontRightWheel.transform.position);
            }
        }

        private void ApplyUprightTorque()
        {
            Vector3 currentUp = transform.up;
            float angle = Vector3.Angle(currentUp, Vector3.up);
            
            if (angle > 5f)
            {
                Vector3 axis = Vector3.Cross(currentUp, Vector3.up);
                
                // If we are almost upside down (angle > 90), double the force to ensure a flip-back
                float forceMultiplier = (angle > 90f) ? 3f : 1f;
                rb.AddTorque(axis * angle * uprightForce * forceMultiplier);
            }
        }

        private void ResetVehicle()
        {
            // Reset rotation and move slightly up to avoid ground clipping
            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            transform.position += Vector3.up * 1.5f;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Debug.Log("[Vehicle] Manual reset performed.");
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
            if (mesh == null || collider == null) return;
            
            Vector3 position;
            Quaternion rotation;
            collider.GetWorldPose(out position, out rotation);
            
            mesh.position = position;
            mesh.rotation = rotation;
        }

        private void DisableMeshColliders(Transform mesh)
        {
            if (mesh == null) return;
            Collider[] colliders = mesh.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                if (!(col is WheelCollider))
                {
                    col.enabled = false;
                }
            }
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