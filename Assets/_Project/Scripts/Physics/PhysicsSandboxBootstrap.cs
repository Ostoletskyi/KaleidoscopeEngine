using System.Collections.Generic;
using KaleidoscopeEngine.FX;
using KaleidoscopeEngine.Geometry;
using KaleidoscopeEngine.Lighting;
using KaleidoscopeEngine.Materials;
using KaleidoscopeEngine.Mirrors;
using KaleidoscopeEngine.Performance;
using KaleidoscopeEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KaleidoscopeEngine.PhysicsSandbox
{
    public static class PhysicsSandboxBootstrap
    {
        private const string PhysicsSandboxSceneName = "01_PhysicsSandbox";
        private const string ChamberLayerName = "KaleidoscopeChamber";
        private const string GemsLayerName = "KaleidoscopeGems";
        private const string ParticlesLayerName = "KaleidoscopeParticles";
        private const string OpticalFxLayerName = "KaleidoscopeOpticalFX";
        private const string ChamberVisualLayerName = "KaleidoscopeChamberVisual";
        private const string PhysicsOnlyLayerName = "KaleidoscopePhysicsOnly";
        private const string DebugLayerName = "KaleidoscopeDebug";
        private const float TubeRadius = 1.28f;
        private const float TubeLength = 5.4f;
        private const float TubeWallThickness = 0.16f;
        private const float TubeCapThickness = 0.2f;
        private const bool ShowFrontCap = false;
        private const bool ShowBackCap = false;
        private const bool ShowTubeVisual = true;
        private const float TubeTransparency = 0.18f;
        private const int TubeVisualSegments = 256;
        private const int TubeColliderSegments = 48;
        private static readonly bool InternalRibsEnabled = true;
        private const int InternalRibCount = 8;
        private const float InternalRibHeight = 0.045f;
        private const float InternalRibWidth = 0.075f;
        private const int InternalRibSegmentsPerTurn = 44;
        private const float InternalRibHelixTurns = 0.82f;
        private const float InternalRibHelixHandedness = 1f;
        private const float InternalRibBackBias = 0.45f;
        private static readonly bool ShowDepthGuideRings = true;
        private const bool DebugColliderVisibility = false;
        private static readonly Vector3 ChamberOuterSize = new Vector3(TubeLength, TubeRadius * 2f, TubeRadius * 2f);

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
            Physics.defaultSolverVelocityIterations = Mathf.Max(Physics.defaultSolverVelocityIterations, 6);
            Physics.defaultContactOffset = Mathf.Min(Physics.defaultContactOffset, 0.01f);
            Physics.defaultMaxDepenetrationVelocity = Mathf.Min(Physics.defaultMaxDepenetrationVelocity, 3.5f);

            GameObject root = new GameObject("PhysicsSandboxRoot");
            GameObject chamber = new GameObject("TubeChamber");
            chamber.transform.SetParent(root.transform, false);
            KaleidoscopeTubeChamberSettings tubeSettings = chamber.AddComponent<KaleidoscopeTubeChamberSettings>();
            tubeSettings.Configure(
                TubeRadius,
                TubeLength,
                TubeWallThickness,
                TubeCapThickness,
                ShowFrontCap,
                ShowBackCap,
                ShowTubeVisual,
                TubeTransparency,
                TubeVisualSegments,
                TubeColliderSegments,
                InternalRibsEnabled,
                InternalRibCount,
                InternalRibHeight,
                InternalRibWidth,
                ShowDepthGuideRings,
                DebugColliderVisibility);

            BuildTubeChamber(chamber.transform);

            GameObject spawnVolume = new GameObject("SpawnVolume");
            spawnVolume.transform.SetParent(root.transform, false);
            spawnVolume.transform.localPosition = Vector3.zero;

            GameObject gemstoneParent = new GameObject("GemstoneParent");
            gemstoneParent.transform.SetParent(root.transform, false);

            KaleidoscopePhysicsChamber physicsChamber = root.AddComponent<KaleidoscopePhysicsChamber>();
            physicsChamber.SetChamberTransform(chamber.transform);

            GemstoneMaterialAssigner materialAssigner = root.AddComponent<GemstoneMaterialAssigner>();
            materialAssigner.SetProfiles(GemstoneMaterialBootstrap.CreateDefaultProfiles());

            GemGeometryAssigner geometryAssigner = root.AddComponent<GemGeometryAssigner>();

            GemstoneSpawner spawner = root.AddComponent<GemstoneSpawner>();
            spawner.SetSpawnVolume(spawnVolume.transform);
            spawner.SetParentTransform(gemstoneParent.transform);
            spawner.SetMaterialAssigner(materialAssigner);
            spawner.SetGeometryAssigner(geometryAssigner);
            spawner.SetLayerNames(GemsLayerName, ParticlesLayerName);
            spawner.totalCount = 156;
            spawner.microParticleRatio = 0.58f;
            spawner.SpawnVolumeSize = new Vector3(TubeLength - TubeCapThickness * 3f, TubeRadius * 1.55f, TubeRadius * 1.55f);
            spawner.SetCylindricalSpawnVolume(TubeRadius * 0.82f, TubeLength - TubeCapThickness * 3f);
            spawner.SetDefinitions(CreateRuntimeDefinitions());

            EntropyCompressionVolume entropyCompression = root.AddComponent<EntropyCompressionVolume>();
            entropyCompression.Configure(chamber.transform, spawner, TubeRadius, TubeLength, PhysicsOnlyLayerName);

            PhysicsSandboxChamberDebugView debugView = root.AddComponent<PhysicsSandboxChamberDebugView>();
            debugView.Configure(chamber.transform);

            PhysicsSandboxMetrics metrics = root.AddComponent<PhysicsSandboxMetrics>();
            metrics.ConfigureTube(spawner, chamber.transform, TubeRadius, TubeLength);

            PhysicsSandboxCameraController cameraController = ConfigureCamera(chamber.transform);
            KaleidoscopeLightingRig lightingRig = ConfigureReadableLighting(root.transform, chamber.transform);
            GemSparkleController sparkleController = ConfigureSparkles(root.transform, spawner, lightingRig, cameraController);
            FakeCausticBunnyProjector causticProjector = ConfigureFakeCaustics(root.transform, chamber.transform, lightingRig, cameraController);
            OpticalSourceChamber opticalSourceChamber = ConfigureOpticalSourceChamber(root.transform, chamber.transform);
            KaleidoscopeMirrorController mirrorController = root.AddComponent<KaleidoscopeMirrorController>();
            KaleidoscopeRenderPipeline mirrorPipeline = ConfigureMirrorPipeline(root.transform, mirrorController, opticalSourceChamber);
            mirrorPipeline.ConfigureQualityTargets(spawner, sparkleController);
            spawner.SetSourceVisibilityCamera(mirrorPipeline.SourceCamera);
            sparkleController.ApplyVisibleSparkleTarget(spawner.VisibleSparkleTarget);

            PhysicsSandboxInput input = root.AddComponent<PhysicsSandboxInput>();
            input.Configure(physicsChamber, spawner);
            GameObject debugPanelObject = new GameObject("Kaleidoscope Debug Panel");
            debugPanelObject.transform.SetParent(root.transform, false);
            KaleidoscopeDebugPanel debugPanel = debugPanelObject.AddComponent<KaleidoscopeDebugPanel>();
            debugPanel.Configure(physicsChamber, spawner);
            debugPanel.ConfigureDebugSystems(cameraController, debugView, metrics, materialAssigner, lightingRig, geometryAssigner, sparkleController, causticProjector, mirrorPipeline, mirrorController, opticalSourceChamber, entropyCompression);
            debugPanel.ConfigureTubeSettings(tubeSettings);

            input.ConfigureDebugSystems(cameraController, debugView, metrics, materialAssigner, lightingRig, geometryAssigner, sparkleController, causticProjector, mirrorPipeline, mirrorController, debugPanel, opticalSourceChamber);

            GameObject operatorConsoleObject = new GameObject("Kaleidoscope Operator Console");
            operatorConsoleObject.transform.SetParent(root.transform, false);
            KaleidoscopeHelpOverlay helpOverlay = operatorConsoleObject.AddComponent<KaleidoscopeHelpOverlay>();
            helpOverlay.Configure(mirrorPipeline, mirrorController, spawner);
            GameObject performanceObject = new GameObject("Kaleidoscope Performance Governor");
            performanceObject.transform.SetParent(root.transform, false);
            KaleidoscopeFpsMonitor fpsMonitor = performanceObject.AddComponent<KaleidoscopeFpsMonitor>();
            AdaptiveQualityController adaptiveQuality = performanceObject.AddComponent<AdaptiveQualityController>();
            adaptiveQuality.Configure(fpsMonitor, mirrorPipeline, spawner, sparkleController, causticProjector, physicsChamber, helpOverlay);
            debugPanel.ConfigurePerformanceSystems(fpsMonitor, adaptiveQuality);
            KaleidoscopeInputRouter inputRouter = operatorConsoleObject.AddComponent<KaleidoscopeInputRouter>();
            inputRouter.Configure(
                physicsChamber,
                spawner,
                cameraController,
                mirrorPipeline,
                mirrorController,
                mirrorPipeline.ViewerCameraController,
                debugPanel,
                helpOverlay,
                opticalSourceChamber,
                adaptiveQuality);
        }

        private static void BuildTubeChamber(Transform chamber)
        {
            int visualLayer = ResolveLayer(ChamberVisualLayerName, ChamberLayerName);
            int physicsLayer = ResolveLayer(PhysicsOnlyLayerName, ChamberLayerName);

            GameObject visualRoot = new GameObject("TubeVisual");
            visualRoot.layer = visualLayer;
            visualRoot.transform.SetParent(chamber, false);
            visualRoot.SetActive(ShowTubeVisual);

            GameObject colliderRoot = new GameObject("TubeColliders");
            colliderRoot.layer = physicsLayer;
            colliderRoot.transform.SetParent(chamber, false);

            CreateTubeVisual(visualRoot.transform, visualLayer);
            CreateDepthGuideRings(visualRoot.transform, visualLayer);
            CreateTubeWallColliders(colliderRoot.transform, physicsLayer);
            CreateEndRingColliders(colliderRoot.transform, physicsLayer);
            CreateInternalRibs(colliderRoot.transform, physicsLayer);
            CreateTubeCap("FrontCap", colliderRoot.transform, physicsLayer, -TubeLength * 0.5f, ShowFrontCap, 0.035f);
            CreateTubeCap("BackCap", colliderRoot.transform, physicsLayer, TubeLength * 0.5f, ShowBackCap, 0.09f);
        }

        private static void CreateTubeVisual(Transform parent, int layer)
        {
            GameObject tube = new GameObject("Transparent Tube Shell");
            tube.name = "Transparent Tube Shell";
            tube.layer = layer;
            tube.transform.SetParent(parent, false);

            MeshFilter meshFilter = tube.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateOpenTubeMesh(TubeRadius, TubeLength, TubeVisualSegments);

            MeshRenderer renderer = tube.AddComponent<MeshRenderer>();
            if (renderer != null)
            {
                Color color = new Color(0.58f, 0.82f, 1f, TubeTransparency);
                renderer.sharedMaterial = CreateTransparentMaterial("Transparent Tube Shell", color);
            }
        }

        private static Mesh CreateOpenTubeMesh(float radius, float length, int segments)
        {
            int safeSegments = Mathf.Max(32, segments);
            Vector3[] vertices = new Vector3[(safeSegments + 1) * 2];
            Vector3[] normals = new Vector3[vertices.Length];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[safeSegments * 6];

            for (int i = 0; i <= safeSegments; i++)
            {
                float u = i / (float)safeSegments;
                float angle = u * Mathf.PI * 2f;
                float y = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                Vector3 normal = new Vector3(0f, Mathf.Cos(angle), Mathf.Sin(angle));

                int front = i * 2;
                int back = front + 1;
                vertices[front] = new Vector3(-length * 0.5f, y, z);
                vertices[back] = new Vector3(length * 0.5f, y, z);
                normals[front] = normal;
                normals[back] = normal;
                uvs[front] = new Vector2(u, 0f);
                uvs[back] = new Vector2(u, 1f);
            }

            for (int i = 0; i < safeSegments; i++)
            {
                int baseVertex = i * 2;
                int nextBaseVertex = baseVertex + 2;
                int triangle = i * 6;
                triangles[triangle] = baseVertex;
                triangles[triangle + 1] = nextBaseVertex;
                triangles[triangle + 2] = baseVertex + 1;
                triangles[triangle + 3] = baseVertex + 1;
                triangles[triangle + 4] = nextBaseVertex;
                triangles[triangle + 5] = nextBaseVertex + 1;
            }

            Mesh mesh = new Mesh
            {
                name = $"Open Tube Visual {safeSegments}"
            };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void CreateDepthGuideRings(Transform parent, int layer)
        {
            if (!ShowDepthGuideRings)
            {
                return;
            }

            float[] positions = { -1.8f, -0.6f, 0.6f, 1.8f };
            Material material = CreateTransparentMaterial("Tube Depth Guide Rings", new Color(0.72f, 0.9f, 1f, 0.1f));
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject ring = new GameObject($"DepthGuideRing_{i:00}");
                ring.layer = layer;
                ring.transform.SetParent(parent, false);
                ring.transform.localPosition = new Vector3(positions[i], 0f, 0f);

                MeshFilter meshFilter = ring.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = CreateRingBandMesh(TubeRadius * 0.985f, 0.018f, TubeVisualSegments);
                MeshRenderer renderer = ring.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
            }
        }

        private static Mesh CreateRingBandMesh(float radius, float thickness, int segments)
        {
            int safeSegments = Mathf.Max(32, segments);
            Vector3[] vertices = new Vector3[(safeSegments + 1) * 2];
            Vector3[] normals = new Vector3[vertices.Length];
            int[] triangles = new int[safeSegments * 6];

            for (int i = 0; i <= safeSegments; i++)
            {
                float angle = (i / (float)safeSegments) * Mathf.PI * 2f;
                float y = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                Vector3 normal = new Vector3(0f, Mathf.Cos(angle), Mathf.Sin(angle));

                int front = i * 2;
                int back = front + 1;
                vertices[front] = new Vector3(-thickness * 0.5f, y, z);
                vertices[back] = new Vector3(thickness * 0.5f, y, z);
                normals[front] = normal;
                normals[back] = normal;
            }

            for (int i = 0; i < safeSegments; i++)
            {
                int baseVertex = i * 2;
                int nextBaseVertex = baseVertex + 2;
                int triangle = i * 6;
                triangles[triangle] = baseVertex;
                triangles[triangle + 1] = baseVertex + 1;
                triangles[triangle + 2] = nextBaseVertex;
                triangles[triangle + 3] = baseVertex + 1;
                triangles[triangle + 4] = nextBaseVertex + 1;
                triangles[triangle + 5] = nextBaseVertex;
            }

            Mesh mesh = new Mesh
            {
                name = $"Tube Ring Band {safeSegments}"
            };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void CreateTubeWallColliders(Transform parent, int layer)
        {
            const float overlap = 1.22f;
            int segmentCount = Mathf.Max(24, TubeColliderSegments);
            float segmentWidth = (2f * Mathf.PI * (TubeRadius + TubeWallThickness * 0.5f)) / segmentCount * overlap;

            for (int i = 0; i < segmentCount; i++)
            {
                float angle = i * (360f / segmentCount);
                float radians = angle * Mathf.Deg2Rad;
                Vector3 radial = new Vector3(0f, Mathf.Cos(radians), Mathf.Sin(radians));
                Vector3 center = radial * (TubeRadius + TubeWallThickness * 0.5f);

                CreateColliderBox(
                    $"WallSegment_{i:00}",
                    parent,
                    layer,
                    center,
                    Quaternion.AngleAxis(angle, Vector3.right),
                    new Vector3(TubeLength, TubeWallThickness, segmentWidth));
            }
        }

        private static void CreateEndRingColliders(Transform parent, int layer)
        {
            int segmentCount = Mathf.Max(24, TubeColliderSegments);
            float segmentWidth = (2f * Mathf.PI * (TubeRadius + TubeWallThickness * 0.5f)) / segmentCount * 1.28f;
            float ringLength = TubeCapThickness * 2.6f;
            float[] xPositions =
            {
                -TubeLength * 0.5f + TubeCapThickness * 0.35f,
                TubeLength * 0.5f - TubeCapThickness * 0.35f
            };

            for (int side = 0; side < xPositions.Length; side++)
            {
                for (int i = 0; i < segmentCount; i++)
                {
                    float angle = i * (360f / segmentCount);
                    float radians = angle * Mathf.Deg2Rad;
                    Vector3 radial = new Vector3(0f, Mathf.Cos(radians), Mathf.Sin(radians));
                    Vector3 center = new Vector3(xPositions[side], 0f, 0f) + radial * (TubeRadius + TubeWallThickness * 0.48f);

                    CreateColliderBox(
                        $"{(side == 0 ? "Front" : "Back")}EndRingSegment_{i:00}",
                        parent,
                        layer,
                        center,
                        Quaternion.AngleAxis(angle, Vector3.right),
                        new Vector3(ringLength, TubeWallThickness * 1.35f, segmentWidth));
                }
            }
        }

        private static void CreateInternalRibs(Transform parent, int layer)
        {
            if (!InternalRibsEnabled)
            {
                return;
            }

            int ribCount = Mathf.Clamp(InternalRibCount, 0, 8);
            if (ribCount <= 0)
            {
                return;
            }

            float ribLength = TubeLength - TubeCapThickness * 2.4f;
            float usableStartX = -ribLength * 0.5f;
            float usableEndX = ribLength * 0.5f;
            int segmentsPerRib = Mathf.Max(8, Mathf.CeilToInt(InternalRibSegmentsPerTurn * InternalRibHelixTurns));
            float angularTravel = Mathf.PI * 2f * InternalRibHelixTurns * Mathf.Sign(Mathf.Approximately(InternalRibHelixHandedness, 0f) ? 1f : InternalRibHelixHandedness);
            float ribRadius = Mathf.Max(0.1f, TubeRadius - InternalRibHeight * 0.5f);
            Material ribMaterial = CreateTransparentMaterial("Rifled Internal Land Debug", new Color(0.9f, 0.95f, 1f, 0.08f));

            for (int i = 0; i < ribCount; i++)
            {
                float startAngle = i * (Mathf.PI * 2f / ribCount);
                for (int segment = 0; segment < segmentsPerRib; segment++)
                {
                    float t0 = segment / (float)segmentsPerRib;
                    float t1 = (segment + 1) / (float)segmentsPerRib;
                    float shapedT0 = ShapeHelixProgress(t0, InternalRibBackBias);
                    float shapedT1 = ShapeHelixProgress(t1, InternalRibBackBias);

                    Vector3 p0 = HelixPoint(usableStartX, usableEndX, ribRadius, startAngle, angularTravel, shapedT0);
                    Vector3 p1 = HelixPoint(usableStartX, usableEndX, ribRadius, startAngle, angularTravel, shapedT1);
                    Vector3 center = (p0 + p1) * 0.5f;
                    Vector3 tangent = (p1 - p0).normalized;
                    Vector3 radial = new Vector3(0f, center.y, center.z).normalized;
                    Vector3 binormal = Vector3.Cross(tangent, radial).normalized;
                    if (binormal.sqrMagnitude < 0.001f)
                    {
                        binormal = Vector3.forward;
                    }

                    Quaternion rotation = Quaternion.LookRotation(binormal, radial);
                    float segmentLength = Vector3.Distance(p0, p1) * 1.12f;
                    GameObject rib = CreateColliderBox(
                        $"RiflingLand_{i:00}_{segment:00}",
                        parent,
                        layer,
                        center,
                        rotation,
                        new Vector3(segmentLength, InternalRibHeight, InternalRibWidth));

                    Renderer renderer = rib.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.sharedMaterial = ribMaterial;
                    }
                }
            }
        }

        private static float ShapeHelixProgress(float t, float backBias)
        {
            float clamped = Mathf.Clamp01(t);
            if (backBias <= 0.001f)
            {
                return clamped;
            }

            // Subtle density bias near the back wall so rifling behaves like a gentle transport,
            // not like a visible auger blade.
            return Mathf.Pow(clamped, Mathf.Lerp(1f, 0.82f, Mathf.Clamp01(backBias)));
        }

        private static Vector3 HelixPoint(float startX, float endX, float radius, float startAngle, float angularTravel, float t)
        {
            float x = Mathf.Lerp(startX, endX, Mathf.Clamp01(t));
            float angle = startAngle + angularTravel * t;
            return new Vector3(x, Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
        }

        private static void CreateTubeCap(string name, Transform parent, int layer, float xPosition, bool visible, float alpha)
        {
            GameObject cap = CreateColliderBox(
                name,
                parent,
                layer,
                new Vector3(xPosition, 0f, 0f),
                Quaternion.identity,
                new Vector3(TubeCapThickness, TubeRadius * 2f + TubeWallThickness * 2f, TubeRadius * 2f + TubeWallThickness * 2f));

            Renderer renderer = cap.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = visible;
                renderer.sharedMaterial = CreateTransparentMaterial(name, new Color(0.58f, 0.82f, 1f, alpha));
            }
        }

        private static GameObject CreateColliderBox(string name, Transform parent, int layer, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.layer = layer;
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = localPosition;
            wall.transform.localRotation = localRotation;
            wall.transform.localScale = localScale;

            Renderer renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = DebugColliderVisibility;
                renderer.sharedMaterial = CreateTransparentMaterial("Tube Collider Debug", new Color(1f, 0.75f, 0.15f, 0.18f));
            }

            Collider collider = wall.GetComponent<Collider>();
            if (collider != null)
            {
                collider.sharedMaterial = new PhysicMaterial("Chamber High Friction")
                {
                    dynamicFriction = 0.9f,
                    staticFriction = 1f,
                    bounciness = 0.03f,
                    frictionCombine = PhysicMaterialCombine.Maximum,
                    bounceCombine = PhysicMaterialCombine.Minimum
                };
            }

            return wall;
        }

        private static List<GemstoneDefinition> CreateRuntimeDefinitions()
        {
            return new List<GemstoneDefinition>
            {
                Definition("opal", "Opal", GemstoneShapeHint.Rounded, new Color(0.94f, 0.98f, 1f, 1f), new Vector3(0.16f, 0.12f, 0.18f), new Vector3(0.34f, 0.25f, 0.36f), 0.1f, 0.34f, 0.86f, 0.06f, 1.22f, GemstoneParticleCategory.Gem),
                Definition("ruby", "Ruby", GemstoneShapeHint.Faceted, new Color(1f, 0.03f, 0.06f, 1f), new Vector3(0.13f, 0.095f, 0.16f), new Vector3(0.31f, 0.22f, 0.38f), 0.15f, 0.56f, 0.72f, 0.08f, 0.55f, GemstoneParticleCategory.HeavyAnchor),
                Definition("emerald", "Emerald", GemstoneShapeHint.Elongated, new Color(0.02f, 0.9f, 0.32f, 1f), new Vector3(0.11f, 0.18f, 0.11f), new Vector3(0.25f, 0.48f, 0.25f), 0.12f, 0.43f, 0.78f, 0.05f, 1.18f, GemstoneParticleCategory.Gem),
                Definition("amethyst", "Amethyst", GemstoneShapeHint.Shard, new Color(0.58f, 0.18f, 1f, 1f), new Vector3(0.12f, 0.06f, 0.09f), new Vector3(0.38f, 0.17f, 0.27f), 0.07f, 0.25f, 0.96f, 0.04f, 0.9f, GemstoneParticleCategory.Gem),
                Definition("quartz", "Quartz", GemstoneShapeHint.Shard, new Color(1f, 1f, 1f, 0.96f), new Vector3(0.1f, 0.05f, 0.08f), new Vector3(0.36f, 0.18f, 0.24f), 0.06f, 0.22f, 0.68f, 0.03f, 1.35f, GemstoneParticleCategory.Slider),
                Definition("glass_fragment", "Glass Fragment", GemstoneShapeHint.ThinShard, new Color(0.45f, 0.95f, 1f, 0.92f), new Vector3(0.11f, 0.022f, 0.055f), new Vector3(0.42f, 0.06f, 0.18f), 0.018f, 0.09f, 0.55f, 0.02f, 1.42f, GemstoneParticleCategory.GlassFragment),
                Definition("micro_particle", "Micro Particle", GemstoneShapeHint.Pebble, new Color(0.82f, 0.84f, 0.88f, 1f), new Vector3(0.026f, 0.02f, 0.026f), new Vector3(0.125f, 0.09f, 0.125f), 0.0045f, 0.034f, 1.05f, 0.01f, 6.2f, GemstoneParticleCategory.MicroParticle),
                Definition("dust", "Dust", GemstoneShapeHint.Pebble, new Color(0.68f, 0.7f, 0.74f, 1f), new Vector3(0.012f, 0.01f, 0.012f), new Vector3(0.055f, 0.042f, 0.055f), 0.0014f, 0.009f, 1.25f, 0f, 4.4f, GemstoneParticleCategory.Dust)
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

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.2f);
            }

            if (material.HasProperty("_DoubleSidedEnable"))
            {
                material.SetFloat("_DoubleSidedEnable", 1f);
            }

            if (material.HasProperty("_CullMode"))
            {
                material.SetFloat("_CullMode", 0f);
            }

            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return material;
        }

        private static PhysicsSandboxCameraController ConfigureCamera(Transform target)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
            }

            PhysicsSandboxCameraController controller = camera.GetComponent<PhysicsSandboxCameraController>();
            if (controller == null)
            {
                controller = camera.gameObject.AddComponent<PhysicsSandboxCameraController>();
            }

            controller.Configure(camera, target);
            return controller;
        }

        private static KaleidoscopeLightingRig ConfigureReadableLighting(Transform root, Transform target)
        {
            GameObject lightingRoot = new GameObject("Optical Lighting Rig");
            lightingRoot.transform.SetParent(root, false);
            KaleidoscopeLightingRig rig = lightingRoot.AddComponent<KaleidoscopeLightingRig>();
            rig.Configure(target);

            bool hasDirectionalLight = false;
            bool hasFillLight = false;
            Light[] lights = Object.FindObjectsOfType<Light>();
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    hasDirectionalLight = true;
                    light.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
                    light.intensity = Mathf.Max(light.intensity, 2.2f);
                    light.color = new Color(1f, 0.96f, 0.9f);
                    light.shadows = LightShadows.None;
                    light.transform.SetParent(lightingRoot.transform, true);
                }

                if (light.name == "Soft Fill Light")
                {
                    hasFillLight = true;
                }
            }

            if (!hasDirectionalLight)
            {
                GameObject mainLight = new GameObject("Main Directional Light");
                Light main = mainLight.AddComponent<Light>();
                main.type = LightType.Directional;
                main.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
                main.intensity = 2.2f;
                main.color = new Color(1f, 0.96f, 0.9f);
                mainLight.transform.SetParent(lightingRoot.transform, true);
            }

            if (hasFillLight)
            {
                return rig;
            }

            GameObject fillLight = new GameObject("Soft Fill Light");
            Light fill = fillLight.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.transform.position = new Vector3(-2.8f, 2.2f, -3.2f);
            fill.range = 8f;
            fill.intensity = 1.15f;
            fill.color = new Color(0.66f, 0.82f, 1f);
            fillLight.transform.SetParent(lightingRoot.transform, true);
            return rig;
        }

        private static GemSparkleController ConfigureSparkles(
            Transform root,
            GemstoneSpawner spawner,
            KaleidoscopeLightingRig lightingRig,
            PhysicsSandboxCameraController cameraController)
        {
            GameObject sparkleObject = new GameObject("Gem Sparkle FX");
            sparkleObject.layer = ResolveLayer(OpticalFxLayerName);
            sparkleObject.transform.SetParent(root, false);
            GemSparkleController sparkleController = sparkleObject.AddComponent<GemSparkleController>();
            sparkleController.SetOpticalFxLayerName(OpticalFxLayerName);
            sparkleController.Configure(spawner, lightingRig, Camera.main);
            return sparkleController;
        }

        private static FakeCausticBunnyProjector ConfigureFakeCaustics(
            Transform root,
            Transform chamber,
            KaleidoscopeLightingRig lightingRig,
            PhysicsSandboxCameraController cameraController)
        {
            GameObject causticObject = new GameObject("Fake Caustic Bunnies");
            causticObject.layer = ResolveLayer(OpticalFxLayerName);
            causticObject.transform.SetParent(root, false);
            FakeCausticBunnyProjector caustics = causticObject.AddComponent<FakeCausticBunnyProjector>();
            caustics.SetOpticalFxLayerName(OpticalFxLayerName);
            caustics.Configure(chamber, lightingRig, Camera.main, TubeRadius * 0.93f, TubeLength - TubeCapThickness * 2f);
            return caustics;
        }

        private static OpticalSourceChamber ConfigureOpticalSourceChamber(Transform root, Transform chamber)
        {
            GameObject sourceObject = new GameObject("Object Chamber Light Module");
            sourceObject.layer = ResolveLayer(OpticalFxLayerName);
            sourceObject.transform.SetParent(root, false);
            OpticalSourceChamber sourceChamber = sourceObject.AddComponent<OpticalSourceChamber>();
            sourceChamber.Configure(chamber, TubeRadius, TubeLength, OpticalFxLayerName);
            return sourceChamber;
        }

        private static KaleidoscopeRenderPipeline ConfigureMirrorPipeline(
            Transform root,
            KaleidoscopeMirrorController mirrorController,
            OpticalSourceChamber opticalSourceChamber)
        {
            GameObject mirrorObject = new GameObject("Kaleidoscope Mirror Pipeline");
            mirrorObject.transform.SetParent(root, false);
            KaleidoscopeRenderPipeline pipeline = mirrorObject.AddComponent<KaleidoscopeRenderPipeline>();
            pipeline.Configure(Camera.main, mirrorController);
            pipeline.ConfigureOpticalSource(opticalSourceChamber);
            return pipeline;
        }

        private static int ResolveLayer(string preferredLayerName, string fallbackLayerName = null)
        {
            int layer = LayerMask.NameToLayer(preferredLayerName);
            if (layer >= 0)
            {
                return layer;
            }

            if (!string.IsNullOrEmpty(fallbackLayerName))
            {
                layer = LayerMask.NameToLayer(fallbackLayerName);
                if (layer >= 0)
                {
                    return layer;
                }
            }

            return 0;
        }
    }
}
