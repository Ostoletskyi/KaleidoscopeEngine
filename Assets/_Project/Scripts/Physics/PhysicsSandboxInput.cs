using UnityEngine;
using System.Collections.Generic;
using KaleidoscopeEngine.FX;
using KaleidoscopeEngine.Geometry;
using KaleidoscopeEngine.Lighting;
using KaleidoscopeEngine.Materials;
using KaleidoscopeEngine.Mirrors;

namespace KaleidoscopeEngine.PhysicsSandbox
{
    [DisallowMultipleComponent]
    public sealed class PhysicsSandboxInput : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private KaleidoscopePhysicsChamber chamber;
        [SerializeField] private GemstoneSpawner spawner;
        [SerializeField] private PhysicsSandboxCameraController cameraController;
        [SerializeField] private PhysicsSandboxChamberDebugView chamberDebugView;
        [SerializeField] private PhysicsSandboxMetrics metrics;
        [SerializeField] private GemstoneMaterialAssigner materialAssigner;
        [SerializeField] private KaleidoscopeLightingRig lightingRig;
        [SerializeField] private GemGeometryAssigner geometryAssigner;
        [SerializeField] private GemSparkleController sparkleController;
        [SerializeField] private FakeCausticBunnyProjector causticProjector;
        [SerializeField] private KaleidoscopeRenderPipeline mirrorPipeline;
        [SerializeField] private KaleidoscopeMirrorController mirrorController;
        [SerializeField] private KaleidoscopeDebugPanel debugPanel;
        [SerializeField] private OpticalSourceChamber opticalSourceChamber;

        [Header("Input")]
        [SerializeField] private float shakeStrength = 1f;
        [SerializeField] private float axialSpeedChangePerSecond = 120f;
        [SerializeField] private float movingLightIntensityChangePerSecond = 1.5f;
        [SerializeField] private float sparkleIntensityChangePerSecond = 1.2f;

        private readonly HashSet<char> heldCharacters = new HashSet<char>();

        public void Configure(KaleidoscopePhysicsChamber physicsChamber, GemstoneSpawner gemstoneSpawner)
        {
            chamber = physicsChamber;
            spawner = gemstoneSpawner;
        }

        public void ConfigureDebugSystems(
            PhysicsSandboxCameraController sandboxCamera,
            PhysicsSandboxChamberDebugView debugView,
            PhysicsSandboxMetrics sandboxMetrics,
            GemstoneMaterialAssigner opticalAssigner = null,
            KaleidoscopeLightingRig opticalLightingRig = null,
            GemGeometryAssigner proceduralGeometryAssigner = null,
            GemSparkleController gemSparkles = null,
            FakeCausticBunnyProjector fakeCaustics = null,
            KaleidoscopeRenderPipeline kaleidoscopePipeline = null,
            KaleidoscopeMirrorController kaleidoscopeMirror = null,
            KaleidoscopeDebugPanel kaleidoscopeDebugPanel = null)
        {
            cameraController = sandboxCamera;
            chamberDebugView = debugView;
            metrics = sandboxMetrics;
            materialAssigner = opticalAssigner;
            lightingRig = opticalLightingRig;
            geometryAssigner = proceduralGeometryAssigner;
            sparkleController = gemSparkles;
            causticProjector = fakeCaustics;
            mirrorPipeline = kaleidoscopePipeline;
            mirrorController = kaleidoscopeMirror;
            debugPanel = kaleidoscopeDebugPanel;
        }

        public void ConfigureDebugSystems(
            PhysicsSandboxCameraController sandboxCamera,
            PhysicsSandboxChamberDebugView debugView,
            PhysicsSandboxMetrics sandboxMetrics,
            GemstoneMaterialAssigner opticalAssigner,
            KaleidoscopeLightingRig opticalLightingRig,
            GemGeometryAssigner proceduralGeometryAssigner,
            GemSparkleController gemSparkles,
            FakeCausticBunnyProjector fakeCaustics,
            KaleidoscopeRenderPipeline kaleidoscopePipeline,
            KaleidoscopeMirrorController kaleidoscopeMirror,
            KaleidoscopeDebugPanel kaleidoscopeDebugPanel,
            OpticalSourceChamber sourceChamber)
        {
            ConfigureDebugSystems(sandboxCamera, debugView, sandboxMetrics, opticalAssigner, opticalLightingRig, proceduralGeometryAssigner, gemSparkles, fakeCaustics, kaleidoscopePipeline, kaleidoscopeMirror, kaleidoscopeDebugPanel);
            opticalSourceChamber = sourceChamber;
        }

        private void Update()
        {
            if (chamber == null)
            {
                return;
            }

            bool kaleidoscopeView = mirrorPipeline != null && mirrorPipeline.KaleidoscopeView;
            Vector2 tilt = new Vector2(
                ReadAxis(KeyCode.D, 'в', KeyCode.A, 'ф'),
                ReadAxis(KeyCode.W, 'ц', KeyCode.S, 'ы'));
            chamber.Tilt(tilt);

            float rotation = 0f;
            if (IsHeld(KeyCode.Q, 'й'))
            {
                chamber.AdjustAxialRotationSpeed(-axialSpeedChangePerSecond * Time.deltaTime);
            }

            if (IsHeld(KeyCode.E, 'у'))
            {
                chamber.AdjustAxialRotationSpeed(axialSpeedChangePerSecond * Time.deltaTime);
            }

            chamber.Rotate(rotation);

            if (Input.GetKeyDown(KeyCode.Space))
            {
                chamber.Shake(shakeStrength);
            }

            if (IsPressed(KeyCode.R, 'к'))
            {
                chamber.ResetPose();
            }

            if (IsPressed(KeyCode.T, 'е') && spawner != null)
            {
                metrics?.ResetEscapes();
                spawner.Respawn();
            }

            if (!kaleidoscopeView && IsPressed(KeyCode.X, 'ч'))
            {
                chamber.ToggleAxialRotation();
            }

            if (IsPressed(KeyCode.C, 'с'))
            {
                cameraController?.ToggleMode();
            }

            if (IsPressed(KeyCode.L, 'д'))
            {
                lightingRig?.ToggleMovingLight();
            }

            if (IsPressed(KeyCode.P, 'з'))
            {
                lightingRig?.NextPreset();
            }

            if (IsPressed(KeyCode.K, 'л'))
            {
                sparkleController?.ToggleSparkles();
            }

            if (IsPressed(KeyCode.N, 'т'))
            {
                causticProjector?.ToggleCaustics();
            }

            if (IsHeld(KeyCode.LeftBracket, 'х'))
            {
                lightingRig?.AdjustMovingLightIntensity(-movingLightIntensityChangePerSecond * Time.deltaTime);
            }

            if (IsHeld(KeyCode.RightBracket, 'ъ'))
            {
                lightingRig?.AdjustMovingLightIntensity(movingLightIntensityChangePerSecond * Time.deltaTime);
            }

            if (IsHeld(KeyCode.Comma, 'б'))
            {
                sparkleController?.AdjustSparkleIntensity(-sparkleIntensityChangePerSecond * Time.deltaTime);
            }

            if (IsHeld(KeyCode.Period, 'ю'))
            {
                sparkleController?.AdjustSparkleIntensity(sparkleIntensityChangePerSecond * Time.deltaTime);
            }
        }

        private void OnGUI()
        {
            Event current = Event.current;
            if (current == null || !current.isKey)
            {
                return;
            }

            char character = char.ToLowerInvariant(current.character);
            char mappedCharacter = MapRussianCharacter(current.keyCode);

            if (current.type == EventType.KeyDown)
            {
                if (character != '\0')
                {
                    heldCharacters.Add(character);
                }

                if (mappedCharacter != '\0')
                {
                    heldCharacters.Add(mappedCharacter);
                }
            }
            else if (current.type == EventType.KeyUp)
            {
                if (character != '\0')
                {
                    heldCharacters.Remove(character);
                }

                if (mappedCharacter != '\0')
                {
                    heldCharacters.Remove(mappedCharacter);
                }
            }
        }

        private float ReadAxis(KeyCode positiveKey, char positiveCharacter, KeyCode negativeKey, char negativeCharacter)
        {
            float axis = 0f;
            if (IsHeld(positiveKey, positiveCharacter))
            {
                axis += 1f;
            }

            if (IsHeld(negativeKey, negativeCharacter))
            {
                axis -= 1f;
            }

            return Mathf.Clamp(axis, -1f, 1f);
        }

        private bool IsHeld(KeyCode key, char russianCharacter)
        {
            return Input.GetKey(key) || heldCharacters.Contains(russianCharacter);
        }

        private bool IsPressed(KeyCode key, char russianCharacter)
        {
            string input = Input.inputString;
            bool characterPressed = false;
            for (int i = 0; i < input.Length; i++)
            {
                if (char.ToLowerInvariant(input[i]) == russianCharacter)
                {
                    characterPressed = true;
                    break;
                }
            }

            return Input.GetKeyDown(key) || characterPressed;
        }

        private static char MapRussianCharacter(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.A: return 'ф';
                case KeyCode.S: return 'ы';
                case KeyCode.D: return 'в';
                case KeyCode.W: return 'ц';
                case KeyCode.Q: return 'й';
                case KeyCode.E: return 'у';
                case KeyCode.R: return 'к';
                case KeyCode.T: return 'е';
                case KeyCode.M: return 'ь';
                case KeyCode.X: return 'ч';
                case KeyCode.Z: return 'я';
                case KeyCode.I: return 'ш';
                case KeyCode.U: return 'г';
                case KeyCode.Y: return 'н';
                case KeyCode.C: return 'с';
                case KeyCode.H: return 'р';
                case KeyCode.B: return 'и';
                case KeyCode.V: return 'м';
                case KeyCode.Semicolon: return 'ж';
                case KeyCode.F: return 'а';
                case KeyCode.G: return 'п';
                case KeyCode.L: return 'д';
                case KeyCode.P: return 'з';
                case KeyCode.O: return 'щ';
                case KeyCode.J: return 'о';
                case KeyCode.K: return 'л';
                case KeyCode.N: return 'т';
                case KeyCode.LeftBracket: return 'х';
                case KeyCode.RightBracket: return 'ъ';
                case KeyCode.Comma: return 'б';
                case KeyCode.Period: return 'ю';
                default: return '\0';
            }
        }
    }
}
