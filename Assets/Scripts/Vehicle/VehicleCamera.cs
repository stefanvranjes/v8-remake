using UnityEngine;

namespace V8Remake.Vehicle
{
    public class VehicleCamera : MonoBehaviour
    {
        [Header("Targets")]
        public Transform target;
        public Rigidbody targetRigidbody;

        [Header("Position")]
        public float distance = 6.0f;
        public float height = 2.0f;
        public float damping = 5.0f;
        
        [Header("Rotation")]
        public float rotationDamping = 10.0f;
        public float lookAheadAmount = 2.0f;

        [Header("FOV Settings")]
        public float baseFOV = 60.0f;
        public float maxFOVKick = 15.0f;
        public float fovSpeedScale = 0.1f;

        private Camera cam;

        void Start()
        {
            cam = GetComponent<Camera>();
            
            // Try to auto-find the target if not set
            if (target == null)
            {
                var controller = FindObjectOfType<VehicleController>();
                if (controller != null)
                {
                    target = controller.transform;
                    targetRigidbody = controller.GetComponent<Rigidbody>();
                }
            }
        }

        void LateUpdate()
        {
            if (target == null) return;

            // 1. Calculate the base target position behind the car
            Vector3 desiredPosition = target.TransformPoint(0, height, -distance);
            
            // 2. Smoothly interpolate to that position
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * damping);

            // 3. Look at the car, but slightly ahead for a better sense of direction
            Vector3 lookTarget = target.position + (target.forward * lookAheadAmount);
            Quaternion desiredRotation = Quaternion.LookRotation(lookTarget - transform.position, target.up);
            
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * rotationDamping);

            // 4. Dynamic FOV based on speed
            if (cam != null && targetRigidbody != null)
            {
                float currentSpeed = targetRigidbody.linearVelocity.magnitude * 3.6f; // km/h
                float targetFOV = baseFOV + (currentSpeed * fovSpeedScale);
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, Mathf.Min(targetFOV, baseFOV + maxFOVKick), Time.deltaTime * 2f);
            }
        }
    }
}
