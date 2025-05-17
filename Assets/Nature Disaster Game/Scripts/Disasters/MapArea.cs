using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class MapArea : MonoBehaviour
{
    [Header("Main Map Area")]
    [SerializeField] private Vector3 min = new Vector3(0, 0, 0);
    [SerializeField] private Vector3 max = new Vector3(100, 0, 100);
    [SerializeField] private Color gizmoColor = new Color(0, 1, 0, 0.25f);
    
    [Header("Safe Spawn Area for Coin Collection")]
    [SerializeField] private Vector3 safeMin = new Vector3(20, 0, 20);
    [SerializeField] private Vector3 safeMax = new Vector3(80, 0, 80);
    [SerializeField] private Color safeGizmoColor = new Color(0, 0.5f, 1, 0.25f);

    public Vector3 GetMin() => min;
    public Vector3 GetMax() => max;
    public Vector3 GetSafeMin() => safeMin;
    public Vector3 GetSafeMax() => safeMax;

    public static MapArea Instance;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Получает случайную позицию внутри основной области карты
    /// </summary>
    public Vector3 GetRandomPosition()
    {
        return new Vector3(
            Random.Range(min.x, max.x),
            Random.Range(min.y, max.y),
            Random.Range(min.z, max.z)
        );
    }
    
    /// <summary>
    /// Получает случайную позицию внутри безопасной области для сбора монет
    /// </summary>
    public Vector3 GetRandomSafePosition()
    {
        return new Vector3(
            Random.Range(safeMin.x, safeMax.x),
            Random.Range(safeMin.y, safeMax.y),
            Random.Range(safeMin.z, safeMax.z)
        );
    }
    
    /// <summary>
    /// Получает список случайных позиций внутри безопасной зоны
    /// </summary>
    public List<Vector3> GetRandomSafePositions(int count, float minDistance = 2f)
    {
        List<Vector3> positions = new List<Vector3>();
        int attempts = 0;
        int maxAttempts = count * 10;
        
        while (positions.Count < count && attempts < maxAttempts)
        {
            Vector3 pos = GetRandomSafePosition();
            bool isFarEnough = true;
            
            // Проверяем, что точка находится достаточно далеко от других
            foreach (var existingPos in positions)
            {
                if (Vector3.Distance(pos, existingPos) < minDistance)
                {
                    isFarEnough = false;
                    break;
                }
            }
            
            if (isFarEnough)
            {
                positions.Add(pos);
            }
            
            attempts++;
        }
        
        return positions;
    }

    private void OnDrawGizmos()
    {
        // Рисуем основную область карты
        Gizmos.color = gizmoColor;
        Vector3 center = (min + max) / 2;
        Vector3 size = max - min;
        Gizmos.DrawCube(center, size);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, size);
        
        // Рисуем безопасную область для спавна
        Gizmos.color = safeGizmoColor;
        Vector3 safeCenter = (safeMin + safeMax) / 2;
        Vector3 safeSize = safeMax - safeMin;
        Gizmos.DrawCube(safeCenter, safeSize);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(safeCenter, safeSize);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Основная область
        UnityEditor.Handles.color = Color.green;
        min = UnityEditor.Handles.PositionHandle(min, Quaternion.identity);
        max = UnityEditor.Handles.PositionHandle(max, Quaternion.identity);
        
        // Безопасная область
        UnityEditor.Handles.color = Color.blue;
        safeMin = UnityEditor.Handles.PositionHandle(safeMin, Quaternion.identity);
        safeMax = UnityEditor.Handles.PositionHandle(safeMax, Quaternion.identity);
        
        // Добавляем подписи
        UnityEditor.Handles.Label(min, "Main Min");
        UnityEditor.Handles.Label(max, "Main Max");
        UnityEditor.Handles.Label(safeMin, "Safe Min");
        UnityEditor.Handles.Label(safeMax, "Safe Max");
    }
#endif
}