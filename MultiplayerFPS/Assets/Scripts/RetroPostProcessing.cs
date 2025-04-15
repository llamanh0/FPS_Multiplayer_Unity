using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FPSGame
{
    [RequireComponent(typeof(Camera))]
    public class RetroPostProcessing : MonoBehaviour
    {
        [Header("Basic Settings")]
        public bool usePostProcessing = true;
        public bool useLowResolution = true;
        
        [Header("Resolution Settings")]
        public int targetWidth = 320;
        public int targetHeight = 240;
        public FilterMode filterMode = FilterMode.Point;

        // Post-processing effects for the retro look
        private Vignette vignette;
        private ChromaticAberration chromaticAberration;
        private FilmGrain filmGrain;
        private ColorAdjustments colorAdjustments;

        private GameObject volumeObject;
        private Volume globalVolume;
        private RenderTexture lowResRenderTexture;
        private Camera cam;

        void Start()
        {
            cam = GetComponent<Camera>();
            
            if (useLowResolution)
            {
                // Create low resolution render texture with depth buffer
                lowResRenderTexture = new RenderTexture(targetWidth, targetHeight, 24);
                lowResRenderTexture.filterMode = filterMode;
                cam.targetTexture = lowResRenderTexture;
            }
            
            if (usePostProcessing)
            {
                SetupPostProcessingVolume();
            }
        }

        void SetupPostProcessingVolume()
        {
            // Find existing volume or use the one in scene
            volumeObject = GameObject.Find("PostProcessing");
            
            if (volumeObject == null)
            {
                // Create a new global volume if not found
                volumeObject = new GameObject("PostProcessing");
                globalVolume = volumeObject.AddComponent<Volume>();
            }
            else
            {
                globalVolume = volumeObject.GetComponent<Volume>();
                if (globalVolume == null)
                {
                    globalVolume = volumeObject.AddComponent<Volume>();
                }
            }

            // Setup the volume
            globalVolume.isGlobal = true;
            globalVolume.priority = 1;

            // Create a profile if it doesn't exist
            if (globalVolume.profile == null)
            {
                globalVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            }

            // Add post-processing effects
            // Chromatic Aberration
            if (!globalVolume.profile.TryGet(out chromaticAberration))
            {
                chromaticAberration = globalVolume.profile.Add<ChromaticAberration>(true);
            }
            chromaticAberration.intensity.value = 0.3f;

            // Film Grain
            if (!globalVolume.profile.TryGet(out filmGrain))
            {
                filmGrain = globalVolume.profile.Add<FilmGrain>(true);
            }
            filmGrain.intensity.value = 0.4f;
            filmGrain.response.value = 0.8f;

            // Vignette
            if (!globalVolume.profile.TryGet(out vignette))
            {
                vignette = globalVolume.profile.Add<Vignette>(true);
            }
            vignette.intensity.value = 0.35f;
            vignette.smoothness.value = 0.5f;
            vignette.color.value = Color.black;

            // Color Adjustments
            if (!globalVolume.profile.TryGet(out colorAdjustments))
            {
                colorAdjustments = globalVolume.profile.Add<ColorAdjustments>(true);
            }
            colorAdjustments.contrast.value = 10f;
            colorAdjustments.saturation.value = 20f;
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (useLowResolution && lowResRenderTexture != null)
            {
                // Blit to screen from our low resolution texture
                Graphics.Blit(lowResRenderTexture, destination);
            }
            else
            {
                // Pass through if not using low resolution
                Graphics.Blit(source, destination);
            }
        }

        void OnDisable()
        {
            if (cam != null)
            {
                cam.targetTexture = null;
            }
            
            if (lowResRenderTexture != null)
            {
                lowResRenderTexture.Release();
                Destroy(lowResRenderTexture);
            }
        }
    }
}