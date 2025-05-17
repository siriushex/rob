using UnityEngine;

namespace DragonWater.Utils
{
    public static class DragonWaterDebug
    {
        public static void Log(string message) => Log(message, null);
        public static void Log(string message, Object context)
        {
            if (DragonWaterManager.Instance.Config.debugLevel <= DragonWaterConfig.DebugLevel.InfoWarningError)
                Debug.Log($"[Dragon Water] {message}", context);
        }

        public static void LogWarning(string message) => LogWarning(message, null);
        public static void LogWarning(string message, Object context)
        {
            if (DragonWaterManager.Instance.Config.debugLevel <= DragonWaterConfig.DebugLevel.WarningError)
                Debug.LogWarning($"[Dragon Water] {message}", context);
        }

        public static void LogError(string message) => LogError(message, null);
        public static void LogError(string message, Object context)
        {
            if (DragonWaterManager.Instance.Config.debugLevel <= DragonWaterConfig.DebugLevel.Error)
                Debug.LogError($"[Dragon Water] {message}", context);
        }
    }
}
