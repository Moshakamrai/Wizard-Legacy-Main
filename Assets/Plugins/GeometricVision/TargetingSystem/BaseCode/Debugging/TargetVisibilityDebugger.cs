
using System;
using System.Collections.Generic;
using System.Linq;
using Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels;
using Plugins.GeometricVision.TargetingSystem.BaseCode.Interfaces;
using Plugins.GeometricVision.TargetingSystem.BaseCode.MainClasses;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Plugins.GeometricVision.TargetingSystem.BaseCode.Debugging
{
    public class TargetVisibilityDebugger
    {
        internal TargetVisibilityDebugger()
        {
        
        }


        List<GameObject> debugPlanes = new List<GameObject>();
        private Plane[] planes;

        internal Vector3[] frustumCornersFar = new Vector3[4];
        internal Vector3[] frustumCornersNear = new Vector3[4];
        

        private GameObject plane;

        public NativeArray<float4> UpdatePlanesVertices(Camera camera, TargetingSystemMemory targetingSystemMemory)
        {
            if (targetingSystemMemory.PlanesVertices.IsCreated && this.debugPlanes.Count == 0 && this.debugPlanes.Count <7)
            {
                targetingSystemMemory.PlanesVertices = this.CreateVertices(this.frustumCornersNear,
                    this.frustumCornersFar, targetingSystemMemory.PlanesVertices);
#if DEBUG_TARGETINGSYSTEM_PLANES
                this.CreateDebugPlanes( this.debugPlanes, targetingSystemMemory.PlanesVertices, camera);
#endif
            }
            else
            {
#if DEBUG_TARGETINGSYSTEM_PLANES
                this.UpdateDebugPlanes( targetingSystemMemory.PlanesVertices, camera);
#endif
                targetingSystemMemory.PlanesVertices = this.CreateVertices(this.frustumCornersNear,
                    this.frustumCornersFar, targetingSystemMemory.PlanesVertices);
                var localToWorldMatrix = camera.transform.localToWorldMatrix;
                targetingSystemMemory.camera4X4m00_m01_m02_m03 = localToWorldMatrix.GetRow(0);
                targetingSystemMemory.camera4X4m10_m11_m12_m13 = localToWorldMatrix.GetRow(1);
                targetingSystemMemory.camera4X4m20_m21_m22_m23 = localToWorldMatrix.GetRow(2);
            }

            return targetingSystemMemory.PlanesVertices;
        }

        internal NativeArray<float4> CreateVertices(Vector3[] frustumCornersNear, Vector3[] frustumCornersFar,
            NativeArray<float4> listToGetResults)
        {
            this.CreatePlaneCorners(frustumCornersNear.Take(2).ToArray(), frustumCornersNear.Skip(2).ToArray(),
                TargetingSystemDataModels.PlaneOrdering.near, ref listToGetResults);
            this.CreatePlaneCorners(frustumCornersFar.Take(2).ToArray(), frustumCornersFar.Skip(2).ToArray(),
                TargetingSystemDataModels.PlaneOrdering.far, ref listToGetResults);
            this.CreatePlaneCorners(frustumCornersFar.Take(2).ToArray(), frustumCornersNear.Take(2).ToArray(),
                TargetingSystemDataModels.PlaneOrdering.left, ref listToGetResults);
            this.CreatePlaneCorners(frustumCornersFar.Skip(2).Take(2).ToArray(),
                frustumCornersNear.Skip(2).Take(2).ToArray(), TargetingSystemDataModels.PlaneOrdering.right,
                ref listToGetResults);

            Vector3[] nearPoints = {frustumCornersNear.Take(1).First(), frustumCornersNear.Skip(3).Take(1).First()};
            Vector3[] farPoints = {frustumCornersFar.Take(1).First(), frustumCornersFar.Skip(3).Take(1).First()};

            this.CreatePlaneCorners(nearPoints, farPoints, TargetingSystemDataModels.PlaneOrdering.down,
                ref listToGetResults);
            this.CreatePlaneCorners(frustumCornersFar.Skip(1).Take(2).Reverse().ToArray(),
                frustumCornersNear.Skip(1).Take(2).ToArray(), TargetingSystemDataModels.PlaneOrdering.up,
                ref listToGetResults);
            return listToGetResults;
        }

        internal void CreateDebugPlanes(List<GameObject> planes, NativeArray<float4> nativeList, Camera camera)
        {
            for (var index = 0; index < 6; index++)
            {
                var vertices = nativeList.ToArray();
                Vector3[] vertices2 = new Vector3[vertices.Length];
                for (int i = 0; i < 36; i += 6)
                {
                    vertices2[i] = camera.transform.TransformPoint(vertices[i].xyz);
                    vertices2[i + 1] = camera.transform.TransformPoint(vertices[i + 1].xyz);
                    vertices2[i + 2] = camera.transform.TransformPoint(vertices[i + 2].xyz);
                    vertices2[i + 3] = camera.transform.TransformPoint(vertices[i + 3].xyz);
                    vertices2[i + 4] = camera.transform.TransformPoint(vertices[i + 4].xyz);
                    vertices2[i + 5] = camera.transform.TransformPoint(vertices[i + 5].xyz);
                    this.debugPlanes.Add(this.CreateUnityPlane(vertices2, Color.clear));
                }
            }
        }

        internal void UpdateDebugPlanes(NativeArray<float4> planesIn, Camera camera)
        {
            for (var index = 0; index < 6; index++)
            {
                var vertices = planesIn.ToArray();
                Vector3[] vertices2 = new Vector3[36];
                int i2 = 0;
                for (int i = 0; i < vertices.Length; i += 6)
                {
                    vertices2[i] = camera.transform.TransformPoint(vertices[i].xyz);
                    vertices2[i + 1] = camera.transform.TransformPoint(vertices[i + 1].xyz);
                    vertices2[i + 2] = camera.transform.TransformPoint(vertices[i + 2].xyz);
                    vertices2[i + 3] = camera.transform.TransformPoint(vertices[i + 3].xyz);
                    vertices2[i + 4] = camera.transform.TransformPoint(vertices[i + 4].xyz);
                    vertices2[i + 5] = camera.transform.TransformPoint(vertices[i + 5].xyz);
                    this.debugPlanes[i2] = this.UpdateUnityPlane(vertices2, Color.clear, this.debugPlanes[i2], i);
                    i2++;
                }
            }
        }

        private GameObject UpdateUnityPlane(Vector3[] vertices2, Color clear, GameObject debugPlane, int i2)
        {
            debugPlane.transform.localPosition = Vector3.zero;
            debugPlane.transform.localRotation = Quaternion.identity;
            // plane2.transform.parent = camera.transform;
            debugPlane.transform.GetComponent<Renderer>().enabled = true;
            this.UpdateMeshPlaneFromCorners(debugPlane.GetComponent<MeshFilter>().mesh, vertices2, i2);
            return debugPlane;
        }


        private Quaternion lastRotation;
        private Quaternion newRotation;
        private float3 lastPos;
        private float3 newPos;

        private GameObject CreateUnityPlane(Vector3[] vertices, Color color)
        {
            var plane2 = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Material debugMaterial = new Material(Shader.Find("Diffuse"));
            color.a = 0.1f;
            debugMaterial.SetColor(Shader.PropertyToID("_Color"), (color));

            var mesh = this.CreateMeshPlaneFromCorners(plane2.GetComponent<MeshFilter>().mesh, vertices);
            plane2.GetComponent<MeshFilter>().mesh = mesh;
            Object.Destroy(plane2.GetComponent<Collider>());

            plane2.transform.localPosition = Vector3.zero;
            plane2.transform.localRotation = Quaternion.identity;
            // plane2.transform.parent = camera.transform;
            plane2.transform.GetComponent<Renderer>().enabled = true;
            this.CreateMeshPlaneFromCorners(plane2.GetComponent<MeshFilter>().mesh, vertices);
            return plane2;
        }

        private void CreatePlaneCorners(Vector3[] frustumCornersFar, Vector3[] frustumCornersNear,
            TargetingSystemDataModels.PlaneOrdering planeOrderingType, ref NativeArray<float4> planesAndVertices)
        {
            if (
                TargetingSystemDataModels.PlaneOrdering.far == planeOrderingType)
            {
                //triangle 0
                planesAndVertices[30] = new float4(frustumCornersFar[0], 0);
                planesAndVertices[31] = new float4(frustumCornersFar[1], 0);

                planesAndVertices[32] = new float4(frustumCornersNear[1], 0);

                //triangle 1
                planesAndVertices[33] = new float4(frustumCornersNear[0], 0);
                planesAndVertices[34] = new float4(frustumCornersNear[1], 0);

                planesAndVertices[35] = new float4(frustumCornersFar[1], 0);
            }
            else if (TargetingSystemDataModels.PlaneOrdering.near == planeOrderingType)
            {
                //triangle 0
                planesAndVertices[24] = new float4(frustumCornersFar[1], 0);
                planesAndVertices[25] = new float4(frustumCornersFar[0], 0);

                planesAndVertices[26] = new float4(frustumCornersNear[1], 0);

                //triangle 1
                planesAndVertices[27] = new float4(frustumCornersNear[1], 0);
                planesAndVertices[28] = new float4(frustumCornersNear[0], 0);

                planesAndVertices[29] = new float4(frustumCornersFar[1], 0);
            }
            else if (TargetingSystemDataModels.PlaneOrdering.down == planeOrderingType)

            {
                //triangle 0
                planesAndVertices[12] = new float4(frustumCornersFar[1], 0);
                planesAndVertices[13] = new float4(frustumCornersFar[0], 0);
                planesAndVertices[14] = new float4(frustumCornersNear[1], 0);

                //triangle 1
                planesAndVertices[15] = new float4(frustumCornersNear[0], 0);
                planesAndVertices[16] = new float4(frustumCornersNear[1], 0);
                planesAndVertices[17] = new float4(frustumCornersFar[0], 0);
            }
            else if (TargetingSystemDataModels.PlaneOrdering.up == planeOrderingType)

            {
                //triangle 0
                planesAndVertices[18] = new float4(frustumCornersFar[0], 0);
                planesAndVertices[19] = new float4(frustumCornersFar[1], 0);
                planesAndVertices[20] = new float4(frustumCornersNear[1], 0);

                //triangle 1
                planesAndVertices[21] = new float4(frustumCornersNear[0], 0);
                planesAndVertices[22] = new float4(frustumCornersNear[1], 0);
                planesAndVertices[23] = new float4(frustumCornersFar[1], 0);
            }
            else if (TargetingSystemDataModels.PlaneOrdering.left == planeOrderingType)
            {
                //triangle 0
                planesAndVertices[0] = new float4(frustumCornersFar[1], 0);
                planesAndVertices[1] = new float4(frustumCornersFar[0], 0);
                planesAndVertices[2] = new float4(frustumCornersNear[1], 0);

                //triangle 1
                planesAndVertices[3] = new float4(frustumCornersNear[0], 0);
                planesAndVertices[4] = new float4(frustumCornersNear[1], 0);
                planesAndVertices[5] = new float4(frustumCornersFar[0], 0);
            }
            else
            {
                //triangle 0
                planesAndVertices[6] = new float4(frustumCornersFar[1], 0);
                planesAndVertices[7] = new float4(frustumCornersFar[0], 0);
                planesAndVertices[8] = new float4(frustumCornersNear[1], 0);

                //triangle 1
                planesAndVertices[9] = new float4(frustumCornersNear[0], 0);
                planesAndVertices[10] = new float4(frustumCornersNear[1], 0);
                planesAndVertices[11] = new float4(frustumCornersFar[0], 0);
            }
        }

        private Mesh CreateMeshPlaneFromCorners(Mesh mesh1, Vector3[] verts)
        {
            mesh1.vertices = verts;

            return mesh1;
        }

        int[] tris = new int[36];

        private Mesh UpdateMeshPlaneFromCorners(Mesh mesh1, Vector3[] verts, int i2)
        {
            this.tris[i2] = i2;
            this.tris[i2 + 1] = i2 + 1;
            this.tris[i2 + 2] = i2 + 2;
            this.tris[i2 + 3] = i2 + 3;
            this.tris[i2 + 4] = i2 + 4;
            this.tris[i2 + 5] = i2 + 5;

            mesh1.vertices = verts;
            mesh1.triangles = this.tris;
            return mesh1;
        }


        public void Debug(ITargetVisibilityProcessor iTargetVisibilityProcessor, GV_TargetingSystem gvTargetingSystem)
        {
#if TARGETING_SYSTEM_GEOMETRY_BASED_TARGETING

            if (iTargetVisibilityProcessor is GameObjectTargetEdgeVisibilityProcessor targetVisibilityProcessor)
            {
                EdgeDrawer.DrawEdgesFromGeoInfos(gvTargetingSystem.SeenGeometryInfos);
            }
#endif
        }


        internal void RefreshFrustumCorners(Camera camera)
        {
            if (Application.isPlaying == false)
            {
                return;
            }

            camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono,
                this.frustumCornersFar);
            camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camera.nearClipPlane,
                Camera.MonoOrStereoscopicEye.Mono, this.frustumCornersNear);
        }

        private void DrawElementsFromGivenData<T>(List<T> ListToDraw, Action<T> draw)
        {
            if (ListToDraw.Count != 0)
            {
                foreach (var geoItem in ListToDraw)
                {
                    draw(geoItem);
                }
            }
        }
    }
}