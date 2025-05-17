using UnityEngine;
using System.Collections;

public class Tornado : MovingDisaster
{
    [Header("Vortex Settings")]
    [SerializeField] private float pullForce = 100f;
    [SerializeField] private float spinForce = 80f;
    [SerializeField] private float upwardForce = 40f;
    [SerializeField] private float maxLiftHeight = 50f;
    [SerializeField] private float forceMultiplier = 2f;

    protected override IEnumerator PullObject(Rigidbody rb)
    {
        while (rb != null)
        {
            Vector3 toCenter = effectCenter.position - rb.worldCenterOfMass;
            float distance = toCenter.magnitude;

            float distanceFactor = Mathf.Clamp01(1 - distance / triggerZone.radius);
            float totalForceMultiplier = 1 + forceMultiplier * distanceFactor;

            Vector3 pullDirection = toCenter.normalized;
            Vector3 tangentDirection = Vector3.Cross(pullDirection, Vector3.up).normalized;

            float verticalFactor = Mathf.Clamp01((maxLiftHeight - rb.position.y) / maxLiftHeight);

            rb.AddForce(pullDirection * pullForce * totalForceMultiplier * Time.deltaTime, ForceMode.Force);
            rb.AddForce(tangentDirection * spinForce * totalForceMultiplier * Time.deltaTime, ForceMode.Force);
            rb.AddForce(Vector3.up * upwardForce * verticalFactor * Time.deltaTime, ForceMode.Force);

            rb.angularVelocity *= 0.9f;

            yield return null;
        }
    }
}
