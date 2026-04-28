using UnityEngine;

namespace KaleidoscopeEngine.PhysicsSandbox
{
    [DisallowMultipleComponent]
    public sealed class PhysicsSandboxDebugHUD : MonoBehaviour
    {
        [SerializeField] private KaleidoscopePhysicsChamber chamber;
        [SerializeField] private GemstoneSpawner spawner;
        [SerializeField] private bool visible = true;

        public void Configure(KaleidoscopePhysicsChamber physicsChamber, GemstoneSpawner gemstoneSpawner)
        {
            chamber = physicsChamber;
            spawner = gemstoneSpawner;
        }

        private void OnGUI()
        {
            if (!visible)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(16f, 16f, 330f, 148f), GUI.skin.box);
            GUILayout.Label("Physics Sandbox");
            GUILayout.Label($"Objects: {(spawner != null ? spawner.SpawnedObjects.Count : 0)}");
            GUILayout.Label($"Seed: {(spawner != null ? spawner.ActiveSeed : 0)}");
            GUILayout.Label($"Tilt: {(chamber != null ? chamber.CurrentTiltInput.ToString("F2") : "n/a")}");
            GUILayout.Label($"Rotation Input: {(chamber != null ? chamber.RequestedRotationSpeed.ToString("F2") : "n/a")}");
            GUILayout.Label($"Shake: {(chamber != null ? chamber.ShakeStrength.ToString("F2") : "n/a")}");
            GUILayout.Label("WASD/Arrows tilt, Q/E rotate, Space shake, R reset, T respawn");
            GUILayout.EndArea();
        }
    }
}
