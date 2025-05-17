using UnityEngine;

public interface ISpawnableEntity
{
    void SpawnToRandomPoint(Transform[] points);
    void ReturnToStartPoint(Transform[] points);
}
