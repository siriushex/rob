using System.Collections;
using UnityEngine;

public class Tor : MonoBehaviour
{
    public Transform tornadoCenter;       // Центр торнадо, к которому притягивает
    public float pullForce = 10f;         // Сила притяжения
    public float refreshRate = 0.05f;     // Частота применения силы

    private readonly string targetTag = "OBJ"; // Тег, который должен быть у притягиваемых объектов

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            StartCoroutine(PullObject(other, true));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            StartCoroutine(PullObject(other, false));
        }
    }

    private IEnumerator PullObject(Collider target, bool shouldPull)
    {
        // Пока объект внутри торнадо   
        while (shouldPull && target != null && target.GetComponent<Rigidbody>() != null)
        {
            Vector3 forceDir = tornadoCenter.position - target.transform.position;

            target.GetComponent<Rigidbody>().AddForce(
                forceDir.normalized * pullForce * Time.deltaTime,
                ForceMode.Force
            );

            yield return new WaitForSeconds(refreshRate);
        }
    }
}
