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
        public float motorTorque = 10000f; // Increased for breakout power
        public float brakeTorque = 5000f;
        public float maxSteerAngle = 35f;
        public float topSpeed = 120f; // km/h

        [Header("Arcade Physics (Studio Tatsu Style)")]
        public float downforce = 100f;
        public float antiRoll = 3000f;
        public float driftFrictionMultiplier = 0.5f;
        public float uprightForce = 500f;
        public float airControlTorque = 5000f;

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
            
            // 2. CENTERED & STABLE Center of Mass
            if (centerOfMass != null)
                rb.centerOfMass = centerOfMass.localPosition;
            else
                // Raising COM slightly from -1.0 to -0.4 to prevent "bottoming out" on small bumps
                rb.centerOfMass = new Vector3(0, -0.4f, 0.0f);

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

            // 5. Sanitize Body Colliders (Fixes the "Green Box" pinning the car to the ground)
            SanitizeBodyColliders();
            
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
            wheel.wheelDampingRate = 1.0f; 
            // Increased suspension travel to help wheels reach the ground when high-centered
            wheel.suspensionDistance = 0.4f; 
            
            JointSpring spring = wheel.suspensionSpring;
            spring.spring = isRear ? 45000f : 35000f;
            spring.damper = isRear ? 6000f : 4500f;
            spring.targetPosition = 0.5f;
            wheel.suspensionSpring = spring;

            // High stiffness for breakout traction
            WheelFrictionCurve friction = wheel.sidewaysFriction;
            friction.stiffness = 1.5f; 
            wheel.sidewaysFriction = friction;
            
            WheelFrictionCurve fFriction = wheel.forwardFriction;
            fFriction.stiffness = 1.5f;
            wheel.forwardFriction = fFriction;
        }

        void Update()
        {
            // Update visuals in Update for maximum smoothness (FixedUpdate causes jitter)
            UpdateWheelMeshes();

            // Jump Recovery (Replaces Manual Reset)
            // If you get stuck, pressing Space will "kick" the car up and out.
            if (Input.GetKeyDown(KeyCode.Space))
            {
                JumpRecovery();
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
            ApplyAirControl(); 
            HandleDrifting();
            HandleRampSnag(); // NEW: Prevents getting stuck on ramp edges
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
            
            // Safety: If we are trying to move, force brakes to zero
            if (Mathf.Abs(inputHandler.MoveInput.y) > 0.1f) braking = 0f;

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

        private void ApplyAirControl()
        {
            // Allow manual rotation if we are in the air OR if we are moving very slowly (stuck)
            bool frontG = frontLeftWheel.isGrounded || frontRightWheel.isGrounded;
            bool rearG = rearLeftWheel.isGrounded || rearRightWheel.isGrounded;
            bool anyGrounded = frontG || rearG;
            bool isStuck = rb.linearVelocity.magnitude < 0.5f;

            if (!anyGrounded || isStuck)
            {
                // Pitch (Forward/Backward)
                float pitch = inputHandler.MoveInput.y * airControlTorque;
                rb.AddRelativeTorque(Vector3.right * pitch);

                // Yaw/Roll (Left/Right)
                float yaw = inputHandler.MoveInput.x * airControlTorque;
                rb.AddRelativeTorque(Vector3.up * yaw);
            }
        }

        private void JumpRecovery()
        {
            // Apply a sudden upward and forward shove to "pop" the car out of stuck positions
            rb.AddForce(Vector3.up * 8000f + transform.forward * 3000f, ForceMode.Impulse);
            
            // Add a small random torque to help "wiggle" out of tight geometry
            rb.AddTorque(Random.insideUnitSphere * 5000f, ForceMode.Impulse);
            
            Debug.Log("[Vehicle] Jump Recovery performed.");
        }

        private void SanitizeBodyColliders()
        {
            // Create a zero-friction material for the body so it slides over ramps
            PhysicsMaterial2D zeroFriction = new PhysicsMaterial2D("ZeroFriction");
            // Note: For 3D we use PhysicMaterial
            PhysicsMaterial slippery = new PhysicsMaterial("SlipperyBody");
            slippery.dynamicFriction = 0f;
            slippery.staticFriction = 0f;
            slippery.frictionCombine = PhysicsMaterialCombine.Minimum;

            BoxCollider[] boxes = GetComponentsInChildren<BoxCollider>();
            foreach (BoxCollider box in boxes)
            {
                box.center = new Vector3(0, 0.6f, 0); 
                box.size = new Vector3(1.6f, 0.8f, 4.5f);
                box.material = slippery;
            }

            MeshCollider[] meshes = GetComponentsInChildren<MeshCollider>();
            foreach (MeshCollider mesh in meshes)
            {
                mesh.convex = true;
                mesh.material = slippery;
            }
        }

        private void HandleRampSnag()
        {
            // If the player is trying to move but speed is near zero, apply a forward "shove"
            // This helps the car clear the "lips" of ramps and geometry snags.
            if (Mathf.Abs(inputHandler.MoveInput.y) > 0.1f && rb.linearVelocity.magnitude < 1.0f)
            {
                // Apply a combined Forward and Upward force to "climb" over the snag
                rb.AddForce(transform.forward * 5000f + Vector3.up * 2000f, ForceMode.Force);
            }
        }

        // Help the car slide along walls instead of sticking
        void OnCollisionStay(Collision collision)
        {
            if (collision.gameObject.CompareTag("Untagged") || collision.gameObject.layer == 0)
            {
                foreach (ContactPoint contact in collision.contacts)
                {
                    // Apply a gentle force away from the wall to help sliding
                    rb.AddForce(contact.normal * 1000f, ForceMode.Force);
                }
            }
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