using UnityEngine;

namespace KaleidoscopeEngine.PhysicsSandbox
{
    [DisallowMultipleComponent]
    public sealed class PhysicsSandboxInput : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private KaleidoscopePhysicsChamber chamber;
        [SerializeField] private GemstoneSpawner spawner;

        [Header("Input")]
        [SerializeField] private float shakeStrength = 1f;

        public void Configure(KaleidoscopePhysicsChamber physicsChamber, GemstoneSpawner gemstoneSpawner)
        {
            chamber = physicsChamber;
            spawner = gemstoneSpawner;
        }

        private void Update()
        {
            if (chamber == null)
            {
                return;
            }

            Vector2 tilt = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            chamber.Tilt(tilt);

            float rotation = 0f;
            if (Input.GetKey(KeyCode.Q))
            {
                rotation -= 1f;
            }

            if (Input.GetKey(KeyCode.E))
            {
                rotation += 1f;
            }

            chamber.Rotate(rotation);

            if (Input.GetKeyDown(KeyCode.Space))
            {
                chamber.Shake(shakeStrength);
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                chamber.ResetPose();
            }

            if (Input.GetKeyDown(KeyCode.T) && spawner != null)
            {
                spawner.Respawn();
            }
        }
    }
}
