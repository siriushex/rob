using UnityEngine;
using System.Collections;


public class FireSpreadManager : MonoBehaviour
{
    public static FireSpreadManager Instance;
   // public float spreadRadius = 5f;
    public float delayBetweenSpread = 1f;

    [SerializeField] private Material orangeMaterial; // 🔸
    [SerializeField] private Material blackMaterial;  // 🔲

    [SerializeField] private float burnTime = 5f;
    [SerializeField] private ParticleSystem fireEffect;

    public float baseSpreadRadius = 3f;
    [SerializeField] [Range(0, 1)] private float spreadChance = 0.4f;
    [SerializeField] private LayerMask flammableMask;

    private void Awake()
    {
        Instance = this;
    //    StartFire();
    }

    public void StartFire()
    {
        DisasterDestructible[] all = FindObjectsOfType<DisasterDestructible>();

        if (all.Length == 0)
        {
            Debug.LogWarning("Нет доступных объектов для пожара.");
            return;
        }

        DisasterDestructible start = all[Random.Range(0, all.Length)];
        Debug.Log("[FireSpread] Пожар начался на объекте: " + start.gameObject.name);
        start.OnFire(orangeMaterial, blackMaterial, fireEffect);
    }


    public void SpreadFrom(DisasterDestructible source)
    {
        StartCoroutine(SpreadRoutine(source));
    }

    private IEnumerator SpreadRoutine(DisasterDestructible source)
    {
        Debug.Log($"Начало распространения от {source.name}");
        yield return new WaitForSeconds(delayBetweenSpread);

        float currentRadius = baseSpreadRadius * GetFireIntensity(source);

        // Визуализация радиуса в редакторе
        //Debug.DrawRay(source.transform.position, Vector3.up * 5f, Color.red, 2f);
        //Gizmos.color = Color.yellow;
        // Gizmos.DrawWireSphere(source.transform.position, currentRadius);
        Vector3 sourceCenter = source.GetObjectCenter();
        Collider[] nearbyObjects = Physics.OverlapSphere(
            sourceCenter,
            currentRadius,
            flammableMask
        );

        // Детальный лог найденных объектов
        Debug.Log($"Поиск в радиусе {currentRadius} от {source.name}. Найдено: {nearbyObjects.Length}");
        foreach (var collider in nearbyObjects)
        {
            Debug.Log($"Обнаружен объект: {collider.name} (слой: {LayerMask.LayerToName(collider.gameObject.layer)})");
        }

        foreach (var collider in nearbyObjects)
        {
            float chance = Random.value;
            Debug.Log($"Проверка {collider.name}. Шанс: {chance} / Требуется: {spreadChance}");

            if (chance > spreadChance)
            {
                Debug.Log($"{collider.name} - не прошел проверку шанса");
                continue;
            }

            var destructible = collider.GetComponent<DisasterDestructible>();
            if (destructible != null)
            {
                if (!destructible.isBurning && !destructible.isBurned)
                {
                    Debug.Log($"Поджигаем {destructible.name}...");
                    destructible.OnFire(orangeMaterial, blackMaterial, fireEffect);

                    // Рекурсивный вызов для новых объектов
                    if (destructible.fireState == DisasterDestructible.FireState.Burning)
                    {
                        StartCoroutine(SpreadRoutine(destructible));
                    }

                    yield return new WaitForSeconds(0.2f);
                }
                else
                {
                    Debug.Log($"{destructible.name} уже горит или сгорел");
                }
            }
            else
            {
                Debug.Log($"У {collider.name} нет компонента DisasterDestructible");
            }
        }
    }

    public float GetFireIntensity(DisasterDestructible source)
    {
        // Интенсивность в зависимости от стадии пожара
        switch (source.fireState)
        {
            case DisasterDestructible.FireState.WaitingToBurn: return 0.8f;
            case DisasterDestructible.FireState.Burning: return 1.2f;
            case DisasterDestructible.FireState.Burned: return 0.5f;
            default: return 1f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, baseSpreadRadius);
        }
    }
}
