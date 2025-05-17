using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using VoxelDestructionPro.Data;
using VoxelDestructionPro.Data.Args;
using VoxelDestructionPro.Data.Fragmenter;
using VoxelDestructionPro.Interfaces;
using VoxelDestructionPro.Jobs.Destruction;
using VoxelDestructionPro.Jobs.Fragmenter;
using VoxelDestructionPro.Settings;

namespace VoxelDestructionPro.VoxelObjects
{
    /// <summary>
    /// This voxel object contains the main destruction functions:
    /// AddDestruction, AddDestruction_Sphere, AddDestruction_Cube and AddDestruction_Line
    /// </summary>
    public class DynamicVoxelObj : IsolatedVoxelObj
    {
        [Header("Settings")]
        
        public DynSettings dynamicSettings;
        
        //Active states
        protected bool destructionActive;
        protected bool fragmenterActive;
        
        private IDestructor destructor;
        private IFragmenter fragmenter;

        [HideInInspector] 
        public Vector3 lastDestructionPoint;
        
        //Events
        /// <summary>
        /// This event gets called whenever a destruction starts, you can set the BlockDestruction bool
        /// inside the EventArgs to true in order to stop the destruction from happening
        /// </summary>
        public EventHandler<VoxDestructionEventArgs> onVoxelDestruction;
        /// <summary>
        /// This event gets called once the destructor job is finished, the nativearray describes
        /// which voxels get removed (voxel data indicies)
        /// </summary>
        public Action<NativeList<int>> onVoxelsRemoved;
        /// <summary>
        /// This event gets called for every fragment that gets created
        /// </summary>
        public Action<GameObject> onFragmentSpawned;
        
        protected override void CreateJobs()
        {
            base.CreateJobs();
            
            destructor ??= new VoxelDestructor(voxelData.length);

            if (dynamicSettings.destructionMode == DynSettings.DestructionMode.SingleFragment)
                fragmenter ??= new SingleFragmenter(voxelData);
            else if (dynamicSettings.destructionMode == DynSettings.DestructionMode.SphereBasedFragments)
                fragmenter ??= new SphereFragmenter(voxelData);
            else if (dynamicSettings.destructionMode == DynSettings.DestructionMode.VoxelFragment)
                fragmenter ??= new VoxelFragmenter(voxelData);
        }

        #region DestructionCalls

        /// <summary>
        /// Create the destruction data yourself,
        /// returns if the destruction will occur, since it will only allow one
        /// destruction at a time
        /// </summary>
        /// <param name="data"></param>
        /// <param name="fragmenterSettings"> Use this to override the default fragmenter settings,
        /// object type depends on destruction mode. For SphereBasedFragments use SphereFragmenterData, ... </param>
        public bool AddDestruction(DestructionData data, object fragmenterSettings = null)
        {
            if (destructionActive || fragmenterActive || !isValidObject || !data.IsValidData())
                return false;

            var args = new VoxDestructionEventArgs();
            args.DestructionDate = data;
            
            if (onVoxelDestruction != null)
            {
                onVoxelDestruction.Invoke(this, args);

                if (args.BlockDestruction)
                    return false;
            }
            
            lastDestructionPoint = data.start;
            lockIsolatorRun = true;
            lockIsolatorRebuild = true;
            destructionActive = true;
            if (isActiveAndEnabled)
                StartCoroutine(_AddDestruction(data, fragmenterSettings));
            return true;
        }
        
        /// <summary>
        /// Removes all voxels that fall into a defined sphere radius
        /// </summary>
        /// <param name="position"></param>
        /// <param name="sphereRadius"></param>
        /// <param name="fragmenterSettings"> Use this to override the default fragmenter settings,
        /// object type depends on destruction mode. For SphereBasedFragments use SphereFragmenterData, ... </param>
        public bool AddDestruction_Sphere(Vector3 position, float sphereRadius, object fragmenterSettings = null)
        {
            DestructionData data = new DestructionData(DestructionData.DestructionType.Sphere, position, Vector3.zero,
                sphereRadius);

            return AddDestruction(data);
        }

        /// <summary>
        /// Removes all voxel that fall into a defined cube half extends
        /// </summary>
        /// <param name="position"></param>
        /// <param name="cubeHalfExtends"></param>
        /// <param name="fragmenterSettings"> Use this to override the default fragmenter settings,
        /// object type depends on destruction mode. For SphereBasedFragments use SphereFragmenterData, ... </param>
        /// <returns></returns>
        public bool AddDestruction_Cube(Vector3 position, float cubeHalfExtends, object fragmenterSettings = null)
        {
            DestructionData data =
                new DestructionData(DestructionData.DestructionType.Cube, position, Vector3.zero, cubeHalfExtends);

            return AddDestruction(data);
        }

        /// <summary>
        /// Removes all voxel that fall into a defined line
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="radius"></param>
        /// <param name="fragmenterSettings"> Use this to override the default fragmenter settings,
        /// object type depends on destruction mode. For SphereBasedFragments use SphereFragmenterData, ... </param>
        /// <returns></returns>
        public bool AddDestruction_Line(Vector3 start, Vector3 end, float radius, object fragmenterSettings = null)
        {
            DestructionData data =
                new DestructionData(DestructionData.DestructionType.Line, start, end, radius);

            return AddDestruction(data);
        }

        #endregion

        private IEnumerator _AddDestruction(DestructionData data, object fragmenterSettings)
        {
            data.start = targetFilter.transform.InverseTransformPoint(data.start) / GetSingleVoxelSize();
            if (data.destructionType == DestructionData.DestructionType.Line)
                data.end = targetFilter.transform.InverseTransformPoint(data.end) / GetSingleVoxelSize();
            
            destructor.Prepare(data);

            while (true)
            {
                if (destructor.isFinished())
                    break;
                
                yield return null;
            }
            
            NativeList<int> voxelIndex = destructor.GetData();

            //This just delets small fragments without running the fragmneter, since
            //there wont be any new fragments anyway
            if (dynamicSettings.destructionMode == DynSettings.DestructionMode.Remove)
            {
                if (!voxelData.ActiveCountLarger(voxelIndex.Length))
                {
                    objectDestructionRequested = true;
                    destructionActive = false;
                    yield break;
                }
            }
            else if (dynamicSettings.destructionMode == DynSettings.DestructionMode.SphereBasedFragments)
            {
                int sphereMin;

                if (fragmenterSettings is SphereFragmenterData sfd)
                    sphereMin = sfd.minSphereRadius;
                else
                    sphereMin = dynamicSettings.defaultSphereSettings.minSphereRadius;

                if (!voxelData.ActiveCountLarger(sphereMin) && voxelIndex.Length > sphereMin)
                {
                    objectDestructionRequested = true;
                    destructionActive = false;
                    yield break;
                }
            }
            
            //Fragmenter transforms the removed voxels into new voxelobjects
            if (dynamicSettings.destructionMode != DynSettings.DestructionMode.Remove)
            {
                //If the fragmenter settings were not assigned use the default ones
                if (dynamicSettings.destructionMode == DynSettings.DestructionMode.SphereBasedFragments &&
                    fragmenterSettings is not SphereFragmenterData)
                    fragmenterSettings = dynamicSettings.defaultSphereSettings;

                if (dynamicSettings.destructionMode == DynSettings.DestructionMode.VoxelFragment &&
                    fragmenterSettings is not VoxelFragmenterData)
                    fragmenterSettings = dynamicSettings.defaultVoxelSettings;
                
                fragmenter.StartFragmenting(voxelData, voxelIndex, fragmenterSettings);
                fragmenterActive = true;
            }
            
            //remove the voxels that fall into destruction range
            Voxel emptyVoxel = Voxel.emptyVoxel;
            for (var i = 0; i < voxelIndex.Length; i++)
                voxelData.voxels[voxelIndex[i]] = emptyVoxel;

            if (onVoxelsRemoved != null)
                onVoxelsRemoved.Invoke(voxelIndex);
            
            destructionActive = false;
            lockIsolatorRun = false;
            lockIsolatorRebuild = false;
            
            if (voxelIndex.Length > 0)
            {
                meshRegenerationRequested = true;
                
                if (isoSettings.isolationMode != IsoSettings.IsolationMode.None)
                    isolatorRequested = true;
            }
        }

        protected override void Update()
        {
            base.Update();

            if (fragmenterActive && fragmenter.IsFinished())
                FinishFragmenting();
        }

        private void FinishFragmenting()
        {
            fragmenterActive = false;
            VoxelData[] fragments = fragmenter.CreateFragments(voxelData, out Vector3[] positions);

            if (fragments == null)
            {
                if (positions != null && fragmenter.UseVoxelFragments())
                {
                    for (int i = 0; i < positions.Length; i++)
                    {
                        GameObject nObj = Instantiate(dynamicSettings.voxelPrefab, targetFilter.transform.TransformPoint(positions[i] * GetSingleVoxelSize()), transform.rotation);
                        nObj.transform.parent = fragmentParent;
                        nObj.transform.localScale = GetSingleVoxelSize() * Vector3.one;
                        
                        if (onFragmentSpawned != null)
                            onFragmentSpawned.Invoke(nObj);
                    }
                }
                    
                return;
            }
            
            for (int i = 0; i < fragments.Length; i++)
            {
                GameObject nObj = Instantiate(dynamicSettings.fragmentPrefab, targetFilter.transform.TransformPoint(positions[i] * GetSingleVoxelSize()), transform.rotation);
                nObj.transform.parent = fragmentParent;
                
                VoxelObjBase vox = nObj.GetComponent<VoxelObjBase>();

                if (vox != null)
                {
                    vox.scaleType = ScaleType.Voxel;
                    vox.objectScale = GetSingleVoxelSize();
                    if (vox is IsolatedVoxelObj iso)
                        iso.fragmentParent = fragmentParent;
                        
                    vox.AssignVoxelData(fragments[i]);   
                }
                else
                    fragments[i].Dispose();
                    
                if (onFragmentSpawned != null)
                    onFragmentSpawned.Invoke(nObj);
            }
        }

        public override void QuickSetup(VoxelManager manager)
        {
            base.QuickSetup(manager);

            dynamicSettings = manager.standardDynamicSettings;
        }

        protected override bool AssertVoxelObject()
        {
            if (dynamicSettings == null)
            {
                Debug.LogError("No dynamic Voxel object settings assigned!");
                return false;
            }

            return base.AssertVoxelObject();
        }

        protected override bool CanDestroyObject()
        {
            if (fragmenterActive)
                return false;
            
            return base.CanDestroyObject();
        }

        protected override void DisposeAll()
        {
            if (destructor != null)
                destructor.Dispose();
            destructor = null;
            if (fragmenter != null)
                fragmenter.Dispose();
            fragmenter = null;
            
            base.DisposeAll();
        }
        
        protected override void DestroyVoxObj()
        {
            destructionActive = false;
            fragmenterActive = false;
            base.DestroyVoxObj();
        }
    }   
}