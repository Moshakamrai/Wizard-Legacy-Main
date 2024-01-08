using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Plugins.GeometricVision.TargetingSystem.GameObjectExamples.BaseDemo.ObjectPicking.Code
{
    public static class LineEffect
    {
        internal static Vector3[] ElectrifyPoints(Vector3 sourcePosition, float frequency, Vector3 closestTargetPosition,
            float closestTargetDistanceToCastOrigin, float time, float sinTime, Vector3[] positions,
            float strengthModifier, float strengthModifier1, float strengthModifier2)
        {
            time += Time.deltaTime * frequency * frequency * frequency;
            sinTime += Time.deltaTime * frequency * frequency;

            if (time > frequency)
            {
                time = 0f;
                positions[0] = sourcePosition + new Vector3(Mathf.Sin(time) * 0.1f, Mathf.Sin(time) * 0.1f,
                    Mathf.Sin(time) * 0.1f);
                positions[9] = closestTargetPosition + new Vector3(Mathf.Sin(sinTime) * 0.1f, Mathf.Sin(sinTime) * 0.1f,
                    Mathf.Sin(sinTime) * 0.1f);
                positions = ElectrifyPointsBetweenStartToEnd(closestTargetDistanceToCastOrigin, positions,
                    strengthModifier, strengthModifier1, strengthModifier2, new float4(sourcePosition, 1));
                
            }

            return positions;

            Vector3[] ElectrifyPointsBetweenStartToEnd(float distanceToTarget, Vector3[] positions,
                float strengthModifier, float strengthModifier1, float strengthModifier2, float4 sourcePosition)
            {
                int breaker = 1;
                for (int index = 1; index < positions.Length - 1; index++)
                {
                    breaker = breaker * -1;

                    //Divides distanceToTarget with amount of lightning points and multiplies that with the points index resulting in point between start to finish
                    float pointOffsetFromStartToFinish = (distanceToTarget / (positions.Length)) * (index )+1;

                    var strength = breaker * index * strengthModifier * (distanceToTarget * strengthModifier1);
                    var random = Random.Range(-strengthModifier2 * pointOffsetFromStartToFinish, strengthModifier2 * pointOffsetFromStartToFinish * 2);
                    positions[index] = ElectrifyPoint(sourcePosition, pointOffsetFromStartToFinish, strength, sinTime, random,
                        new float4(closestTargetPosition,0));
                }

                return positions;
            }
        }

        private static Vector3 ElectrifyPoint(float4 sourcePosition, float pointOffsetFromStartToFinish, float strength, float sinTime, float random, float4 targetPosition)
        {
            var directionNormalized = Vector3.Normalize(targetPosition.xyz - sourcePosition.xyz);
            var newDirScaled  = new float4(
                directionNormalized.x * pointOffsetFromStartToFinish + Random.Range(-0.1f, 0.1f),
                directionNormalized.y * pointOffsetFromStartToFinish + Random.Range(-0.1f, 0.1f),
                directionNormalized.z * pointOffsetFromStartToFinish + Random.Range(-0.1f, 0.1f), 1);
            
            float3 airLocationForCurvePoint = newDirScaled.xyz + sourcePosition.xyz;
            return new Vector3(
                airLocationForCurvePoint.x + Mathf.Sin(sinTime) * strength * 2 + random,
                airLocationForCurvePoint.y + Mathf.Sin(sinTime) * strength,
                airLocationForCurvePoint.z + Mathf.Sin(sinTime) * strength * 2 + random);
        }
    }
}