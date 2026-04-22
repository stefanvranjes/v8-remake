using UnityEngine;
using V8Remake.Core;

namespace V8Remake.Weapons
{
    public class WeaponManager : MonoBehaviour
    {
        [Header("References")]
        public InputHandler inputHandler;
        public Transform firePointL;
        public Transform firePointR;

        [Header("Machine Gun (Default)")]
        public GameObject machineGunProjectile;
        public float fireRate = 0.1f;
        private float nextFireTime = 0f;
        private bool fireLeftNext = true;

        [Header("Special Weapon")]
        public GameObject specialProjectile;
        public int specialAmmo = 0;
        public float specialFireRate = 1.0f;
        private float nextSpecialFireTime = 0f;

        void Start()
        {
            if (inputHandler == null)
            {
                inputHandler = FindObjectOfType<InputHandler>();
            }
        }

        void Update()
        {
            if (inputHandler == null) return;

            HandleMachineGun();
            HandleSpecialWeapon();
        }

        private void HandleMachineGun()
        {
            if (inputHandler.FireMachineGun && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + fireRate;
                Transform currentFirePoint = fireLeftNext ? firePointL : firePointR;
                
                if (machineGunProjectile != null && currentFirePoint != null)
                {
                    GameObject projGo = Instantiate(machineGunProjectile, currentFirePoint.position, currentFirePoint.rotation);
                    Projectile proj = projGo.GetComponent<Projectile>();
                    if (proj != null)
                    {
                        proj.Initialize(gameObject);
                    }
                    fireLeftNext = !fireLeftNext; // Alternate barrels
                }
            }
        }

        private void HandleSpecialWeapon()
        {
            if (inputHandler.FireSpecial && specialAmmo > 0 && Time.time >= nextSpecialFireTime)
            {
                nextSpecialFireTime = Time.time + specialFireRate;
                
                if (specialProjectile != null && firePointL != null)
                {
                    // Fire from center or specific launcher point
                    Vector3 firePos = (firePointL.position + firePointR.position) / 2f;
                    GameObject projGo = Instantiate(specialProjectile, firePos, firePointL.rotation);
                    Projectile proj = projGo.GetComponent<Projectile>();
                    if (proj != null)
                    {
                        proj.Initialize(gameObject);
                    }
                    specialAmmo--;
                }
            }
        }

        public void AddSpecialAmmo(GameObject newSpecialProjectile, int amount)
        {
            specialProjectile = newSpecialProjectile;
            specialAmmo += amount;
            Debug.Log($"Picked up special weapon! Ammo: {specialAmmo}");
        }
    }
}
