using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using VoxReader.Interfaces;
using Random = UnityEngine.Random;

namespace VoxelDestructionPro.Data
{
    /// <summary>
    /// This is the data object that contains all needed data for the Voxel model of a
    /// Voxel object. Note: If you create a VoxelData you also have to dispose it once it is not
    /// used anymore
    /// <br/><br/>
    /// The Voxels are stored inside a NativeArray of Voxels, this improves
    /// the performance since the voxels are often passed into jobs.
    /// <br/><br/>
    /// A second NativeArray of Color defines the Color palette of the object.
    /// This is limited to 255 entries since the Voxels use a byte to reference the index
    /// of the Color. MagicaVoxel currently also uses Color palettes with maximal 255 colors
    /// </summary>
    public class VoxelData : IDisposable
    {
        public NativeArray<Voxel> voxels;
        /// <summary>
        /// This can directly be used in jobs, since we will
        /// never modify the color pallete after creating it
        /// </summary>
        public NativeArray<Color> palette;
        /// <summary>
        /// xyz size of the voxel object
        /// </summary>
        public int3 length;

        public VoxelData(Voxel[] voxels, Color[] palette, int3 length)
        {
            this.voxels = new NativeArray<Voxel>(voxels, Allocator.Persistent);
            this.palette = new NativeArray<Color>(palette, Allocator.Persistent);
            this.length = length;
        }

        public VoxelData(Voxel[] voxels, NativeArray<Color> palette, int3 length)
        {
            this.voxels = new NativeArray<Voxel>(voxels, Allocator.Persistent);
            this.palette = new NativeArray<Color>(palette, Allocator.Persistent);
            this.length = length;
        }
        
        /// <summary>
        /// Creates Voxeldata from an IModel (Vox file model),
        /// loads the color palette
        /// </summary>
        /// <param name="model"></param>
        public VoxelData(IModel model)
        {
            Vector3Int l = Vector3Int.FloorToInt(model.Size);
            length = new int3(l.x, l.y, l.z);
            
            //Create color palette
            List<Color> colors = new List<Color>();

            for (int i = 0; i < model.Voxels.Length; i++)
            {
                if (!colors.Contains(model.Voxels[i].Color))
                    colors.Add(model.Voxels[i].Color);
            }

            palette = new NativeArray<Color>(colors.Select(t => new Color(t.r / 255f, t.g / 255f, t.b / 255f, 1f)).ToArray(), Allocator.Persistent);
            
            //Create 3D array
            Voxel[,,] tmpSorted = new Voxel[length.x, length.y, length.z];
            
            Voxel emptyVoxel = Voxel.emptyVoxel;
            for (int x = 0; x < length.x; x++)
                for (int y = 0; y < length.y; y++)
                    for (int z = 0; z < length.z; z++)
                        tmpSorted[x, y, z] = emptyVoxel;
            
            //Create Voxels
            for (int i = 0; i < model.Voxels.Length; i++)
            {
                try
                {
                    tmpSorted[(int)model.Voxels[i].Position.x, (int)model.Voxels[i].Position.y, (int)model.Voxels[i].Position.z] =
                        new Voxel((byte)colors.IndexOf(model.Voxels[i].Color), 1);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Error sorting Voxel Array! \n Voxel: {i}, Array Position: {(int)model.Voxels[i].Position.x}/{(int)model.Voxels[i].Position.y}/{(int)model.Voxels[i].Position.z}" + e.Message + e.StackTrace);
                    throw;
                }
            }
         
            voxels = new NativeArray<Voxel>(length.x * length.y * length.z, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
         
            for (int x = 0; x < length.x; x++)
                for (int y = 0; y < length.y; y++)
                    for (int z = 0; z < length.z; z++)
                        voxels[To1D(x, y, z, length)] = tmpSorted[x, y, z];
        }

        /// <summary>
        /// Creates a Voxeldata from a CachedVoxelData reference
        /// </summary>
        /// <param name="data"></param>
        public VoxelData(CachedVoxelData data)
        {
            voxels = new NativeArray<Voxel>(data.voxels, Allocator.Persistent);
            palette = new NativeArray<Color>(data.palette, Allocator.Persistent);
            length = data.length;
        }
        
        public CachedVoxelData ToCachedVoxelData()
        {
            return new CachedVoxelData(voxels.ToArray(), palette.ToArray(), length);
        }

        /// <summary>
        /// 3D Index to 1D index conversion
        /// </summary>
        /// <param name="index"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private static int To1D(int x, int y, int z, int3 length)
        {
            return x + length.x * (y + length.y * z);
        }
        
        /// <summary>
        /// Checks if the VoxelData is empty (no active voxels)
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty()
        {
            for (int i = 0; i < voxels.Length; i++)
                if (voxels[i].active != 0)
                    return false;

            return true;
        }

        /// <summary>
        /// Checks if the count of active voxels is larger than
        /// a defines minimum
        /// </summary>
        /// <param name="min"></param>
        /// <returns></returns>
        public bool ActiveCountLarger(int min)
        {
            int voxelCount = 0;
            
            for (int i = 0; i < voxels.Length; i++)
                if (voxels[i].active != 0)
                {
                    voxelCount++;

                    if (voxelCount > min)
                        return true;
                }

            return false;
        }

        /// <summary>
        /// Returns how many of the voxels are active
        /// </summary>
        /// <returns></returns>
        public int GetActiveVoxelCount()
        {
            int count = 0;
            int vLength = voxels.Length;
    
            for (int i = 0; i < vLength; i++)
            {
                count += voxels[i].active > 0 ? 1 : 0;
            }

            return count;
        }
        
        public void Dispose()
        {
            if (voxels.IsCreated)
                voxels.Dispose();
            if (palette.IsCreated)
                palette.Dispose();
        }
    }
}
