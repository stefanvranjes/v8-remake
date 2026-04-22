using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace V8Remake.Core
{
    public class InputHandler : MonoBehaviour
    {
        public Vector2 MoveInput { get; private set; }
        public bool FireMachineGun { get; private set; }
        public bool FireSpecial { get; private set; }
        public bool Handbrake { get; private set; }

        void Update()
        {
#if ENABLE_INPUT_SYSTEM && false // Set to true once Input System package is installed and configured
            // Assuming PlayerInput component or generated C# class is used
            // This is a placeholder for the new Input System logic
#else
            // Legacy Input Manager (Works out of the box with Gamepad and Keyboard)
            float steering = Input.GetAxis("Horizontal"); // A/D or Left Stick X
            float acceleration = Input.GetAxis("Vertical"); // W/S or Right Trigger/Left Trigger (depends on legacy setup)

            MoveInput = new Vector2(steering, acceleration);
            
            FireMachineGun = Input.GetButton("Fire1"); // Left Click or Gamepad Button (usually A or X)
            FireSpecial = Input.GetButtonDown("Fire2"); // Right Click or Gamepad Button
            Handbrake = Input.GetButton("Jump"); // Space or Gamepad Button (usually B or Y)
#endif
        }
    }
}
