using UnityEngine;
using UnityEngine.SceneManagement;
using VoxelDestructionPro.VoxelObjects;

namespace VoxelDestructionPro.Demo
{
    public class ClickToDestroy : MonoBehaviour
    {
        public Camera cam;

        public float destructionRadius = 2;
    
        private void Update()
        {
            bool oneClick = Input.GetKey(KeyCode.LeftShift);
        
            if ((!oneClick && Input.GetMouseButton(0)) || (oneClick && Input.GetMouseButtonDown(0)))
            {
                Ray r = cam.ScreenPointToRay(Input.mousePosition);
            
                if (!Physics.Raycast(r, out RaycastHit hit, 999))
                    return;
            
                DynamicVoxelObj vo = hit.transform.GetComponentInParent<DynamicVoxelObj>();
            
                if (vo == null)
                    return;
                
                vo.AddDestruction_Sphere(hit.point, destructionRadius);
            }
        }

        public void SwitchToMovement()
        {
            SceneManager.LoadScene(0);
        }
    }
}