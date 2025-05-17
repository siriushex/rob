using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeteorImpact : MonoBehaviour
{
    [SerializeField] private GameObject explosionEffect;

    private void OnCollisionEnter(Collision collision)
    {
        Instantiate(explosionEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}