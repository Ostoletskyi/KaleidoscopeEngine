using System.Collections.Generic;
using UnityEngine;

namespace KaleidoscopeEngine.Performance
{
    [DisallowMultipleComponent]
    public sealed class KaleidoscopeRebuildGuard : MonoBehaviour
    {
        private const float WindowSeconds = 2f;
        private static KaleidoscopeRebuildGuard instance;

        private readonly List<string> warnings = new List<string>();
        private float windowStart;
        private int gameObjectDestroyCount;
        private int gameObjectInstantiateCount;
        private int renderTextureRecreateCount;
        private int fullRespawnCount;
        private int materialInstanceCount;
        private int sourceModeRebuildCount;
        private int totalRenderTextureRecreateCount;
        private int totalMaterialInstanceCount;
        private int totalSourceModeRebuildCount;
        private string lastExpensiveEvent = "No expensive rebuild event recorded.";
        private float lastExpensiveEventTime;

        public static KaleidoscopeRebuildGuard Instance => instance;
        public IReadOnlyList<string> Warnings => warnings;
        public int GameObjectDestroyCount => gameObjectDestroyCount;
        public int GameObjectInstantiateCount => gameObjectInstantiateCount;
        public int RenderTextureRecreateCount => renderTextureRecreateCount;
        public int FullRespawnCount => fullRespawnCount;
        public int MaterialInstanceCount => materialInstanceCount;
        public int SourceModeRebuildCount => sourceModeRebuildCount;
        public int TotalRenderTextureRecreateCount => totalRenderTextureRecreateCount;
        public int TotalMaterialInstanceCount => totalMaterialInstanceCount;
        public int TotalSourceModeRebuildCount => totalSourceModeRebuildCount;
        public string LastExpensiveEvent => lastExpensiveEvent;
        public float TimeSinceLastExpensiveEvent => lastExpensiveEventTime > 0f ? Time.realtimeSinceStartup - lastExpensiveEventTime : 0f;

        private void Awake()
        {
            instance = this;
            windowStart = Time.realtimeSinceStartup;
        }

        private void Update()
        {
            if (Time.realtimeSinceStartup - windowStart < WindowSeconds)
            {
                return;
            }

            EvaluateWindow();
            gameObjectDestroyCount = 0;
            gameObjectInstantiateCount = 0;
            renderTextureRecreateCount = 0;
            fullRespawnCount = 0;
            materialInstanceCount = 0;
            sourceModeRebuildCount = 0;
            windowStart = Time.realtimeSinceStartup;
        }

        public static void RecordGameObjectInstantiate(string reason)
        {
            if (instance != null)
            {
                instance.gameObjectInstantiateCount++;
                instance.RecordExpensiveEvent("GameObject instantiated", reason);
            }
        }

        public static void RecordGameObjectDestroy(string reason)
        {
            if (instance != null)
            {
                instance.gameObjectDestroyCount++;
                instance.RecordExpensiveEvent("GameObject destroyed", reason);
            }
        }

        public static void RecordRenderTextureRecreate(string reason)
        {
            if (instance != null)
            {
                instance.renderTextureRecreateCount++;
                instance.totalRenderTextureRecreateCount++;
                instance.RecordExpensiveEvent("RenderTexture recreated", reason);
            }
        }

        public static void RecordFullRespawn(string reason)
        {
            if (instance != null)
            {
                instance.fullRespawnCount++;
                instance.RecordExpensiveEvent("Full source respawn", reason);
            }
        }

        public static void RecordMaterialInstance(string reason)
        {
            if (instance != null)
            {
                instance.materialInstanceCount++;
                instance.totalMaterialInstanceCount++;
                instance.RecordExpensiveEvent("Material instantiated", reason);
            }
        }

        public static void RecordSourceModeRebuild(string reason)
        {
            if (instance != null)
            {
                instance.sourceModeRebuildCount++;
                instance.totalSourceModeRebuildCount++;
                instance.RecordExpensiveEvent("Source mode rebuilt", reason);
            }
        }

        private void RecordExpensiveEvent(string eventName, string reason)
        {
            lastExpensiveEvent = string.IsNullOrEmpty(reason) ? eventName : $"{eventName}: {reason}";
            lastExpensiveEventTime = Time.realtimeSinceStartup;
        }

        private void EvaluateWindow()
        {
            if (sourceModeRebuildCount >= 3)
            {
                AddWarning($"Warning: Source mode rebuilt {sourceModeRebuildCount} times in {WindowSeconds:F0} seconds");
            }

            if (renderTextureRecreateCount > 0)
            {
                AddWarning($"Warning: RenderTexture recreated {renderTextureRecreateCount} time(s) during runtime");
            }

            if (fullRespawnCount > 0)
            {
                AddWarning($"Warning: Full gemstone respawn triggered {fullRespawnCount} time(s)");
            }

            if (gameObjectDestroyCount + gameObjectInstantiateCount >= 20)
            {
                AddWarning($"Warning: High scene churn, instantiate {gameObjectInstantiateCount}, destroy {gameObjectDestroyCount}");
            }

            if (materialInstanceCount >= 8)
            {
                AddWarning($"Warning: Material instances created {materialInstanceCount} times");
            }
        }

        private void AddWarning(string warning)
        {
            warnings.Insert(0, $"{Time.realtimeSinceStartup:0000.0}s  {warning}");
            if (warnings.Count > 48)
            {
                warnings.RemoveAt(warnings.Count - 1);
            }
        }
    }
}
