using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

namespace FPSGame
{
    public class WeaponController : MonoBehaviour
    {
        [Header("Weapon Settings")]
        public float damage = 10f;
        public float range = 100f;
        public float fireRate = 10f;
        public int maxAmmo = 30;
        public float reloadTime = 1.5f;
        public bool automatic = true;
        
        [Header("Weapon Handling")]
        public float recoilAmount = 2f;
        public float aimDownSightsSpeed = 10f;
        public float weaponSwayAmount = 0.02f;
        public float weaponSwaySmoothing = 10f;
        public Vector3 aimDownSightsPosition = new Vector3(0f, -0.01f, 0.15f);
        public Vector3 hipPosition = new Vector3(0.2f, -0.2f, 0.5f);
        
        [Header("Reload Animation")]
        public float reloadDownAmount = 0.3f;       // How far down the weapon moves during reload
        public float reloadRotationAmount = 30f;    // How many degrees the weapon rotates during reload
        public AnimationCurve reloadCurve = new AnimationCurve(
            new Keyframe(0f, 0f),                   // Start normal
            new Keyframe(0.2f, 1f),                 // Move down quickly
            new Keyframe(0.8f, 1f),                 // Stay down
            new Keyframe(1f, 0f)                    // Return to normal position
        );  // Animation timing is based on reloadTime
        
        [Header("References")]
        public Camera playerCamera;
        public GameObject weaponModel;
        public Transform muzzlePoint;
        public AudioSource audioSource;
        public GameObject bulletPrefab; // Prefab for the bullet

        [Header("Effects")]
        public GameObject muzzleFlashEffect;
        public Light muzzleFlashLight;
        public ParticleSystem ejectedShellParticle;
        public AudioClip fireSound;
        public AudioClip reloadSound;
        public AudioClip emptySound;

        // Public for UI access
        [HideInInspector] public int currentAmmo;
        [HideInInspector] public bool isReloading = false;

        // Private variables
        private float nextFireTime = 0f;
        private Vector3 targetWeaponRotation;
        private Vector3 targetWeaponPosition;
        private Vector3 newWeaponRotation;
        private Vector3 newWeaponPosition;
        private Vector3 initialPosition;
        private bool isAiming = false;
        private Vector3 weaponSwayPosition;
        private UIManager uiManager;
        private FPSController playerController;
        
        // Private animation variables
        private float reloadAnimationTime = 0f;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private bool isPlayingReloadAnimation = false;

        void Start()
        {
            currentAmmo = maxAmmo;
            initialPosition = transform.localPosition;
            targetWeaponPosition = hipPosition;

            // Try to find UIManager
            uiManager = FindAnyObjectByType<UIManager>();
            playerController = FindAnyObjectByType<FPSController>();

            // Create weapon model if not set
            if (weaponModel == null)
            {
                weaponModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                weaponModel.transform.parent = transform;
                weaponModel.transform.localPosition = Vector3.zero;
                weaponModel.transform.localScale = new Vector3(0.1f, 0.1f, 0.5f);
                Destroy(weaponModel.GetComponent<Collider>());
            }

            // Create muzzle point if not set
            if (muzzlePoint == null)
            {
                GameObject muzzle = new GameObject("MuzzlePoint");
                muzzle.transform.parent = weaponModel.transform;
                muzzle.transform.localPosition = new Vector3(0, 0, 0.5f);
                muzzlePoint = muzzle.transform;
            }

            // Create muzzle flash light if not set
            if (muzzleFlashLight == null && muzzlePoint != null)
            {
                GameObject lightGO = new GameObject("MuzzleFlashLight");
                lightGO.transform.parent = muzzlePoint;
                lightGO.transform.localPosition = Vector3.zero;
                
                muzzleFlashLight = lightGO.AddComponent<Light>();
                muzzleFlashLight.color = new Color(1f, 0.7f, 0.3f);
                muzzleFlashLight.intensity = 2f;
                muzzleFlashLight.range = 2f;
                muzzleFlashLight.enabled = false;
            }

            // Auto-assign camera if not set
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
                
                // Double check if we got a camera
                if (playerCamera == null)
                {
                    Debug.LogError("No camera found! WeaponController needs a camera reference.");
                    enabled = false; // Disable this component if no camera is found
                    return;
                }
            }

            // Set up audio source if needed
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0.5f; // Half 3D, half 2D
                audioSource.playOnAwake = false;
                audioSource.volume = 0.7f;
            }
            
            // Mermi prefabı oluştur
            if (bulletPrefab == null)
            {
                CreateBulletPrefab();
            }
        }

        // Mermi prefabını kodda oluştur
        private void CreateBulletPrefab()
        {
            GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bullet.name = "BulletPrefab";
            bullet.transform.localScale = new Vector3(0.1f, 0.1f, 0.3f);
            
            // Mermi katmanına ekle
            bullet.layer = LayerMask.NameToLayer("Bullet");
            bullet.tag = "Bullet";
            
            // Collider ayarlarını düzenle
            SphereCollider collider = bullet.GetComponent<SphereCollider>();
            if (collider != null)
            {
                // Change from trigger to physical collider for wall collisions
                collider.isTrigger = false;
                collider.radius = 0.5f;
                
                // Add a separate trigger collider for damage detection
                GameObject triggerObject = new GameObject("BulletTrigger");
                triggerObject.transform.parent = bullet.transform;
                triggerObject.transform.localPosition = Vector3.zero;
                triggerObject.layer = bullet.layer;
                
                SphereCollider triggerCollider = triggerObject.AddComponent<SphereCollider>();
                triggerCollider.isTrigger = true;
                triggerCollider.radius = 0.6f; // Slightly larger than the physical collider
            }
            
            // Rigidbody ekle
            Rigidbody rb = bullet.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.mass = 0.1f;
            rb.linearDamping = 0f;
            
            // Renderer ayarla
            Renderer renderer = bullet.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Try to find our bullet material first
                Material bulletMaterial = null;
                
                // Look for any renderer using our bullet material
                Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
                foreach (Renderer rend in renderers)
                {
                    if (rend.sharedMaterial != null && 
                        rend.sharedMaterial.name != null &&
                        rend.sharedMaterial.name.Contains("BulletMaterial_URP"))
                    {
                        bulletMaterial = rend.sharedMaterial;
                        break;
                    }
                }
                
                // If not found, create a new URP material
                if (bulletMaterial == null)
                {
                    bulletMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    bulletMaterial.SetColor("_BaseColor", new Color(1f, 0.7f, 0f, 1f));
                    bulletMaterial.SetColor("_EmissionColor", new Color(2f, 1.4f, 0f, 1f));
                    bulletMaterial.EnableKeyword("_EMISSION");
                }
                
                renderer.material = bulletMaterial;
            }
            
            // Bullet script ekle
            bullet.AddComponent<Bullet>();
            
            // Sahne dışında tut
            bullet.SetActive(false);
            
            // Prefab olarak referansını al
            bulletPrefab = bullet;
        }

        void Update()
        {
            // Make sure we have all needed references
            if (playerCamera == null || weaponModel == null || muzzlePoint == null)
            {
                Debug.LogError("WeaponController missing references!");
                return;
            }
            
            // Handle weapon sway
            if (!isPlayingReloadAnimation)
            {
                WeaponSway();
            }
            
            // Handle aiming
            HandleAiming();
            
            // Handle reload animation if active
            if (isPlayingReloadAnimation)
            {
                UpdateReloadAnimation();
            }
            else
            {
                // Update weapon position and rotation
                UpdateWeaponPosition();
            }
            
            if (isReloading)
                return;

            if (currentAmmo <= 0 && Input.GetButtonDown("Fire1"))
            {
                // Play empty sound
                if (audioSource != null && emptySound != null)
                {
                    audioSource.clip = emptySound;
                    audioSource.Play();
                }
                
                StartReload();
                return;
            }

            // Fire logic
            if (automatic)
            {
                // Automatic fire
                if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
                {
                    nextFireTime = Time.time + 1f / fireRate;
                    Shoot();
                }
            }
            else
            {
                // Semi-automatic fire
                if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime)
                {
                    nextFireTime = Time.time + 1f / fireRate;
                    Shoot();
                }
            }

            if (Input.GetKeyDown(KeyCode.R) && currentAmmo < maxAmmo && !isReloading)
            {
                StartReload();
            }
        }

        void WeaponSway()
        {
            // Get mouse movement
            float mouseX = Input.GetAxis("Mouse X") * weaponSwayAmount;
            float mouseY = Input.GetAxis("Mouse Y") * weaponSwayAmount;
            
            // Calculate target weapon rotation
            targetWeaponRotation = new Vector3(-mouseY, mouseX, 0);
            
            // Interpolate to the target rotation for smooth weapon movement
            newWeaponRotation = Vector3.Lerp(newWeaponRotation, targetWeaponRotation, Time.deltaTime * weaponSwaySmoothing);
            
            // Apply weapon sway rotation
            transform.localRotation = Quaternion.Euler(newWeaponRotation);
        }

        void HandleAiming()
        {
            // Toggle aim mode
            if (Input.GetButtonDown("Fire2")) // Right mouse button
            {
                isAiming = !isAiming;
            }
            
            // Set target position based on aim state
            targetWeaponPosition = isAiming ? aimDownSightsPosition : hipPosition;
        }

        void UpdateWeaponPosition()
        {
            // Smoothly move weapon between hip and aim positions
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetWeaponPosition, Time.deltaTime * aimDownSightsSpeed);
        }

        void Shoot()
        {
            // Check references again to be safe
            if (playerCamera == null || weaponModel == null || muzzlePoint == null)
            {
                Debug.LogError("Cannot shoot - WeaponController missing references!");
                return;
            }
            
            // Mermi kontrolü
            if (currentAmmo <= 0)
            {
                // Play empty sound
                if (audioSource != null && emptySound != null)
                {
                    audioSource.clip = emptySound;
                    audioSource.Play();
                }
                
                StartReload();
                return;
            }
            
            currentAmmo--;
            Debug.Log("Shot fired! Ammo left: " + currentAmmo);

            // Play gunshot sound
            if (audioSource != null && fireSound != null)
            {
                audioSource.clip = fireSound;
                audioSource.Play();
            }

            // Eject shell effect
            if (ejectedShellParticle != null)
            {
                ejectedShellParticle.Play();
            }

            // Create muzzle flash light effect
            if (muzzleFlashLight != null)
            {
                StartCoroutine(MuzzleFlashLightEffect());
            }

            // Create simple muzzle flash effect
            if (muzzleFlashEffect == null)
            {
                GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flash.transform.position = muzzlePoint.position + muzzlePoint.forward * 0.2f;
                flash.transform.localScale = Vector3.one * 0.1f;
                
                Renderer flashRenderer = flash.GetComponent<Renderer>();
                if (flashRenderer != null)
                {
                    flashRenderer.material.color = Color.yellow;
                    flashRenderer.material.EnableKeyword("_EMISSION");
                    flashRenderer.material.SetColor("_EmissionColor", Color.yellow * 2f);
                }
                
                Destroy(flash.GetComponent<Collider>()); // Collider'ı sil, çarpışma olmasın
                Destroy(flash, 0.05f);
            }
            else
            {
                Instantiate(muzzleFlashEffect, muzzlePoint.position + muzzlePoint.forward * 0.2f, muzzlePoint.rotation);
            }

            // Apply recoil
            if (playerController != null)
            {
                playerController.AddRecoil(recoilAmount, isAiming);
            }

            // Weapon recoil effect
            weaponModel.transform.localPosition = new Vector3(
                weaponModel.transform.localPosition.x,
                weaponModel.transform.localPosition.y,
                weaponModel.transform.localPosition.z - 0.1f);
            
            // Reset position after a small delay
            Invoke("ResetWeaponPosition", 0.1f);

            // Show hit marker in UI
            if (uiManager != null)
            {
                uiManager.ShowHitMarker();
            }

            // Fire bullet
            if (bulletPrefab != null)
            {
                // Mermi konumunu biraz daha ileride oluşturalım ki silahla çarpışmasın
                Vector3 bulletPosition = muzzlePoint.position + muzzlePoint.forward * 0.3f;
                bulletPosition += muzzlePoint.up * Random.Range(-0.01f, 0.01f);
                bulletPosition += muzzlePoint.right * Random.Range(-0.01f, 0.01f);
                
                // Mermi rotasyonunu da hafif rastgele yap - gerçekçi sapma
                Quaternion bulletRotation = muzzlePoint.rotation * Quaternion.Euler(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    0);
                
                // Instantiate bullet at muzzle point with slight randomness
                GameObject bullet = Instantiate(bulletPrefab, bulletPosition, bulletRotation);
                
                // Direk namludan ileri fırlatalım
                Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
                if (bulletRb != null)
                {
                    // İleri doğru güçlü bir itme verelim
                    bulletRb.AddForce(bullet.transform.forward * 50f, ForceMode.Impulse);
                }
                
                // Set the bullet's impact effect
                Bullet bulletComponent = bullet.GetComponent<Bullet>();
                if (bulletComponent != null)
                {
                    bulletComponent.damage = damage;
                }
            }
            else
            {
                Debug.LogWarning("No bullet prefab assigned to the WeaponController!");
                
                // Fallback to raycast
                Vector3 rayOrigin = playerCamera.transform.position + playerCamera.transform.forward * 0.5f;
                
                RaycastHit hit;
                if (Physics.Raycast(rayOrigin, playerCamera.transform.forward, out hit, range))
                {
                    Debug.DrawLine(rayOrigin, hit.point, Color.red, 1.0f);

                    // Create impact effect directly
                    CreateImpactEffect(hit.point, hit.normal);
                    
                    // Damage logic
                    if (hit.transform != null && hit.transform.GetComponent<FPSController>() != null)
                    {
                        hit.transform.GetComponent<FPSController>().TakeDamage((int)damage);
                    }
                }
            }
        }

        IEnumerator MuzzleFlashLightEffect()
        {
            muzzleFlashLight.enabled = true;
            yield return new WaitForSeconds(0.05f);
            muzzleFlashLight.enabled = false;
        }

        void ResetWeaponPosition()
        {
            if (weaponModel != null)
            {
                weaponModel.transform.localPosition = Vector3.zero;
            }
        }

        void StartReload()
        {
            if (currentAmmo == maxAmmo) return;
            
            isReloading = true;
            
            // Start reload animation
            StartReloadAnimation();
            
            // Play reload sound
            if (audioSource != null && reloadSound != null)
            {
                audioSource.clip = reloadSound;
                audioSource.Play();
            }
            
            Debug.Log("Reloading...");
            Invoke("FinishReload", reloadTime);
        }

        void FinishReload()
        {
            currentAmmo = maxAmmo;
            isReloading = false;
            
            // Animation should be complete, but ensure weapon returns to normal
            if (isPlayingReloadAnimation)
            {
                StopReloadAnimation();
            }
            
            Debug.Log("Reload complete!");
        }
        
        // Start the reload animation
        void StartReloadAnimation()
        {
            if (isPlayingReloadAnimation) return;
            
            // Store original position and rotation
            originalPosition = weaponModel.transform.localPosition;
            originalRotation = weaponModel.transform.localRotation;
            
            // Reset animation time
            reloadAnimationTime = 0f;
            isPlayingReloadAnimation = true;
        }
        
        // Update the reload animation
        void UpdateReloadAnimation()
        {
            if (!isPlayingReloadAnimation) return;
            
            // Update animation time - use reloadTime directly for timing
            reloadAnimationTime += Time.deltaTime;
            
            // Get animation progress (0 to 1) based on reload time
            float t = reloadAnimationTime / reloadTime;
            
            // Clamp and check for completion
            if (t >= 1f)
            {
                t = 1f;
                if (!isReloading)
                {
                    StopReloadAnimation();
                    return;
                }
            }
            
            // Get curve value for smooth movement
            float curveValue = reloadCurve.Evaluate(t);
            
            // Apply position change - move down and slightly back
            Vector3 targetPos = originalPosition;
            targetPos.y -= reloadDownAmount * curveValue;
            targetPos.z += reloadDownAmount * 0.5f * curveValue;
            
            // Apply rotation change - tilt the weapon
            Quaternion targetRot = originalRotation * Quaternion.Euler(-reloadRotationAmount * curveValue, 0f, 0f);
            
            // Apply the position and rotation
            weaponModel.transform.localPosition = targetPos;
            weaponModel.transform.localRotation = targetRot;
        }
        
        // Stop the reload animation
        void StopReloadAnimation()
        {
            isPlayingReloadAnimation = false;
            
            // Restore original position and rotation
            if (weaponModel != null)
            {
                weaponModel.transform.localPosition = originalPosition;
                weaponModel.transform.localRotation = originalRotation;
            }
        }

        // Çarpma efektini kodda oluştur
        private void CreateImpactEffect(Vector3 position, Vector3 normal)
        {
            // Create empty GameObject for the impact effect
            GameObject impactObj = new GameObject("BulletImpact");
            impactObj.transform.position = position;
            impactObj.transform.rotation = Quaternion.LookRotation(normal);
            
            // Add URP material if needed (for any additional renderers)
            Renderer[] renderers = impactObj.GetComponentsInChildren<Renderer>();
            if (renderers != null && renderers.Length > 0)
            {
                // Try to find our impact material first
                Material impactMaterial = null;
                
                // Look for any renderer using our impact material
                Renderer[] allRenderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
                foreach (Renderer rend in allRenderers)
                {
                    if (rend.sharedMaterial != null && 
                        rend.sharedMaterial.name != null &&
                        rend.sharedMaterial.name.Contains("BulletImpactMaterial_URP"))
                    {
                        impactMaterial = rend.sharedMaterial;
                        break;
                    }
                }
                
                // Apply material if found
                if (impactMaterial != null)
                {
                    foreach (Renderer r in renderers)
                    {
                        r.material = impactMaterial;
                    }
                }
            }
            
            // Impact Effect bileşenini ekle
            ImpactEffect impactEffect = impactObj.AddComponent<ImpactEffect>();
            impactEffect.duration = 0.3f;
            impactEffect.startScale = 0.1f;
            impactEffect.endScale = 0.05f;
            
            Debug.Log("Impact effect created at: " + position);
        }
    }
}