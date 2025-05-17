using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeteorRain : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private GameObject meteorPrefab; // Префаб метеора
    [SerializeField] private float spawnHeight = 100f; // Высота спавна
    [SerializeField] private int meteorsPerWave = 10; // Метеоров за волну
    [SerializeField] private float waveInterval = 2f; // Интервал между волнами
    [SerializeField] private float meteorSpeed = 50f; // Скорость падения

    void Start()
    {
        StartCoroutine(RainRoutine());
    }

    private IEnumerator RainRoutine()
    {
        while (true)
        {
            // Спавн волны метеоров
            for (int i = 0; i < meteorsPerWave; i++)
            {
                SpawnMeteor();
                yield return new WaitForSeconds(0.1f); // Задержка между метеорами в волне
            }
            yield return new WaitForSeconds(waveInterval); // Пауза между волнами
        }
    }

    private void SpawnMeteor()
    {
        Vector3 spawnPosition = new Vector3(
            Random.Range(MapArea.Instance.GetMin().x, MapArea.Instance.GetMax().x),
            spawnHeight,
            Random.Range(MapArea.Instance.GetMin().z, MapArea.Instance.GetMax().z)
        );

        GameObject meteor = Instantiate(meteorPrefab, spawnPosition, Quaternion.identity);
        SetupMeteor(meteor);
    }
    private void SetupMeteor(GameObject meteor)
    {
        Rigidbody rb = meteor.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Случайный наклон траектории
            Vector3 direction = Vector3.down + new Vector3(
                Random.Range(-0.3f, 0.3f),
                0,
                Random.Range(-0.3f, 0.3f)
            ).normalized;

            rb.velocity = direction * meteorSpeed;
            rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse); // Вращение
        }
    }
}