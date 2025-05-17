using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(SphereCollider))]
public abstract class MovingDisaster : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] protected float moveSpeed = 10f;
    [SerializeField] protected float switchTargetInterval = 3f;

    [Header("Center")]
    [SerializeField] protected Transform effectCenter;

    protected SphereCollider triggerZone;
    protected Vector3 targetPosition;
    protected Dictionary<Rigidbody, Coroutine> pullingBodies = new Dictionary<Rigidbody, Coroutine>();

    protected virtual void Start()
    {
        triggerZone = GetComponent<SphereCollider>();
        triggerZone.isTrigger = true;

        if (effectCenter == null)
            effectCenter = transform;

        ChooseNewTarget();
        StartCoroutine(ChangeTargetRoutine());
    }

    protected virtual void Update()
    {
        MoveTowardsTarget();
    }

    protected void MoveTowardsTarget()
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
    }

    private IEnumerator ChangeTargetRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(switchTargetInterval);
            ChooseNewTarget();
        }
    }

    protected virtual void ChooseNewTarget()
    {
        Vector3 min = MapArea.Instance.GetMin();
        Vector3 max = MapArea.Instance.GetMax();
        targetPosition = new Vector3(
            Random.Range(min.x, max.x),
            transform.position.y,
            Random.Range(min.z, max.z)
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        var rb = other.attachedRigidbody;
        if (rb != null && !pullingBodies.ContainsKey(rb))
        {
            Coroutine pullRoutine = StartCoroutine(PullObject(rb));
            pullingBodies.Add(rb, pullRoutine);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var rb = other.attachedRigidbody;
        if (rb != null && pullingBodies.TryGetValue(rb, out Coroutine routine))
        {
            StopCoroutine(routine);
            pullingBodies.Remove(rb);
        }
    }

    protected abstract IEnumerator PullObject(Rigidbody rb);

    protected virtual void OnDrawGizmosSelected()
    {
        if (MapArea.Instance != null)
        {
            Gizmos.color = new Color(0, 0, 0, 0.15f);
            Vector3 center = (MapArea.Instance.GetMin() + MapArea.Instance.GetMax()) / 2;
            Vector3 size = MapArea.Instance.GetMax() - MapArea.Instance.GetMin();
            Gizmos.DrawCube(center, size);
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
