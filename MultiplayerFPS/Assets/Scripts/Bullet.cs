using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FPSGame
{
    public class Bullet : MonoBehaviour
    {
        public float speed = 50f;
        public float damage = 10f;
        public float lifeTime = 3f;
        
        private float timeAlive = 0f;
        private bool hasHit = false;
        private Rigidbody rb;
        private TrailRenderer trail;
        
        void Start()
        {
            // Get the rigidbody
            rb = GetComponent<Rigidbody>();
            
            // Apply forward force on start
            if (rb != null)
            {
                rb.linearVelocity = transform.forward * speed;
                // Ayrıca bir de kuvvet uygulayalım
                rb.AddForce(transform.forward * speed, ForceMode.Impulse);
            }
            
            // Mermi çarpışmalarını düzenle
            Physics.IgnoreLayerCollision(8, 8); // 8: Bullet layer - mermi-mermi çarpışmasını engelle
            Physics.IgnoreLayerCollision(8, 9); // 9: Weapon layer - mermi-silah çarpışmasını engelle
            Physics.IgnoreLayerCollision(8, 10); // 10: Player layer - mermi-oyuncu çarpışmasını engelle (kendi mermilerimiz bize çarpmasın)
            
            // Add a trail renderer for better visibility
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.startWidth = 0.05f;
            trail.endWidth = 0.0f;
            trail.time = 0.1f;
            
            // Trail material setup
            SetupTrailMaterial(trail);
            
            // Add a light component
            Light light = gameObject.AddComponent<Light>();
            light.color = new Color(1f, 0.7f, 0f);
            light.intensity = 2f;
            light.range = 2f;
        }
        
        private void SetupTrailMaterial(TrailRenderer trail)
        {
            // Try to find an existing material in the scene first
            Material trailMaterial = null;
            
            // Try to find a GameObject with the material by name
            Renderer bulletTrailRenderer = GameObject.Find("BulletTrailMaterial_URP")?.GetComponent<Renderer>();
            if (bulletTrailRenderer != null && bulletTrailRenderer.sharedMaterial != null)
            {
                trailMaterial = bulletTrailRenderer.sharedMaterial;
            }
            else
            {
                // Try to find any object with BulletTrail in its name
                Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
                foreach (Renderer renderer in renderers)
                {
                    if (renderer.sharedMaterial != null && 
                        renderer.sharedMaterial.name.Contains("BulletTrail"))
                    {
                        trailMaterial = renderer.sharedMaterial;
                        break;
                    }
                }
            }
            
            // If still not found, create a new material
            if (trailMaterial == null)
            {
                trailMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                trailMaterial.SetColor("_BaseColor", new Color(1.0f, 0.8f, 0.0f, 1.0f));
                trailMaterial.SetColor("_EmissionColor", new Color(2.0f, 1.6f, 0.0f, 1.0f));
                trailMaterial.EnableKeyword("_EMISSION");
            }
            
            // Trail materyalini uygula
            trail.material = trailMaterial;
            
            // Set trail color to orange/yellow
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(1.0f, 0.8f, 0.0f, 1.0f), 0.0f), 
                    new GradientColorKey(new Color(1.0f, 0.5f, 0.0f, 1.0f), 1.0f) 
                }, 
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1.0f, 0.0f), 
                    new GradientAlphaKey(0.0f, 1.0f) 
                }
            );
            trail.colorGradient = gradient;
        }
        
        void Update()
        {
            if (hasHit) return;
            
            // Check lifetime
            timeAlive += Time.deltaTime;
            if (timeAlive >= lifeTime)
            {
                Destroy(gameObject);
            }
            
            // Eğer mermi hala hareketsizse zorla hareket ettirelim
            if (rb != null && rb.linearVelocity.magnitude < 10f)
            {
                rb.linearVelocity = transform.forward * speed;
            }
        }
        
        // Handle physical collisions with walls and other objects
        void OnCollisionEnter(Collision collision)
        {
            if (hasHit) return;
            
            // Ignore collisions with other bullets
            if (collision.gameObject.CompareTag("Bullet"))
            {
                return;
            }
            
            // Process the hit
            hasHit = true;
            
            // Get the contact point for impact effect
            ContactPoint contact = collision.contacts[0];
            Vector3 position = contact.point;
            Vector3 normal = contact.normal;
            
            Debug.Log("Bullet collision with: " + collision.gameObject.name);
            
            // Create impact effect at collision point
            CreateImpactEffect(position, normal);
            
            // Detach the trail so it remains visible after bullet is destroyed
            if (trail != null)
            {
                DetachTrail();
            }
            
            // Disable bullet visuals but keep it around for a tiny bit to ensure the trail is visible
            DisableAndDestroyLater();
        }
        
        // This is used for trigger colliders (like player damage detection)
        void OnTriggerEnter(Collider other)
        {
            if (hasHit) return;
            
            // Diğer mermi ise çarpışmayı dikkate almayalım
            if (other.CompareTag("Bullet"))
            {
                return;
            }
            
            // Apply damage if hit a player without destroying the bullet
            if (other.GetComponent<FPSController>() != null)
            {
                other.GetComponent<FPSController>().TakeDamage((int)damage);
                Debug.Log("Bullet trigger hit player: " + other.gameObject.name);
                
                // Only destroy if we hit a player directly
                hasHit = true;
                CreateImpactEffect(transform.position, transform.forward);
                
                // Detach the trail so it remains visible after bullet is destroyed
                if (trail != null)
                {
                    DetachTrail();
                }
                
                // Disable bullet visuals but keep it around for a tiny bit to ensure the trail is visible
                DisableAndDestroyLater();
            }
        }
        
        // Detach trail so it persists after bullet is destroyed
        private void DetachTrail()
        {
            // Create a new GameObject to hold the trail
            GameObject trailObj = new GameObject("BulletTrail");
            trailObj.transform.position = transform.position;
            
            // Move the trail to the new object
            TrailRenderer detachedTrail = trailObj.AddComponent<TrailRenderer>();
            detachedTrail.startWidth = trail.startWidth;
            detachedTrail.endWidth = trail.endWidth;
            detachedTrail.time = trail.time;
            detachedTrail.material = trail.material;
            detachedTrail.colorGradient = trail.colorGradient;
            
            // Copy the trail positions
            detachedTrail.Clear();
            Vector3[] positions = new Vector3[trail.positionCount];
            trail.GetPositions(positions);
            detachedTrail.AddPositions(positions);
            
            // Destroy this trail object after the trail fades
            Destroy(trailObj, trail.time);
            
            // Disable the original trail
            trail.emitting = false;
            trail.enabled = false;
        }
        
        // Disable visuals but keep the object around for a moment to ensure trail effect works
        private void DisableAndDestroyLater()
        {
            // Disable renderer
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
            
            // Disable colliders
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }
            
            // Disable rigidbody - first stop velocity, then make kinematic
            if (rb != null)
            {
                // Set velocity to zero before making it kinematic
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            
            // Destroy the object after a small delay to ensure effects are processed
            Destroy(gameObject, 0.1f);
        }
        
        // Çarpma efektini kodda oluştur
        private void CreateImpactEffect(Vector3 position, Vector3 normal)
        {
            // Create an empty game object for the impact effect instead of a sphere
            GameObject impactObj = new GameObject("BulletImpact");
            impactObj.transform.position = position;
            impactObj.transform.rotation = Quaternion.LookRotation(normal);
            
            // Impact Effect bileşenini ekle
            ImpactEffect impactEffect = impactObj.AddComponent<ImpactEffect>();
            impactEffect.duration = 0.3f;
            impactEffect.startScale = 0.1f;
            impactEffect.endScale = 0.05f;
            
            Debug.Log("Impact effect created at: " + position);
        }
    }
}