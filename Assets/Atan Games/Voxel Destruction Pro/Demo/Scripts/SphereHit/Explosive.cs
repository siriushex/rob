using UnityEngine;
using VoxelDestructionPro.Data;
using VoxelDestructionPro.VoxelObjects;

namespace VoxelDestructionPro.Demo
{
    public class Explosive : MonoBehaviour
    {
        public float explosionDelay;
        private float explosionTime;

        [Space] 
        
        public float explosionRadius = 10f;
        public float explosionForce = 20f;
        
        public DestructionData.DestructionType destructionType = DestructionData.DestructionType.Sphere;
        
        private void Start()
        {
            explosionTime = Time.time + explosionDelay;
        }

        private void Update()
        {
            if (Time.time > explosionTime)
            {
                Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

                for (int i = 0; i < colliders.Length; i++)
                {
                    DynamicVoxelObj vox = colliders[i].GetComponentInParent<DynamicVoxelObj>();
                    
                    if (vox == null)
                        continue;

                    if (destructionType == DestructionData.DestructionType.Sphere)
                        vox.AddDestruction_Sphere(transform.position, explosionForce);
                    else
                        vox.AddDestruction_Cube(transform.position, explosionForce);
                }
                
                Destroy(gameObject);
            }
        }
    }
}