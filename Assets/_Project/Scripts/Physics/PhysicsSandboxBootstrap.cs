using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KaleidoscopeEngine.PhysicsSandbox
{
    public static class PhysicsSandboxBootstrap
    {
        private const string PhysicsSandboxSceneName = "01_PhysicsSandbox";
        private const string ChamberLayerName = "KaleidoscopeChamber";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BuildWhenSceneLoads()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != PhysicsSandboxSceneName)
            {
                return;
            }

            if (Object.FindObjectOfType<GemstoneSpawner>() != null)
            {
                return;
            }

            BuildSandbox();
        }

        private static void BuildSandbox()
        {
            // Required project layers:
            // 8  KaleidoscopeChamber
            // 9  KaleidoscopeGem
            // 10 KaleidoscopeMicroParticle
            // Collision matrix recommendation: chamber collides with gem and micro particle;
            // gems and micro particles may collide with each other for Stage 1 visual richness.
            Physics.defaultSolverIterations = Mathf.Max(Physics.defaultSolverIterations, 10);
            Physics.defaultSolverVelocityIterations = Mathf.Max(Physics.defaultSolverVelocityIterations, 4);
            Physics.defaultContactOffset = Mathf.Min(Physics.defaultContactOffset, 0.01f);

            GameObject root = new GameObject("PhysicsSandboxRoot");
            GameObject chamber = new GameObject("Chamber");
            chamber.transform.SetParent(root.transform, false);

            BuildBoxChamber(chamber.transform);

            GameObject spawnVolume = new GameObject("SpawnVolume");
            spawnVolume.transform.SetParent(root.transform, false);
            spawnVolume.transform.localPosition = new Vector3(0f, 0.55f, 0f);

            GameObject gemstoneParent = new GameObject("GemstoneParent");
            gemstoneParent.transform.SetParent(root.transform, false);

            KaleidoscopePhysicsChamber physicsChamber = root.AddComponent<KaleidoscopePhysicsChamber>();
            physicsChamber.SetChamberTransform(chamber.transform);

            GemstoneSpawner spawner = root.AddComponent<GemstoneSpawner>();
            spawner.SetSpawnVolume(spawnVolume.transform);
            spawner.SetParentTransform(gemstoneParent.transform);
            spawner.SpawnVolumeSize = new Vector3(3.4f, 1.9f, 3.4f);
            spawner.SetDefinitions(CreateRuntimeDefinitions());

            PhysicsSandboxInput input = root.AddComponent<PhysicsSandboxInput>();
            input.Configure(physicsChamber, spawner);

            PhysicsSandboxDebugHUD hud = root.AddComponent<PhysicsSandboxDebugHUD>();
            hud.Configure(physicsChamber, spawner);

            ConfigureCamera();
        }

        private static void BuildBoxChamber(Transform chamber)
        {
            int chamberLayer = LayerMask.NameToLayer(ChamberLayerName);
            if (chamberLayer < 0)
            {
                chamberLayer = 0;
            }

            Vector3 size = new Vector3(4.2f, 2.6f, 4.2f);
            float wall = 0.18f;

            CreateWall("Floor", chamber, chamberLayer, new Vector3(0f, -size.y * 0.5f, 0f), new Vector3(size.x, wall, size.z));
            CreateWall("Ceiling", chamber, chamberLayer, new Vector3(0f, size.y * 0.5f, 0f), new Vector3(size.x, wall, size.z));
            CreateWall("LeftWall", chamber, chamberLayer, new Vector3(-size.x * 0.5f, 0f, 0f), new Vector3(wall, size.y, size.z));
            CreateWall("RightWall", chamber, chamberLayer, new Vector3(size.x * 0.5f, 0f, 0f), new Vector3(wall, size.y, size.z));
            CreateWall("BackWall", chamber, chamberLayer, new Vector3(0f, 0f, size.z * 0.5f), new Vector3(size.x, size.y, wall));
            CreateWall("FrontWall", chamber, chamberLayer, new Vector3(0f, 0f, -size.z * 0.5f), new Vector3(size.x, size.y, wall), true);
        }

        private static void CreateWall(string name, Transform parent, int layer, Vector3 localPosition, Vector3 localScale, bool transparent = false)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.layer = layer;
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = localPosition;
            wall.transform.localScale = localScale;

            Renderer renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color color = transparent ? new Color(0.66f, 0.9f, 1f, 0.2f) : new Color(0.45f, 0.55f, 0.62f, 0.18f);
                renderer.sharedMaterial = CreateTransparentMaterial("Chamber Shell", color);
            }

            Collider collider = wall.GetComponent<Collider>();
            if (collider != null)
            {
                collider.sharedMaterial = new PhysicMaterial("Chamber High Friction")
                {
                    dynamicFriction = 0.82f,
                    staticFriction = 0.95f,
                    bounciness = 0.03f,
                    frictionCombine = PhysicMaterialCombine.Maximum,
                    bounceCombine = PhysicMaterialCombine.Minimum
                };
            }
        }

        private static List<GemstoneDefinition> CreateRuntimeDefinitions()
        {
            return new List<GemstoneDefinition>
            {
                Definition("opal", "Opal", GemstoneShapeHint.Rounded, new Color(0.74f, 0.95f, 1f, 0.78f), new Vector3(0.18f, 0.14f, 0.2f), new Vector3(0.36f, 0.28f, 0.38f), 0.11f, 0.32f, 0.86f, 0.06f, 1.2f, GemstoneParticleCategory.Gem),
                Definition("ruby", "Ruby", GemstoneShapeHint.Faceted, new Color(0.85f, 0.04f, 0.08f, 0.9f), new Vector3(0.16f, 0.12f, 0.2f), new Vector3(0.34f, 0.24f, 0.44f), 0.16f, 0.56f, 0.72f, 0.08f, 1f, GemstoneParticleCategory.HeavyAnchor),
                Definition("emerald", "Emerald", GemstoneShapeHint.Elongated, new Color(0.03f, 0.63f, 0.32f, 0.86f), new Vector3(0.14f, 0.22f, 0.14f), new Vector3(0.28f, 0.52f, 0.28f), 0.15f, 0.42f, 0.78f, 0.05f, 1f, GemstoneParticleCategory.Gem),
                Definition("amethyst", "Amethyst", GemstoneShapeHint.Shard, new Color(0.48f, 0.18f, 0.86f, 0.88f), new Vector3(0.18f, 0.08f, 0.12f), new Vector3(0.48f, 0.2f, 0.32f), 0.09f, 0.28f, 0.96f, 0.04f, 1.1f, GemstoneParticleCategory.Gem),
                Definition("quartz", "Quartz", GemstoneShapeHint.Shard, new Color(0.92f, 0.96f, 1f, 0.62f), new Vector3(0.15f, 0.08f, 0.1f), new Vector3(0.44f, 0.22f, 0.28f), 0.08f, 0.24f, 0.68f, 0.03f, 1f, GemstoneParticleCategory.Slider),
                Definition("glass_fragment", "Glass Fragment", GemstoneShapeHint.ThinShard, new Color(0.65f, 0.92f, 1f, 0.5f), new Vector3(0.18f, 0.035f, 0.08f), new Vector3(0.52f, 0.08f, 0.22f), 0.03f, 0.12f, 0.55f, 0.02f, 0.9f, GemstoneParticleCategory.GlassFragment),
                Definition("micro_particle", "Micro Particle", GemstoneShapeHint.Pebble, new Color(0.82f, 0.82f, 0.86f, 0.78f), new Vector3(0.035f, 0.025f, 0.035f), new Vector3(0.095f, 0.07f, 0.095f), 0.006f, 0.03f, 1.05f, 0.01f, 3.1f, GemstoneParticleCategory.MicroParticle),
                Definition("dust", "Dust", GemstoneShapeHint.Pebble, new Color(0.7f, 0.73f, 0.78f, 0.62f), new Vector3(0.018f, 0.014f, 0.018f), new Vector3(0.042f, 0.032f, 0.042f), 0.002f, 0.008f, 1.25f, 0f, 1.8f, GemstoneParticleCategory.Dust)
            };
        }

        private static GemstoneDefinition Definition(
            string id,
            string displayName,
            GemstoneShapeHint shape,
            Color color,
            Vector3 minScale,
            Vector3 maxScale,
            float minMass,
            float maxMass,
            float friction,
            float bounciness,
            float spawnWeight,
            GemstoneParticleCategory category)
        {
            GemstoneDefinition definition = ScriptableObject.CreateInstance<GemstoneDefinition>();
            definition.id = id;
            definition.displayName = displayName;
            definition.shapeHint = shape;
            definition.placeholderColor = color;
            definition.minScale = minScale;
            definition.maxScale = maxScale;
            definition.minMass = minMass;
            definition.maxMass = maxMass;
            definition.friction = friction;
            definition.bounciness = bounciness;
            definition.spawnWeight = spawnWeight;
            definition.particleCategory = category;
            ApplyCategoryTuning(definition, category);

            return definition;
        }

        private static void ApplyCategoryTuning(GemstoneDefinition definition, GemstoneParticleCategory category)
        {
            switch (category)
            {
                case GemstoneParticleCategory.HeavyAnchor:
                    definition.density = 12.5f;
                    definition.centerOfMassOffsetRange = new Vector3(0.045f, 0.035f, 0.045f);
                    definition.angularVelocityRange = new Vector2(0.35f, 4f);
                    definition.spawnImpulseRange = new Vector2(0.01f, 0.045f);
                    definition.sleepThreshold = 0.025f;
                    definition.lowMotionWakeThreshold = 0.035f;
                    definition.restlessness = 0.003f;
                    definition.inertiaLag = 0.18f;
                    break;

                case GemstoneParticleCategory.Slider:
                    definition.density = 7f;
                    definition.centerOfMassOffsetRange = new Vector3(0.035f, 0.018f, 0.04f);
                    definition.angularVelocityRange = new Vector2(1f, 9f);
                    definition.spawnImpulseRange = new Vector2(0.025f, 0.11f);
                    definition.sleepThreshold = 0.045f;
                    definition.lowMotionWakeThreshold = 0.06f;
                    definition.restlessness = 0.01f;
                    definition.inertiaLag = 0.42f;
                    break;

                case GemstoneParticleCategory.GlassFragment:
                    definition.density = 5.2f;
                    definition.centerOfMassOffsetRange = new Vector3(0.055f, 0.01f, 0.035f);
                    definition.angularVelocityRange = new Vector2(1.4f, 11f);
                    definition.spawnImpulseRange = new Vector2(0.04f, 0.16f);
                    definition.sleepThreshold = 0.05f;
                    definition.lowMotionWakeThreshold = 0.075f;
                    definition.restlessness = 0.016f;
                    definition.inertiaLag = 0.58f;
                    break;

                case GemstoneParticleCategory.MicroParticle:
                    definition.density = 3.8f;
                    definition.centerOfMassOffsetRange = new Vector3(0.005f, 0.004f, 0.005f);
                    definition.angularVelocityRange = new Vector2(2f, 13f);
                    definition.spawnImpulseRange = new Vector2(0.025f, 0.12f);
                    definition.sleepThreshold = 0.075f;
                    definition.lowMotionWakeThreshold = 0.11f;
                    definition.restlessness = 0.022f;
                    definition.inertiaLag = 0.72f;
                    break;

                case GemstoneParticleCategory.Dust:
                    definition.density = 2.1f;
                    definition.centerOfMassOffsetRange = new Vector3(0.002f, 0.002f, 0.002f);
                    definition.angularVelocityRange = new Vector2(5f, 18f);
                    definition.spawnImpulseRange = new Vector2(0.04f, 0.18f);
                    definition.sleepThreshold = 0.09f;
                    definition.lowMotionWakeThreshold = 0.13f;
                    definition.restlessness = 0.03f;
                    definition.inertiaLag = 0.85f;
                    break;

                default:
                    definition.density = 8f;
                    definition.centerOfMassOffsetRange = new Vector3(0.035f, 0.025f, 0.035f);
                    definition.angularVelocityRange = new Vector2(0.8f, 8f);
                    definition.spawnImpulseRange = new Vector2(0.015f, 0.085f);
                    definition.sleepThreshold = 0.035f;
                    definition.lowMotionWakeThreshold = 0.055f;
                    definition.restlessness = 0.007f;
                    definition.inertiaLag = 0.32f;
                    break;
            }
        }

        private static Material CreateTransparentMaterial(string name, Color color)
        {
            Material material = new Material(Shader.Find("HDRP/Lit") ?? Shader.Find("Standard"))
            {
                name = name,
                color = color
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_SurfaceType"))
            {
                material.SetFloat("_SurfaceType", 1f);
            }

            if (material.HasProperty("_AlphaCutoffEnable"))
            {
                material.SetFloat("_AlphaCutoffEnable", 0f);
            }

            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return material;
        }

        private static void ConfigureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.transform.position = new Vector3(0f, 2.25f, -6.3f);
            camera.transform.rotation = Quaternion.Euler(17f, 0f, 0f);
            camera.fieldOfView = 45f;
        }
    }
}
