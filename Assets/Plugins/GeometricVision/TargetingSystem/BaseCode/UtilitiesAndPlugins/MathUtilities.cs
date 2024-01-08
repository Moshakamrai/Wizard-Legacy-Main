using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Plugins.GeometricVision.TargetingSystem.BaseCode.UtilitiesAndPlugins
{
    public static class MathUtilities
    {
        /// <summary>
        /// Projects point to direction.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="onNormal"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [BurstCompile]
        public static float4 Project(float3 point, float4 onNormal)
        {
            float4 dotProductOnNormalToSelf = Dot(onNormal, onNormal);

            if (dotProductOnNormalToSelf.x < float.Epsilon)
            {
                return float4.zero;
            }

            float4 dorProductVectorOnNormal = Dot(point, onNormal);
            return new float4(onNormal.x * dorProductVectorOnNormal.x / dotProductOnNormalToSelf.x,
                onNormal.y * dorProductVectorOnNormal.x / dotProductOnNormalToSelf.x,
                onNormal.z * dorProductVectorOnNormal.x / dotProductOnNormalToSelf.x, 1);
        }

        /// <summary>
        /// <para>Sets a plane using three points that lie within it.  The points go around clockwise as you look down on the top surface of the plane.</para>
        /// </summary>
        /// <param name="a">First point in clockwise order.</param>
        /// <param name="b">Second point in clockwise order.</param>
        /// <param name="c">Third point in clockwise order.</param>
        internal static float4 SetPlaneNormal(float4 a, float4 b, float4 c)
        {
            return Normalize(Cross(b - a, c - a, float4.zero));
        }

        /// <summary>
        /// <para>Sets a plane using three points that lie within it.  The points go around clockwise as you look down on the top surface of the plane.</para>
        /// </summary>
        /// <param name="a">First point in clockwise order.</param>
        /// <param name="b">Second point in clockwise order.</param>
        /// <param name="c">Third point in clockwise order.</param>
        public static float SetPlaneDistance(float4 a, float4 normal)
        {
            var distance = DotF(normal, a) * -1;
            return distance;
        }

        [BurstCompile]
        public static float4 Dot(float4 vector1, float4 vector2)
        {
            var result = vector1 * vector2;
            return (result.x) + (result.y) + (result.z);
        }

        [BurstCompile]
        public static float4 Dot(float3 vector1, float4 vector2)
        {
            return (vector1.x * vector2.x) + (vector1.y * vector2.y) + (vector1.z * vector2.z);
        }

        [BurstCompile]
        public static float4 Dot(float4 vector1, float3 vector2)
        {
            return (vector1.x * vector2.x) + (vector1.y * vector2.y) + (vector1.z * vector2.z);
        }


        [BurstCompile]
        public static float4 Dot(float3 vector1, float3 vector2)
        {
            var result = vector1 * vector2;
            return (result.x) + (result.y) + (result.z);
        }

        [BurstCompile]
        public static float DotF(float4 vector1, float4 vector2)
        {
            return (vector1.x * vector2.x) + (vector1.y * vector2.y) + (vector1.z * vector2.z);
        }

        public static float3 GetLeftSideVector(float3 direction)
        {
            return -Vector3.Cross(Vector3.up, direction);
        }

        [BurstCompile]
        public static float3 PointFromRaySpaceToObjectSpaceF3(float3 point, float4 origin)
        {
            return new float3(point.x + origin.x, point.y + origin.y, point.z + origin.z);
        }

        [BurstCompile]
        public static float3 PointFromRaySpaceToObjectSpaceF3(float3 point, float3 origin)
        {
            return new float3(point.x + origin.x, point.y + origin.y, point.z + origin.z);
        }


        [BurstCompile]
        /// <summary>
        ///   <para>Transforms a position by this matrix (fast).</para>
        /// </summary>
        /// <param name="point"></param>
        public static float4 MultiplyPoint3x4(float4 point, float4 m_01, float4 m_10, float4 m_20, float4 result)
        {
            result.x = (m_01.x * point.x + m_01.y * point.y + m_01.z * point.z) + m_01.w;
            result.y = (m_10.x * point.x + m_10.y * point.y + m_10.z * point.z) + m_10.w;
            result.z = (m_20.x * point.x + m_20.y * point.y + m_20.z * point.z) + m_20.w;
            return result;
        }

        [BurstCompile]
        public static float3 GetDirection(float3 start, float3 end)
        {
            return new float3(end.x - start.x, end.y - start.y, end.z - start.z);
        }

        /// <summary>
        ///   <para>Returns the distance between a and b.</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        [BurstCompile]
        public static float4 Float4Distance(float4 a, float4 b)
        {
            float num1 = a.x - b.x;
            float num2 = a.y - b.y;
            float num3 = a.z - b.z;
            return (float4) Math.Sqrt(num1 * num1 + num2 * num2 + num3 * num3);
        }

        /// <summary>
        ///   <para>Returns the distance between a and b.</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        [BurstCompile]
        public static float3 Float3Distance(float4 a, float3 b)
        {
            float num1 = a.x - b.x;
            float num2 = a.y - b.y;
            float num3 = a.z - b.z;
            return (float3) Math.Sqrt(num1 * num1 + num2 * num2 + num3 * num3);
        }

        /// <summary>
        ///   <para>Returns the distance between a and b.</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        [BurstCompile]
        public static float4 Float4Distance(float3 a, float4 b)
        {
            float num1 = a.x - b.x;
            float num2 = a.y - b.y;
            float num3 = a.z - b.z;
            return (float4) Math.Sqrt(num1 * num1 + num2 * num2 + num3 * num3);
        }

        /// <summary>
        ///   <para>Returns the distance between a and b.</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        [BurstCompile]
        public static float4 Float4DistanceNoSqrt(float4 a, float4 b)
        {
            float num1 = a.x - b.x;
            float num2 = a.y - b.y;
            float num3 = a.z - b.z;
            return (num1 * num1 + num2 * num2 + num3 * num3);
        }

        /// <summary>
        ///   <para>Returns the distance between a and b.</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 Float4DistanceNoSqrt(float3 a, float4 b)
        {
            var num1 = a.x - b.x;
            var num2 = a.y - b.y;
            var num3 = a.z - b.z;
            return (num1 * num1 + num2 * num2 + num3 * num3);
        }

        /// <summary>
        ///   <para>Returns the distance between a and b.</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 Float4DistanceNoSqrt(float4 a, float3 b)
        {
            float num1 = a.x - b.x;
            float num2 = a.y - b.y;
            float num3 = a.z - b.z;
            return (num1 * num1 + num2 * num2 + num3 * num3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        public static float4 PointToGivenSpace(float4 rayLocation, float4 target)
        {
            return target - rayLocation;
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 PointToGivenSpace(float4 rayLocation, float3 target)
        {
            target.x -= rayLocation.x;
            target.y -= rayLocation.y;
            target.z -= rayLocation.z;
            return target;
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 PointToGivenSpace(float3 origin, float3 target)
        {
            target.x -= origin.x;
            target.y -= origin.y;
            target.z -= origin.z;
            return target;
        }


        [BurstCompile]
        public static float4 Normalize(float4 normal)
        {
            float num = Magnitude(normal);
            if (num > 9.99999974737875E-06)
            {
                return normal / num;
            }

            return float4.zero;
        }

        [BurstCompile]
        private static float Magnitude(float4 edgeTargetingSystemDirection)
        {
            return Mathf.Sqrt(edgeTargetingSystemDirection.x * edgeTargetingSystemDirection.x +
                              edgeTargetingSystemDirection.y * edgeTargetingSystemDirection.y +
                              edgeTargetingSystemDirection.z * edgeTargetingSystemDirection.z);
        }

        public static float4 Cross(float4 lhs, float4 rhs, float4 result)
        {
            result.x = (float) ((double) lhs.y * (double) rhs.z - (double) lhs.z * (double) rhs.y);
            result.y = (float) ((double) lhs.z * (double) rhs.x - (double) lhs.x * (double) rhs.z);
            result.z = (float) ((double) lhs.x * (double) rhs.y - (double) lhs.y * (double) rhs.x);
            return result;
        }
    }
}