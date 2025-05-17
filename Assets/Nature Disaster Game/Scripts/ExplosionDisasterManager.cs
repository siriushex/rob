using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// «апускает серию случайных взрывов по сцене.
/// </summary>
public class ExplosionDisasterManager : MonoBehaviour
{
    public static ExplosionDisasterManager Instance;

    private ExplosionSettings settings;
    private readonly List<DisasterDestructible> allTargets = new();
    private Coroutine routine;

    private void Awake() => Instance = this;

    /// <summary>—тарт бедстви€.</summary>
    public void StartExplosionStorm(ExplosionSettings sett)
    {
        if (sett == null) { Debug.LogError("ExplosionSettings == null"); return; }

        settings = sett;

        // набираем живые цели
        allTargets.Clear();
        allTargets.AddRange(FindObjectsOfType<DisasterDestructible>().Where(d => d != null));

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ExplosionRoutine());
    }

    private IEnumerator ExplosionRoutine()
    {
        int spawned = 0;

        while (spawned < settings.explosionsCount && allTargets.Count > 0)
        {
            int id = Random.Range(0, allTargets.Count);
            DisasterDestructible target = allTargets[id];

            if (target == null) { allTargets.RemoveAt(id); yield return null; continue; }

            // 1) кинематика off
            if (target.TryGetComponent(out Rigidbody rbT)) rbT.isKinematic = false;

            // 2) VFX
            Instantiate(settings.explosionVFX, target.transform.position, Quaternion.identity);

            // 3) ударна€ волна
            Collider[] hits = Physics.OverlapSphere(target.transform.position, settings.explosionRadius);
            foreach (var col in hits)
            {
                if (col.TryGetComponent(out DisasterDestructible other) && other != target)
                {
                    if (other.TryGetComponent(out Rigidbody rbO)) rbO.isKinematic = false;
                }
            }

            // 4) уничтожаем цель
            Destroy(target.gameObject, 0.1f);
            allTargets.RemoveAt(id);
            spawned++;

            // 5) пауза
            float delay = Random.Range(settings.delayRange.x, settings.delayRange.y);
            yield return new WaitForSeconds(delay);
        }
    }
}
