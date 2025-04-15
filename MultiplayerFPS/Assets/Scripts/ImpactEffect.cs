using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

namespace FPSGame
{
    public class ImpactEffect : MonoBehaviour
    {
        public float duration = 0.3f;
        public float expandSpeed = 0.3f;
        public float startScale = 0.1f;
        public float endScale = 0.05f;
        
        private float timeAlive = 0f;
        private ParticleSystem mainParticleSystem;
        private ParticleSystem sparkParticleSystem;
        private Light impactLight;
        
        void Awake()
        {
            // Setup all particle systems before Start runs
            SetupMainParticleSystem();
            SetupSparkParticleSystem();
            SetupLight();
        }
        
        void Start()
        {
            // Play all particle systems after setup is complete
            if (mainParticleSystem != null)
            {
                mainParticleSystem.Play();
            }
            
            if (sparkParticleSystem != null)
            {
                sparkParticleSystem.Play();
            }
        }
        
        private void SetupMainParticleSystem()
        {
            // Create main particle system
            mainParticleSystem = gameObject.AddComponent<ParticleSystem>();
            
            // Stop the system initially
            mainParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            
            // Main module settings
            var main = mainParticleSystem.main;
            main.duration = 0.2f;
            main.loop = false;
            main.startLifetime = 0.3f;
            main.startSpeed = 1.5f;
            main.startSize = 0.12f;
            main.startColor = new Color(1f, 0.6f, 0f, 1f); // Bright orange
            main.maxParticles = 20;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false; // Don't play automatically
            
            // Emission settings
            var emission = mainParticleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(
                new ParticleSystem.Burst[]{ 
                    new ParticleSystem.Burst(0f, 10)
                }
            );
            
            // Shape settings - emit in a cone facing outward
            var shape = mainParticleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 20f;
            shape.radius = 0.05f;
            
            // Color over lifetime - start orange, fade to red
            var colorOverLifetime = mainParticleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            
            Gradient colorGradient = new Gradient();
            colorGradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(1f, 0.7f, 0f), 0.0f), // Bright orange/yellow
                    new GradientColorKey(new Color(1f, 0.3f, 0f), 1.0f)  // Darker orange/red
                }, 
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1.0f, 0.0f), 
                    new GradientAlphaKey(0.0f, 1.0f) 
                }
            );
            colorOverLifetime.color = colorGradient;
            
            // Size over lifetime - start small, expand, then shrink
            var sizeOverLifetime = mainParticleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            
            AnimationCurve sizeOverLifetimeCurve = new AnimationCurve();
            sizeOverLifetimeCurve.AddKey(0f, 0.3f);
            sizeOverLifetimeCurve.AddKey(0.3f, 0.7f);
            sizeOverLifetimeCurve.AddKey(1f, 0f);
            
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(0.7f, sizeOverLifetimeCurve);
            
            // Renderer settings
            var renderer = mainParticleSystem.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            renderer.material.SetColor("_BaseColor", new Color(1f, 0.6f, 0f, 1f));
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", new Color(1f, 0.6f, 0f, 1f) * 2f);
        }
        
        private void SetupSparkParticleSystem()
        {
            // Create a child game object for sparks
            GameObject sparkObj = new GameObject("Sparks");
            sparkObj.transform.parent = transform;
            sparkObj.transform.localPosition = Vector3.zero;
            sparkObj.transform.localRotation = Quaternion.identity;
            
            // Add particle system
            sparkParticleSystem = sparkObj.AddComponent<ParticleSystem>();
            
            // Stop the system initially
            sparkParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            
            // Main module settings
            var main = sparkParticleSystem.main;
            main.duration = 0.15f;
            main.loop = false;
            main.startLifetime = 0.2f;
            main.startSpeed = 3f;
            main.startSize = 0.03f;
            main.startColor = new Color(1f, 0.8f, 0.3f, 1f); // Yellow
            main.maxParticles = 15;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false; // Don't play automatically
            
            // Emission settings
            var emission = sparkParticleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(
                new ParticleSystem.Burst[]{ 
                    new ParticleSystem.Burst(0f, 12)
                }
            );
            
            // Shape settings - emit from a point in all directions
            var shape = sparkParticleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.01f;
            
            // Velocity over lifetime - make sparks shoot out in all directions
            var velocityOverLifetime = sparkParticleSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.speedModifier = 0.8f;
            
            // Trail module for spark trails
            var trails = sparkParticleSystem.trails;
            trails.enabled = true;
            trails.ratio = 0.5f;
            trails.lifetime = 0.08f;
            trails.minVertexDistance = 0.05f;
            trails.widthOverTrail = 0.05f;
            
            // Renderer settings
            var renderer = sparkParticleSystem.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 1.5f;
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            renderer.material.SetColor("_BaseColor", new Color(1f, 0.8f, 0.2f, 1f));
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.2f, 1f) * 2.5f);
        }
        
        private void SetupLight()
        {
            // Add light component
            impactLight = gameObject.AddComponent<Light>();
            impactLight.color = new Color(1f, 0.6f, 0.2f); // Orange
            impactLight.intensity = 2f;
            impactLight.range = 1.5f;
            
            // Set light to use URP settings
            impactLight.renderMode = LightRenderMode.Auto;
            impactLight.shadows = LightShadows.None;
        }
        
        void Update()
        {
            // Update time alive
            timeAlive += Time.deltaTime;
            
            // Fade out the light
            if (impactLight != null)
            {
                // Quickly brighten then fade
                float lightIntensity = timeAlive < 0.04f ? 
                    Mathf.Lerp(2f, 3f, timeAlive / 0.04f) :
                    Mathf.Lerp(3f, 0f, (timeAlive - 0.04f) / (duration - 0.04f));
                
                impactLight.intensity = lightIntensity;
            }
            
            // Destroy after duration
            if (timeAlive >= duration)
            {
                Destroy(gameObject);
            }
        }
    }
}