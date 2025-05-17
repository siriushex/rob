using UnityEngine;
using Unity.AI.Navigation;

[RequireComponent(typeof(NavMeshSurface))]
public class MapNavMeshBaker : MonoBehaviour
{
    NavMeshSurface surf;
    bool rebakeRequested;

    void Awake() => surf = GetComponent<NavMeshSurface>();

    void Start() => surf.BuildNavMesh();     // первый bake после загрузки

    void Update()
    {
        if (rebakeRequested)
        {
            rebakeRequested = false;
          //  surf.BuildNavMeshAsync();        // быстрая асинхронная перестройка
        }
    }

    /// <summary>Вызывайте после «массового» разрушения.</summary>
    public void RequestRebake() => rebakeRequested = true;
}
