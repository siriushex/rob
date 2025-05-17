using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DragonWater.Utils
{
    [Serializable]
    public class GerstnerWavesGenerator
    {
        public AnimationCurve steepnessDistribution = new(new(0.0f, 0.0f), new(0.2f, 0.3f), new(0.45f, 0.25f), new(0.7f, 0.15f));
        public Vector2 frequencyRange = new Vector2(0.05f, 0.6f);
        public float steepnessMultiplier = 0.75f;
        public float gravity = 9.81f;
        public float lsRatio = 0.143f;
        public float directionRandomness = 0.5f;
        public int wavesCount = 8;

        public GerstnerWave[] Generate()
        {
            var waves = new GerstnerWave[wavesCount];

            var freqSubRange = (frequencyRange.y - frequencyRange.x) / wavesCount;
            var baseDirection = Random.Range(0.0f, Mathf.PI * 2.0f);

            for (int i = 0; i < wavesCount; i++)
            {
                var wave = GerstnerWave.CreateDefault();

                var directionOffset = Random.Range(-1.0f, 1.0f);
                var directionOffsetSnapped = Mathf.Pow(Mathf.Abs(directionOffset), Mathf.Lerp(10.0f, 1.0f, Mathf.Sqrt(directionRandomness)));
                directionOffset = Mathf.Sign(directionOffset) * directionOffsetSnapped;
                var direction = baseDirection + directionOffset * Mathf.PI;

                var frequency = Random.Range(frequencyRange.x + freqSubRange * i, frequencyRange.x + freqSubRange * (i + 1));
                var length = GerstnerWave.GetLength(frequency, gravity);
                var distribution = steepnessDistribution.Evaluate(frequency);

                wave.direction = new Vector2(
                        Mathf.Sin(direction),
                        Mathf.Cos(direction)
                        );
                wave.number = GerstnerWave.LengthToNumber(length);
                wave.steepness = distribution * steepnessMultiplier;
                wave.amplitude = GerstnerWave.GetAmplitude(length, wave.steepness, lsRatio);
                wave.speed = GerstnerWave.GetSpeed(wave.number, gravity);

                waves[i] = wave;
            }

            return waves;
        }
    }
}
