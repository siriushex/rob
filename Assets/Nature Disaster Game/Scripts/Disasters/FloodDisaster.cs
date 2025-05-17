using UnityEngine;
using Invector.vCharacterController; // для доступа к vHealthController
using System.Collections;
public class FloodDisaster : MonoBehaviour
{
    [Header("Наводнение")]
    [SerializeField] private float floodDuration = 40f;
    [SerializeField] private float targetHeight = 10f;
    [SerializeField] private Transform waterTransform;

    [Header("Урон")]
    [SerializeField] private float damagePerSecond = 10f;

    private Vector3 startPos;
    private Vector3 endPos;
    private Coroutine floodRoutine;

    private bool isFloodActive = false;

    void Start()
    {
        startPos = waterTransform.position;
        endPos = new Vector3(startPos.x, startPos.y + targetHeight, startPos.z);
    }

    public void StartFlood()
    {
        Debug.Log("StartFlood");
        if (floodRoutine != null)
            StopCoroutine(floodRoutine);

        waterTransform.position = startPos;
        isFloodActive = true;
        floodRoutine = StartCoroutine(RaiseWater());
    }

    private IEnumerator RaiseWater()
    {
        float elapsed = 0f;

        while (elapsed < floodDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / floodDuration);
            waterTransform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        waterTransform.position = endPos;
        Debug.Log("Вода достигла максимума.");
    }

    private void OnTriggerStay(Collider other)
    {
        //if (!isFloodActive) return;

        if (other.CompareTag("Player"))
        {
            Debug.Log("stay");
            var health = other.GetComponent<vThirdPersonController>();
            if (health != null)
            {
                health.TakeDamage(new Invector.vDamage((int)(damagePerSecond * Time.deltaTime/2)));
            }
        }
    }
}
