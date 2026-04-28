using System.Collections.Generic;
using UnityEngine;

namespace KaleidoscopeEngine.PhysicsSandbox
{
    [DisallowMultipleComponent]
    public sealed class PhysicsSandboxChamberDebugView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform chamberTransform;

        [Header("Visibility")]
        [SerializeField] private bool showChamberVisuals = true;
        [SerializeField] private bool cutawayMode = true;
        [SerializeField] private bool showCollidersDebug;
        [SerializeField] private Color colliderDebugColor = new Color(1f, 0.85f, 0.1f, 0.45f);

        private readonly List<Renderer> wallRenderers = new List<Renderer>();
        private Renderer frontWallRenderer;
        private Collider[] chamberColliders;

        public bool ShowChamberVisuals => showChamberVisuals;
        public bool CutawayMode => cutawayMode;
        public bool ShowCollidersDebug => showCollidersDebug;

        public void Configure(Transform chamber)
        {
            chamberTransform = chamber;
            RefreshReferences();
            ApplyVisibility();
        }

        public void ToggleChamberVisuals()
        {
            showChamberVisuals = !showChamberVisuals;
            ApplyVisibility();
        }

        public void ToggleCutaway()
        {
            cutawayMode = !cutawayMode;
            ApplyVisibility();
        }

        public void ToggleColliderDebug()
        {
            showCollidersDebug = !showCollidersDebug;
        }

        private void RefreshReferences()
        {
            wallRenderers.Clear();
            if (chamberTransform == null)
            {
                chamberColliders = new Collider[0];
                frontWallRenderer = null;
                return;
            }

            wallRenderers.AddRange(chamberTransform.GetComponentsInChildren<Renderer>(true));
            chamberColliders = chamberTransform.GetComponentsInChildren<Collider>(true);
            Transform frontWall = chamberTransform.Find("FrontWall");
            frontWallRenderer = frontWall != null ? frontWall.GetComponent<Renderer>() : null;
        }

        private void ApplyVisibility()
        {
            foreach (Renderer wallRenderer in wallRenderers)
            {
                if (wallRenderer != null)
                {
                    wallRenderer.enabled = showChamberVisuals;
                }
            }

            if (frontWallRenderer != null)
            {
                frontWallRenderer.enabled = showChamberVisuals && !cutawayMode;
            }
        }

        private void OnDrawGizmos()
        {
            if (!showCollidersDebug)
            {
                return;
            }

            if (chamberColliders == null || chamberColliders.Length == 0)
            {
                RefreshReferences();
            }

            Gizmos.color = colliderDebugColor;
            foreach (Collider collider in chamberColliders)
            {
                if (collider is not BoxCollider boxCollider)
                {
                    continue;
                }

                Matrix4x4 previous = Gizmos.matrix;
                Gizmos.matrix = boxCollider.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
                Gizmos.matrix = previous;
            }
        }
    }
}
