using UnityEngine;

namespace V8Remake.Vehicle
{
    public class VehicleCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 8.0f;
        public float height = 3.0f;
        public float damping = 5.0f;
        public float rotationDamping = 10.0f;

        void FixedUpdate()
        {
            if (target == null) return;

            // Calculate the desired position
            Vector3 desiredPosition = target.position - target.forward * distance + Vector3.up * height;
            
            // Interpolate position
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * damping);

            // Calculate desired rotation
            Quaternion desiredRotation = Quaternion.LookRotation(target.position - transform.position, target.up);
            
            // Interpolate rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * rotationDamping);
        }
    }
}
