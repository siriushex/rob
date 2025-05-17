using UnityEngine;
using Invector.vCharacterController;
using System.Collections;

public class AcidRainReceiver : MonoBehaviour
{
    [Header("Acid Rain Settings")]
    [SerializeField] private float checkInterval = 1f;
    [SerializeField] private float damagePerTick = 10f;
    [SerializeField] private float rayDistance = 100f;
    [SerializeField] private LayerMask coverLayers;

    private Invector.vHealthController health;
    private Coroutine checkRoutine;
    private bool isRaining = false;

    BodyPartsController partsController;

    private void Awake()
    {
        health = GetComponent<Invector.vHealthController>();
        partsController = GetComponent<BodyPartsController>();
    }

    private void OnEnable()
    {
        if (checkRoutine == null)
            checkRoutine = StartCoroutine(DamageCheckRoutine());
    }

    private void OnDisable()
    {
        if (checkRoutine != null)
            StopCoroutine(checkRoutine);
    }

    public void EnableAcidRain()
    {
        Debug.Log("[AcidRainReceiver] Acid rain enabled");
        isRaining = true;
    }

    public void DisableAcidRain()
    {
        Debug.Log("[AcidRainReceiver] Acid rain disabled");
        isRaining = false;
    }

    private IEnumerator DamageCheckRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);

            if (!isRaining)
            {
                Debug.Log("[AcidRain] Rain not active.");
                continue;
            }

            if (health == null)
            {
                Debug.LogWarning("[AcidRain] No health component found.");
                yield break;
            }

            if (health.isDead)
            {
                Debug.Log("[AcidRain] Player already dead.");
                continue;
            }

            if (!IsUnderCover())
            {
                Debug.Log("[AcidRain] ❌ Player is under open sky — applying damage.");
                health.TakeDamage(new Invector.vDamage((int)damagePerTick));
                float x = Random.Range(0, 1);
                if(x <= 0.5f)
                {
                    partsController.DeleteRandomPart();
                }
            }
            else
            {
                Debug.Log("[AcidRain] ✅ Player is under cover — no damage.");
            }
        }
    }

    private bool IsUnderCover()
    {
        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Ray ray = new Ray(origin, Vector3.up);

        bool hit = Physics.Raycast(ray, out RaycastHit hitInfo, rayDistance, coverLayers);

        Debug.DrawRay(origin, Vector3.up * rayDistance, hit ? Color.green : Color.red, 1f);

        return hit;
    }
}
