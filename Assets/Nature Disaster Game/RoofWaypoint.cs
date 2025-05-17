using UnityEngine;

public class RoofWaypoint : MonoBehaviour
{
    [Tooltip("Высота точки (для определения безопасности от цунами)")]
    public float height;
    
    [Tooltip("Безопасна ли точка от цунами")]
    public bool isSafeFromTsunami = true;
    public bool isSafeFromAcidRain = false;
    [Header("Лестница")]
    [Tooltip("Точка начала лестницы (внизу)")]
    public Transform ladderStart;
    
    [Tooltip("Точка конца лестницы (наверху)")]
    public Transform ladderEnd;
    
    [Tooltip("Скорость подъёма бота по лестнице")]
    public float climbSpeed = 1.5f;
    
    void Start()
    {
        // Автоматически определяем высоту, если она не указана
        if (height <= 0)
        {
            height = transform.position.y;
        }
        
        // Добавляем тег "Waypoint", если его нет
        if (gameObject.tag != "Waypoint")
        {
            gameObject.tag = "Waypoint";
        }
    }
    
    void OnDrawGizmos()
    {
        // Визуализация в редакторе
        Gizmos.color = isSafeFromTsunami ? Color.green : Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.5f);
        
        // Отображаем линию лестницы, если указаны точки
       if (ladderStart != null && ladderEnd != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(ladderStart.position, ladderEnd.position);
            
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(ladderStart.position, 0.3f);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(ladderEnd.position, 0.3f);
        }
    }
}