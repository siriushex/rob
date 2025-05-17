using UnityEngine;
using System.Collections;

public class BlackHole : MovingDisaster
{
    [Header("Black Hole Settings")]
    [SerializeField] private float pullForce = 500f;

    protected override IEnumerator PullObject(Rigidbody rb)
    {
        while (rb != null)
        {
            Vector3 forceDir = (effectCenter.position - rb.worldCenterOfMass).normalized;
            float distance = Vector3.Distance(rb.worldCenterOfMass, effectCenter.position);
            float forceMultiplier = 1 + (1 / Mathf.Max(distance, 0.1f));

            rb.AddForce(forceDir * pullForce * forceMultiplier * Time.deltaTime, ForceMode.Force);
            rb.angularVelocity *= 0.95f;

            yield return null;
        }
    }
}
