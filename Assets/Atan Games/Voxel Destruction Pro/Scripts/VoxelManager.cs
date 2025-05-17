using System;
using System.Collections.Generic;
using System.Linq;
using Better.StreamingAssets;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using VoxelDestructionPro.Data;
using VoxelDestructionPro.Settings;
using VoxelDestructionPro.Tools;

namespace VoxelDestructionPro
{
    public class VoxelManager : MonoBehaviour
    {
        private static VoxelManager _Instance;

        public static VoxelManager Instance
        {
            get
            {
                if (_Instance == null)
                    Debug.LogError("There is no Voxel Manager in the scene!");

                return _Instance;
            }
        }
        
        public enum ColliderType
        {
            None, Standard, Convex
        }
        
        [Header("Quick setup")]
        
        public Material standardMaterial;
        public ColliderType standardCollider;
        public MeshSettingsObj standardMeshSettings;
        public IsoSettings standardIsolationSettings;
        public DynSettings standardDynamicSettings;
        public Transform fragmentParent;
        
        //Vox obj caching
        private Dictionary<Tuple<string, int>, CachedVoxelData> voxelCache;

        private bool betterStreamingAssetsLoaded;
        
        private void Awake()
        {
            if (_Instance == null)
            {
                _Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_Instance != this)
            {
                Destroy(gameObject);
            }
            
            betterStreamingAssetsLoaded = false;
        }
        
        #region VoxCaching
        
        public void LoadBSA()
        {
            //I think it is better if we dont load it multiple times
            if (betterStreamingAssetsLoaded)
                return;

            betterStreamingAssetsLoaded = true;
            BetterStreamingAssets.Initialize();
        }
        
        /// <summary>
        /// Loads Voxeldata and caches it, this allows
        /// the reuse of Voxeldata
        /// </summary>
        /// <param name="modelpath"></param>
        /// <param name="modelIndex"></param>
        /// <returns></returns>
        public VoxelData LoadAndCacheVoxFile(string modelpath, int modelIndex)
        {
            voxelCache ??= new Dictionary<Tuple<string, int>, CachedVoxelData>();
            
            Tuple<string, int> key = new Tuple<string, int>(modelpath, modelIndex);
            if (voxelCache.ContainsKey(key))
            {
                //Aleady cached, we dont need to load
                return new VoxelData(voxelCache[key].GetCopy());
            }

            VoxelParser parser = new VoxelParser(modelpath, modelIndex);
            VoxelData file = parser.ParseToVoxelData();
            voxelCache.Add(key, file.ToCachedVoxelData().GetCopy());

            return file;
        }
        
        #endregion
    }
}