#if UNITY_EDITOR
using DragonWater.Utils;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DragonWater.Editor
{
    [CustomEditor(typeof(WaveProfile))]
    internal class WaveProfileDrawer : EditorEx
    {
        static GerstnerWavesGenerator _gerstnerGenerator = new();

        protected override void DrawInspector()
        {
            var profile = (WaveProfile)target;

            DrawFoldoutSection("waveslist", "Waves List", () => DrawWavesEditor(profile));

            EditorGUILayout.Space();
            DrawTitle("Projection");
            profile.TextureSize = EditorGUILayout.IntPopup("Texture Size", profile.TextureSize, TEXTURE_SIZES_STR, TEXTURE_SIZES);
            PropertyField(serializedObject.FindProperty(nameof(WaveProfile.projectionSize)));

            EditorGUILayout.Space();
            DrawTitle("Wave Area");
            PropertyField(serializedObject.FindProperty(nameof(WaveProfile.globalAmplitudeMultiplier)), "Amplitude Multiplier");
            PropertyField(serializedObject.FindProperty(nameof(WaveProfile.globalSteepnessMultiplier)), "Steepness Multiplier");
            PropertyField(serializedObject.FindProperty(nameof(WaveProfile.globalHillnessMultiplier)), "Hillness Multiplier");
            PropertyField(serializedObject.FindProperty(nameof(WaveProfile.includeLocalAreas)));

            EditorGUILayout.Space();
            DrawTitle("Hillness");
            PropertyField(serializedObject.FindProperty(nameof(WaveProfile.hillnessOffsetFactor)));
            PropertyField(serializedObject.FindProperty(nameof(WaveProfile.hillnessNormalPower)));

            EditorGUILayout.Space();
            DrawTitle("Ripple effect");
            PropertyField(serializedObject.FindProperty(nameof(WaveProfile.rippleMaxDepth)));
            PropertyField(serializedObject.FindProperty(nameof(WaveProfile.rippleTime)));
            PropertyField(serializedObject.FindProperty(nameof(WaveProfile.rippleRestoreTime)));
            PropertyField(serializedObject.FindProperty(nameof(WaveProfile.rippleBlurStep)));
            PropertyField(serializedObject.FindProperty(nameof(WaveProfile.rippleBlurAttenuation)));
        }

        protected void DrawWavesEditor(WaveProfile profile)
        {
            PushGUITint(0.75f, 1.0f, 1.0f);
            for (int i=0; i<profile.WavesCount; i++)
            {
                var wave = profile.GetWave(i);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                var angle = wave.direction.magnitude < 0.1f ? 0.0f : -Vector2.SignedAngle(Vector2.up, wave.direction);
                var newAngle = EditorGUILayout.Slider("Direction", angle, -180f, 180f);
                if (newAngle != angle)
                {
                    newAngle = Mathf.Clamp(newAngle, -179.99f, 179.99f);
                    wave.direction = new Vector2(
                        Mathf.Sin(newAngle * Mathf.Deg2Rad),
                        Mathf.Cos(newAngle * Mathf.Deg2Rad)
                        );
                }

                wave.number = GerstnerWave.LengthToNumber(Mathf.Clamp(EditorGUILayout.FloatField("Length", wave.length), 0.01f, 1000f));
                wave.steepness = EditorGUILayout.Slider("Steepness", wave.steepness, 0, 1);

                EditorGUILayout.BeginHorizontal();
                EditorGUIUtility.labelWidth /= 3.0f;
                //EditorGUIUtility.fieldWidth /= 2.0f;

                GUI.enabled = !profile.autoWaveHeight;
                var height = EditorGUILayout.FloatField("Height", wave.height);
                wave.amplitude = profile.autoWaveHeight ? GerstnerWave.GetAmplitude(wave.length, wave.steepness, profile.autoWaveHeightRatio) : height * 0.5f;
               
                GUI.enabled = !profile.autoWaveSpeed;
                EditorGUILayout.LabelField("", GUILayout.Width(10));
                var speed = EditorGUILayout.FloatField("Speed", wave.speed);
                wave.speed = profile.autoWaveSpeed ? GerstnerWave.GetSpeed(wave.number, profile.autoWaveSpeedGravity) : speed;
                
                GUI.enabled = false;
                EditorGUIUtility.labelWidth /= 2.0f;
                EditorGUILayout.LabelField("", GUILayout.Width(10));
                EditorGUILayout.FloatField("Hz", wave.frequency);
                EditorGUIUtility.labelWidth *= 2.0f;
                GUI.enabled = true;
                EditorGUILayout.LabelField("", GUILayout.Width(10));
                EditorGUIUtility.labelWidth *= 3.0f;
                //EditorGUIUtility.fieldWidth *= 2.0f;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                PushGUITint(1.25f, 0.75f, 0.75f);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                if (GUILayout.Button("X"))
                {
                    profile.RemoveWave(i);
                    break;
                }
                EditorGUILayout.EndVertical();
                PopGUITint();
                EditorGUILayout.EndHorizontal();
                profile.SetWave(i, wave);
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (GUILayout.Button("Add Wave"))
            {
                profile.AddWave(profile.WavesCount == 0 ? GerstnerWave.CreateDefault() : profile.GetWave(profile.WavesCount - 1));
            }
            EditorGUILayout.EndVertical();

            // draw config
            EditorGUILayout.Space();
            RepushGUITint(1.0f, 1.0f, 0.75f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            profile.autoWaveHeight = EditorGUILayout.ToggleLeft("Auto Wave Height", profile.autoWaveHeight);
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = profile.autoWaveHeight;
            EditorGUILayout.LabelField("", GUILayout.Width(20));
            profile.autoWaveHeightRatio = EditorGUILayout.Slider("LS Ratio", profile.autoWaveHeightRatio, 0.01f, 0.25f);
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;

            profile.autoWaveSpeed = EditorGUILayout.ToggleLeft("Auto Wave Speed", profile.autoWaveSpeed);
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = profile.autoWaveSpeed;
            EditorGUILayout.LabelField("", GUILayout.Width(20));
            profile.autoWaveSpeedGravity = EditorGUILayout.FloatField("Gravity", profile.autoWaveSpeedGravity);
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;

            EditorGUILayout.EndVertical();


            // draw generator
            EditorGUILayout.Space();
            RepushGUITint(0.75f, 1.0f, 0.75f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            _gerstnerGenerator.steepnessDistribution = EditorGUILayout.CurveField("Steepness distribution", _gerstnerGenerator.steepnessDistribution);
            _gerstnerGenerator.frequencyRange = EditorGUILayout.Vector2Field("Frequency range", _gerstnerGenerator.frequencyRange);
            _gerstnerGenerator.steepnessMultiplier = EditorGUILayout.FloatField("Steepness multiplier", _gerstnerGenerator.steepnessMultiplier);
            _gerstnerGenerator.gravity = EditorGUILayout.FloatField("Gravity", _gerstnerGenerator.gravity);
            _gerstnerGenerator.lsRatio = EditorGUILayout.FloatField("LS Ratio", _gerstnerGenerator.lsRatio);
            _gerstnerGenerator.directionRandomness = EditorGUILayout.Slider("Direction randomness", _gerstnerGenerator.directionRandomness, 0.0f, 1.0f);
            _gerstnerGenerator.wavesCount = EditorGUILayout.IntSlider("Waves count", _gerstnerGenerator.wavesCount, 1, 24);

            if (GUILayout.Button("Generate"))
            {
                var waves = _gerstnerGenerator.Generate();
                while (profile.WavesCount > 0) profile.RemoveWave(0);
                foreach (var wave in waves)
                {
                    profile.AddWave(wave);
                }
            }

            EditorGUILayout.EndVertical();

            PopGUITint();
        }

        protected override void OnChanges()
        {
            var profile = (WaveProfile)target;
            var uninitialize = false;

            //
            //if (profile.WavesCount > (profile._waveBuffer?.count ?? 0))
            //    uninitialize = true;
            //

            if (uninitialize)
                profile.Uninitalize();
        }

        protected override void DrawInspectorDebug()
        {
            var profile = (WaveProfile)target;
            EditorGUILayout.BeginVertical();
            EditorGUILayout.ObjectField("Texture Offset", profile.TextureOffset, typeof(RenderTexture), false);
            EditorGUILayout.ObjectField("Texture Normal", profile.TextureNormal, typeof(RenderTexture), false);
            EditorGUILayout.Toggle("Processing Height Offset", profile.IsProcessingHeighOffsetTexture);
            EditorGUILayout.ObjectField("Texture Height Offset", profile.TextureHeightOffset, typeof(RenderTexture), false);
            EditorGUILayout.EndVertical();
        }

        protected override bool ConstantSceneUpdate()
        {
            return true;
        }
    }
}
#endif