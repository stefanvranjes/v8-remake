using UnityEngine;

namespace V8Remake.Vehicle
{
    public class DamageModel : MonoBehaviour
    {
        [Header("Health & Armor")]
        public float maxHealth = 100f;
        private float currentHealth;

        [Header("Effects")]
        public GameObject smokeEffectPrefab;
        public GameObject fireEffectPrefab;
        public GameObject explosionPrefab;

        private GameObject currentSmokeEffect;
        private GameObject currentFireEffect;

        private bool isDestroyed = false;

        void Start()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (isDestroyed) return;

            currentHealth -= amount;
            UpdateDamageEffects();

            if (currentHealth <= 0)
            {
                DestroyVehicle();
            }
        }

        private void UpdateDamageEffects()
        {
            float healthPercent = currentHealth / maxHealth;

            // Spawn smoke at 50% health
            if (healthPercent <= 0.5f && currentSmokeEffect == null && smokeEffectPrefab != null)
            {
                currentSmokeEffect = Instantiate(smokeEffectPrefab, transform.position, Quaternion.identity, transform);
            }

            // Spawn fire at 20% health
            if (healthPercent <= 0.2f && currentFireEffect == null && fireEffectPrefab != null)
            {
                currentFireEffect = Instantiate(fireEffectPrefab, transform.position, Quaternion.identity, transform);
            }
        }

        private void DestroyVehicle()
        {
            isDestroyed = true;
            Debug.Log($"{gameObject.name} was destroyed!");

            if (explosionPrefab != null)
            {
                Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            }

            // Disable controls
            VehicleController controller = GetComponent<VehicleController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            // Optional: Detach parts, turn model into charred wreckage
        }
    }
}
