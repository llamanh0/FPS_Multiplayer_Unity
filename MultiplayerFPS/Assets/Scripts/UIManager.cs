using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FPSGame
{
    public class UIManager : MonoBehaviour
    {
        [Header("Player UI References")]
        public Image healthBar;
        public TextMeshProUGUI ammoText;
        public TextMeshProUGUI healthText;
        public Image crosshair;
        public GameObject hitMarker;
        public Image damageVignette;

        [Header("Crosshair Settings")]
        public float crosshairSize = 20f;
        public Color crosshairColor = Color.white;
        public Sprite crosshairSprite;

        [Header("HUD Animation")]
        public float hitMarkerDuration = 0.3f;
        public float damageVignetteDuration = 1.0f;

        // Private references
        private FPSController playerController;
        private WeaponController weaponController;
        private float hitMarkerTimer;
        private float damageVignetteTimer;
        private float damageVignetteAlpha;

        void Start()
        {
            // Find player and weapon references
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerController = player.GetComponent<FPSController>();

                // Find weapon controller
                GameObject weaponHolder = GameObject.Find("WeaponHolder");
                if (weaponHolder != null)
                {
                    weaponController = weaponHolder.GetComponent<WeaponController>();
                }
            }

            // Setup crosshair if available
            if (crosshair != null && crosshairSprite != null)
            {
                crosshair.sprite = crosshairSprite;
                crosshair.rectTransform.sizeDelta = new Vector2(crosshairSize, crosshairSize);
                crosshair.color = crosshairColor;
            }

            // Hide the hit marker and damage vignette
            if (hitMarker != null)
                hitMarker.SetActive(false);

            if (damageVignette != null)
            {
                Color c = damageVignette.color;
                c.a = 0f;
                damageVignette.color = c;
            }
        }

        void Update()
        {
            UpdatePlayerUI();
            UpdateHitMarker();
            UpdateDamageVignette();
        }

        void UpdatePlayerUI()
        {
            if (playerController != null)
            {
                // Update health UI
                if (healthBar != null)
                {
                    healthBar.fillAmount = (float)playerController.currentHealth / playerController.maxHealth;
                }

                if (healthText != null)
                {
                    healthText.text = playerController.currentHealth.ToString();
                }
            }

            if (weaponController != null)
            {
                // Update ammo UI
                if (ammoText != null)
                {
                    ammoText.text = weaponController.currentAmmo + " / " + weaponController.maxAmmo;
                }
            }
        }

        void UpdateHitMarker()
        {
            if (hitMarker != null && hitMarkerTimer > 0)
            {
                hitMarkerTimer -= Time.deltaTime;
                
                if (hitMarkerTimer <= 0)
                {
                    hitMarker.SetActive(false);
                }
            }
        }

        void UpdateDamageVignette()
        {
            if (damageVignette != null && damageVignetteTimer > 0)
            {
                damageVignetteTimer -= Time.deltaTime;
                
                // Fade out the damage vignette
                Color c = damageVignette.color;
                c.a = Mathf.Lerp(0, damageVignetteAlpha, damageVignetteTimer / damageVignetteDuration);
                damageVignette.color = c;
            }
        }

        // Public methods called by other scripts
        public void ShowHitMarker()
        {
            if (hitMarker != null)
            {
                hitMarker.SetActive(true);
                hitMarkerTimer = hitMarkerDuration;
            }
        }

        public void ShowDamageVignette(float intensity)
        {
            if (damageVignette != null)
            {
                damageVignetteAlpha = Mathf.Clamp01(intensity);
                damageVignetteTimer = damageVignetteDuration;
                
                Color c = damageVignette.color;
                c.a = damageVignetteAlpha;
                damageVignette.color = c;
            }
        }
    }
}