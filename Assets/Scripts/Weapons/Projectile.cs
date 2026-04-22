using UnityEngine;
using V8Remake.Vehicle;

namespace V8Remake.Weapons
{
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        public float speed = 50f;
        public float lifetime = 5f;
        public float damage = 10f;
        public GameObject impactEffectPrefab;

        private Rigidbody rb;
        private GameObject shooter;

        public void Initialize(GameObject shooterObject)
        {
            shooter = shooterObject;
        }

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            rb.linearVelocity = transform.forward * speed;
            
            // Arcade physics: Projectiles often fly straight without gravity drop in V8 for basic weapons
            rb.useGravity = false; 

            Destroy(gameObject, lifetime);
        }

        void OnCollisionEnter(Collision collision)
        {
            // Ignore collision with the shooter to prevent self-damage
            if (shooter != null && (collision.gameObject == shooter || collision.transform.IsChildOf(shooter.transform)))
            {
                return;
            }

            // Apply damage if we hit a destructible or another vehicle
            DamageModel damageModel = collision.gameObject.GetComponentInParent<DamageModel>();
            if (damageModel != null)
            {
                damageModel.TakeDamage(damage);
            }
            
            if (impactEffectPrefab != null)
            {
                Instantiate(impactEffectPrefab, collision.contacts[0].point, Quaternion.LookRotation(collision.contacts[0].normal));
            }

            // Small physics impulse on impact
            if (collision.rigidbody != null)
            {
                collision.rigidbody.AddForce(-collision.contacts[0].normal * (damage * 50f), ForceMode.Impulse);
            }

            Destroy(gameObject);
        }
    }
}
