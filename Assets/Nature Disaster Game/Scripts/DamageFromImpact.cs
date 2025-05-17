// Assets/Scripts/DamageFromImpact.cs
//
// Добавьте компонент на объект, где уже стоит vThirdPersonController
// или любой другой Invector-Health.
//
// 1. При OnCollisionEnter вычисляем кинетическую энергию удара.
// 2. Если энергия выше порога – вызываем штатный Invector ApplyDamage().

using UnityEngine;
using Invector.vCharacterController;          // vHealthController
using Invector.vCharacterController.vActions; // vDamage, vDamageType


namespace Invector
{
    [RequireComponent(typeof(vThirdPersonController))]
    public class DamageFromImpact : MonoBehaviour
    {
        [Header("Порог срабатывания")]
        [Tooltip("Минимальная энергия удара (Джоули), при которой дипаем урон")]
        public float minImpactEnergy = 50f;

        [Tooltip("Коэффициент перевода энергии удара в очки урона\n" +
                 "damage = energy * coeff")]
        public float energy2Damage = 0.2f;

        [Tooltip("Максимальное количество урона за одно столкновение")]
        public float maxSingleDamage = 50f;

        [Tooltip("Минимальная скорость столкновения, с которой учитываем удар (м/с)")]
        public float minImpactSpeed = 6f;

        [Tooltip("Не реагировать на тела легче этого (кг)")]
        public float minMass = 1f;

        // кэшируем ссылку на Invector-здоровье
        private vHealthController health;

        void Awake()
        {
            health = GetComponent<vHealthController>();
            if (health == null)
                Debug.LogError($"{name}: не найден vHealthController!");
        }

        private void OnCollisionEnter(Collision col)
        {
            if (health == null) return;

            // Игнорируем очень лёгкие тела (масса ? 0) и триггеры
            if (!col.rigidbody || col.rigidbody.isKinematic) return;

            // Относительная скорость столкновения (м/с)
            float v = col.relativeVelocity.magnitude;
            float m = col.rigidbody.mass;

            if (m < minMass) return;
            if (v < minImpactSpeed) return;
            // Кинетическая энергия: E = ?·m·v?  (Дж)
            float E = 0.5f * m * v * v;

            if (E < minImpactEnergy) return;          // слабый удар – ничего

            // Переводим энергию в урон
            float damageValue = Mathf.Clamp(E * energy2Damage, 1f, maxSingleDamage);

            // Формируем структуру vDamage (Invector)
            vDamage damage = new vDamage();
            damage.damageValue = damageValue;
            damage.sender = col.transform;            // кто нанёс
            //damage.damageType = vDamageType.Physical;

            // Применяем
            health.TakeDamage(damage);
        }
    }
}
