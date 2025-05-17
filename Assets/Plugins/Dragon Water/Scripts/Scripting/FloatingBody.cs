using System.Collections.Generic;
using UnityEngine;

namespace DragonWater.Scripting
{
    [AddComponentMenu("Dragon Water/Floating Body")]
    [RequireComponent(typeof(Rigidbody))]
    public class FloatingBody : WaterBehaviour
    {
        [SerializeField] public Vector3[] contactPoints = new Vector3[] { Vector3.zero };
        [SerializeField] internal Vector3 centerOfMass = new Vector3(0, -1, 0);

        [Tooltip("How much force will be applied to buoyancy. This force is split evenly for each contact point.")]
        [SerializeField] [Min(0)] internal float buoyancyForce = 100;
        [Tooltip("By default, buyonacy force is extra divided by rigidbody mass. With this option you can ignore it. You will want to have low buyonacy force with this option enabled.")]
        [SerializeField] internal bool ignoreMass = false;

        [Tooltip("How much wave normal will affect applied buyonacy force. It is useful if you have like only one contact point to achieve more realistic floating.")]
        [SerializeField] [Range(0, 1)] internal float normalAlignemnt = 0.0f;
        [Tooltip("By default, Floating Body adds extra counter-force to neutralize self-moving body and keep it stable on water. Higher instability mean that body will move with wave direction, negative means opposite. Instability scales with amount of normal alignment.")]
        [SerializeField] internal float instability = 0.0f;

        [Tooltip("Extra Drag applied to rigidbody when it's fully submerged (all contact points are underwater).")]
        [SerializeField] internal float submergeDrag = 1.0f;
        [Tooltip("Extra Angular Drag applied to rigidbody when it's fully submerged (all contact points are underwater).")]
        [SerializeField] internal float submergeAngularDrag = 1.0f;

        #region main properties
        public Vector3 CenterOfMass
        {
            get => centerOfMass;
            set
            {
                centerOfMass = value;
                if (Rigidbody) Rigidbody.centerOfMass = centerOfMass;
            }
        }
        public float BuoyancyForce { get => buoyancyForce; set => buoyancyForce = value; }
        public bool IgnoreMass { get => ignoreMass; set => ignoreMass = value; }
        public float NormalAlignment { get => normalAlignemnt; set => normalAlignemnt = value; }
        public float Instability { get => instability; set => instability = value; }
        public float SubmergeDrag { get => submergeDrag; set => submergeDrag = value; }
        public float SubmergeAngularDrag { get => submergeAngularDrag; set => submergeAngularDrag = value; }
        #endregion

        public float CurrentSubmergeLevel { get; private set; } = 0.0f;
        public Rigidbody Rigidbody { get; private set; }


        protected override void Awake()
        {
            base.Awake();
            Rigidbody = GetComponent<Rigidbody>();
        }
        private void Start()
        {
            Rigidbody.centerOfMass = centerOfMass;
        }


        public Vector3 GetContactPoint(int index)
        {
            return contactPoints[index];
        }
        public WaterSampler.HitResult GetContactPointHit(int index)
        {
            if (index < GetResultCount())
                return GetResult(index);
            else
                return default;
        }
        public void SetContactPoint(int index, Vector3 point)
        {
            contactPoints[index] = point;
        }
        public void RemoveContactPoint(int index)
        {
            var tmp = new List<Vector3>(contactPoints);
            tmp.RemoveAt(index);
            contactPoints = tmp.ToArray();
        }
        public void AddContactPoint(Vector3 point)
        {
            var tmp = new List<Vector3>(contactPoints);
            tmp.Add(point);
            contactPoints = tmp.ToArray();
        }


        protected override void InnerFixedUpdate()
        {
            if (contactPoints == null)
                return;

            if (Rigidbody.isKinematic)
            {
                PrepareSamplingPointsSize(0);
                return;
            }

            var pointForce = (buoyancyForce / contactPoints.Length) * -Physics.gravity.y;

            var appliedForce = Vector3.zero;
            var submergedPoints = 0;

            WaterSampler.HitResult hit;
            Vector3 force;
            WaterSurface surface = null;
            GetResults(out var hitResults);
            for (int i = 0; i < hitResults.Count; i++)
            {
                hit = hitResults.Array[i];
                if (!hit.HasHit) continue;

                surface = hit.surface;

                if (hit.IsUnderwater)
                {
                    if (normalAlignemnt == 0)
                        force = Vector3.up * pointForce * hit.Depth;
                    else if (normalAlignemnt == 1)
                        force = hit.hitNormal * pointForce * hit.Depth;
                    else
                        force = Vector3.Lerp(Vector3.up, hit.hitNormal, normalAlignemnt) * pointForce * hit.Depth;

                    force *= hit.surface.physicsBuoyancyFactor;

                    Rigidbody.AddForceAtPosition(force, hit.sampledPoint, ignoreMass ? ForceMode.Acceleration : ForceMode.Force);
                    appliedForce += force;
                    submergedPoints++;
                }
            }

            var counterForce = -appliedForce * (1.0f - instability);
            counterForce.y = 0.0f;
            Rigidbody.AddForce(counterForce, ignoreMass ? ForceMode.Acceleration : ForceMode.Force);

            CurrentSubmergeLevel = (float)submergedPoints / contactPoints.Length;

            if (CurrentSubmergeLevel > 0)
            {
                var drag = submergeDrag + (surface?.physicsExtraDrag ?? 0);
                var angularDrag = submergeAngularDrag + (surface?.physicsExtraAngularDrag ?? 0);

                Rigidbody.AddForce(-Rigidbody.velocity * drag * CurrentSubmergeLevel, ForceMode.Acceleration);
                Rigidbody.AddTorque(-Rigidbody.angularVelocity * angularDrag * CurrentSubmergeLevel, ForceMode.Acceleration);
            }


            if (isSchedulingFrane)
            {
                PrepareSamplingPointsSize(contactPoints.Length);
                var matrix = transform.localToWorldMatrix;
                for (int i = 0; i < contactPoints.Length; i++)
                {
                    samplingPoints[i] = matrix.MultiplyPoint3x4(contactPoints[i]);
                }
            }
        }
    }
}
