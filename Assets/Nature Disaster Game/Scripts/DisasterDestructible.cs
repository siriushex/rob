using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DisasterDestructible : MonoBehaviour, IDisasterReactive
{
    public enum FireState { None, WaitingToBurn, Burning, Burned }
    public FireState fireState = FireState.None;

    [Header("🔥 Огонь")]
    private Material burnedMaterial;
    private ParticleSystem fireEffect;

    private Rigidbody rb;
    private GameObject fireInstance;

    public bool isBurning = false;
    public bool isBurned = false;

    [Header("🌧️ Кислотный дождь")]
    [SerializeField] private bool reactsToAcidRain = true;
    [SerializeField, Range(0f, 1f)] private float acidRainReactionChance = 0.7f;
    [SerializeField] private Vector2 reactionDelayRange = new Vector2(0f, 3f);
    [SerializeField] private Vector2 materialChangeIntervalRange = new Vector2(2f, 5f);

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    // 🔥 Реакция на огонь
    public void OnFire(Material orangeMat, Material blackMat, ParticleSystem fireVFX)
    {
        if (isBurning || isBurned) return;
        burnedMaterial = blackMat;
        fireEffect = fireVFX;
        StartCoroutine(BurnRoutineWithStage(orangeMat));
    }

    private IEnumerator BurnRoutineWithStage(Material orangeMaterial)
    {
        if (!isBurned && !isBurning)
        {
            isBurning = true;
            fireState = FireState.WaitingToBurn;

            fireInstance = Instantiate(fireEffect.gameObject, GetObjectCenter(), Quaternion.identity, transform);

            yield return new WaitForSeconds(5f); // до активного горения

            fireState = FireState.Burning;
            GetComponent<MeshRenderer>().material = orangeMaterial;
            rb.isKinematic = false;

            yield return new WaitForSeconds(2f); // до распространения

            FireSpreadManager.Instance.SpreadFrom(this);

            float burnDuration = Random.Range(5f, 10f);
            yield return new WaitForSeconds(burnDuration);
            Debug.Log("IS BURNED");
            fireState = FireState.Burned;
            GetComponent<MeshRenderer>().material = burnedMaterial;
            if (fireInstance != null) Destroy(fireInstance);
            isBurning = false;
            isBurned = true;
            // Толчок при разрушении
            Vector3 randomDirection = new Vector3(Random.Range(-0.3f, 0.3f), -1f, Random.Range(-0.3f, 0.3f)).normalized;
            rb.AddForce(randomDirection * 80f, ForceMode.Impulse);  
        }
    }

    // 🌧️ Реакция на кислотный дождь
    public void OnAcidRain(Material[] materialsList)
    {
        if (!reactsToAcidRain)
        {
            Debug.Log($"[AcidRain] {gameObject.name} — отключена реакция на кислоту");
            return;
        }

        float roll = Random.value;
        if (roll > acidRainReactionChance)
        {
            Debug.Log($"[AcidRain] {gameObject.name} — не пострадал (шанс {roll:F2})");
            return;
        }

        float delay = Random.Range(reactionDelayRange.x, reactionDelayRange.y);
        StartCoroutine(DelayedAcidRainReaction(materialsList, delay));
    }

    private IEnumerator DelayedAcidRainReaction(Material[] materialsList, float delay)
    {
        yield return new WaitForSeconds(delay);

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            Debug.LogWarning($"[AcidRain] Нет MeshRenderer у {gameObject.name}");
            yield break;
        }

        foreach (var mat in materialsList)
        {
            renderer.materials = new Material[] { mat };
            float interval = Random.Range(materialChangeIntervalRange.x, materialChangeIntervalRange.y);
            yield return new WaitForSeconds(interval);
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Disaster" || other.tag == "Tsunami")
        {
            rb.isKinematic = false;
            Invoke("OnDestroyObj", 20f);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.collider.tag == "Meteors")
        {
            rb.isKinematic = false;
        }
    }
    void OnDestroyObj()
    {
        Destroy(this.gameObject);
    }
    

    // Реакция на другие бедствия
    public void OnFlood() { rb.isKinematic = false; }

    public void OnEarthquake()
    {
        rb.isKinematic = false;
        rb.AddExplosionForce(100f, transform.position + Vector3.down, 5f);
    }

    public void OnTornado() { } //rb.isKinematic = false;
    public void OnBlackHole() {  } //rb.isKinematic = false;
    public void OnVolcanoEroption() { } //rb.isKinematic = false;

    // Центр объекта для эффектов
    public Vector3 GetObjectCenter()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null) return renderer.bounds.center;

        Collider collider = GetComponent<Collider>();
        if (collider != null) return collider.bounds.center;

        return transform.position;
    }
    // В классе DisasterDestructible
    private void OnDrawGizmosSelected()
{
    if (fireState == FireState.Burning && FireSpreadManager.Instance != null)
    {
        Vector3 center = GetObjectCenter();
        float radius = FireSpreadManager.Instance.baseSpreadRadius 
                     * FireSpreadManager.Instance.GetFireIntensity(this);
        
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawWireSphere(center, radius);
    }

    
}
}
