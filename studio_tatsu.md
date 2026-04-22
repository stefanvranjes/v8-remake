# Vigilante 8 Remake - Implementation Plan

## Goal Description

The goal is to create a vehicular combat game inspired by the classic **Vigilante 8**, featuring advanced, heavy-impact, and grounded vehicle physics reminiscent of **Studio Tatsu's** recent works. 

This means blending the arcade-style weapon combat (machine guns, missiles, mines, special character abilities) with a more sophisticated, physics-driven vehicle controller that emphasizes weight transfer, suspension dynamics, drifting, and satisfying collisions.

## User Review Required

> [!IMPORTANT]
> **Engine Selection**: We need to decide on the game engine before proceeding with directory setup and code generation. 
> *   **Unreal Engine 5**: Studio Tatsu recently shifted to Unreal Engine 5 to leverage the **Chaos Physics system** for their vehicles. UE5 provides excellent built-in tools for heavy, realistic vehicle physics and destruction. (Highly recommended for the "Studio Tatsu" physics feel).
> *   **Unity 3D**: We have previously used Unity for your 2D fighting game and isometric projects. We can build a robust custom physics-based vehicle controller in Unity using WheelColliders or a custom raycast-based suspension system, but it will require more manual tuning to match Unreal's Chaos system.

Please confirm which engine you would like to initialize this project with. 

## Open Questions

> [!WARNING]
> 1. **Visual Style**: Are we aiming for a retro PS1/N64 low-poly aesthetic (like original V8) or a high-fidelity modern look?
> 2. **Input System**: Will we prioritize gamepad support from the start (essential for arcade driving games)?
> 3. **Scale**: For the initial prototype, should we focus on a single arena map with one drivable vehicle and a basic weapon pickup system?

## Proposed Architecture (Engine Agnostic Overview)

Regardless of the engine chosen, the core architecture will require the following modular systems:

### 1. Vehicle Controller System
*   **Suspension & Weight Transfer**: Tuning suspension springs, dampers, and center of mass to allow vehicles to lean into corners and dip during acceleration/braking.
*   **Tire Friction Model**: Implementing slip curves (forward and lateral) to allow for controlled drifting and power-slides.
*   **Collision System**: Detecting impacts to apply visual damage and physics impulses.

### 2. Combat & Weapon System
*   **Weapon Manager**: Handles weapon switching, ammo tracking, and firing logic.
*   **Projectile System**: Physics-based projectiles (e.g., unguided rockets, homing missiles, arcing mortars).
*   **Health & Damage System**: Component-based system to handle vehicle health, armor states, and destruction states.

### 3. Pickup & Arena System
*   **Spawners**: Logic for spawning weapon crates and health pickups across the arena.
*   **Environment Interaction**: Destructible props and terrain hazards.

## Proposed Initial Project Structure

Assuming we proceed with **Unity** (as a placeholder, adjust if UE5 is chosen):

```text
/Assets
  /Scripts
    /Vehicle
      VehicleController.cs
      VehicleCamera.cs
      DamageModel.cs
    /Weapons
      WeaponManager.cs
      Projectile.cs
      WeaponPickup.cs
    /Core
      GameManager.cs
      InputHandler.cs
  /Prefabs
    /Vehicles
    /Weapons
    /Environment
  /PhysicsMaterials
  /Scenes
    Arena_Prototype.unity
```

## Verification Plan

### Automated/Play Testing
- Implement the core vehicle controller and verify that drifting, acceleration, and suspension feel grounded.
- Spawn a test box and verify collision impulses.
- Implement a basic machine gun and verify projectile instantiation and hit detection.

### Manual Verification
- Playtest the vehicle handling in a sandbox arena to tune the "Studio Tatsu" feel.
- Verify that weapon pickups correctly attach to the vehicle and fire based on player input.
