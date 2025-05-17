using System.Collections;
using UnityEngine;

public class Vulcano : MonoBehaviour
{
    [Header("Основные настройки")]
    [SerializeField] private Animator anim;

    [Header("Настройки извержения")]
    [SerializeField] private GameObject volcanoMeteorPrefab;
    [SerializeField] private Transform eruptionPoint;
    [SerializeField] private int pauseBeforeEruption = 2;

    [Header("Режимы выброса")]
    [SerializeField] private int burstCount = 3;          // Количество залпов
    [SerializeField] private int meteorsPerBurst = 10;    // Метеоров в одном залпе
    [SerializeField] private float delayBetweenBursts = 1.5f;

    [Header("Баллистика")]
    [SerializeField] private float minDistance = 10f;     // Минимальная дальность полёта
    [SerializeField] private float maxDistance = 80f;     // Максимальная дальность полёта
    [SerializeField] private float heightMultiplier = 1.2f; // Высота траектории (множитель)
    [SerializeField] private float spreadAngle = 60f;     // Угол разлёта
    [SerializeField] private float meteorSpeed = 15f;     // Скорость метеоров
    [SerializeField] private float eruptionDelayInBurst = 0.1f; // Задержка между метеорами в залпе

    // Кэш для оптимизации
    private MapArea mapArea;

    void Start()
    {
        // Пытаемся найти MapArea
        mapArea = FindObjectOfType<MapArea>();
        
        if (mapArea == null)
        {
            Debug.LogWarning("MapArea не найдена на сцене! Метеоры будут использовать альтернативную логику целей.");
        }
    }

    public void ShowVulcano()
    {
        anim.Play("Show");
        Invoke(nameof(Eruption), 5 + pauseBeforeEruption);
    }

    void Eruption()
    {
        StartCoroutine(LaunchBursts());
    }

    IEnumerator LaunchBursts()
    {
        for (int burst = 0; burst < burstCount; burst++)
        {
            // Запускаем один залпа метеоров
            StartCoroutine(LaunchSingleBurst());

            // Ждем перед следующим залпом
            yield return new WaitForSeconds(delayBetweenBursts);
        }
    }

    IEnumerator LaunchSingleBurst()
    {
        for (int i = 0; i < meteorsPerBurst; i++)
        {
            Vector3 startPoint = eruptionPoint.position;
            Vector3 targetPoint = GetRandomPointOnMap();

            // Создаем метеор
            GameObject meteor = Instantiate(
                volcanoMeteorPrefab,
                startPoint,
                Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360))
            );

            // Настраиваем метеор
            VolcanoMeteorSingle meteorComponent = meteor.GetComponent<VolcanoMeteorSingle>();
            if (meteorComponent)
            {
                meteorComponent.SetRandomObj();
            }

            // Добавляем физику
            Rigidbody rb = meteor.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Расчет баллистической траектории
                Vector3 velocity = CalculateBallisticVelocity(startPoint, targetPoint, meteorSpeed);
                
                // Добавляем небольшую случайность к траектории
                velocity += new Vector3(
                    Random.Range(-2f, 2f),
                    Random.Range(-1f, 1f),
                    Random.Range(-2f, 2f)
                );
                
                rb.velocity = velocity;
            }

            yield return new WaitForSeconds(eruptionDelayInBurst);
        }
    }

    // Расчет баллистической траектории
    private Vector3 CalculateBallisticVelocity(Vector3 start, Vector3 target, float speed)
    {
        Vector3 direction = target - start;
        float distance = direction.magnitude;
        float gravity = Physics.gravity.magnitude;
        
        // Расчет угла для попадания в цель
        float angle = Mathf.Deg2Rad * 45f; // 45 градусов - оптимальный угол для максимальной дальности
        
        // Корректировка угла на основе дистанции
        float distanceRatio = Mathf.Clamp01(distance / maxDistance);
        angle = Mathf.Lerp(Mathf.Deg2Rad * 60f, Mathf.Deg2Rad * 30f, distanceRatio);
        
        // Добавляем высоту траектории на основе дистанции
        direction.y = 0; // Обнуляем вертикальную составляющую для расчета только горизонтального направления
        
        // Расчет скорости на основе баллистических формул
        float vx = Mathf.Sqrt((distance * gravity) / (2 * Mathf.Sin(2 * angle)));
        
        // Если скорость получается слишком большой, корректируем угол
        if (float.IsNaN(vx) || vx > speed * 2)
        {
            vx = speed;
        }
        
        direction = direction.normalized;
        Vector3 velocity = direction * vx;
        
        // Добавляем вертикальную составляющую
        velocity.y = vx * Mathf.Sin(angle) * heightMultiplier;
        
        return velocity;
    }

    // Получаем случайную точку в пределах карты
    private Vector3 GetRandomPointOnMap()
    {
        if (mapArea != null)
        {
            // Используем MapArea для получения случайной точки
            return mapArea.GetRandomPosition();
        }
        else
        {
            // Если MapArea не найдена, используем свою логику
            Vector3 direction = GetRandomSpreadDirection();
            float distance = Random.Range(minDistance, maxDistance);
            return eruptionPoint.position + direction * distance;
        }
    }

    // Получение случайного направления в пределах заданного угла
    private Vector3 GetRandomSpreadDirection()
    {
        float randomAngle = Random.Range(-spreadAngle / 2, spreadAngle / 2);
        Quaternion spreadRotation = Quaternion.Euler(0, randomAngle, 0);
        return spreadRotation * transform.forward;
    }
}