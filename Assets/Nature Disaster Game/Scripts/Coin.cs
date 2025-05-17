using UnityEngine;
public class Coin : MonoBehaviour
{
    int value;
    public static System.Action addCoin;
    AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }
    public void OnTriggerEnter(Collider other)
    {
         if(other.tag == "Player")
        {
            value = Random.Range(1, 10);
            addCoin.Invoke();
            audioSource.Play();
            Destroy(gameObject);    
        }
    }
}
