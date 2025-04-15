using UnityEngine;
using System.Collections;

namespace FPSGame
{
    public class FPSController : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float walkSpeed = 5.0f;
        public float sprintSpeed = 10.0f;
        public float crouchSpeed = 2.5f;
        public float jumpForce = 5.0f;
        public float gravity = 20.0f;
        public float lookSensitivity = 2.0f;
        public float lookXLimit = 45.0f;
        public float footstepInterval = 0.5f;

        [Header("Camera Effects")]
        public float headBobSpeed = 10f;
        public float headBobAmount = 0.1f;
        public float landingImpactAmount = 0.2f;
        public float sprintFOVModifier = 1.15f;
        public float aimingFOVModifier = 0.8f;
        
        [Header("References")]
        public Camera playerCamera;
        public AudioSource audioSource;
        public AudioClip[] footstepSounds;
        public AudioClip jumpSound;
        public AudioClip landSound;

        [Header("Player Stats")]
        public int maxHealth = 100;
        public int currentHealth;
        
        // Private variables
        private CharacterController characterController;
        private Vector3 moveDirection = Vector3.zero;
        private float rotationX = 0;
        private bool canMove = true;
        private bool isSprinting = false;
        private bool isCrouching = false;
        private bool isGrounded;
        private bool wasGrounded;
        private float defaultHeight;
        private float crouchHeight = 1.0f;
        private Vector3 defaultCameraPos;
        private Vector3 crouchCameraPos;
        private float defaultY = 0;
        private float timer = 0;
        private float footstepTimer = 0;
        private float defaultFOV;
        private float targetFOV;
        private bool isAiming = false;
        private UIManager uiManager;

        void Start()
        {
            characterController = GetComponent<CharacterController>();
            defaultHeight = characterController.height;
            
            // Auto-assign camera if not set
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }
            
            // Store default positions
            defaultCameraPos = playerCamera.transform.localPosition;
            crouchCameraPos = new Vector3(defaultCameraPos.x, defaultCameraPos.y - 0.5f, defaultCameraPos.z);
            defaultY = playerCamera.transform.localPosition.y;
            defaultFOV = playerCamera.fieldOfView;
            targetFOV = defaultFOV;
            
            // Initialize health
            currentHealth = maxHealth;
            
            // Find UI Manager
            uiManager = Object.FindAnyObjectByType<UIManager>();
            
            // Setup audio source if needed
            if (audioSource == null && footstepSounds != null && footstepSounds.Length > 0)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0.8f; // Mostly 3D
                audioSource.volume = 0.7f;
                audioSource.playOnAwake = false;
            }
            
            // Lock cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            if (!canMove)
                return;

            // Store grounded state for landing detection
            wasGrounded = isGrounded;
            isGrounded = characterController.isGrounded;
            
            // Play landing sound when hitting the ground
            if (!wasGrounded && isGrounded)
            {
                PlayLandingSound();
                StartCoroutine(LandingImpact());
            }

            // Handle crouching
            if (Input.GetKeyDown(KeyCode.C))
            {
                ToggleCrouch();
            }

            // Handle sprinting
            isSprinting = Input.GetKey(KeyCode.LeftShift) && !isCrouching;
            
            // Handle aiming
            isAiming = Input.GetMouseButton(1); // Right mouse button
            
            // Update FOV based on sprint/aim state
            UpdateFOV();

            // Camera rotation
            rotationX += -Input.GetAxis("Mouse Y") * lookSensitivity;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSensitivity, 0);

            // Movement
            float currentSpeed = isSprinting ? sprintSpeed : (isCrouching ? crouchSpeed : walkSpeed);
            
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right);
            
            float curSpeedX = currentSpeed * Input.GetAxis("Vertical");
            float curSpeedY = currentSpeed * Input.GetAxis("Horizontal");
            float movementDirectionY = moveDirection.y;
            moveDirection = (forward * curSpeedX) + (right * curSpeedY);

            // Apply headbobbing when moving
            if ((curSpeedX != 0 || curSpeedY != 0) && characterController.isGrounded)
            {
                // Increase timer speed based on movement speed
                float bobSpeed = isSprinting ? headBobSpeed * 1.5f : headBobSpeed;
                timer += Time.deltaTime * bobSpeed;
                
                // Apply head bob effect
                float bobAmount = isSprinting ? headBobAmount * 1.5f : headBobAmount;
                playerCamera.transform.localPosition = new Vector3(
                    playerCamera.transform.localPosition.x,
                    defaultY + Mathf.Sin(timer) * bobAmount,
                    playerCamera.transform.localPosition.z);
                
                // Play footstep sounds
                footstepTimer -= Time.deltaTime;
                if (footstepTimer <= 0)
                {
                    PlayFootstepSound();
                    footstepTimer = isSprinting ? footstepInterval * 0.6f : footstepInterval;
                }
            }
            else
            {
                // Reset to default position when not moving
                timer = 0;
            }

            // Jump
            if (Input.GetButton("Jump") && characterController.isGrounded)
            {
                moveDirection.y = jumpForce;
                PlayJumpSound();
            }
            else
            {
                moveDirection.y = movementDirectionY;
            }

            // Apply gravity
            if (!characterController.isGrounded)
            {
                moveDirection.y -= gravity * Time.deltaTime;
            }

            // Move the controller
            characterController.Move(moveDirection * Time.deltaTime);
        }

        void UpdateFOV()
        {
            // Calculate target FOV based on states
            if (isAiming)
            {
                targetFOV = defaultFOV * aimingFOVModifier;
            }
            else if (isSprinting)
            {
                targetFOV = defaultFOV * sprintFOVModifier;
            }
            else
            {
                targetFOV = defaultFOV;
            }
            
            // Smooth FOV transition
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * 10f);
        }

        void ToggleCrouch()
        {
            isCrouching = !isCrouching;
            
            if (isCrouching)
            {
                characterController.height = crouchHeight;
                playerCamera.transform.localPosition = crouchCameraPos;
            }
            else
            {
                characterController.height = defaultHeight;
                playerCamera.transform.localPosition = defaultCameraPos;
            }
        }
        
        void PlayFootstepSound()
        {
            if (audioSource != null && footstepSounds != null && footstepSounds.Length > 0)
            {
                // Select random footstep sound
                AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
                if (clip != null)
                {
                    audioSource.PlayOneShot(clip, isSprinting ? 0.8f : 0.5f);
                }
            }
        }
        
        void PlayJumpSound()
        {
            if (audioSource != null && jumpSound != null)
            {
                audioSource.PlayOneShot(jumpSound, 0.7f);
            }
        }
        
        void PlayLandingSound()
        {
            if (audioSource != null && landSound != null)
            {
                audioSource.PlayOneShot(landSound, 0.5f);
            }
        }
        
        IEnumerator LandingImpact()
        {
            float elapsed = 0f;
            float duration = 0.15f;
            
            Vector3 originalPos = playerCamera.transform.localPosition;
            Vector3 targetPos = new Vector3(originalPos.x, originalPos.y - landingImpactAmount, originalPos.z);
            
            // Quick down movement
            while (elapsed < duration)
            {
                playerCamera.transform.localPosition = Vector3.Lerp(originalPos, targetPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            elapsed = 0f;
            
            // Slower up movement
            while (elapsed < duration * 2)
            {
                playerCamera.transform.localPosition = Vector3.Lerp(targetPos, originalPos, elapsed / (duration * 2));
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Ensure we return to the exact original position
            playerCamera.transform.localPosition = originalPos;
        }
        
        public void TakeDamage(int damage)
        {
            currentHealth -= damage;
            
            // Show damage vignette in UI
            if (uiManager != null)
            {
                float damageIntensity = Mathf.Clamp01((float)damage / 30f); // Scale based on damage amount
                uiManager.ShowDamageVignette(damageIntensity);
            }
            
            if (currentHealth <= 0)
            {
                Die();
            }
        }
        
        void Die()
        {
            // Death logic here
            Debug.Log("Player died");
            canMove = false;
            
            // Example: Disable control
            characterController.enabled = false;
            
            // Example: Enable spectator mode or restart level
            StartCoroutine(RespawnAfterDelay(3f));
        }
        
        IEnumerator RespawnAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            // Reset health
            currentHealth = maxHealth;
            
            // Re-enable control
            characterController.enabled = true;
            canMove = true;
            
            // Reset position (example)
            transform.position = new Vector3(0, 1, 0);
        }
        
        // Called by WeaponController for recoil
        public void AddRecoil(float amount, bool isAiming)
        {
            // Apply camera recoil based on whether we're aiming or not
            float recoilModifier = isAiming ? 0.3f : 1.0f;
            rotationX -= amount * recoilModifier;
        }
    }
}