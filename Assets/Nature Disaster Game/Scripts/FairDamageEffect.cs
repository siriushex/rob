using System.Collections;
using UnityEngine;
using Invector.vCharacterController; 
public class FairDamageEffect : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private GameObject fireEffect;
    [SerializeField] private int burningDamage = 5;
    [SerializeField] private int burnedDamage = 10;
    [SerializeField] private float damageInterval = 1f;

    private Invector.vHealthController playerHealth;
    private Coroutine damageCoroutine;

    private void Start()
    {
        // Получаем компонент здоровья игрока
        playerHealth = GetComponent<vThirdPersonController>();
        if (playerHealth == null)
        {
            Debug.LogError("vHealthController не найден на игроке!");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        var destructible = collision.collider.GetComponent<DisasterDestructible>();
        if (destructible == null) return;

        if (destructible.fireState == DisasterDestructible.FireState.Burning)
        {
            fireEffect.SetActive(true);
            if (damageCoroutine == null)
            {
                damageCoroutine = StartCoroutine(ApplyBurningDamage());
            }
        }
        else if (destructible.fireState == DisasterDestructible.FireState.Burned)
        {
            ApplySingleDamage(burnedDamage);
        }
    }



    private IEnumerator ApplyBurningDamage()
    {
        while (true)
        {
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(new Invector.vDamage(burningDamage));
            }
            yield return new WaitForSeconds(damageInterval);
        }
    }

    private void ApplySingleDamage(int damage)
    {
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(new Invector.vDamage(damage));
        }
    }
}