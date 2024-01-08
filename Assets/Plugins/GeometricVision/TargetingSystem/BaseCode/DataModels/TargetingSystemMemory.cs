// Copyright © 2020-2022 Mikael Korpinen(Finland). All Rights Reserved.

using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.BaseCode.TargetingComponents;
using Unity.Collections;
using Unity.Mathematics;

namespace Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels
{
    public class TargetingSystemMemory
    {
        public NativeArray<float4> PlanesVertices { get; set; }
        internal float4 camera4X4m00_m01_m02_m03;
        internal float4 camera4X4m10_m11_m12_m13;
        internal float4 camera4X4m20_m21_m22_m23;
        private NativeArray<float4> planesAsNative;
        internal NativeList<Target> GameObjectProcessorTargets;
        public NativeArray<float4> PlanesAsNative
        {
            get { return this.planesAsNative; }
            set { this.planesAsNative = value; }
        }

        public float LastTargetingDistance { get; set; }
        public float LastFieldOfView { get; set; }

        internal void InitPersistentNativeArrays()
        {
            //6 planes * 6 vertices
            this.PlanesVertices = new NativeArray<float4>(36, Allocator.Persistent);
            this.planesAsNative = new NativeArray<float4>(6, Allocator.Persistent);
            
        }

        internal void DisposePersistentNativeArrays()
        {
            if (this.PlanesVertices.IsCreated)
            {
                this.PlanesVertices.Dispose();
            }
            if (this.planesAsNative.IsCreated)
            {
                this.planesAsNative.Dispose();
            }
        }
    }
}