using UnityEngine;
using Invector.vCharacterController;

namespace Invector
{
    [RequireComponent(typeof(vHealthController))]
    public class ImpactDamageReceiver : MonoBehaviour
    {
        [Header("Настройки урона от удара")]
        public float damageThreshold = 10f;     // минимальная сила удара
        public float maxDamage = 100f;          // максимальный урон
        public float damageMultiplier = 1f;     // множитель урона

        private vHealthController health;

        void Awake()
        {
            health = GetComponent<vHealthController>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            // У объекта должен быть Rigidbody
            Rigidbody rb = collision.rigidbody;
            if (rb == null) return;

            // Игнорировать, если столкнулись с землёй или чем-то неопасным
            if (collision.gameObject.CompareTag("Ground")) return;

            // Вычисляем силу удара
            float impactForce = collision.relativeVelocity.magnitude * rb.mass;

            if (impactForce < damageThreshold) return;

            float damageValue = Mathf.Clamp((impactForce - damageThreshold) * damageMultiplier, 0, maxDamage);

            Debug.Log($"[ImpactDamageReceiver] Удар от: {collision.gameObject.name} | Сила: {impactForce:F1} | Урон: {damageValue:F0}");

            health.TakeDamage(new vDamage((int)damageValue));
        }
    }
}
