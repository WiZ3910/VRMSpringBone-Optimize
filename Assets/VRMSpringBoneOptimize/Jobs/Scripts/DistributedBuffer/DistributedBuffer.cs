﻿namespace VRM.Optimize.Jobs
{
    using System.Collections.Generic;
    using UnityEngine;
    using Unity.Collections;
    using Unity.Mathematics;
    using IDisposable = System.IDisposable;

    public sealed class DistributedBuffer : MonoBehaviour, IDisposable
    {
        // ------------------------------

        #region // Properties

        // Components References
        public VRMSpringBoneJob[] SpringBones { get; private set; }
        public List<VRMSpringBoneJob> UpdateCenterBones { get; private set; }
        public List<VRMSpringBoneColliderGroupJob> ColliderGroups { get; } = new List<VRMSpringBoneColliderGroupJob>();

        // Jobs Data
        public SpringBoneJobData SpringBoneJobDataValue { get; private set; }
        public ColliderGroupJobData ColliderGroupJobDataValue { get; private set; }

        // Collider Data
        public NativeMultiHashMap<int, SphereCollider> ColliderHashMap { get; set; }
        public int ColliderHashMapLength { get; private set; }

        // Parent Rotations
        public NativeArray<quaternion> ParentRotations { get; private set; }

        #endregion // Properties


        // ----------------------------------------------------

        #region // Public Methods

        public void Initialize()
        {
            var allNodes = new List<VRMSpringBoneJob.Node>();

            // VRMSpringBoneの初期化
            this.SpringBones = this.GetComponents<VRMSpringBoneJob>();
            foreach (var springBone in this.SpringBones)
            {
                springBone.Initialize();
                allNodes.AddRange(springBone.Nodes);

                // m_centerを持つ物を保持
                if (springBone.m_center != null)
                {
                    if (this.UpdateCenterBones == null)
                    {
                        this.UpdateCenterBones = new List<VRMSpringBoneJob>();
                    }

                    this.UpdateCenterBones.Add(springBone);
                }

                // SpringBoneに登録されている全コライダーの取得
                // →同じコライダーが参照されている時があるので重複は取り除く
                foreach (var collider in springBone.ColliderGroups)
                {
                    if (collider.Colliders == null || collider.Colliders.Length <= 0)
                    {
                        continue;
                    }

                    if (this.ColliderGroups.Contains(collider))
                    {
                        continue;
                    }

                    this.ColliderGroups.Add(collider);
                }
            }

            this.SpringBoneJobDataValue = new SpringBoneJobData(allNodes);
            this.ParentRotations = new NativeArray<quaternion>(allNodes.Count, Allocator.Persistent);

            // VRMSpringBoneColliderGroupの初期化
            foreach (var collider in this.ColliderGroups)
            {
                collider.Initialize();
                this.ColliderHashMapLength += collider.BlittableFieldsArray.Length;
            }

            this.ColliderGroupJobDataValue = new ColliderGroupJobData(this.ColliderGroups);
        }

        public void Dispose()
        {
            if (this.ColliderHashMap.IsCreated)
            {
                this.ColliderHashMap.Dispose();
            }

            this.SpringBoneJobDataValue.Dispose();
            this.ColliderGroupJobDataValue.Dispose();
            this.ParentRotations.Dispose();

            foreach (var springBone in this.SpringBones)
            {
                springBone.Dispose();
            }

            foreach (var collider in this.ColliderGroups)
            {
                collider.Dispose();
            }

            if (this.UpdateCenterBones != null)
            {
                this.UpdateCenterBones.Clear();
            }
        }

        #endregion // Public Methods
    }
}
