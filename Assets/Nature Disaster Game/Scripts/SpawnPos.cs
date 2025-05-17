using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPos : MonoBehaviour
{
    public Transform[] spawns;

    /*
    public Transform GetRandomPos()
    {
        Transform randomPoint =  spawns[Random.Range(0, spawns.Length)];
        return randomPoint;
    }
    */
    public Transform[] GetSpawnPoint()
    {
        return spawns;
    }
}
