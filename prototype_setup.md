# Vigilante 8 Remake - Prototype Setup Walkthrough

I have completed generating the core C# scripts necessary for the **Vigilante 8** style combat prototype with **Studio Tatsu** inspired heavy-physics. 

Because we are using Unity, the next phase requires assembling these components within the Unity Editor itself.

## Scripts Created

The following foundational systems have been created in `c:\Users\Stefan\Documents\GitHub\v8-remake\Assets\Scripts\`:

1. **`VehicleController.cs`**: Custom physics controller utilizing `WheelColliders`. It implements heavy downforce, anti-roll bars (to keep the vehicle planted during turns), and dynamic friction adjustments for handbrake drifting.
2. **`InputHandler.cs`**: Maps legacy axis/button inputs ("Horizontal", "Vertical", "Fire1", "Fire2", "Jump") to be fully compatible with both Keyboard and Gamepads out of the box.
3. **`WeaponManager.cs` & `Projectile.cs`**: Handles alternating machine gun fire, special weapon ammunition tracking, and physical projectile impacts.
4. **`DamageModel.cs`**: Manages vehicle health and spawns progressive particle effects (smoke and fire) as damage increases.
5. **`GameManager.cs` & `VehicleCamera.cs`**: Handles match initialization, vehicle spawning, and an arcade-style follow camera.

## Next Steps (Unity Editor Assembly)

To get the prototype running, please follow these steps in the Unity Editor:

### 1. Initialize Unity Project
Open the `v8-remake` directory using **Unity Hub** and add it as an existing project (preferably using Unity 2022 LTS or newer).

### 2. Setup the Vehicle Prefab
1. Create an empty GameObject named `PlayerVehicle` and add a **Rigidbody** (mass ~1500).
2. Attach the `VehicleController`, `InputHandler`, `DamageModel`, and `WeaponManager` scripts.
3. Create 4 empty child objects for your `WheelColliders` (FL, FR, RL, RR) and configure their suspension springs.
4. Drag these `WheelColliders` into the appropriate slots on the `VehicleController` script.
5. Assign a mesh for the body and the 4 wheels, dragging the wheel meshes into the controller as well.

> [!TIP]
> **Tuning Studio Tatsu Physics:** 
> In the `VehicleController`, adjust the `AntiRoll` and `Downforce` multipliers. A higher anti-roll will make the vehicle feel much heavier and less prone to flipping, which is key for arcade vehicle combat.

### 3. Setup the Weapons
1. Create a simple sphere prefab for your machine gun projectile, give it a `Rigidbody` and attach the `Projectile.cs` script.
2. Assign this prefab to the `machineGunProjectile` slot in the `WeaponManager`.
3. Create two empty GameObjects on your vehicle as `FirePointL` and `FirePointR` and assign them to the manager.

### 4. Scene Setup
1. Create a basic Arena (e.g., a large Plane with a BoxCollider).
2. Create an empty GameObject named `GameManager` and attach `GameManager.cs`.
3. Create a spawn point (Empty GameObject) and assign it to the manager.
4. Make your completed `PlayerVehicle` a Prefab, delete it from the scene, and assign it to the `playerVehiclePrefab` slot in the `GameManager`.
5. Attach the `VehicleCamera` script to your Main Camera.

Let me know once you have the basic scene assembled, and we can iterate on the weapon pickups, destructible environments, or visual effects!
