using DragonWater.Scripting;
using DragonWater.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DragonWater
{
    [ExecuteAlways]
    public class DragonWaterManager : MonoBehaviour
    {
        public delegate void UnderwaterChangedCallback(WaterSurface from, WaterSurface to);

        #region singleton
        static DragonWaterManager _instance;
        public static DragonWaterManager Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }
   
                var go = GameObject.Find($"/#{nameof(DragonWaterManager)}");
                if (go == null)
                {
                    go = new GameObject($"#{nameof(DragonWaterManager)}");
                    go.hideFlags = HideFlags.HideAndDontSave;
                    go.AddComponent<DragonWaterManager>();
                }

                _instance = go.GetComponent<DragonWaterManager>();
                _instance.EnsureInitialized();
                return _instance;
            }
        }

        internal static DragonWaterManager InstanceUnsafe
        {
            get
            {
                if (_instance == null)
                    return null;
                else
                    return _instance;
            }
        }
        #endregion

        DragonWaterConfig _config;
        public DragonWaterConfig Config
        {
            get
            {
                if (_config != null)
                    return _config;

                _config = Resources.Load<DragonWaterConfig>($"DragonWater/{nameof(DragonWaterConfig)}");

                if (_config == null)
                {
                    _config = ScriptableObject.CreateInstance<DragonWaterConfig>();
                    _config.name = nameof(DragonWaterConfig);
#if UNITY_EDITOR
                    if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode
                        && !UnityEditor.EditorApplication.isUpdating
                        && !UnityEditor.EditorApplication.isCompiling
                        && !_editor_isSaving)
                    {
                        UnityEditor.AssetDatabase.CreateAsset(_config, Constants.ResourcesPath + $"/DragonWater/{nameof(DragonWaterConfig)}.asset");
                    }
                    else
                    {
                        var tmp = _config;
                        _config = null;
                        return tmp;
                    }
#endif
                }

                return _config;
            }
        }

        Material _volumeCutoutMaterial;
        public Material VolumeCutoutMaterial
        {
            get
            {
                if (_volumeCutoutMaterial == null)
                {
                    _volumeCutoutMaterial = new Material(Shader.Find(Constants.Shader.CutoutShaderName));
                }
                return _volumeCutoutMaterial;
            }
        }

        Material _rippleCasterMaterial;
        public Material RippleCasterMaterial
        {
            get
            {
                if (_rippleCasterMaterial == null)
                {
                    _rippleCasterMaterial = new Material(Shader.Find(Constants.Shader.RippleCasterShaderName));
                }
                return _rippleCasterMaterial;
            }
        }

        DragonUnderwaterRenderer _underwaterRenderer;
        public DragonUnderwaterRenderer UnderwaterRenderer
        {
            get
            {
                if (_underwaterRenderer == null)
                {
                    _underwaterRenderer = GetComponent<DragonUnderwaterRenderer>();
                    if (_underwaterRenderer == null)
                    {
                        _underwaterRenderer = gameObject.AddComponent<DragonUnderwaterRenderer>();
                    }
                }
                return _underwaterRenderer;
            }
        }

        public float Time { get; set; } = 0.0f;
        public WaterSurface UnderwaterSurface { get; private set; } = null;
        public WaterSampler.HitResult CameraHitResult { get; private set; } = new();
        public Camera MainCamera => Camera.main;


        public event UnderwaterChangedCallback OnUnderwaterChanged;

        internal event Action onUpdate;
        internal event Action onLateUpdate;
        internal event Action onFixedUpdate;
        internal int fixedUpdateCount = 0;


        List<WaterSurface> _surfaces = new();
        List<WaterRippleProjector> _rippleProjectors = new();
        List<LocalWaveArea> _localWaveAreas = new();
        public IReadOnlyList<WaterSurface> Surfaces => _surfaces;
        public WaterSurface DefaultSurface { get; private set; }
        public IReadOnlyList<WaterRippleProjector> RippleProjectors => _rippleProjectors;
        public IReadOnlyList<LocalWaveArea> LocalWaveAreas => _localWaveAreas;

        List<WaveProfile> _waveProfileRequested = new();
        Collider[] _colliderCastCache = new Collider[64];
        WaterSampler _samplerCamera;
        WaterSampler _samplerInstant;

        ComputeBuffer _localWaveAreasBuffer;
        NativeArray<LocalWaveAreaEntry> _localWaveAreasArray;

#if UNITY_EDITOR
        static bool _editor_isSaving = false;
#endif

        bool _initialized = false;
        private void EnsureInitialized()
        {
            if (_initialized) return;

#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.sceneClosing += EditorSceneClosing;
            UnityEditor.SceneManagement.EditorSceneManager.sceneClosed += EditorSceneClosed;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpening += EditorSceneOpening;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += EditorSceneOpened;
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += EditorBeginSave;
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += EditorEndSave;
            UnityEditor.EditorApplication.playModeStateChanged += EditorPlayModeChanged;
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += EditorBeforeAssemblyReload;
#endif

            _samplerCamera = new WaterSampler(1);
            _samplerCamera.SurfaceDetection = WaterSampler.SurfaceDetectionMode.AutoCull;
            _samplerCamera.Precision = 7; // high precision for camera

            _samplerInstant = new WaterSampler(1);
            _samplerInstant.SurfaceDetection = WaterSampler.SurfaceDetectionMode.AutoCull;

            _initialized = true;
        }
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                gameObject.name = "<>";
                UnityEx.SafeDestroy(gameObject);
                return;
            }

            EnsureInitialized();

            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);

                if (!URP.DragonWaterRenderFeature.CheckCurrentInstallation(out var rendererData))
                {
                    Debug.LogError("Your current URP renderer is missing Dragon Water Render Feature!", rendererData);
                }
            }
        }

        private void OnDestroy()
        {
            if (!_initialized) return;

#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.sceneClosing -= EditorSceneClosing;
            UnityEditor.SceneManagement.EditorSceneManager.sceneClosed -= EditorSceneClosed;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpening -= EditorSceneOpening;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= EditorSceneOpened;
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaving -= EditorBeginSave;
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaved -= EditorEndSave;
            UnityEditor.EditorApplication.playModeStateChanged -= EditorPlayModeChanged;
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= EditorBeforeAssemblyReload;
#endif

            NotifyUnderwaterSurface(null);

            _samplerCamera?.Dispose();
            _samplerInstant?.Dispose();

            if (_localWaveAreasArray.IsCreated) _localWaveAreasArray.Dispose();
            if (_localWaveAreasBuffer != null) _localWaveAreasBuffer.Release();
        }

#if UNITY_EDITOR
        // due to HideAndDontSave flags, we have to manually manage freeing resources up here

        static void EditorBeforeAssemblyReload()
        {
            EditorSafeClearSelf();
        }
        [UnityEditor.InitializeOnLoadMethod]
        static void EditorInitializeOnLoad()
        {
            EditorSafeClearSelf();
        }
        static void EditorSafeClearSelf()
        {
            if (_instance)
            {
                _instance.gameObject.name = "<>";
                GameObject.DestroyImmediate(_instance.gameObject);
                _instance = null;
            }
        }


        static void EditorSceneClosing(Scene _, bool __)
        {
            EditorSafeClearSelf();
        }
        static void EditorSceneClosed(Scene _)
        {
            EditorSafeClearSelf();
        }
        static void EditorSceneOpening(string _, UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            //
        }
        static void EditorSceneOpened(Scene _, UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode && mode == UnityEditor.SceneManagement.OpenSceneMode.Single)
            {
                EditorReinitializeSelf();
            }
        }
        static void EditorBeginSave(Scene _, string __)
        {
            InstanceUnsafe?.NotifyUnderwaterSurface(null); // restore originals before save
            _editor_isSaving = true;
        }
        static void EditorEndSave(Scene _)
        {
            _editor_isSaving = false;
        }

        static void EditorReinitializeSelf()
        {
            EditorSafeClearSelf();
            Instance.EditorNothing();
        }
        [UnityEditor.Callbacks.DidReloadScripts]
        static void EditorDidReloadScripts()
        {
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                UnityEditor.EditorApplication.delayCall += EditorReinitializeSelf;
            }
        }

        static void EditorPlayModeChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingEditMode)
            {
                InstanceUnsafe?.NotifyUnderwaterSurface(null); // restore originals before enter
                _editor_isSaving = true;
            }
            else if (state == UnityEditor.PlayModeStateChange.EnteredPlayMode)
            {
                _editor_isSaving = false;
            }
        }

        void EditorNothing() { }
#endif

        internal void RegisterSurface(WaterSurface surface) { _surfaces.Add(surface); UpdateDefaultSurface(); }
        internal void UnregisterSurface(WaterSurface surface) { _surfaces.Remove(surface); UpdateDefaultSurface(); }
        internal void UpdateDefaultSurface()
        {
            var defaults = _surfaces.Where(s => s.isDefaultSurface).ToList();
            if (defaults.Count > 1)
            {
                DragonWaterDebug.LogError("More than 2 default water surfaces detected in the scene. First one on the list will be used...");
            }

            var newDefault = defaults.Count > 0 ? defaults[0] : null;
            if (DefaultSurface != newDefault)
            {
                DefaultSurface = newDefault;
                DragonWaterDebug.Log($"New default water suface: {newDefault}", newDefault);
            }
        }

        internal void RegisterRippleProjector(WaterRippleProjector rippleProjector) {  _rippleProjectors.Add(rippleProjector); }
        internal void UnregisterRippleProjector(WaterRippleProjector rippleProjector) { _rippleProjectors.Remove(rippleProjector); }
        
        internal void RegisterLocalWaveArea(LocalWaveArea localWaveArea) {  _localWaveAreas.Add(localWaveArea); }
        internal void UnregisterLocalWaveArea(LocalWaveArea localWaveArea) { _localWaveAreas.Remove(localWaveArea); }



        internal void RequestWaveProcess(WaveProfile profile)
        {
            if (_waveProfileRequested.Contains(profile))
                return;

            profile.Initialize();
            _waveProfileRequested.Add(profile);
        }


        public void CullWaterSurfaces(ref Bounds bounds, ref List<WaterSurface> list)
        {
            for (int i=0; i<_surfaces.Count; i++)
            {
                if (_surfaces[i].Bounds.Intersects(bounds))
                {
                    list.Add(_surfaces[i]);
                }
            }
        }

        public void CullCutoutColliders(ref Bounds bounds, ref List<Collider> list)
        {
            var sphere = bounds.ToSphere();
            var count = Physics.OverlapSphereNonAlloc(
                sphere,
                sphere.w,
                _colliderCastCache,
                Config.CutoutMask,
                QueryTriggerInteraction.Collide
                );

            for (int i = 0; i < count; i++)
                list.Add(_colliderCastCache[i]);
        }

        public void UpdateUnderwaterProfile()
        {
            if (UnderwaterSurface != null)
            {
                if (UnderwaterSurface.UnderwaterProfile != null)
                {
                    UnderwaterSurface.UnderwaterProfile.Hide();
                    UnderwaterSurface.UnderwaterProfile.Show();
                }
            }
        }


        public WaterSampler.HitResult SampleWater(Vector3 position)
        {
            return SampleWater(position, true);
        }
        public WaterSampler.HitResult SampleWater(Vector3 position, bool considerCutouts)
        {
            _samplerInstant.CutoutDetection = considerCutouts ? WaterSampler.CutoutDetectionMode.AutoCull : WaterSampler.CutoutDetectionMode.DontCutout;
            _samplerInstant.SetPoint(0, position);
            _samplerInstant.Schedule();
            _samplerInstant.Complete();
            return _samplerInstant.Results[0];
        }


        private void Update()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
#endif
                onUpdate?.Invoke();
#if UNITY_EDITOR
            }
            else
            {
                if (_instance != this)
                {
                    gameObject.name = "<>";
                    DestroyImmediate(gameObject);
                    return;
                }

                var camera = UnityEditor.SceneView.lastActiveSceneView?.camera;
                if (camera != null)
                {
                    if (Config.autoTimeSimulation)
                        Time += UnityEngine.Time.deltaTime;

                    UpdateWater(camera);

                    // process ripplers in editor mode
                    ProcessRipplers(camera, 1.0f / 60.0f);
                }
            }
#endif
        }

        private void LateUpdate()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                if (_instance != this)
                {
                    gameObject.name = "<>";
                    DestroyImmediate(gameObject);
                    return;
                }
#endif
                onLateUpdate?.Invoke();

                var camera = MainCamera;
                if (camera != null)
                {
                    if (Config.autoTimeSimulation)
                        Time += UnityEngine.Time.deltaTime;

                    UpdateWater(camera);
                    ProcessRipplers(camera, UnityEngine.Time.deltaTime);
                }
#if UNITY_EDITOR
            }
#endif
        }

        private void FixedUpdate()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
#endif
                // call event
                onFixedUpdate?.Invoke();
                fixedUpdateCount++;

                // process ripplers in physics update due to fixedDeltaTime
                var camera = MainCamera;
                if (camera != null)
                {
                    //ProcessRipplers(camera);
                }

                // most of jobs most likely got scheduled in event before
                // let's actually run them now
                JobHandle.ScheduleBatchedJobs();
#if UNITY_EDITOR
            }
#endif
        }

        private void UpdateWater(Camera camera)
        {
#if UNITY_EDITOR
            if (_editor_isSaving)
                return;
#endif

            // force hide ripple layer from main camera
            if (camera.cullingMask == 0)
            {
                camera.cullingMask = (~0) ^ Config.RippleMask;
            }
            else if ((camera.cullingMask & Config.RippleMask) > 0)
            {
                camera.cullingMask ^= Config.RippleMask;
            }

            // dequeue recently active ripplers
            for (int i = 0; i < _rippleProjectors.Count; i++)
                _rippleProjectors[i].DequeueAllRipplers();

            // update surfaces
            for (int i = 0; i < _surfaces.Count; i++)
                _surfaces[i].UpdateWater(camera);

            // complete all active wave samplers in case of future update
            WaveProfile.Sampler.CompleteAll();

            // update local wave areas
            UpdateLocalWaveAreas();

            // dispatch waves
            var waveComputeShader = Constants.Shader.WaveCompute;
            var waveComputeKernel = waveComputeShader.FindKernel("CSMain");
            waveComputeShader.SetFloat(Constants.Shader.Property.ComputeTime, Time);
            waveComputeShader.SetBuffer(waveComputeKernel, Constants.Shader.Property.ComputeLocalAreas, _localWaveAreasBuffer);
            waveComputeShader.SetInt(Constants.Shader.Property.ComputeLocalAreaCount, _localWaveAreas.Count);

            for (int i = 0; i < _waveProfileRequested.Count; i++)
            {
                if (_waveProfileRequested[i].Initialized)
                    _waveProfileRequested[i].DispatchWaveCompute(waveComputeShader, waveComputeKernel, camera);
            }
            _waveProfileRequested.Clear();

            // update underwater detection
            _samplerCamera.SetPoint(0, camera.transform.position);
            _samplerCamera.Schedule(1);
            _samplerCamera.Complete();

            CameraHitResult = _samplerCamera.Results[0];
            if (CameraHitResult.HasHit && CameraHitResult.IsUnderwater)
            {
                NotifyUnderwaterSurface(CameraHitResult.surface);
            }
            else
            {
                NotifyUnderwaterSurface(null);
            }

            // update underwater fx
            UnderwaterRenderer.UpdateUnderwater(camera);
        }

        private void UpdateLocalWaveAreas()
        {
            if (!_localWaveAreasArray.IsCreated)
                _localWaveAreasArray = new(Constants.MaxLocalWaveAreas, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            for (int i=0; i<_localWaveAreas.Count; i++)
            {
                _localWaveAreasArray[i] = _localWaveAreas[i].GetEntry();
            }

            if (_localWaveAreasBuffer == null || !_localWaveAreasBuffer.IsValid())
            {
                _localWaveAreasBuffer = new(Constants.MaxLocalWaveAreas, LocalWaveAreaEntry.SIZEOF);
            }

            _localWaveAreasBuffer.SetData(_localWaveAreasArray, 0, 0, _localWaveAreas.Count);
        }

        private void NotifyUnderwaterSurface(WaterSurface surface)
        {
            if (UnderwaterSurface == surface)
                return;

            var from = UnderwaterSurface;

            if (UnderwaterSurface != null)
            {
                if (UnderwaterSurface.UnderwaterProfile != null)
                {
                    UnderwaterSurface.UnderwaterProfile.Hide();
                }
            }

            UnderwaterSurface = surface;

            if (UnderwaterSurface != null)
            {
                if (UnderwaterSurface.UnderwaterProfile != null)
                {
                    UnderwaterSurface.UnderwaterProfile.Show();
                }
            }

            OnUnderwaterChanged?.Invoke(from, UnderwaterSurface);
        }

        private void ProcessRipplers(Camera camera, float dt)
        {
            var rippleComputeShader = Constants.Shader.RippleCompute;
            var rippleKernelMain = rippleComputeShader.FindKernel("CSMain");

            rippleComputeShader.SetVector(Constants.Shader.Property.ComputeCameraOffset, camera.GetWaterOffset());
            rippleComputeShader.SetFloat(Constants.Shader.Property.CumputeDeltaTime, dt);

            foreach (var projector in _rippleProjectors)
            {
                projector.DispatchRippleCompute(rippleComputeShader, rippleKernelMain);
                // first dispatch, then update for new offset in case of infinite projector
                projector.UpdateProjector(camera);
            }
        }


        internal void GetSamplerData(out NativeSlice<LocalWaveAreaEntry> areas)
        {
            if (!_localWaveAreasArray.IsCreated)
                UpdateLocalWaveAreas();

            areas = _localWaveAreasArray.Slice(0, LocalWaveAreas.Count);
        }
    }
}