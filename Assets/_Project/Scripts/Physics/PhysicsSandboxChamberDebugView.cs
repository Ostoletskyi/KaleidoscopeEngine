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
        private Renderer frontCapRenderer;
        private Renderer[] colliderRenderers;
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
            ApplyVisibility();
        }

        private void RefreshReferences()
        {
            wallRenderers.Clear();
            if (chamberTransform == null)
            {
                chamberColliders = new Collider[0];
                frontCapRenderer = null;
                colliderRenderers = new Renderer[0];
                return;
            }

            wallRenderers.AddRange(chamberTransform.GetComponentsInChildren<Renderer>(true));
            chamberColliders = chamberTransform.GetComponentsInChildren<Collider>(true);
            Transform frontCap = chamberTransform.Find("TubeColliders/FrontCap");
            frontCapRenderer = frontCap != null ? frontCap.GetComponent<Renderer>() : null;
            Transform colliderRoot = chamberTransform.Find("TubeColliders");
            colliderRenderers = colliderRoot != null ? colliderRoot.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
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

            foreach (Renderer colliderRenderer in colliderRenderers)
            {
                if (colliderRenderer != null)
                {
                    colliderRenderer.enabled = showCollidersDebug;
                }
            }

            if (frontCapRenderer != null)
            {
                frontCapRenderer.enabled = showChamberVisuals && !cutawayMode;
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
