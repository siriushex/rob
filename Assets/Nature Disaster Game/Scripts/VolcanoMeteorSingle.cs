using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VolcanoMeteorSingle : MonoBehaviour
{
    [Header("Визуализация")]
    [SerializeField] private Material[] material;
    [SerializeField] private Vector3 minScale;
    [SerializeField] private Vector3 maxScale;
    [SerializeField] private GameObject explosionEffectPrefab;

    [Header("Настройки взрыва")]
    [SerializeField] private float explosionChance = 0.5f; // 50% шанс взрыва
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float explosionForce = 500f;
    [SerializeField] private float upwardsModifier = 1f;
    [SerializeField] private LayerMask explosionLayerMask = ~0; // По умолчанию все слои
    
    private MeshRenderer meshRenderer;
    private Rigidbody rb;
    private bool hasExploded = false;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        rb = GetComponent<Rigidbody>();
        
        if (meshRenderer == null)
        {
            meshRenderer = GetComponentInChildren<MeshRenderer>();
        }
    }

    public void SetRandomObj()
    {
        transform.localScale = new Vector3(
            Random.Range(minScale.x, maxScale.x), 
            Random.Range(minScale.y, maxScale.y), 
            Random.Range(minScale.z, maxScale.z)
        );
        
        if (meshRenderer && material.Length > 0)
        {
            meshRenderer.material = material[Random.Range(0, material.Length)];
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;
        
        // Проверяем шанс взрыва (50%)
        if (Random.value <= explosionChance)
        {
            Explode();
        }
    }
    
    private void Explode()
    {
        hasExploded = true;
        
        // Создаем эффект взрыва
        if (explosionEffectPrefab != null)
        {
            GameObject explosionFX = Instantiate(
                explosionEffectPrefab, 
                transform.position, 
                Quaternion.identity
            );
            
            // Уничтожаем эффект через 3 секунды
            Destroy(explosionFX, 3f);
        }
        
        // Находим все объекты с Rigidbody в радиусе взрыва
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius, explosionLayerMask);
        
        foreach (Collider hit in colliders)
        {
            Rigidbody targetRb = hit.GetComponent<Rigidbody>();
            
            if (targetRb != null)
            {
                targetRb.isKinematic = false; 
                // Применяем взрывную силу
                targetRb.AddExplosionForce(
                    explosionForce, 
                    transform.position, 
                    explosionRadius, 
                    upwardsModifier, 
                    ForceMode.Impulse
                );
            }
        }
        
        // Уничтожаем метеор после взрыва
        Destroy(gameObject);
    }
    
    private void OnDrawGizmosSelected()
    {
        // Визуализируем радиус взрыва
        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
    }
}