using UnityEngine;

public class BlackHoleCore : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Игрок уничтожен чёрной дырой!");
            // Здесь можешь вызвать смерть игрока
            // other.GetComponent<Player>().Die();
        }
        else
        {
            Destroy(other.gameObject);
        }
    }
}
